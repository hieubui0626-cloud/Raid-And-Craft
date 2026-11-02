using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Vật phẩm đang CẦM trên con trỏ (kiểu click-nhấc / click-thả). Dùng chung cho lưới Inventory và ô trang bị.
/// - Inventory_UI / Equipment_Slot_UI gọi SetHeld khi nhấc, đọc Held khi thả.
/// - Đang cầm thì IsHoldingItem = true: Player_Controller và Player_Combat không nhận click ra thế giới.
/// - Hình vật phẩm đi theo con trỏ với đúng kích thước như trong lưới (GridSize x ô cơ sở, xem Item_UI_Metrics).
/// - Đóng panel (Inventory_UI tắt) thì ReturnHeldIfAny trả vật phẩm về Inventory.
/// Tự tạo ở lần dùng đầu tiên (Item_Drag_Controller.Instance), không cần đặt vào Scene.
/// </summary>
public class Item_Drag_Controller : MonoBehaviour
{
    private static Item_Drag_Controller instance;

    public static Item_Drag_Controller Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<Item_Drag_Controller>();
            if (instance == null) instance = new GameObject("Item_Drag_Controller").AddComponent<Item_Drag_Controller>();
            return instance;
        }
    }

    /// <summary>Có đang cầm vật phẩm không (không tự tạo controller khi hỏi).</summary>
    public static bool IsHoldingItem => instance != null && !instance.held.IsEmpty;

    /// <summary>Trả vật phẩm đang cầm về Inventory (nếu có). Không tự tạo controller.</summary>
    public static void ReturnHeldIfAny()
    {
        if (instance != null) instance.ReturnHeld();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private Grid_Placement held;
    private Canvas canvas;
    private RectTransform icon;
    private Image iconImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI countText;

    /// <summary>Thứ đang cầm (Origin không có ý nghĩa). IsEmpty = tay trống.</summary>
    public Grid_Placement Held => held;

    private void Awake()
    {
        if (instance == null) instance = this;
        Build();
        Refresh();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        if (held.IsEmpty) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Canvas không có CanvasScaler: tự khớp tỉ lệ với Canvas chứa Inventory để vật phẩm to bằng trong lưới.
        canvas.scaleFactor = Item_UI_Metrics.ReferenceScale;
        icon.position = mouse.position.ReadValue();
        icon.sizeDelta = (Vector2)held.Size * Item_UI_Metrics.CellSize;
    }

    // ------------------------------------------------------------------
    // NHẤC / THẢ
    // ------------------------------------------------------------------

    /// <summary>Cầm thứ này trên con trỏ (thay cho thứ đang cầm, nếu có). Truyền placement rỗng = tay trống.</summary>
    public void SetHeld(Grid_Placement placement)
    {
        held = placement.IsEmpty ? default : new Grid_Placement(placement.Item, placement.Instance, placement.Count, default);
        Refresh();
    }

    public void SetHeldInstance(Item_Instance item)
    {
        SetHeld(item == null ? default : new Grid_Placement(item.Base, item, 1, default));
    }

    public void Clear()
    {
        held = default;
        Refresh();
    }

    /// <summary>
    /// Trả thứ đang cầm về Inventory. True nếu tay đã trống. Kho hết chỗ thì phần còn lại vẫn nằm trên con trỏ
    /// (mở lại Inventory để đặt xuống).
    /// </summary>
    public bool ReturnHeld()
    {
        if (held.IsEmpty) return true;

        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null) return false;

        if (held.Instance != null)
        {
            if (inventory.AddInstance(held.Instance))
            {
                Clear();
                return true;
            }
        }
        else
        {
            int leftover = inventory.Add(held.Item, held.Count);
            if (leftover <= 0)
            {
                Clear();
                return true;
            }

            held = new Grid_Placement(held.Item, null, leftover, default);
            Refresh();
        }

        Debug.LogWarning($"[{nameof(Item_Drag_Controller)}] Inventory hết chỗ, '{held.Item.DisplayName}' vẫn nằm trên con trỏ. Mở Inventory và đặt xuống.", this);
        return false;
    }

    // ------------------------------------------------------------------
    // HIỂN THỊ
    // ------------------------------------------------------------------

    private void Refresh()
    {
        if (canvas == null) return;

        bool has = !held.IsEmpty;
        canvas.gameObject.SetActive(has);
        if (!has) return;

        Sprite sprite = held.Item.Icon;
        iconImage.sprite = sprite;
        iconImage.color = sprite != null ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.25f, 0.3f, 0.4f, 0.85f);

        nameText.gameObject.SetActive(sprite == null);
        nameText.text = held.Item.DisplayName;

        countText.gameObject.SetActive(held.Count > 1);
        countText.text = held.Count.ToString();

        canvas.scaleFactor = Item_UI_Metrics.ReferenceScale;
        icon.sizeDelta = (Vector2)held.Size * Item_UI_Metrics.CellSize;

        Mouse mouse = Mouse.current;
        if (mouse != null) icon.position = mouse.position.ReadValue();
    }

    private void Build()
    {
        var canvasObject = new GameObject("Held_Item_Canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120; // Trên mọi tooltip.

        var iconObject = new GameObject("Held_Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(canvasObject.transform, false);
        icon = (RectTransform)iconObject.transform;
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0f);

        iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = false;

        nameText = CreateText("Name", iconObject.transform, 14f, TextAlignmentOptions.Center);
        Stretch((RectTransform)nameText.transform, 2f);

        countText = CreateText("Count", iconObject.transform, 16f, TextAlignmentOptions.BottomRight);
        Stretch((RectTransform)countText.transform, 3f);
        countText.fontStyle = FontStyles.Bold;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
