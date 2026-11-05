using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cửa hàng NPC dạng LƯỚI giống Inventory: mỗi mặt hàng là một vật phẩm vẽ theo Grid Size x ô cơ sở (Item_UI_Metrics.CellSize),
/// xếp theo thứ tự trong Npc_Shop.Entries vào lưới Columns x Rows của NPC. Giá nằm ở góc dưới trái, số lượng mỗi lần mua ở góc dưới phải.
/// - Chuột trái vào mặt hàng: mua (trừ Coin, hàng vào Inventory). Rê chuột: tooltip kèm giá.
/// - Đóng: nút X, phím Esc, hoặc Player đi xa NPC.
/// Npc_Shop gọi Shop_UI.Instance.Open(...); tự dựng giao diện bằng code, không cần đặt vào Scene.
/// </summary>
public class Shop_UI : MonoBehaviour
{
    private const float Padding = 12f;
    private const float HeaderHeight = 96f;
    private const float FooterHeight = 44f;
    private const float MinPanelWidth = 340f;
    private const float CellGap = 1f;
    private const float CloseExtraDistance = 1.5f;

    private static readonly Color CellColor = new Color(1f, 1f, 1f, 0.07f);
    private static readonly Color ItemBackColor = new Color(0.12f, 0.15f, 0.22f, 0.9f);
    private static readonly Color HoverOkColor = new Color(1f, 1f, 1f, 0.2f);
    private static readonly Color HoverBadColor = new Color(0.95f, 0.2f, 0.2f, 0.3f);

    private static Shop_UI instance;

    public static Shop_UI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<Shop_UI>();
            if (instance == null) instance = new GameObject("Shop_UI").AddComponent<Shop_UI>();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    // Một mặt hàng đã xếp vào lưới.
    private sealed class Slot
    {
        public Shop_Entry Entry;
        public Vector2Int Origin;
        public Vector2Int Size;
        public Item_Instance PreviewInstance;
        public TextMeshProUGUI PriceText;
    }

    private Canvas canvas;
    private RectTransform panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI messageText;
    private RectTransform gridRect;
    private RectTransform cellsRoot;
    private RectTransform itemsRoot;
    private Image hoverImage;

    private readonly List<Slot> slots = new List<Slot>();
    private Slot hoverSlot;
    private bool pointerInside;

    private Npc_Shop shop;
    private Inventory inventory;
    private Player_Wallet wallet;
    private Transform player;

    // Cạnh ô dùng để dựng lưới, CHỐT lúc dựng và dùng cho mọi tính toán (hover, click, đặt khung sáng).
    // Không đọc lại Item_UI_Metrics mỗi lần: Inventory_UI đổi giá trị đó khi mở, làm hover và click lệch khỏi hình đã vẽ.
    private float cellStep = Item_UI_Metrics.DefaultCellSize;

    private float CellStep => cellStep;

    private float ResolveCellStep()
    {
        return shop != null && shop.CellSize > 0f ? shop.CellSize : Item_UI_Metrics.CellSize;
    }

    public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        if (instance == null) instance = this;
        Build();
        canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ------------------------------------------------------------------
    // MỞ / ĐÓNG
    // ------------------------------------------------------------------

    public void Open(Npc_Shop npcShop, World_Item_Picker interactor)
    {
        if (npcShop == null || interactor == null) return;

        Unsubscribe();

        shop = npcShop;
        inventory = interactor.Inventory;
        wallet = interactor.Wallet;
        player = interactor.transform;

        titleText.text = shop.NpcName;
        messageText.text = string.Empty;
        BuildGrid();

        if (wallet != null) wallet.OnCoinsChanged += HandleCoinsChanged;
        if (inventory != null) inventory.OnContainerChanged += Refresh;

        canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        Unsubscribe();
        shop = null;
        pointerInside = false;
        hoverSlot = null;
        Item_Tooltip_UI.HideIfExists();
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (wallet != null) wallet.OnCoinsChanged -= HandleCoinsChanged;
        if (inventory != null) inventory.OnContainerChanged -= Refresh;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!IsOpen) return;

        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gone = shop == null || player == null;

        if (!gone)
        {
            Vector3 delta = shop.Position - player.position;
            delta.y = 0f;
            float limit = shop.InteractRange + CloseExtraDistance;
            gone = delta.sqrMagnitude > limit * limit;
        }

        if (escape || gone)
        {
            Close();
            return;
        }

        // Cell Size để 0 (theo Inventory_UI): Inventory_UI vừa đổi cỡ ô thì dựng lại lưới cho khớp.
        if (shop.CellSize <= 0f && !Mathf.Approximately(ResolveCellStep(), cellStep))
        {
            BuildGrid();
            Refresh();
        }

