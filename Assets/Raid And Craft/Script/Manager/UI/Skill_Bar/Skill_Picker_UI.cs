using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Popup danh sách skill để chọn gán vào một ô. Skill_Bar_UI tạo và gọi Open/Close; tự dựng toàn bộ giao diện bằng code
/// (không cần prefab): một lớp nền bấm ra ngoài để đóng, bảng danh sách cuộn được, mỗi dòng gồm icon + tên + mana.
/// Dòng đầu luôn là "Empty" để xoá skill khỏi ô. Bấm Esc cũng đóng.
/// </summary>
public class Skill_Picker_UI : MonoBehaviour
{
    private const float PanelWidth = 280f;
    private const float RowHeight = 44f;
    private const float RowSpacing = 4f;
    private const float TitleHeight = 36f;
    private const float Padding = 6f;
    private const float MaxListHeight = 320f;

    private Canvas canvas;
    private RectTransform panel;
    private RectTransform content;
    private TextMeshProUGUI titleText;
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

    /// <summary>
    /// Mở popup cạnh một ô.
    /// anchor: RectTransform của ô; anchorCanvas: Canvas chứa ô (để quy đổi vị trí);
    /// onPicked: gọi khi chọn (null = bỏ trống).
    /// </summary>
    public void Open(string slotLabel, RectTransform anchor, Canvas anchorCanvas, IReadOnlyList<Skill_Data> skills,
                     Skill_Data current, Action<Skill_Data> onPicked)
    {
        this.onPicked = onPicked;
        titleText.text = $"Assign skill to slot {slotLabel}";

        RebuildRows(skills, current);

        float listHeight = Mathf.Min(MaxListHeight, content.sizeDelta.y);
        panel.sizeDelta = new Vector2(PanelWidth, TitleHeight + listHeight + Padding * 2f);

        canvas.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        PlaceNextTo(anchor, anchorCanvas);
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
        onPicked = null;
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

        // Vùng cuộn.
        var scrollObject = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel, false);
        var scrollRectTransform = (RectTransform)scrollObject.transform;
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(Padding, Padding);
        scrollRectTransform.offsetMax = new Vector2(-Padding, -TitleHeight);
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(scrollObject.transform, false);
        content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = scrollRectTransform;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
    }

    private void RebuildRows(IReadOnlyList<Skill_Data> skills, Skill_Data current)
    {
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        // Destroy trễ tới cuối frame nên tách khỏi cây ngay để bố cục tính đúng.
        content.DetachChildren();

        AddRow(null, "- Empty -", null, current == null);

        if (skills == null || skills.Count == 0)
        {
            AddMessageRow("No skills yet. Add skills to 'Available Skills' on Player_Skill_Slots.");
        }
        else
        {
            foreach (Skill_Data skill in skills)
            {
                if (skill == null) continue;

                string label = skill.manaCost > 0f ? $"{skill.displayName}   ({skill.manaCost:0.#} mana)" : skill.displayName;
                AddRow(skill, label, skill.icon, skill == current);
            }
        }

        // Tính chiều cao nội dung ngay (ContentSizeFitter chỉ tính ở cuối frame).
        float height = 0f;
        for (int i = 0; i < content.childCount; i++)
        {
            height += content.GetChild(i).GetComponent<LayoutElement>().preferredHeight;
            if (i > 0) height += RowSpacing;
        }
        content.sizeDelta = new Vector2(0f, height);
    }

    private void AddRow(Skill_Data skill, string label, Sprite icon, bool isCurrent)
    {
        var rowObject = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        rowObject.transform.SetParent(content, false);
        rowObject.GetComponent<LayoutElement>().preferredHeight = RowHeight;

        var background = rowObject.GetComponent<Image>();
        background.color = isCurrent ? new Color(0.25f, 0.4f, 0.6f, 1f) : new Color(0.17f, 0.18f, 0.23f, 1f);

        var button = rowObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Pick(skill));

        float textLeft = Padding;

        if (icon != null)
        {
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(rowObject.transform, false);

            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(RowHeight - 8f, RowHeight - 8f);
            iconRect.anchoredPosition = new Vector2(Padding, 0f);

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            textLeft = Padding * 2f + RowHeight - 8f;
        }

        TextMeshProUGUI text = CreateText((RectTransform)rowObject.transform, "Label", 18f, TextAlignmentOptions.MidlineLeft);
        text.text = label;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(textLeft, 0f);
        text.rectTransform.offsetMax = new Vector2(-Padding, 0f);
    }

    private void AddMessageRow(string message)
    {
        var rowObject = new GameObject("Message", typeof(RectTransform), typeof(LayoutElement));
        rowObject.transform.SetParent(content, false);
        rowObject.GetComponent<LayoutElement>().preferredHeight = RowHeight * 2f;

        TextMeshProUGUI text = CreateText((RectTransform)rowObject.transform, "Text", 14f, TextAlignmentOptions.Center);
        text.text = message;
        Stretch(text.rectTransform, Padding);
        text.color = new Color(0.8f, 0.8f, 0.8f);
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
