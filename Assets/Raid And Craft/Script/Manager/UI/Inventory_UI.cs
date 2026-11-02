using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Giao diện lưới Inventory. Gắn lên object PANEL Inventory (cái được bật/tắt bằng phím B qua Panel_Hotkey_Toggle).
/// Tự dựng lưới bằng code, mỗi ô = Cell Size (mặc định 48 đơn vị Canvas trên màn hình tham chiếu 1920x1080);
/// vật phẩm Grid Size 2x3 vẽ sprite đúng 96x144.
/// Cách chơi (click-nhấc / click-thả):
///   - Chuột trái vào vật phẩm: nhấc lên con trỏ. Chuột trái vào lưới khi đang cầm: thả xuống
///     (ô trống = đặt, đè đúng một chồng cùng loại = gộp tới Max Stack và phần dư ở lại con trỏ, đè đúng một chồng khác = đổi chỗ).
///   - Chuột phải vào trang bị (không cầm gì): mặc luôn.
///   - Rê chuột vào vật phẩm: tooltip. Đang cầm vật phẩm: vùng thả hiện xanh (được) hoặc đỏ (không được).
///   - Đóng panel thì vật phẩm đang cầm tự về lại Inventory.
/// Ô trang bị là Equipment_Slot_UI riêng.
/// </summary>
[DisallowMultipleComponent]
public class Inventory_UI : MonoBehaviour
{
    [Header("Nguồn dữ liệu")]
    [Tooltip("Bỏ trống thì tự tìm Inventory trong Scene (Player).")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Bỏ trống thì lấy Character_Equipment cùng object với Inventory (dùng cho chuột phải để mặc).")]
    [SerializeField] private Character_Equipment equipment;

    [Header("Bố cục")]
    [Tooltip("Vùng chứa lưới. Bỏ trống = chính object này. Lưới được neo ở góc trên-trái vùng này.")]
    [SerializeField] private RectTransform gridContainer;

    [Tooltip("Cạnh một ô (đơn vị Canvas). Ô cơ sở của dự án = 48 trên màn hình tham chiếu 1920x1080.")]
    [SerializeField, Min(8f)] private float cellSize = Item_UI_Metrics.DefaultCellSize;

    [Tooltip("Khoảng cách từ góc trên-trái vùng chứa tới góc trên-trái lưới (X sang phải, Y xuống dưới).")]
    [SerializeField] private Vector2 gridOffset = new Vector2(8f, 8f);

    [Tooltip("Khe hở giữa các ô (đơn vị Canvas).")]
    [SerializeField, Min(0f)] private float cellGap = 1f;

    [Header("Màu")]
    [SerializeField] private Color cellColor = new Color(1f, 1f, 1f, 0.07f);
    [SerializeField] private Color itemBackColor = new Color(0.12f, 0.15f, 0.22f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color dropOkColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    [SerializeField] private Color dropBadColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

    private RectTransform gridRect;
    private RectTransform cellsRoot;
    private RectTransform itemsRoot;
    private Image hoverImage;
    private Image dropImage;
    private UI_Pointer_Relay relay;

    private Canvas rootCanvas;
    private bool built;
    private bool pointerInside;
    private int builtWidth = -1;
    private int builtHeight = -1;

    private bool hoverShown;
    private Vector2Int hoverOrigin;

    private float CellStep => cellSize;

    // ------------------------------------------------------------------
    // VÒNG ĐỜI
    // ------------------------------------------------------------------

    private void OnEnable()
    {
        pointerInside = false;
        Item_UI_Metrics.CellSize = cellSize;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        Item_UI_Metrics.ReferenceCanvas = rootCanvas;

        EnsureBuilt();
        if (TryResolve()) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        pointerInside = false;
        HideHover();
        HideDropPreview();
        Item_Tooltip_UI.HideIfExists();

        // Đóng panel khi đang cầm đồ: trả về kho để không mất, không kẹt chuột.
        Item_Drag_Controller.ReturnHeldIfAny();
    }

    private bool subscribed;

    private bool TryResolve()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null) return false;

        if (equipment == null) equipment = inventory.GetComponent<Character_Equipment>();

        RebuildCellsIfResized();
        RebuildItems();
        return true;
    }

    private void Subscribe()
    {
        if (subscribed || inventory == null) return;

        inventory.OnContainerChanged += HandleContainerChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (inventory != null) inventory.OnContainerChanged -= HandleContainerChanged;
        subscribed = false;
    }

    private void HandleContainerChanged()
    {
        RebuildCellsIfResized();
        RebuildItems();
        hoverShown = false; // buộc Update tính lại hover/tooltip với nội dung mới
    }

