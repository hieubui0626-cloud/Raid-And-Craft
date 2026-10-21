using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Điều phối lưu/tải. Đặt 1 object trong Scene khởi đầu (cùng chỗ với Input_Manager).
/// - Luôn lưu local trước (nhanh, chạy offline), rồi đẩy lên cloud nếu đã gắn provider (SetCloudProvider).
/// - Khi tải: nếu có cả local và cloud, lấy bản có revision cao hơn.
/// - Tự lưu: theo chu kỳ, khi app bị tạm dừng (mobile) và khi thoát game.
/// </summary>
public class Save_Manager : MonoBehaviour
{
    public static Save_Manager Instance { get; private set; }

    /// <summary>Bắn sau khi lưu xong / tải xong (UI hiện biểu tượng "đã lưu"...).</summary>
    public event Action OnSaved;
    public event Action OnLoaded;

    [Header("Cấu hình")]
    [Tooltip("Mỗi slot một file save. Sau này có nhiều nhân vật/tài khoản thì đổi theo slot.")]
    [SerializeField] private string slotId = "slot_0";

    [Tooltip("Tự tải save (nếu có) ngay khi Player đăng ký.")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Tooltip("Giây giữa hai lần tự lưu. 0 = tắt tự lưu theo chu kỳ.")]
    [SerializeField] private float autoSaveInterval = 60f;

    private readonly Local_Save_Provider local = new Local_Save_Provider();
    private ISave_Provider cloud; // null cho tới khi gắn cloud
    private Player_Save player;

    private long revision;
    private bool busy;
    private float autoSaveTimer;

    public string SlotId => slotId;
    public bool HasCloud => cloud != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Gắn nơi lưu cloud (gọi sau khi đăng nhập). Truyền null để tắt cloud.</summary>
    public void SetCloudProvider(ISave_Provider provider)
    {
        cloud = provider;
    }

    // ------------------------------------------------------------------
    // Đăng ký
    // ------------------------------------------------------------------

    public void Register(Player_Save playerSave)
    {
        player = playerSave;
        autoSaveTimer = 0f;

        if (autoLoadOnStart) RunSafe(LoadGameAsync(), "tải");
    }

    public void Unregister(Player_Save playerSave)
    {
        if (player == playerSave) player = null;
    }

    // ------------------------------------------------------------------
    // Lưu
    // ------------------------------------------------------------------

    public void SaveGame() => RunSafe(SaveGameAsync(), "lưu");

    public async Task SaveGameAsync()
    {
        if (player == null || busy) return;
        busy = true;

        try
        {
            string json = BuildJson();
            local.Save(slotId, json);

            if (cloud != null)
            {
                try { await cloud.SaveAsync(slotId, json); }
                catch (Exception e) { Debug.LogWarning($"[{nameof(Save_Manager)}] Đẩy cloud thất bại (local vẫn đã lưu): {e.Message}"); }
            }

            OnSaved?.Invoke();
        }
        finally
        {
            busy = false;
        }
    }

    /// <summary>Lưu đồng bộ chỉ ra local - dùng lúc app tạm dừng/thoát, khi không kịp chờ async.</summary>
    private void SaveLocalBlocking()
    {
        if (player == null) return;

        try { local.Save(slotId, BuildJson()); }
        catch (Exception e) { Debug.LogError($"[{nameof(Save_Manager)}] Lưu local thất bại: {e.Message}"); }
    }

    private string BuildJson()
    {
        var data = new Save_Data
        {
            revision = ++revision,
            saveTimeUtc = DateTime.UtcNow.ToString("o"),
        };
        player.Capture(data.player);

        return JsonUtility.ToJson(data, true);
    }

    // ------------------------------------------------------------------
    // Tải
    // ------------------------------------------------------------------

    /// <summary>Trả về true nếu đã tải và áp dụng được một bản save.</summary>
    public async Task<bool> LoadGameAsync()
    {
        if (player == null || busy) return false;
        busy = true;

        try
        {
            Save_Data data = await ReadNewestAsync();
            if (data == null || player == null) return false;

            revision = data.revision;
            player.Apply(data.player);

            OnLoaded?.Invoke();
            return true;
        }
        finally
        {
            busy = false;
        }
    }

    private async Task<Save_Data> ReadNewestAsync()
    {
        Save_Data localData = ParseOrNull(local.Load(slotId)) ?? ParseOrNull(local.Load(slotId, backup: true));
        Save_Data cloudData = null;

        if (cloud != null)
        {
            try { cloudData = ParseOrNull(await cloud.LoadAsync(slotId)); }
            catch (Exception e) { Debug.LogWarning($"[{nameof(Save_Manager)}] Đọc cloud thất bại, dùng local: {e.Message}"); }
        }

        if (localData == null) return cloudData;
        if (cloudData == null) return localData;
        return cloudData.revision > localData.revision ? cloudData : localData;
    }

    private static Save_Data ParseOrNull(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            Save_Data data = JsonUtility.FromJson<Save_Data>(json);
            if (data == null) return null;

            if (data.version > Save_Data.CurrentVersion)
            {
                Debug.LogWarning($"[{nameof(Save_Manager)}] Save phiên bản {data.version} mới hơn game ({Save_Data.CurrentVersion}), bỏ qua.");
                return null;
            }

            // Chỗ migrate save cũ: if (data.version < 2) { ...; data.version = 2; }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{nameof(Save_Manager)}] Save hỏng, bỏ qua: {e.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Xoá / tự lưu
    // ------------------------------------------------------------------

    public void DeleteSave()
    {
        local.Delete(slotId);
        if (cloud != null) RunSafe(cloud.DeleteAsync(slotId), "xoá cloud");
    }

    private void Update()
    {
        if (autoSaveInterval <= 0f || player == null) return;

        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            SaveGame();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveLocalBlocking();
    }

    private void OnApplicationQuit()
    {
        SaveLocalBlocking();
    }

    private static async void RunSafe(Task task, string what)
    {
        try { await task; }
        catch (Exception e) { Debug.LogError($"[{nameof(Save_Manager)}] Lỗi khi {what}: {e}"); }
    }
}
