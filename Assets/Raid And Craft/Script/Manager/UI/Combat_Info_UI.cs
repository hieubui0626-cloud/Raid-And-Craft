using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thông tin chiến đấu hiện trên màn hình, gồm HAI mục tách biệt, bật/tắt riêng lẻ:
/// 1) Số sát thương nổi + chữ Miss / Block (nghe Combat_Events.OnAnyAttackResolved).
/// 2) Thanh máu trên đầu Enemy (mọi Character_Stats không mang tag Player).
/// Không cần prefab hay Canvas có sẵn: script tự dựng một Canvas riêng khi chạy. Chỉ cần đặt MỘT object có script này trong Scene.
///
/// CÀI ĐẶT GAME (để làm menu Settings sau này): hai thuộc tính static bên dưới tự lưu bằng PlayerPrefs và bắn OnSettingsChanged.
///   Combat_Info_UI.FloatingTextEnabled = toggle.isOn;
///   Combat_Info_UI.EnemyHealthBarEnabled = toggle.isOn;
/// Đặt giá trị là UI cập nhật ngay (tắt = ẩn hết). Giá trị mặc định lần đầu chơi: bật cả hai.
/// </summary>
public class Combat_Info_UI : MonoBehaviour
{
    // ------------------------------------------------------------------
    // CÀI ĐẶT (static, lưu PlayerPrefs)
    // ------------------------------------------------------------------

    private const string FloatingTextKey = "Setting_Combat_FloatingText";
    private const string HealthBarKey = "Setting_Combat_EnemyHealthBar";

    /// <summary>Bắn khi một trong hai cài đặt đổi giá trị.</summary>
    public static event Action OnSettingsChanged;

    private static bool settingsLoaded;
    private static bool floatingTextEnabled = true;
    private static bool enemyHealthBarEnabled = true;

    /// <summary>Bật/tắt số sát thương nổi và chữ Miss/Block.</summary>
    public static bool FloatingTextEnabled
    {
        get { LoadSettings(); return floatingTextEnabled; }
        set
        {
            LoadSettings();
            if (floatingTextEnabled == value) return;

            floatingTextEnabled = value;
            PlayerPrefs.SetInt(FloatingTextKey, value ? 1 : 0);
            OnSettingsChanged?.Invoke();
        }
    }

    /// <summary>Bật/tắt thanh máu trên đầu Enemy.</summary>
    public static bool EnemyHealthBarEnabled
    {
        get { LoadSettings(); return enemyHealthBarEnabled; }
        set
        {
            LoadSettings();
            if (enemyHealthBarEnabled == value) return;

            enemyHealthBarEnabled = value;
            PlayerPrefs.SetInt(HealthBarKey, value ? 1 : 0);
            OnSettingsChanged?.Invoke();
        }
    }

    private static void LoadSettings()
    {
        if (settingsLoaded) return;
        settingsLoaded = true;

        floatingTextEnabled = PlayerPrefs.GetInt(FloatingTextKey, 1) != 0;
        enemyHealthBarEnabled = PlayerPrefs.GetInt(HealthBarKey, 1) != 0;
    }

    // ------------------------------------------------------------------
    // CẤU HÌNH TRONG INSPECTOR
    // ------------------------------------------------------------------

    [Header("Chung")]
    [Tooltip("Object mang tag này được coi là Player: không hiện thanh máu trên đầu, và sát thương nó nhận hiện màu 'Damage Taken'.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Canvas của mục này vẽ DƯỚI UI chính (số nhỏ hơn = vẽ trước).")]
    [SerializeField] private int canvasSortingOrder = -10;

    [Header("Số sát thương nổi")]
    [SerializeField] private float textFontSize = 28f;
    [Tooltip("Độ cao chữ xuất hiện so với chân mục tiêu (mét).")]
    [SerializeField] private float textSpawnHeight = 1.8f;
    [Tooltip("Số giây chữ tồn tại.")]
    [SerializeField] private float textLifetime = 0.9f;
    [Tooltip("Tốc độ chữ bay lên (pixel mỗi giây, theo độ phân giải 1080p).")]
    [SerializeField] private float textRiseSpeed = 70f;
    [Tooltip("Chữ chí mạng to gấp bao nhiêu lần.")]
    [SerializeField] private float criticalScale = 1.5f;
    [SerializeField] private Color damageDealtColor = Color.white;
    [SerializeField] private Color damageTakenColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private Color missColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color blockColor = new Color(0.45f, 0.75f, 1f);