    private void Update()
    {
        // Player có thể được tạo sau khi panel bật: thử lại cho tới khi tìm được kho.
        if (inventory == null || !subscribed)
        {
            if (TryResolve()) Subscribe();
            return;
        }

        if (!pointerInside)
        {
            if (hoverShown) HideHover();
            HideDropPreview();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !TryGetGridPixel(mouse.position.ReadValue(), out Vector2 gridPixel)) return;

        Grid_Placement held = Item_Drag_Controller.IsHoldingItem ? Item_Drag_Controller.Instance.Held : default;

        if (!held.IsEmpty)
        {
            HideHover();
            Item_Tooltip_UI.HideIfExists();
            ShowDropPreview(held, GetDropOrigin(held, gridPixel));
            return;
        }

        HideDropPreview();
        UpdateHover(CellAt(gridPixel));
    }

    // ------------------------------------------------------------------
    // HOVER / TOOLTIP
    // ------------------------------------------------------------------

    private void UpdateHover(Vector2Int cell)
    {
        if (inventory.TryGetPlacementAt(cell, out Grid_Placement placement))
        {
            if (hoverShown && hoverOrigin == placement.Origin) return;

            hoverShown = true;
            hoverOrigin = placement.Origin;

            PlaceRect(hoverImage.rectTransform, placement.Origin, placement.Size);
            hoverImage.gameObject.SetActive(true);
            Item_Tooltip_UI.Instance.Show(placement);
        }
        else if (hoverShown)
        {
            HideHover();
        }
    }

    private void HideHover()
    {
        hoverShown = false;
        if (hoverImage != null) hoverImage.gameObject.SetActive(false);
        Item_Tooltip_UI.HideIfExists();
    }

    private void ShowDropPreview(Grid_Placement held, Vector2Int origin)
    {
        PlaceRect(dropImage.rectTransform, origin, held.Size);
        dropImage.color = inventory.CanDrop(held, origin) ? dropOkColor : dropBadColor;
        dropImage.gameObject.SetActive(true);
    }

    private void HideDropPreview()
    {
        if (dropImage != null && dropImage.gameObject.activeSelf) dropImage.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // CLICK
    // ------------------------------------------------------------------

    private void HandleClick(PointerEventData eventData)
    {
        if (inventory == null || !TryGetGridPixel(eventData.position, out Vector2 gridPixel)) return;

        Item_Drag_Controller drag = Item_Drag_Controller.Instance;

        if (!drag.Held.IsEmpty)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            DropHeld(drag, gridPixel);
            return;
        }

        Vector2Int cell = CellAt(gridPixel);
        if (!inventory.TryGetPlacementAt(cell, out Grid_Placement placement)) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (inventory.TryRemoveAt(cell, out Grid_Placement removed))
            {
                Item_Tooltip_UI.HideIfExists();
                drag.SetHeld(removed);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Chuột phải: mặc trang bị.
            if (placement.Instance != null && equipment != null && equipment.CanEquip(placement.Instance))
            {
                Item_Tooltip_UI.HideIfExists();
                equipment.TryEquip(placement.Instance);
            }
        }
    }

    private void DropHeld(Item_Drag_Controller drag, Vector2 gridPixel)
    {
        Grid_Placement held = drag.Held;
        Vector2Int origin = GetDropOrigin(held, gridPixel);

        if (inventory.TryDrop(held, origin, out Grid_Placement leftover))
        {
            drag.SetHeld(leftover); // rỗng = tay trống; phần dư khi gộp quá Max Stack ở lại con trỏ
        }
    }

    // ------------------------------------------------------------------
    // TOẠ ĐỘ
    // ------------------------------------------------------------------

