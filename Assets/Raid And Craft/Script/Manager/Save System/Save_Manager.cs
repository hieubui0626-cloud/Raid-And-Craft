using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Tooltip("Tiền tố tên file của các nhân vật: slot_0, slot_1... (mỗi nhân vật một slot). Slot 'Slot Id' ở trên là slot dùng khi chạy thẳng một Scene trong Editor.")]
    [SerializeField] private string slotPrefix = "slot_";

    [Tooltip("Số nhân vật tối đa (số ô ở màn chọn nhân vật).")]
    [SerializeField, Min(1)] private int maxCharacterSlots = 5;

    [Tooltip("Tự tải save (nếu có) ngay khi Player đăng ký.")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Tooltip("Giây giữa hai lần tự lưu. 0 = tắt tự lưu theo chu kỳ.")]
    [SerializeField] private float autoSaveInterval = 60f;

    [Tooltip("Lần tải đầu tiên: nếu save ghi Scene base khác Scene đang mở thì nạp Scene đó trước rồi mới áp save. " +
             "Tắt khi muốn thử riêng một Scene trong Editor.")]
    [SerializeField] private bool loadSavedScene = true;

    [Tooltip("Tên Scene Main Menu (Pause Menu dùng khi bấm 'Về Main Menu'). Phải có trong Build Settings.")]
    [SerializeField] private string mainMenuScene = "Main_Menu";

    [Tooltip("Scene base dùng khi cần 'về base' mà save chưa ghi Scene nào (ví dụ chết ở Scene tạm trước lần lưu đầu).")]
    [SerializeField] private string fallbackBaseScene;

    private readonly Local_Save_Provider local = new Local_Save_Provider();
    private ISave_Provider cloud; // null cho tới khi gắn cloud
    private Player_Save player;

    private long revision;
    private bool busy;
    private bool saveBlocked;
    private bool blockedWarned;
    private float autoSaveTimer;
    private bool sceneResolved; // đã xét việc chuyển Scene theo save trong lần chạy này chưa (chỉ làm một lần, tránh lặp)

    public string SlotId => slotId;
    public int MaxCharacterSlots => Mathf.Max(1, maxCharacterSlots);

    /// <summary>Tên/lớp của nhân vật đang chơi (slot hiện tại). Lưu cùng save.</summary>
    public string CharacterName { get; private set; } = string.Empty;
    public string CharacterClass { get; private set; } = string.Empty;
    public string MainMenuScene => mainMenuScene;
    public string FallbackBaseScene => fallbackBaseScene;
    public bool HasCloud => cloud != null;

    /// <summary>Tên Scene base gần nhất Player đã ở. Player_Save cập nhật khi lưu ở Scene base; ở Scene tạm vẫn giữ giá trị này để lưu lại.</summary>
    public string LastBaseScene { get; set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad chỉ hoạt động với object gốc (không có cha); nằm dưới object cha thì bỏ qua để khỏi báo lỗi.
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
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
    // Nhân vật (mỗi nhân vật một slot)
    // ------------------------------------------------------------------

    /// <summary>Tóm tắt một nhân vật để hiện ở màn chọn nhân vật.</summary>
    public struct Character_Summary
    {
        public int index;
        public bool exists;
        public string name;
        public string className;
        public int level;
        public string baseScene;
    }

    public string SlotIdOf(int index) => slotPrefix + index;

    /// <summary>Đọc tóm tắt nhân vật ở ô index (không đổi slot hiện tại). Ô trống: exists = false.</summary>
    public Character_Summary GetSummary(int index)
    {
        var summary = new Character_Summary { index = index, name = string.Empty, className = string.Empty, level = 1, baseScene = string.Empty };

        string id = SlotIdOf(index);
        Save_Data data = ParseOrNull(local.Load(id)) ?? ParseOrNull(local.Load(id, backup: true));
        if (data == null) return summary;

        summary.exists = true;
        summary.name = string.IsNullOrEmpty(data.characterName) ? $"Character {index + 1}" : data.characterName;
        summary.className = data.characterClass ?? string.Empty;
        summary.baseScene = data.player.baseSceneName ?? string.Empty;

        foreach (Stat_Save_Entry entry in data.player.baseStats)
        {
            if (entry.type == (int)Player_Stat_Type.CharacterLevel) summary.level = Mathf.Max(1, Mathf.RoundToInt(entry.baseValue));
        }

        return summary;
    }

    /// <summary>Chọn nhân vật ở ô index làm slot hiện tại (mọi lưu/tải sau đó dùng slot này). Gọi trước khi nạp Scene chơi.</summary>
    public void SelectSlot(int index)
    {
        Scene_State_Store.Clear(); // trạng thái Scene thuộc về nhân vật: đổi slot thì nạp lại từ save của slot đó
        slotId = SlotIdOf(index);
        revision = 0;
        saveBlocked = false;
        blockedWarned = false;
        sceneResolved = false;
        LastBaseScene = string.Empty;
        CharacterName = string.Empty;
        CharacterClass = string.Empty;

        Save_Data data = ReadLocalOrNull();
        if (data == null) return;

        CharacterName = data.characterName ?? string.Empty;
        CharacterClass = data.characterClass ?? string.Empty;
        LastBaseScene = data.player.baseSceneName ?? string.Empty;
    }

    /// <summary>Tạo nhân vật mới ở ô index (xoá save cũ của ô đó) và chọn làm slot hiện tại. Player khởi đầu như prefab; file save ghi ngay khi vào Scene chơi.</summary>
    public void CreateCharacter(int index, string characterName, string characterClass = "")
    {
        slotId = SlotIdOf(index);
        local.Delete(slotId);
        if (cloud != null) RunSafe(cloud.DeleteAsync(slotId), "xoá cloud");

        SelectSlot(index);
        CharacterName = characterName ?? string.Empty;
        CharacterClass = characterClass ?? string.Empty;
    }

    /// <summary>Xoá nhân vật ở ô index. Nếu là slot đang chọn thì bỏ chọn luôn.</summary>
    public void DeleteCharacter(int index)
    {
        Scene_State_Store.Clear();
        string id = SlotIdOf(index);
        local.Delete(id);
        if (cloud != null) RunSafe(cloud.DeleteAsync(id), "xoá cloud");

        if (slotId == id)
        {
            CharacterName = string.Empty;
            CharacterClass = string.Empty;
            LastBaseScene = string.Empty;
        }
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
        if (player == null || busy || IsSaveBlocked()) return;
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

    /// <summary>Lưu NGAY, đồng bộ (chỉ local). Dùng trước khi chuyển Scene. False nếu không lưu được (chưa có Player, đang bị chặn lưu, lỗi ghi file).</summary>
    public bool SaveNow()
    {
        if (player == null || IsSaveBlocked()) return false;

        try
        {
            local.Save(slotId, BuildJson());
            OnSaved?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(Save_Manager)}] Lưu local thất bại: {e.Message}");
            return false;
        }
    }

    /// <summary>Có file save (hợp lệ) cho slot này không - dùng cho nút Continue của Main Menu.</summary>
    public bool HasSave => ReadLocalOrNull() != null;

    /// <summary>
    /// Tên Scene base ghi trong save, để Main Menu nạp khi bấm Continue. Trả về false nếu chưa có save;
    /// save cũ không ghi tên Scene thì true với sceneName rỗng (Main Menu tự chọn Scene mặc định).
    /// </summary>
    public bool TryGetSavedBaseScene(out string sceneName)
    {
        Save_Data data = ReadLocalOrNull();
        sceneName = data != null ? data.player.baseSceneName ?? string.Empty : string.Empty;
        return data != null;
    }

    /// <summary>Trạng thái các Scene ghi trong save hiện tại (Scene_State_Store nạp khi cần). Danh sách trống nếu chưa có save.</summary>
    public System.Collections.Generic.List<Scene_State_Entry> ReadSceneStates()
    {
        Save_Data data = ReadLocalOrNull();
        return data != null && data.player.sceneStates != null ? data.player.sceneStates : new System.Collections.Generic.List<Scene_State_Entry>();
    }

    private Save_Data ReadLocalOrNull()
    {
        return ParseOrNull(local.Load(slotId)) ?? ParseOrNull(local.Load(slotId, backup: true));
    }

    /// <summary>Lưu đồng bộ chỉ ra local - dùng lúc app tạm dừng/thoát, khi không kịp chờ async.</summary>
    private void SaveLocalBlocking()
    {
        if (player == null || IsSaveBlocked()) return;

        try { local.Save(slotId, BuildJson()); }
        catch (Exception e) { Debug.LogError($"[{nameof(Save_Manager)}] Lưu local thất bại: {e.Message}"); }
    }

    // Đang bị chặn lưu (lần nạp trước lỗi). Cảnh báo một lần, không spam mỗi lần tự lưu.
    private bool IsSaveBlocked()
    {
        if (!saveBlocked) return false;

        if (!blockedWarned)
        {
            blockedWarned = true;
            Debug.LogWarning($"[{nameof(Save_Manager)}] Đang chặn lưu vì lần nạp save trước chưa trọn vẹn (xem lỗi lúc nạp).");
        }
        return true;
    }

    [ContextMenu("Debug: Bỏ chặn lưu")]
    public void UnblockSaving()
    {
        saveBlocked = false;
        blockedWarned = false;
    }

    private string BuildJson()
    {
        var data = new Save_Data
        {
            revision = ++revision,
            saveTimeUtc = DateTime.UtcNow.ToString("o"),
            characterName = CharacterName,
            characterClass = CharacterClass,
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
            if (data == null || player == null)
            {
                // Nhân vật MỚI (vừa tạo ở màn chọn nhân vật, chưa có file): lưu ngay trạng thái khởi đầu để nhân vật hiện trong danh sách.
                if (data == null && player != null && !string.IsNullOrEmpty(CharacterName)) SaveNow();
                return false;
            }

            revision = data.revision;
            CharacterName = data.characterName ?? CharacterName;
            CharacterClass = data.characterClass ?? CharacterClass;
            LastBaseScene = data.player.baseSceneName ?? string.Empty;

            // Save ghi Player ở Scene base khác Scene đang mở: nạp Scene đó trước. Player ở Scene mới sẽ tự đăng ký lại và nạp save ở đó.
            if (TryRedirectToSavedScene(data)) return false;

            bool applied;
            try { applied = player.Apply(data.player); }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(Save_Manager)}] Lỗi khi áp save: {e}");
                applied = false;
            }

            // Nạp lỗi/dở thì Player đang ở trạng thái thiếu dữ liệu: nếu cho tự lưu sẽ GHI ĐÈ bản save tốt bằng bản thiếu.
            if (!applied)
            {
                saveBlocked = true;
                Debug.LogError($"[{nameof(Save_Manager)}] Nạp save chưa trọn vẹn nên TẠM CHẶN lưu để không ghi đè save cũ. " +
                               "Xem các lỗi phía trên, sửa rồi chạy lại. (Chuột phải Save_Manager -> Debug: Bỏ chặn lưu nếu cố ý muốn lưu tiếp.)");
            }

            OnLoaded?.Invoke();
            return applied;
        }
        finally
        {
            busy = false;
        }
    }

    private bool TryRedirectToSavedScene(Save_Data data)
    {
        if (sceneResolved) return false;
        sceneResolved = true;
        if (!loadSavedScene || player == null) return false;

        string target = data.player.baseSceneName;
        if (string.IsNullOrEmpty(target) || target == player.gameObject.scene.name) return false;

        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogWarning($"[{nameof(Save_Manager)}] Save ghi Scene base '{target}' nhưng Scene này không nằm trong Build Settings, " +
                             "nên ở lại Scene hiện tại (vị trí sẽ không được áp).");
            return false;
        }

        SceneManager.LoadScene(target);
        return true;
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

            // Chỗ migrate save cũ. v1 -> v2: chỉ thêm field mới; save v1 có hasProgress = false nên Player_Save giữ nguyên
            // Inventory/trang bị/skill hiện có. Khi đổi cấu trúc cũ ở các phiên bản sau, thêm: if (data.version < 3) { ...; data.version = 3; }
            if (data.version < 2) data.version = 2;
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
        UnblockSaving(); // xoá save = bắt đầu lại, không còn gì để bảo vệ
        Scene_State_Store.Clear();
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
