using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Popup chọn skill để gán vào một ô, dạng LƯỚI ô vuông (mặc định 3 cột). Skill_Bar_UI tạo và gọi Open/Close;
/// tự dựng toàn bộ giao diện bằng code (không cần prefab): lớp nền bấm ra ngoài để đóng, bảng lưới cuộn được.
/// - Mỗi ô là một skill (icon, hoặc tên nếu chưa có icon). Ô đầu tiên luôn là "Empty" để bỏ trống ô skill.
/// - Số cột, số hàng hiển thị và cỡ ô chỉnh bằng Configure (Skill_Bar_UI có các ô nhập trong Inspector).
///   Skill nhiều hơn số hàng hiển thị thì cuộn bằng con lăn chuột hoặc kéo.
/// - Rê chuột vào ô: bắn OnHoverBegin/OnHoverEnd để hiện tooltip. Bấm Esc cũng đóng.
/// </summary>
public class Skill_Picker_UI : MonoBehaviour
{
    private const float TitleHeight = 36f;
    private const float HintHeight = 30f;
    private const float Padding = 8f;
    private const float Spacing = 6f;

    /// <summary>Rê chuột vào ô skill (skill khác null).</summary>
    public event Action<Skill_Data> OnHoverBegin;

    /// <summary>Con trỏ rời ô skill hoặc popup đóng.</summary>
    public event Action OnHoverEnd;

    private int columns = 3;
    private int visibleRows = 3;
    private float cellSize = 72f;

    private Canvas canvas;
    private RectTransform panel;
    private RectTransform scrollArea;
    private RectTransform content;
    private GridLayoutGroup grid;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI hintText;
    private ScrollRect scrollRect;
    private Action<Skill_Data> onPicked;