        UpdateHover();
    }

    // ------------------------------------------------------------------
    // HOVER / TOOLTIP
    // ------------------------------------------------------------------

    private void UpdateHover()
    {
        Slot slot = null;

        if (pointerInside && !Item_Drag_Controller.IsHoldingItem && Mouse.current != null
            && TryGetGridPixel(Mouse.current.position.ReadValue(), out Vector2 gridPixel))
        {
            slot = SlotAt(CellAt(gridPixel));
        }

        if (slot == hoverSlot)
        {
            // Giá đổi (mua xong hết Coin) thì tô lại màu ô, không cần dựng lại tooltip.
            if (slot != null) hoverImage.color = CanAfford(slot) ? HoverOkColor : HoverBadColor;
            return;
        }

        hoverSlot = slot;

        if (slot == null)
        {
            hoverImage.gameObject.SetActive(false);
            Item_Tooltip_UI.HideIfExists();
            return;
        }

        PlaceRect(hoverImage.rectTransform, slot.Origin, slot.Size);
        hoverImage.color = CanAfford(slot) ? HoverOkColor : HoverBadColor;
        hoverImage.gameObject.SetActive(true);
        ShowTooltip(slot);
    }

    private void ShowTooltip(Slot slot)
    {
        string priceColor = CanAfford(slot) ? "#FFD27F" : "#FF6B6B";
        string footer = "<color=" + priceColor + ">Giá: " + Player_Wallet.FormatCoins(slot.Entry.price) + " Coin</color>"
                      + "\n<size=85%><color=#8FA8C8>Chuột trái để mua</color></size>";

        if (slot.PreviewInstance != null) Item_Tooltip_UI.Instance.Show(slot.PreviewInstance, footer);
        else Item_Tooltip_UI.Instance.Show(slot.Entry.item, EffectiveCount(slot.Entry), footer);
    }

    // Số lượng thật mỗi lần mua: trang bị và vật phẩm không xếp chồng luôn là 1, bất kể Count trong Npc_Shop
    // (Count 50 trên một cây Bow nghĩa là mua 50 cây Bow, kho không bao giờ đủ chỗ).
    private static int EffectiveCount(Shop_Entry entry)
    {
        return entry.item != null && entry.item.CanStack ? Mathf.Max(1, entry.count) : 1;
    }

    private bool CanAfford(Slot slot)
    {
        return wallet != null && wallet.CanAfford(slot.Entry.price);
    }

    // ------------------------------------------------------------------
    // MUA
    // ------------------------------------------------------------------

    private void HandleClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Item_Drag_Controller.IsHoldingItem) return;
        if (!TryGetGridPixel(eventData.position, out Vector2 gridPixel)) return;

        Slot slot = SlotAt(CellAt(gridPixel));
        if (slot != null) Buy(slot);
    }

    private void Buy(Slot slot)
    {
        Shop_Entry entry = slot.Entry;
        if (entry.item == null || inventory == null || wallet == null) return;

        int count = EffectiveCount(entry);

        if (!wallet.CanAfford(entry.price))
        {
            ShowMessage("Không đủ Coin.", false);
            return;
        }

        if (!inventory.CanAdd(entry.item, count))
        {
            ShowMessage("Inventory hết chỗ.", false);
            return;
        }

        int leftover = inventory.Add(entry.item, count);
        if (leftover > 0)
        {
            // Không xảy ra vì đã kiểm CanAdd; hoàn lại phần đã thêm để không mất cân bằng.
            if (leftover < count) inventory.TryRemove(entry.item, count - leftover);
            ShowMessage("Inventory hết chỗ.", false);
            return;
        }

        wallet.TrySpend(entry.price);
        ShowMessage("Đã mua " + entry.item.DisplayName + (count > 1 ? " x" + count : string.Empty) + ".", true);
    }

    private void ShowMessage(string message, bool good)
    {
        messageText.text = "<color=" + (good ? "#7FE08A" : "#FF6B6B") + ">" + message + "</color>";
    }

    // ------------------------------------------------------------------
    // HIỂN THỊ
    // ------------------------------------------------------------------

    private void HandleCoinsChanged(int coins)
    {
        Refresh();
    }

    // Cập nhật số Coin và màu giá theo khả năng chi trả.
    private void Refresh()
    {
        if (!IsOpen) return;

        coinText.text = "Coin: " + (wallet != null ? Player_Wallet.FormatCoins(wallet.Coins) : "0");

        foreach (Slot slot in slots)
        {
            string color = CanAfford(slot) ? "#FFD27F" : "#FF6B6B";
            slot.PriceText.text = "<color=" + color + ">" + Player_Wallet.FormatCoins(slot.Entry.price) + "</color>";
        }

        hoverSlot = null; // buộc UpdateHover tô lại ô và tooltip theo giá mới
    }

    // ------------------------------------------------------------------
    // XẾP LƯỚI
    // ------------------------------------------------------------------

    private void BuildGrid()
    {
        cellStep = ResolveCellStep();

        int columns = Mathf.Max(1, shop.Columns);
        int rows = Mathf.Max(1, shop.Rows);

        // Xếp từng mặt hàng vào chỗ trống đầu tiên (từ trên xuống, trái sang phải), dùng luôn luật lưới của Inventory.
        var layout = new Item_Grid(columns, rows);
        slots.Clear();

        foreach (Shop_Entry entry in shop.Entries)
        {
            if (entry.item == null) continue;

            if (!layout.TryFindSpace(entry.item, out Vector2Int origin) || !layout.TryPlaceAt(entry.item, 1, origin))
            {
                Debug.LogWarning($"[{nameof(Shop_UI)}] Lưới {columns}x{rows} của '{shop.NpcName}' hết chỗ cho '{entry.item.DisplayName}' (cần {entry.item.GridSize.x}x{entry.item.GridSize.y}). Tăng Columns/Rows của Npc_Shop.", shop);
                continue;
            }

            slots.Add(new Slot
            {
                Entry = entry,
                Origin = origin,
                Size = entry.item.GridSize,
                PreviewInstance = entry.item.UsesInstance ? Item_Instance.Create(entry.item) : null
            });
        }

        // Cỡ lưới và bảng.
        float gridWidth = columns * CellStep;
        float gridHeight = rows * CellStep;
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        panel.sizeDelta = new Vector2(Mathf.Max(MinPanelWidth, gridWidth + Padding * 2f), HeaderHeight + gridHeight + FooterHeight + Padding);

        ClearChildren(cellsRoot);
        ClearChildren(itemsRoot);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var cell = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(cellsRoot, false);

                var image = cell.GetComponent<Image>();
                image.color = CellColor;
                image.raycastTarget = false;
                PlaceRect(image.rectTransform, new Vector2Int(x, y), Vector2Int.one);
            }
        }

        foreach (Slot slot in slots) CreateItemView(slot);

        hoverSlot = null;
        hoverImage.gameObject.SetActive(false);
    }

    private void CreateItemView(Slot slot)
    {
        Item_Data item = slot.Entry.item;

        var go = new GameObject(item.name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(itemsRoot, false);

        var back = go.GetComponent<Image>();
        back.color = ItemBackColor;
        back.raycastTarget = false;
        PlaceRect(back.rectTransform, slot.Origin, slot.Size);

        if (item.Icon != null)
        {
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(go.transform, false);

            var icon = iconObject.GetComponent<Image>();
            icon.sprite = item.Icon;
            icon.preserveAspect = false; // sprite vẽ đúng theo khối GridSize x ô cơ sở
            icon.raycastTarget = false;
            UI_Builder.Stretch((RectTransform)iconObject.transform);
        }
        else
        {
            TextMeshProUGUI label = UI_Builder.CreateText("Name", go.transform, 13f, TextAlignmentOptions.Center);
            label.text = item.DisplayName;
            UI_Builder.Stretch((RectTransform)label.transform, 2f);
        }

        // Giá ở góc dưới trái.
        slot.PriceText = UI_Builder.CreateText("Price", go.transform, 14f, TextAlignmentOptions.BottomLeft, true);
        slot.PriceText.fontStyle = FontStyles.Bold;
        UI_Builder.Stretch((RectTransform)slot.PriceText.transform, 3f);

        // Số lượng mỗi lần mua ở góc dưới phải.
        int buyCount = EffectiveCount(slot.Entry);
        if (buyCount > 1)
        {
            TextMeshProUGUI count = UI_Builder.CreateText("Count", go.transform, 16f, TextAlignmentOptions.BottomRight);
            count.text = buyCount.ToString();
            count.fontStyle = FontStyles.Bold;
            UI_Builder.Stretch((RectTransform)count.transform, 3f);
        }
    }

    private Slot SlotAt(Vector2Int cell)
    {
        foreach (Slot slot in slots)
        {
            if (cell.x >= slot.Origin.x && cell.x < slot.Origin.x + slot.Size.x
                && cell.y >= slot.Origin.y && cell.y < slot.Origin.y + slot.Size.y)
            {
                return slot;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------
    // TOẠ ĐỘ
    // ------------------------------------------------------------------

    // Toạ độ trong lưới theo đơn vị Canvas: (0,0) = góc trên-trái, x sang phải, y xuống dưới. Canvas là Overlay nên camera = null.
    private bool TryGetGridPixel(Vector2 screenPosition, out Vector2 gridPixel)
    {
        gridPixel = default;
        if (gridRect == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, null, out Vector2 local)) return false;

        gridPixel = new Vector2(local.x, -local.y); // pivot trên-trái nên local.y <= 0
        return true;
    }

    private Vector2Int CellAt(Vector2 gridPixel)
    {
        return new Vector2Int(Mathf.FloorToInt(gridPixel.x / CellStep), Mathf.FloorToInt(gridPixel.y / CellStep));
    }

    private void PlaceRect(RectTransform rect, Vector2Int origin, Vector2Int size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(origin.x * CellStep + CellGap * 0.5f, -(origin.y * CellStep + CellGap * 0.5f));
        rect.sizeDelta = new Vector2(size.x * CellStep - CellGap, size.y * CellStep - CellGap);
    }

    private static void ClearChildren(RectTransform root)
    {
        foreach (Transform child in root)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void Build()
    {
        canvas = UI_Builder.CreateOverlayCanvas("Shop_Canvas", transform, 80, true, true); // dưới tooltip (110, 115) và vật phẩm đang cầm (120)

        // Bảng (chặn click ra thế giới nhờ Image bật Raycast Target). Neo giữa màn hình, mở rộng sang trái.
        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        panel = (RectTransform)panelObject.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-20f, 0f);
        panelObject.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        panelObject.AddComponent<UI_Draggable_Panel>(); // kéo cửa hàng bằng chuột (giữ vị trí giữa các lần mở)

        // Tiêu đề.
        titleText = UI_Builder.CreateText("Title", panel, 26f, TextAlignmentOptions.MidlineLeft, true);
        titleText.fontStyle = FontStyles.Bold;
        SetTopRect((RectTransform)titleText.transform, Padding, 56f, 44f, 8f);

        // Nút đóng.
        var closeObject = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(panel, false);
        var closeRect = (RectTransform)closeObject.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-10f, -10f);
        closeRect.sizeDelta = new Vector2(36f, 36f);
        var closeImage = closeObject.GetComponent<Image>();
        closeImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        var closeButton = closeObject.GetComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);
        TextMeshProUGUI closeText = UI_Builder.CreateText("X", closeObject.transform, 20f, TextAlignmentOptions.Center);
        closeText.text = "X";
        UI_Builder.Stretch((RectTransform)closeText.transform);

        // Dòng Coin.
        coinText = UI_Builder.CreateText("Coin", panel, 20f, TextAlignmentOptions.MidlineLeft, true);
        coinText.color = new Color(1f, 0.82f, 0.5f);
        SetTopRect((RectTransform)coinText.transform, Padding, Padding, 30f, 52f);

        // Lưới hàng, neo trên-trái dưới phần đầu bảng.
        var gridObject = new GameObject("Shop_Grid", typeof(RectTransform));
        gridObject.transform.SetParent(panel, false);
        gridRect = (RectTransform)gridObject.transform;
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(Padding, -HeaderHeight);

        cellsRoot = UI_Builder.CreateStretchedChild("Cells", gridRect);
        itemsRoot = UI_Builder.CreateStretchedChild("Items", gridRect);

        var hoverObject = new GameObject("Hover_Highlight", typeof(RectTransform), typeof(Image));
        hoverObject.transform.SetParent(gridRect, false);
        hoverImage = hoverObject.GetComponent<Image>();
        hoverImage.raycastTarget = false;
        hoverObject.SetActive(false);

        // Vùng bắt chuột phủ cả lưới, nằm trên cùng. Gần như trong suốt nhưng vẫn nhận raycast.
        RectTransform hit = UI_Builder.CreateStretchedChild("Hit_Area", gridRect);
        var hitImage = hit.gameObject.AddComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0.004f);
        hitImage.raycastTarget = true;

        var relay = hit.gameObject.AddComponent<UI_Pointer_Relay>();
        relay.OnClick += HandleClick;
        relay.OnEnter += () => pointerInside = true;
        relay.OnExit += () =>
        {
            pointerInside = false;
            hoverSlot = null;
            hoverImage.gameObject.SetActive(false);
            Item_Tooltip_UI.HideIfExists();
        };

        // Thông báo cuối bảng.
        messageText = UI_Builder.CreateText("Message", panel, 18f, TextAlignmentOptions.MidlineLeft, true);
        var messageRect = (RectTransform)messageText.transform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = new Vector2(Padding, 8f);
        messageRect.offsetMax = new Vector2(-Padding, 38f);
    }

    // Đặt một dải ngang sát mép trên: lề trái/phải, cao height, cách mép trên top.
    private static void SetTopRect(RectTransform rect, float left, float right, float height, float top)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
