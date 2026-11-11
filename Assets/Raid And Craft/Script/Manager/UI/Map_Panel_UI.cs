using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// BẢN PHÁC của bảng Map: liệt kê các điểm dịch chuyển nhanh đã mở (Player_Fast_Travel), nhóm theo Scene; bấm một điểm để dịch chuyển tới.
/// Sau này thay bằng World Map thật (modifier, quest); phần dịch chuyển vẫn gọi Player_Fast_Travel.TryTravel.
/// Mở: phím M (Player_Fast_Travel lo), nút Map (gắn Map_Button lên Button), hoặc bấm vào portal. Đóng: Esc, nút Đóng, hoặc dịch chuyển xong.
/// Tự dựng giao diện bằng code, không cần đặt vào Scene.
/// </summary>
public class Map_Panel_UI : MonoBehaviour
{
    private const float PanelWidth = 560f;

    private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.09f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.14f, 0.2f, 0.3f, 1f);
    private static readonly Color ButtonOffColor = new Color(0.14f, 0.14f, 0.16f, 1f);

    private static Map_Panel_UI instance;

    public static Map_Panel_UI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<Map_Panel_UI>();
            if (instance == null) instance = new GameObject("Map_Panel_UI").AddComponent<Map_Panel_UI>();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private Canvas canvas;
    private RectTransform listRoot;
    private TextMeshProUGUI locationText;
    private TextMeshProUGUI messageText;
    private Player_Fast_Travel travel;

    public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

    /// <summary>Bảng Map đang mở, hoặc vừa đóng trong frame này (Esc đóng Map thì Pause Menu không mở theo). Không tạo Map_Panel_UI.</summary>
    public static bool IsOpenOrJustClosed => instance != null && (instance.IsOpen || instance.closedFrame == Time.frameCount);

    private int closedFrame = -1;

    private void Awake()
    {
        if (instance == null) instance = this;
        Build();
        canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (instance == this) instance = null;
    }

    // ------------------------------------------------------------------
    // MỞ / ĐÓNG
    // ------------------------------------------------------------------

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        Unsubscribe();

        travel = FindFirstObjectByType<Player_Fast_Travel>();
        if (travel == null)
        {
            Scene_Transition.ReportMessage("Player chưa có Player_Fast_Travel nên chưa mở được Map.");
            return;
        }

        travel.OnChanged += Refresh;
        messageText.text = string.Empty;
        Refresh();
        canvas.gameObject.SetActive(true);
    }

    public void Close()
    {
        Unsubscribe();
        closedFrame = Time.frameCount;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (travel != null) travel.OnChanged -= Refresh;
        travel = null;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void Build()
    {
        canvas = UI_Builder.CreateOverlayCanvas("Map_Canvas", transform, 40, true, true);

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(canvas.transform, false);
        var panel = (RectTransform)panelObject.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, 0f);
        panelObject.GetComponent<Image>().color = PanelColor;

        var layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = AddText(panel, "Title", "Map - Điểm dịch chuyển", 30f, 44f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        locationText = AddText(panel, "Location", string.Empty, 20f, 30f, TextAlignmentOptions.Center);

        var listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
        listObject.transform.SetParent(panel, false);
        listRoot = (RectTransform)listObject.transform;
        var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        messageText = AddText(panel, "Message", string.Empty, 20f, 30f, TextAlignmentOptions.Center);
        messageText.color = new Color(1f, 0.6f, 0.45f);

        CreateButton("CloseButton", panel, "Đóng", 44f, TextAlignmentOptions.Center, ButtonColor, Close);
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string objectName, string text, float size, float height, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = UI_Builder.CreateText(objectName, parent, size, alignment);
        tmp.text = text;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return tmp;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, float height, TextAlignmentOptions alignment,
                                       Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        go.GetComponent<LayoutElement>().preferredHeight = height;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = UI_Builder.CreateText("Text", go.transform, 22f, alignment);
        UI_Builder.Stretch((RectTransform)text.transform, 12f);
        text.text = label;
        return button;
    }

    // ------------------------------------------------------------------
    // DANH SÁCH
    // ------------------------------------------------------------------

    private void Refresh()
    {
        if (travel == null) return;

        // Destroy chỉ có hiệu lực cuối frame: tách khỏi cây ngay để layout không tính nhầm hàng cũ.
        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = listRoot.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        string currentScene = travel.gameObject.scene.name;
        Scene_Info info = FindFirstObjectByType<Scene_Info>();
        bool allowed = info == null || info.AllowFastTravel;
        locationText.text = "Đang ở: " + currentScene + (allowed ? string.Empty : "  (không thể dịch chuyển nhanh ở đây)");

        if (travel.Entries.Count == 0)
        {
            TextMeshProUGUI empty = AddText(listRoot, "Empty", "Chưa mở điểm dịch chuyển nào.\nTìm và bấm vào các portal trong thế giới.", 20f, 64f, TextAlignmentOptions.Center);
            empty.color = new Color(1f, 1f, 1f, 0.6f);
            return;
        }

        // Nhóm theo Scene, giữ thứ tự mở.
        var order = new List<string>();
        foreach (Fast_Travel_Entry entry in travel.Entries)
        {
            if (!order.Contains(entry.sceneName)) order.Add(entry.sceneName);
        }

        foreach (string sceneName in order)
        {
            TextMeshProUGUI header = AddText(listRoot, "Header_" + sceneName, sceneName, 20f, 28f, TextAlignmentOptions.MidlineLeft);
            header.color = new Color(1f, 0.82f, 0.5f);

            foreach (Fast_Travel_Entry entry in travel.Entries)
            {
                if (entry.sceneName != sceneName) continue;

                Fast_Travel_Entry captured = entry;
                bool here = sceneName == currentScene;
                string label = string.IsNullOrEmpty(entry.displayName) ? entry.spawnId : entry.displayName;
                CreateButton("Point_" + entry.spawnId, listRoot, label, 44f, TextAlignmentOptions.MidlineLeft,
                             allowed ? ButtonColor : ButtonOffColor, () => OnPointClicked(captured));
            }
        }
    }

    private void OnPointClicked(Fast_Travel_Entry entry)
    {
        if (travel == null) return;

        if (travel.TryTravel(entry, out string reason)) Close();
        else messageText.text = reason;
    }
}