    [Header("Thanh máu Enemy")]
    [Tooltip("Kích thước thanh (pixel, theo độ phân giải 1080p).")]
    [SerializeField] private Vector2 barSize = new Vector2(90f, 10f);
    [Tooltip("Độ cao thanh so với chân Enemy (mét).")]
    [SerializeField] private float barHeight = 2.2f;
    [Tooltip("Bật: chỉ hiện thanh khi Enemy đã mất máu (đầy máu thì ẩn).")]
    [SerializeField] private bool hideBarWhenFull;
    [SerializeField] private Color barBackgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color barFillColor = new Color(0.85f, 0.15f, 0.15f);

    // ------------------------------------------------------------------
    // TRẠNG THÁI
    // ------------------------------------------------------------------

    private class Floating_Entry
    {
        public TextMeshProUGUI text;
        public Vector3 worldPosition;
        public float age;
        public Color color;
        public float scale;
    }

    private class Bar_Entry
    {
        public RectTransform root;
        public RectTransform fill;
        public float lastFill = -1f;
    }

    private RectTransform canvasRoot;
    private Camera mainCamera;

    private readonly List<Floating_Entry> activeTexts = new List<Floating_Entry>();
    private readonly Stack<Floating_Entry> textPool = new Stack<Floating_Entry>();

    private readonly Dictionary<Character_Stats, Bar_Entry> bars = new Dictionary<Character_Stats, Bar_Entry>();
    private readonly Stack<Bar_Entry> barPool = new Stack<Bar_Entry>();
    private readonly List<Character_Stats> removeBuffer = new List<Character_Stats>();

    private void Awake()
    {
        BuildCanvas();
    }

    private void OnEnable()
    {
        Combat_Events.OnAnyAttackResolved += HandleAttackResolved;
        Character_Stats.OnAnySpawned += HandleCharacterSpawned;
        OnSettingsChanged += HandleSettingsChanged;
    }

    private void OnDisable()
    {
        Combat_Events.OnAnyAttackResolved -= HandleAttackResolved;
        Character_Stats.OnAnySpawned -= HandleCharacterSpawned;
        OnSettingsChanged -= HandleSettingsChanged;
    }

    private void Start()
    {
        // Nhân vật đã có sẵn trong Scene từ trước khi script này chạy.
        foreach (Character_Stats stats in FindObjectsByType<Character_Stats>(FindObjectsSortMode.None))
        {
            HandleCharacterSpawned(stats);
        }
    }

    private void OnDestroy()
    {
        if (canvasRoot != null) Destroy(canvasRoot.gameObject);
    }

    private void BuildCanvas()
    {
        // Canvas riêng nằm ở gốc Scene (không lồng trong Canvas khác để không bị ảnh hưởng chế độ hiển thị của nó).
        var canvasObject = new GameObject("Combat_Info_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot = canvasObject.GetComponent<RectTransform>();
    }

    private void HandleSettingsChanged()
    {
        // Tắt số nổi: bỏ hết chữ đang bay. (Thanh máu tự ẩn trong LateUpdate.)
        if (!FloatingTextEnabled) ClearFloatingTexts();
    }

    // ------------------------------------------------------------------
    // SỐ SÁT THƯƠNG NỔI
    // ------------------------------------------------------------------

    private void HandleAttackResolved(Character_Stats attacker, Character_Stats target, Damage_Result result)
    {
        if (!FloatingTextEnabled || target == null) return;

        bool targetIsPlayer = IsPlayer(target);
        Vector3 position = target.transform.position + Vector3.up * textSpawnHeight;
        position += new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0f, UnityEngine.Random.Range(-0.3f, 0.3f));

        switch (result.outcome)
        {
            case Damage_Outcome.Hit:
                string amount = Mathf.RoundToInt(result.damageDealt).ToString();
                if (result.isCritical)
                {
                    SpawnText(position, amount + "!", criticalColor, criticalScale);
                }
                else
                {
                    SpawnText(position, amount, targetIsPlayer ? damageTakenColor : damageDealtColor, 1f);
                }
                break;

            case Damage_Outcome.Evaded:
                SpawnText(position, "Miss", missColor, 1f);
                break;

            case Damage_Outcome.Blocked:
                SpawnText(position, "Block", blockColor, 1f);
                break;
        }
    }

    private void SpawnText(Vector3 worldPosition, string content, Color color, float scale)
    {
        Floating_Entry entry = textPool.Count > 0 ? textPool.Pop() : CreateTextEntry();

        entry.worldPosition = worldPosition;
        entry.age = 0f;
        entry.color = color;
        entry.scale = scale;
        entry.text.text = content;
        entry.text.color = color;
        entry.text.gameObject.SetActive(true);

        activeTexts.Add(entry);
    }