    // Toạ độ pixel (đơn vị Canvas) trong lưới: (0,0) = góc trên-trái, x sang phải, y xuống dưới.
    private bool TryGetGridPixel(Vector2 screenPosition, out Vector2 gridPixel)
    {
        gridPixel = default;
        if (gridRect == null) return false;

        Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, cam, out Vector2 local)) return false;

        gridPixel = new Vector2(local.x, -local.y); // pivot trên-trái nên local.y <= 0
        return true;
    }

    private Vector2Int CellAt(Vector2 gridPixel)
    {
        return new Vector2Int(Mathf.FloorToInt(gridPixel.x / CellStep), Mathf.FloorToInt(gridPixel.y / CellStep));
    }

    // Vật phẩm được căn giữa theo con trỏ; kẹp vào trong lưới để thả sát mép vẫn dễ.
    private Vector2Int GetDropOrigin(Grid_Placement held, Vector2 gridPixel)
    {
        Vector2 size = (Vector2)held.Size * CellStep;
        Vector2 topLeft = gridPixel - size * 0.5f;

        var origin = new Vector2Int(Mathf.RoundToInt(topLeft.x / CellStep), Mathf.RoundToInt(topLeft.y / CellStep));
        origin.x = Mathf.Clamp(origin.x, 0, Mathf.Max(0, inventory.Width - held.Size.x));
        origin.y = Mathf.Clamp(origin.y, 0, Mathf.Max(0, inventory.Height - held.Size.y));
        return origin;
    }

    private void PlaceRect(RectTransform rect, Vector2Int origin, Vector2Int size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(origin.x * CellStep + cellGap * 0.5f, -(origin.y * CellStep + cellGap * 0.5f));
        rect.sizeDelta = new Vector2(size.x * CellStep - cellGap, size.y * CellStep - cellGap);
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        if (gridContainer == null) gridContainer = (RectTransform)transform;

        var gridObject = new GameObject("Inventory_Grid", typeof(RectTransform), typeof(LayoutElement));
        gridObject.transform.SetParent(gridContainer, false);
        gridObject.GetComponent<LayoutElement>().ignoreLayout = true; // panel có Layout Group cũng không xếp lưới
        gridRect = (RectTransform)gridObject.transform;
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(gridOffset.x, -gridOffset.y);

        cellsRoot = CreateChild("Cells", gridRect);
        itemsRoot = CreateChild("Items", gridRect);

        hoverImage = CreateOverlay("Hover_Highlight", hoverColor);
        dropImage = CreateOverlay("Drop_Preview", dropOkColor);

        // Vùng bắt chuột phủ cả lưới, nằm trên cùng. Gần như trong suốt nhưng vẫn nhận raycast.
        RectTransform hit = CreateChild("Hit_Area", gridRect);
        var hitImage = hit.gameObject.AddComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0.004f);
        hitImage.raycastTarget = true;

        relay = hit.gameObject.AddComponent<UI_Pointer_Relay>();
        relay.OnClick += HandleClick;
        relay.OnEnter += HandlePointerEnter;
        relay.OnExit += HandlePointerExit;
    }

    private void HandlePointerEnter()
    {
        pointerInside = true;
    }

    private void HandlePointerExit()
    {
        pointerInside = false;
        HideHover();
        HideDropPreview();
    }

    private Image CreateOverlay(string objectName, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(gridRect, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        go.SetActive(false);
        return image;
    }

    private static RectTransform CreateChild(string objectName, RectTransform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Vẽ lại các ô nền khi kho đổi kích thước (mở rộng) và cập nhật cỡ lưới.
    private void RebuildCellsIfResized()
    {
        if (inventory == null || gridRect == null) return;
        if (inventory.Width == builtWidth && inventory.Height == builtHeight) return;

        builtWidth = inventory.Width;
        builtHeight = inventory.Height;

        gridRect.sizeDelta = new Vector2(builtWidth * CellStep, builtHeight * CellStep);

        foreach (Transform child in cellsRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int y = 0; y < builtHeight; y++)
        {
            for (int x = 0; x < builtWidth; x++)
            {
                var go = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(cellsRoot, false);

                var image = go.GetComponent<Image>();
                image.color = cellColor;
                image.raycastTarget = false;

                PlaceRect(image.rectTransform, new Vector2Int(x, y), Vector2Int.one);
            }
        }
    }

    // Vẽ lại toàn bộ vật phẩm (kho nhỏ nên đủ nhanh và đơn giản).
    private void RebuildItems()
    {
        if (inventory == null || itemsRoot == null) return;

        foreach (Transform child in itemsRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < inventory.PlacementCount; i++)
        {
            CreateItemView(inventory.GetPlacement(i));
        }
    }

    private void CreateItemView(Grid_Placement placement)
    {
        if (placement.IsEmpty) return;

        var go = new GameObject(placement.Item.name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(itemsRoot, false);

        var back = go.GetComponent<Image>();
        back.color = itemBackColor;
        back.raycastTarget = false;
        PlaceRect(back.rectTransform, placement.Origin, placement.Size);

        Sprite sprite = placement.Item.Icon;
        if (sprite != null)
        {
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(go.transform, false);

            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = false; // sprite vẽ đúng theo khối GridSize x ô cơ sở
            icon.raycastTarget = false;
            Stretch((RectTransform)iconObject.transform, 0f);
        }
        else
        {
            TextMeshProUGUI label = CreateText("Name", go.transform, 13f, TextAlignmentOptions.Center);
            label.text = placement.Item.DisplayName;
            Stretch((RectTransform)label.transform, 2f);
        }

        if (placement.Count > 1)
        {
            TextMeshProUGUI count = CreateText("Count", go.transform, 16f, TextAlignmentOptions.BottomRight);
            count.text = placement.Count.ToString();
            count.fontStyle = FontStyles.Bold;
            Stretch((RectTransform)count.transform, 3f);
        }
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

    private void OnValidate()
    {
        cellSize = Mathf.Max(8f, cellSize);
    }
}
