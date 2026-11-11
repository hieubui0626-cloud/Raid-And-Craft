using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu tạm dừng (Esc): Tiếp tục / Lưu game / Về Main Menu / Thoát game. TỰ có mặt trong mọi Scene chơi (Scene có Player_Save), không cần đặt tay.
/// Về Main Menu và Thoát đều lưu trước. Tên Scene Main Menu chỉnh ở Save_Manager.
/// Esc đang đóng bảng Map thì không mở Pause Menu cùng lúc.
/// </summary>
public class Pause_Menu : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.09f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.14f, 0.2f, 0.3f, 1f);

    private static Pause_Menu instance;

    private Canvas canvas;
    private TextMeshProUGUI messageText;

    public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null) return;
        if (FindFirstObjectByType<Player_Save>() == null) return; // Main Menu và Scene không có Player thì không cần

        instance = new GameObject("Pause_Menu").AddComponent<Pause_Menu>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Build();
        canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (IsOpen) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (IsOpen) Close();
        else if (!Map_Panel_UI.IsOpenOrJustClosed) Open();
    }

    public void Open()
    {
        messageText.text = string.Empty;
        canvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Close()
    {
        canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void SaveClicked()
    {
        bool ok = Save_Manager.Instance != null && Save_Manager.Instance.SaveNow();
        messageText.text = ok ? "Đã lưu game." : "Chưa lưu được (xem Console).";
    }

    private void MainMenuClicked()
    {
        if (!Scene_Transition.GoToMainMenu()) messageText.text = "Không về được Main Menu (xem Console).";
    }

    private void QuitClicked()
    {
        if (Save_Manager.Instance != null) Save_Manager.Instance.SaveNow();
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Build()
    {
        canvas = UI_Builder.CreateOverlayCanvas("Pause_Canvas", transform, 60, true, true);

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvas.transform, false);
        UI_Builder.Stretch((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(canvas.transform, false);
        var panel = (RectTransform)panelObject.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(420f, 0f);
        panelObject.GetComponent<Image>().color = PanelColor;

        var layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panelObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddText(panel, "Tạm dừng", 32f, 48f, FontStyles.Bold);
        CreateButton(panel, "Tiếp tục", Close);
        CreateButton(panel, "Lưu game", SaveClicked);
        CreateButton(panel, "Về Main Menu", MainMenuClicked);
        CreateButton(panel, "Thoát game", QuitClicked);
        messageText = AddText(panel, string.Empty, 20f, 30f, FontStyles.Normal);
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string text, float size, float height, FontStyles style)
    {
        TextMeshProUGUI tmp = UI_Builder.CreateText("Text", parent, size, TextAlignmentOptions.Center);
        tmp.text = text;
        tmp.fontStyle = style;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return tmp;
    }

    private static void CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = ButtonColor;
        go.GetComponent<LayoutElement>().preferredHeight = 50f;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = UI_Builder.CreateText("Text", go.transform, 24f, TextAlignmentOptions.Center);
        UI_Builder.Stretch((RectTransform)text.transform, 12f);
        text.text = label;
    }
}