    public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        Build();
        Close();
    }

    private void Update()
    {
        if (!IsOpen) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    /// <summary>Số cột (>= 1), số hàng hiển thị trước khi phải cuộn (>= 1) và cạnh ô vuông (pixel, theo độ phân giải 1080p).</summary>
    public void Configure(int columns, int visibleRows, float cellSize)
    {
        this.columns = Mathf.Max(1, columns);
        this.visibleRows = Mathf.Max(1, visibleRows);
        this.cellSize = Mathf.Max(24f, cellSize);
    }

    /// <summary>
    /// Mở popup cạnh một ô.
    /// anchor: RectTransform của ô trên thanh kỹ năng; anchorCanvas: Canvas chứa ô (để quy đổi vị trí);
    /// onPicked: gọi khi chọn (null = bỏ trống).
    /// </summary>
    public void Open(string slotLabel, RectTransform anchor, Canvas anchorCanvas, IReadOnlyList<Skill_Data> skills,
                     Skill_Data current, Action<Skill_Data> onPicked)
    {
        this.onPicked = onPicked;
        titleText.text = $"Assign skill to slot {slotLabel}";

        int skillCount = RebuildCells(skills, current);
        bool noSkills = skillCount == 0;

        hintText.gameObject.SetActive(noSkills);
        hintText.text = "No skills yet. Add skills to 'Available Skills' on Player_Skill_Slots.";
        float hintHeight = noSkills ? HintHeight : 0f;

        int cellCount = skillCount + 1; // + ô Empty
        int rowsNeeded = Mathf.CeilToInt(cellCount / (float)columns);

        float contentHeight = RowsHeight(rowsNeeded);
        float listHeight = Mathf.Min(contentHeight, RowsHeight(visibleRows));
        float width = columns * cellSize + (columns - 1) * Spacing + Padding * 2f;

        panel.sizeDelta = new Vector2(width, TitleHeight + hintHeight + listHeight + Padding * 2f);

        scrollArea.offsetMin = new Vector2(Padding, Padding);
        scrollArea.offsetMax = new Vector2(-Padding, -(TitleHeight + hintHeight));
        content.sizeDelta = new Vector2(0f, contentHeight);

        canvas.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        PlaceNextTo(anchor, anchorCanvas);
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        bool wasOpen = IsOpen;

        if (canvas != null) canvas.gameObject.SetActive(false);
        onPicked = null;

        if (wasOpen) OnHoverEnd?.Invoke();
    }

    private float RowsHeight(int rows)
    {
        return rows * cellSize + Mathf.Max(0, rows - 1) * Spacing;
    }

    // ------------------------------------------------------------------
    // Vị trí
    // ------------------------------------------------------------------

    // Đặt ngay phía trên ô (thanh kỹ năng thường nằm dưới màn hình); không đủ chỗ thì đặt phía dưới. Luôn nằm trong màn hình.
    private void PlaceNextTo(RectTransform anchor, Canvas anchorCanvas)
    {
        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners); // 0 dưới-trái, 1 trên-trái, 2 trên-phải, 3 dưới-phải

        Canvas rootCanvas = anchorCanvas != null ? anchorCanvas.rootCanvas : null;
        Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        Vector2 topCenter = RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) * 0.5f);
        Vector2 bottomCenter = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[3]) * 0.5f);

        float scale = canvas.scaleFactor;
        float width = panel.sizeDelta.x * scale;
        float height = panel.sizeDelta.y * scale;

        bool above = topCenter.y + height <= Screen.height;

        panel.pivot = new Vector2(0.5f, above ? 0f : 1f);
        Vector2 position = above ? topCenter : bottomCenter;

        position.x = Mathf.Clamp(position.x, width * 0.5f, Screen.width - width * 0.5f);
        panel.position = position;
    }

    // ------------------------------------------------------------------
    // Dựng giao diện
    // ------------------------------------------------------------------

    private void Build()
    {
        var canvasObject = new GameObject("Skill_Picker_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Trên mọi UI khác.

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Lớp nền phủ toàn màn hình: bấm ra ngoài thì đóng, đồng thời chặn click xuyên xuống thế giới game.
        var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(canvasObject.transform, false);
        Stretch((RectTransform)blocker.transform, 0f);
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        blocker.GetComponent<Button>().onClick.AddListener(Close);

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        panel = (RectTransform)panelObject.transform;
        panelObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.97f);

        titleText = CreateText(panel, "Title", 20f, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(0f, TitleHeight);
        titleText.rectTransform.anchoredPosition = Vector2.zero;

        // Dòng gợi ý (chỉ hiện khi chưa có skill nào), nằm ngay dưới tiêu đề.
        hintText = CreateText(panel, "Hint", 13f, TextAlignmentOptions.Center);
        hintText.color = new Color(0.8f, 0.8f, 0.8f);
        hintText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hintText.rectTransform.anchorMax = new Vector2(1f, 1f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hintText.rectTransform.sizeDelta = new Vector2(-Padding * 2f, HintHeight);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -TitleHeight);

        // Vùng cuộn chứa lưới.
        var scrollObject = new GameObject("Grid_Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel, false);
        scrollArea = (RectTransform)scrollObject.transform;
        scrollArea.anchorMin = Vector2.zero;
        scrollArea.anchorMax = Vector2.one;
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(scrollObject.transform, false);
        content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.spacing = new Vector2(Spacing, Spacing);

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = scrollArea;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
    }

    // Dựng lại các ô; trả về số skill (không tính ô Empty).
    private int RebuildCells(IReadOnlyList<Skill_Data> skills, Skill_Data current)
    {
        // Cấu hình lưới theo giá trị hiện tại (Configure có thể được gọi sau khi Build).
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellSize, cellSize);

        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        // Destroy trễ tới cuối frame nên tách khỏi cây ngay để bố cục tính đúng.
        content.DetachChildren();

        AddCell(null, current == null);

        int count = 0;
        if (skills != null)
        {
            foreach (Skill_Data skill in skills)
            {
                if (skill == null) continue;

                AddCell(skill, skill == current);
                count++;
            }
        }

        return count;
    }

    private void AddCell(Skill_Data skill, bool isCurrent)
    {
        var cellObject = new GameObject(skill != null ? skill.displayName : "Empty", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Skill_Hover_Target));
        cellObject.transform.SetParent(content, false);

        var background = cellObject.GetComponent<Image>();
        background.color = isCurrent ? new Color(0.25f, 0.4f, 0.6f, 1f) : new Color(0.17f, 0.18f, 0.23f, 1f);

        var button = cellObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Pick(skill));

        if (skill != null)
        {
            var hover = cellObject.GetComponent<Skill_Hover_Target>();
            hover.OnEnter = () => OnHoverBegin?.Invoke(skill);
            hover.OnExit = () => OnHoverEnd?.Invoke();
        }

        bool showIcon = skill != null && skill.icon != null;

        if (showIcon)
        {
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(cellObject.transform, false);
            Stretch((RectTransform)iconObject.transform, 6f);

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = skill.icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        else
        {
            // Chưa có icon (hoặc ô Empty): hiện chữ co giãn vừa ô.
            TextMeshProUGUI label = CreateText((RectTransform)cellObject.transform, "Label", 16f, TextAlignmentOptions.Center);
            label.text = skill != null ? skill.displayName : "Empty";
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 18f;
            if (skill == null) label.color = new Color(0.7f, 0.7f, 0.7f);
            Stretch(label.rectTransform, 4f);
        }
    }

    private void Pick(Skill_Data skill)
    {
        Action<Skill_Data> callback = onPicked;
        Close();
        callback?.Invoke(skill);
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string objectName, float size, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
