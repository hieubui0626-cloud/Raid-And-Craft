using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hàm dựng UI bằng code dùng chung (Canvas, chữ TextMeshPro, giãn kín RectTransform, khung tooltip).
/// Trước đây mỗi UI (Shop, Skill_Picker, tooltip, Inventory...) tự chép một bản các đoạn này.
/// </summary>
public static class UI_Builder
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const float MatchWidthOrHeight = 0.5f;

    /// <summary>
    /// Tạo Canvas Screen Space Overlay. withScaler: thêm CanvasScaler (ScaleWithScreenSize 1920x1080);
    /// withRaycaster: thêm GraphicRaycaster (cần khi Canvas có nút bấm). parent = null: nằm ở gốc Scene.
    /// </summary>
    public static Canvas CreateOverlayCanvas(string objectName, Transform parent, int sortingOrder, bool withScaler = true, bool withRaycaster = false)
    {
        var canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas));
        if (parent != null) canvasObject.transform.SetParent(parent, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        if (withScaler)
        {
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
        }

        if (withRaycaster) canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>Tạo chữ TextMeshPro (không nhận raycast).</summary>
    public static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment, bool richText = false)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.richText = richText;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>Giãn RectTransform kín phần tử cha, thụt vào inset mỗi cạnh.</summary>
    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>Tạo object con rỗng chỉ có RectTransform, giãn kín phần tử cha.</summary>
    public static RectTransform CreateStretchedChild(string objectName, RectTransform parent, float inset = 0f)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        Stretch(rect, inset);
        return rect;
    }

    /// <summary>
    /// Dựng Canvas + khung nền tự co theo chữ + một dòng chữ rich text (dùng cho tooltip).
    /// Khung và chữ không nhận raycast nên không cản chuột.
    /// </summary>
    public static Canvas CreateTooltip(string canvasName, Transform parent, int sortingOrder, float panelWidth, float padding,
                                       out RectTransform panel, out TextMeshProUGUI text)
    {
        Canvas canvas = CreateOverlayCanvas(canvasName, parent, sortingOrder);

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(canvas.transform, false);
        panel = (RectTransform)panelObject.transform;
        panel.sizeDelta = new Vector2(panelWidth, 0f);

        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        background.raycastTarget = false;

        var layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text = CreateText("Text", panel, 18f, TextAlignmentOptions.TopLeft, true);
        return canvas;
    }
}
