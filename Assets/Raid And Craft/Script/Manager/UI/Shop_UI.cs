using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cửa hàng NPC: danh sách hàng (icon, tên, số lượng, giá) và nút Mua. Mua bằng Coin (Player_Wallet), hàng vào Inventory.
/// Npc_Shop gọi Shop_UI.Instance.Open(...); tự tạo giao diện bằng code, không cần đặt vào Scene.
/// Đóng: nút X, phím Esc, hoặc Player đi xa NPC. Rê chuột vào mặt hàng: tooltip vật phẩm.
/// Mua được khi đủ Coin và Inventory còn chỗ; nếu không có thông báo ở cuối bảng.
/// </summary>
public class Shop_UI : MonoBehaviour
{
    private const float PanelWidth = 520f;
    private const float PanelHeight = 620f;
    private const float RowHeight = 56f;
    private const float CloseExtraDistance = 1.5f;

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

    private sealed class Row
    {
        public Shop_Entry Entry;
        public Item_Instance PreviewInstance;
        public TextMeshProUGUI Text;
        public Button BuyButton;
    }

    private Canvas canvas;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI messageText;
    private RectTransform content;
    private readonly List<Row> rows = new List<Row>();

    private Npc_Shop shop;
    private Inventory inventory;
    private Player_Wallet wallet;
    private Transform player;

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
        BuildRows();

        if (wallet != null) wallet.OnCoinsChanged += HandleCoinsChanged;
        if (inventory != null) inventory.OnContainerChanged += Refresh;

        canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        Unsubscribe();
        shop = null;
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

        if (escape || gone) Close();
    }

    // ------------------------------------------------------------------
    // MUA
    // ------------------------------------------------------------------

    private void Buy(Row row)
    {
        Shop_Entry entry = row.Entry;
        if (entry.item == null || inventory == null || wallet == null) return;

        int count = Mathf.Max(1, entry.count);

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

    // Cập nhật số Coin, màu giá và nút Mua theo khả năng chi trả.
    private void Refresh()
    {
        if (!IsOpen) return;

        coinText.text = "Coin: " + (wallet != null ? wallet.Coins.ToString("N0") : "0");

        foreach (Row row in rows)
        {
            Shop_Entry entry = row.Entry;
            int count = Mathf.Max(1, entry.count);
            bool affordable = wallet != null && wallet.CanAfford(entry.price);

            string name = entry.item != null ? entry.item.DisplayName : "(trống)";
            string amount = count > 1 ? " x" + count : string.Empty;
            string priceColor = affordable ? "#FFD27F" : "#FF6B6B";
            row.Text.text = name + amount + "\n<size=85%><color=" + priceColor + ">" + entry.price.ToString("N0") + " Coin</color></size>";
            row.BuyButton.interactable = affordable && entry.item != null;
        }
    }

    private void BuildRows()
    {
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        rows.Clear();

        foreach (Shop_Entry entry in shop.Entries)
        {
            if (entry.item == null) continue;
            CreateRow(entry);
        }
    }

    private void CreateRow(Shop_Entry entry)
    {
        var row = new Row { Entry = entry };
        if (entry.item.UsesInstance) row.PreviewInstance = Item_Instance.Create(entry.item);

        var rowObject = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(UI_Pointer_Relay));
        rowObject.transform.SetParent(content, false);

        var background = rowObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.07f);
        background.raycastTarget = true;

        var layout = rowObject.GetComponent<LayoutElement>();
        layout.minHeight = RowHeight;
        layout.preferredHeight = RowHeight;

        // Tooltip khi rê chuột vào mặt hàng.
        var relay = rowObject.GetComponent<UI_Pointer_Relay>();
        relay.OnEnter += () => ShowTooltip(row);
        relay.OnExit += () => Item_Tooltip_UI.HideIfExists();

        // Icon.
        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rowObject.transform, false);
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(6f, 0f);
        iconRect.sizeDelta = new Vector2(44f, 44f);

        var icon = iconObject.GetComponent<Image>();
        icon.sprite = entry.item.Icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = entry.item.Icon != null;

        // Tên + giá.
        row.Text = CreateText("Info", rowObject.transform, 18f, TextAlignmentOptions.MidlineLeft);
        var textRect = (RectTransform)row.Text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(58f, 2f);
        textRect.offsetMax = new Vector2(-116f, -2f);

        // Nút Mua.
        var buttonObject = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(rowObject.transform, false);
        var buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-8f, 0f);
        buttonRect.sizeDelta = new Vector2(96f, 36f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.55f, 0.3f, 1f);

        row.BuyButton = buttonObject.GetComponent<Button>();
        row.BuyButton.targetGraphic = buttonImage;
        row.BuyButton.onClick.AddListener(() => Buy(row));

        TextMeshProUGUI buttonText = CreateText("Label", buttonObject.transform, 18f, TextAlignmentOptions.Center);
        buttonText.text = "Mua";
        Stretch((RectTransform)buttonText.transform);

        rows.Add(row);
    }

    private void ShowTooltip(Row row)
    {
        if (Item_Drag_Controller.IsHoldingItem || row.Entry.item == null) return;

        if (row.PreviewInstance != null) Item_Tooltip_UI.Instance.Show(row.PreviewInstance);
        else Item_Tooltip_UI.Instance.Show(row.Entry.item, Mathf.Max(1, row.Entry.count));
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void Build()
    {
        var canvasObject = new GameObject("Shop_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80; // Dưới tooltip (110, 115) và vật phẩm đang cầm (120).

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Bảng (chặn click ra thế giới nhờ Image bật Raycast Target).
        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        var panel = (RectTransform)panelObject.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(-340f, 0f);
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelObject.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.96f);

        // Tiêu đề.
        titleText = CreateText("Title", panel, 26f, TextAlignmentOptions.MidlineLeft);
        titleText.fontStyle = FontStyles.Bold;
        SetTopRect((RectTransform)titleText.transform, 12f, 12f, 44f, 8f);

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
        TextMeshProUGUI closeText = CreateText("X", closeObject.transform, 20f, TextAlignmentOptions.Center);
        closeText.text = "X";
        Stretch((RectTransform)closeText.transform);

        // Dòng Coin.
        coinText = CreateText("Coin", panel, 20f, TextAlignmentOptions.MidlineLeft);
        coinText.color = new Color(1f, 0.82f, 0.5f);
        SetTopRect((RectTransform)coinText.transform, 12f, 12f, 30f, 52f);

        // Danh sách cuộn.
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel, false);
        var scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10f, 44f);
        scrollRect.offsetMax = new Vector2(-10f, -90f);

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewport = (RectTransform)viewportObject.transform;
        Stretch(viewport);
        var viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.004f); // nhận con lăn chuột trên cả vùng trống
        viewportImage.raycastTarget = true;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport, false);
        content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Thông báo cuối bảng.
        messageText = CreateText("Message", panel, 18f, TextAlignmentOptions.MidlineLeft);
        var messageRect = (RectTransform)messageText.transform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = new Vector2(12f, 8f);
        messageRect.offsetMax = new Vector2(-12f, 38f);
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

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