    private Floating_Entry CreateTextEntry()
    {
        var textObject = new GameObject("Floating_Text", typeof(RectTransform));
        textObject.transform.SetParent(canvasRoot, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = textFontSize;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(240f, 60f);

        return new Floating_Entry { text = text };
    }

    private void UpdateFloatingTexts()
    {
        if (activeTexts.Count == 0) return;

        Camera cam = GetCamera();
        float screenScale = Screen.height / 1080f;

        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            Floating_Entry entry = activeTexts[i];
            entry.age += Time.deltaTime;

            if (entry.age >= textLifetime || cam == null)
            {
                RecycleText(i);
                continue;
            }

            Vector3 screen = cam.WorldToScreenPoint(entry.worldPosition);
            if (screen.z <= 0f)
            {
                entry.text.enabled = false;
                continue;
            }

            entry.text.enabled = true;

            float t = entry.age / textLifetime;
            screen.y += textRiseSpeed * screenScale * entry.age;
            screen.z = 0f;
            entry.text.rectTransform.position = screen;

            // Nảy to lên lúc đầu rồi về cỡ thường; mờ dần ở 40% cuối.
            float pop = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(entry.age / 0.15f));
            entry.text.rectTransform.localScale = Vector3.one * (entry.scale * pop);

            Color c = entry.color;
            c.a = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            entry.text.color = c;
        }
    }

    private void RecycleText(int index)
    {
        Floating_Entry entry = activeTexts[index];
        activeTexts.RemoveAt(index);

        entry.text.gameObject.SetActive(false);
        textPool.Push(entry);
    }

    private void ClearFloatingTexts()
    {
        for (int i = activeTexts.Count - 1; i >= 0; i--) RecycleText(i);
    }

    // ------------------------------------------------------------------
    // THANH MÁU ENEMY
    // ------------------------------------------------------------------

    private bool IsPlayer(Character_Stats stats)
    {
        return stats.gameObject.tag == playerTag;
    }

    private void HandleCharacterSpawned(Character_Stats stats)
    {
        if (stats == null || IsPlayer(stats) || bars.ContainsKey(stats)) return;

        Bar_Entry bar = barPool.Count > 0 ? barPool.Pop() : CreateBar();
        bar.lastFill = -1f;
        bar.root.gameObject.SetActive(false);
        bars[stats] = bar;
    }

    private Bar_Entry CreateBar()
    {
        var rootObject = new GameObject("Enemy_Health_Bar", typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(canvasRoot, false);

        var background = rootObject.GetComponent<Image>();
        background.color = barBackgroundColor;
        background.raycastTarget = false;

        var rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = barSize;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(rootRect, false);

        var fillImage = fillObject.GetComponent<Image>();
        fillImage.color = barFillColor;
        fillImage.raycastTarget = false;

        // Co giãn bằng anchor (không cần sprite): anchorMax.x = tỉ lệ máu.
        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        return new Bar_Entry { root = rootRect, fill = fillRect };
    }

    private void UpdateHealthBars()
    {
        bool enabledNow = EnemyHealthBarEnabled;
        Camera cam = enabledNow ? GetCamera() : null;

        removeBuffer.Clear();

        foreach (KeyValuePair<Character_Stats, Bar_Entry> pair in bars)
        {
            Character_Stats stats = pair.Key;
            Bar_Entry bar = pair.Value;

            // Enemy đã bị Destroy: thu hồi thanh.
            if (stats == null)
            {
                removeBuffer.Add(stats);
                continue;
            }

            bool show = enabledNow && cam != null && !stats.IsDead &&
                        !(hideBarWhenFull && stats.CurrentHealth >= stats.MaxHealth);

            if (show)
            {
                Vector3 screen = cam.WorldToScreenPoint(stats.transform.position + Vector3.up * barHeight);
                show = screen.z > 0f;

                if (show)
                {
                    screen.z = 0f;
                    bar.root.position = screen;

                    float fill = stats.MaxHealth > 0f ? Mathf.Clamp01(stats.CurrentHealth / stats.MaxHealth) : 0f;
                    if (!Mathf.Approximately(fill, bar.lastFill))
                    {
                        bar.lastFill = fill;
                        bar.fill.anchorMax = new Vector2(fill, 1f);
                    }
                }
            }

            if (bar.root.gameObject.activeSelf != show) bar.root.gameObject.SetActive(show);
        }

        foreach (Character_Stats destroyed in removeBuffer)
        {
            Bar_Entry bar = bars[destroyed];
            bars.Remove(destroyed);
            bar.root.gameObject.SetActive(false);
            barPool.Push(bar);
        }
    }

    // ------------------------------------------------------------------

    private void LateUpdate()
    {
        // Chạy sau khi nhân vật/camera đã di chuyển trong frame để chữ và thanh không bị trễ.
        if (FloatingTextEnabled) UpdateFloatingTexts();
        UpdateHealthBars();
    }

    private Camera GetCamera()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        return mainCamera;
    }
}
