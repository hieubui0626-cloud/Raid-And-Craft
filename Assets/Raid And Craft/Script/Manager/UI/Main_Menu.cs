using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu: Continue / New Game / Quit. Đặt MỘT object trong Scene Main Menu, điền Scene rồi bấm Play là chạy, không cần nối OnClick.
/// Scene Main Menu cần Save_Manager (object gốc, KHÔNG nằm dưới object cha để DontDestroyOnLoad hoạt động).
/// - Giao diện: không gán nút nào thì Main_Menu tự dựng menu (tên game + 3 nút + dòng thông báo). Đã có nút tự vẽ thì kéo vào các ô Button
///   (Main_Menu tự nối sự kiện bấm, KHÔNG nối thêm OnClick trong Inspector kẻo chạy hai lần).
/// - Continue: nạp Scene base ghi trong save (Player ở đó nạp phần còn lại). Khoá khi chưa có save. Save cũ không ghi Scene thì dùng New Game Scene.
/// - New Game: nếu đang có save thì phải bấm lần thứ hai (trong vài giây) để xác nhận xoá save cũ. Sau đó vào Character Creation Scene nếu có điền,
///   không thì vào New Game Scene (Scene base đầu tiên, Player có sẵn).
/// Mọi Scene nạp được phải có trong Build Settings.
/// </summary>
public class Main_Menu : MonoBehaviour
{
    private const float ConfirmSeconds = 4f;

    [Header("Scene")]
    [Tooltip("Scene base đầu tiên của game (New Game vào đây khi chưa có Scene tạo nhân vật; Continue dùng khi save chưa ghi Scene).")]
    [SerializeField] private string newGameScene;

    [Tooltip("Scene chọn/tạo nhân vật (Character_Select_Menu). Điền thì nút Start/New Game vào đây; Scene đó lo tạo nhân vật rồi vào game. Trống = vào thẳng New Game Scene.")]
    [SerializeField] private string characterCreationScene;

    [Header("Giao diện")]
    [Tooltip("Tên game hiện ở menu tự dựng.")]
    [SerializeField] private string gameTitle = "Raid And Craft";

    [Header("Nút tự vẽ (tuỳ chọn - để trống cả ba thì Main_Menu tự dựng menu)")]
    [Tooltip("Start: vào Scene chọn nhân vật (Character Creation Scene).")]
    [SerializeField] private Button startButton;
    [Tooltip("Setting: chưa có màn cài đặt, hiện thông báo.")]
    [SerializeField] private Button settingButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitButton;

    [Tooltip("Chỗ hiện thông báo (ví dụ xác nhận New Game). Trống thì chỉ in Console (menu tự dựng có sẵn dòng này).")]
    [SerializeField] private TextMeshProUGUI statusText;

    private float confirmUntil;
    private bool loading;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();

        if (startButton == null && settingButton == null && continueButton == null && newGameButton == null && quitButton == null) BuildDefaultUI();

        if (startButton != null) startButton.onClick.AddListener(OnStart);
        if (settingButton != null) settingButton.onClick.AddListener(OnSetting);

        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
    }

    private void Start()
    {
        RefreshContinueButton();
    }

    private void Update()
    {
        // Hết thời gian xác nhận thì bỏ lời nhắc.
        if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0f;
            SetStatus(string.Empty);
        }
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null) continueButton.interactable = Save_Manager.Instance != null && Save_Manager.Instance.HasSave;
    }

    /// <summary>Start: sang màn chọn/tạo nhân vật (Character Creation Scene).</summary>
    public void OnStart()
    {
        if (loading) return;

        if (string.IsNullOrEmpty(characterCreationScene))
        {
            SetStatus("Chưa điền Scene chọn nhân vật (Character Creation Scene) trong Main_Menu.");
            return;
        }

        Load(characterCreationScene);
    }

    public void OnSetting()
    {
        SetStatus("Chưa có màn Setting.");
    }

    public void OnContinue()
    {
        if (loading) return;

        if (Save_Manager.Instance == null || !Save_Manager.Instance.TryGetSavedBaseScene(out string sceneName))
        {
            SetStatus("Chưa có dữ liệu lưu để tiếp tục.");
            return;
        }

        Load(string.IsNullOrEmpty(sceneName) ? newGameScene : sceneName);
    }

    public void OnNewGame()
    {
        if (loading) return;

        Save_Manager manager = Save_Manager.Instance;
        bool hasSave = manager != null && manager.HasSave;

        // Đang có save: bấm lần hai trong ConfirmSeconds mới xoá (tránh bấm nhầm mất tiến trình).
        if (hasSave && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = Time.unscaledTime + ConfirmSeconds;
            SetStatus("Đã có dữ liệu lưu. Bấm New Game lần nữa để XOÁ và chơi mới.");
            return;
        }

        if (manager != null)
        {
            manager.DeleteSave();
            manager.LastBaseScene = string.Empty;
        }

        Load(string.IsNullOrEmpty(characterCreationScene) ? newGameScene : characterCreationScene);
    }

    /// <summary>Scene tạo nhân vật gọi khi xong để vào Scene base đầu tiên (static, không cần Main_Menu trong Scene đó: truyền tên Scene).</summary>
    public static void StartFirstScene(string firstScene)
    {
        Load(firstScene);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        if (!string.IsNullOrEmpty(message)) Debug.Log($"[{nameof(Main_Menu)}] {message}");
    }

    private static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[{nameof(Main_Menu)}] Scene '{sceneName}' trống hoặc không có trong Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------------------------
    // GIAO DIỆN TỰ DỰNG
    // ------------------------------------------------------------------

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void BuildDefaultUI()
    {
        Canvas canvas = UI_Builder.CreateOverlayCanvas("Main_Menu_Canvas", transform, 10, true, true);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvas.transform, false);
        UI_Builder.Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 1f);

        var column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        column.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)column.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(460f, 0f);

        var layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        column.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = AddText(rect, gameTitle, 56f, 120f);
        title.fontStyle = FontStyles.Bold;

        if (!string.IsNullOrEmpty(characterCreationScene))
        {
            startButton = AddButton(rect, "Start");
        }
        else
        {
            continueButton = AddButton(rect, "Continue");
            newGameButton = AddButton(rect, "New Game");
        }

        quitButton = AddButton(rect, "Quit");

        statusText = AddText(rect, string.Empty, 20f, 60f);
        statusText.color = new Color(1f, 0.75f, 0.45f);
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string text, float size, float height)
    {
        TextMeshProUGUI tmp = UI_Builder.CreateText("Text", parent, size, TextAlignmentOptions.Center);
        tmp.text = text;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return tmp;
    }

    private static Button AddButton(RectTransform parent, string label)
    {
        var go = new GameObject(label + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.14f, 0.2f, 0.3f, 1f);
        go.GetComponent<LayoutElement>().preferredHeight = 56f;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI text = UI_Builder.CreateText("Text", go.transform, 26f, TextAlignmentOptions.Center);
        UI_Builder.Stretch((RectTransform)text.transform, 12f);
        text.text = label;
        return button;
    }
}
