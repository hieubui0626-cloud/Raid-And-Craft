using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Một ô trang bị trong khung Equipment (Character Overview). Gắn lên MỖI object ô trang bị đã có sẵn trong UI,
/// chọn Slot tương ứng (MainHand, Head, Chest...). Script tự tạo hình vật phẩm, viền sáng và vùng bắt chuột con bên trong.
/// - Chuột trái, tay trống: nhấc món đang mặc lên con trỏ (tháo khỏi người).
/// - Chuột trái, đang cầm một trang bị đúng ô: mặc vào; món cũ (nếu có) ở lại trên con trỏ.
/// - Chuột phải, tay trống: tháo món về Inventory.
/// - Rê chuột: tooltip. Đang cầm trang bị hợp ô này thì ô sáng xanh.
/// Hình vật phẩm vừa khung ô (giữ tỉ lệ sprite), vì ô trang bị không nhất thiết bằng kích thước lưới.
/// </summary>
[DisallowMultipleComponent]
public class Equipment_Slot_UI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Ô trang bị")]
    [SerializeField] private Equipment_Slot slot = Equipment_Slot.MainHand;

    [Header("Nguồn dữ liệu (bỏ trống thì tự tìm Inventory của Player)")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private Character_Equipment equipment;

    [Header("Hiển thị")]
    [Tooltip("Lề giữa khung ô và hình vật phẩm (đơn vị Canvas).")]
    [SerializeField, Min(0f)] private float iconInset = 4f;

    [SerializeField] private Color fitColor = new Color(0.2f, 0.9f, 0.3f, 0.3f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.15f);

    private Image iconImage;
    private Image highlightImage;
    private bool built;
    private bool subscribed;
    private bool pointerInside;

    public Equipment_Slot Slot => slot;

    // ------------------------------------------------------------------
    // VÒNG ĐỜI
    // ------------------------------------------------------------------

    private void OnEnable()
    {
        EnsureBuilt();
        pointerInside = false;
        if (TryResolve()) Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        pointerInside = false;
        Item_Tooltip_UI.HideIfExists();

        // Panel đóng khi đang cầm đồ: trả về Inventory để không mất.
        Item_Drag_Controller.ReturnHeldIfAny();
    }

    private bool TryResolve()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();

        // Enemy cũng có Character_Equipment nên chỉ lấy cái cùng object với Inventory của Player.
        if (equipment == null && inventory != null) equipment = inventory.GetComponent<Character_Equipment>();

        return equipment != null;
    }

    private void Subscribe()
    {
        if (subscribed || equipment == null) return;

        equipment.OnEquipmentChanged += HandleEquipmentChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (equipment != null) equipment.OnEquipmentChanged -= HandleEquipmentChanged;
        subscribed = false;
    }

    private void HandleEquipmentChanged(Equipment_Slot changedSlot, Item_Instance oldItem, Item_Instance newItem)
    {
        if (changedSlot != slot) return;

        Refresh();
        if (pointerInside) ShowTooltip();
    }

    private void Update()
    {
        // Player có thể được tạo sau khi panel bật: thử lại cho tới khi tìm được.
        if (!subscribed)
        {
            if (TryResolve())
            {
                Subscribe();
                Refresh();
            }
            return;
        }

        UpdateHighlight();
    }

    // ------------------------------------------------------------------
    // HIỂN THỊ
    // ------------------------------------------------------------------

    private void Refresh()
    {
        if (iconImage == null) return;

        Item_Instance item = equipment != null ? equipment.GetEquipped(slot) : null;
        Sprite sprite = item != null ? item.Base.Icon : null;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    // Sáng xanh khi con trỏ đang cầm một trang bị hợp ô này, sáng nhẹ khi rê chuột.
    private void UpdateHighlight()
    {
        Color color = default;
        bool show = false;

        Grid_Placement held = Item_Drag_Controller.IsHoldingItem ? Item_Drag_Controller.Instance.Held : default;
        if (!held.IsEmpty && held.Instance != null && held.Item.EquipSlot == slot && equipment.CanEquip(held.Instance))
        {
            color = fitColor;
            show = true;
        }
        else if (pointerInside)
        {
            color = hoverColor;
            show = true;
        }

        if (highlightImage.gameObject.activeSelf != show) highlightImage.gameObject.SetActive(show);
        if (show) highlightImage.color = color;
    }

    private void ShowTooltip()
    {
        Item_Instance item = equipment != null ? equipment.GetEquipped(slot) : null;
        if (item == null || Item_Drag_Controller.IsHoldingItem)
        {
            Item_Tooltip_UI.HideIfExists();
            return;
        }

        Item_Tooltip_UI.Instance.Show(item);
    }

    // ------------------------------------------------------------------
    // CHUỘT
    // ------------------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Item_Tooltip_UI.HideIfExists();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (equipment == null) return;

        Item_Drag_Controller drag = Item_Drag_Controller.Instance;

        if (drag.Held.IsEmpty)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (equipment.TryTakeEquipped(slot, out Item_Instance taken))
                {
                    Item_Tooltip_UI.HideIfExists();
                    drag.SetHeldInstance(taken);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                equipment.TryUnequip(slot);
                if (pointerInside) ShowTooltip();
            }
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left) return;

        Item_Instance heldInstance = drag.Held.Instance;
        if (heldInstance == null) return;

        if (equipment.TryEquipHeld(slot, heldInstance, out Item_Instance previous))
        {
            drag.SetHeldInstance(previous); // null = tay trống; có món cũ thì cầm tiếp
        }
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        highlightImage = CreateImage("Slot_Highlight", Color.clear, false);
        highlightImage.gameObject.SetActive(false);

        iconImage = CreateImage("Slot_Icon", Color.white, false);
        iconImage.preserveAspect = true;
        var iconRect = iconImage.rectTransform;
        iconRect.offsetMin = new Vector2(iconInset, iconInset);
        iconRect.offsetMax = new Vector2(-iconInset, -iconInset);
        iconImage.enabled = false;

        // Cần một Graphic bật Raycast Target để nhận click/rê chuột; nếu ô chưa có thì thêm vùng gần như trong suốt.
        Graphic existing = GetComponent<Graphic>();
        if (existing == null || !existing.raycastTarget)
        {
            Image hit = CreateImage("Slot_Hit", new Color(1f, 1f, 1f, 0.004f), true);
            hit.transform.SetAsLastSibling();
        }
    }

    private Image CreateImage(string objectName, Color color, bool raycast)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }
}
