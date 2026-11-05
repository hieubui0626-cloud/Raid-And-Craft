using TMPro;
using UnityEngine;

/// <summary>
/// Vật phẩm nằm trên đất, có thẻ tên nổi phía trên (luôn quay về camera). Click vào để đi tới và nhặt (World_Item_Picker trên Player).
/// Hai cách có vật phẩm trên đất:
///   1. Đặt sẵn trong Scene để thử: tạo một GameObject (kéo model của bạn làm con của nó, hoặc để trống để dùng model của asset / khối vuông tạm),
///      Add Component World_Item, chọn Item và Count. Trang bị tự có một món mặc định (Item_Instance.Create).
///   2. Sinh lúc chơi: World_Item.Spawn(item, count, instance, vị trí) (thả từ Inventory, sau này quái rơi đồ).
/// Model: nếu object đã có Renderer ở con thì dùng luôn; không thì dùng Item_Data.WorldModel; không có nữa thì khối vuông tạm.
/// Vùng bấm: nếu chưa có Collider nào thì tự thêm một BoxCollider (Is Trigger) bao cả model và thẻ tên.
/// </summary>
[DisallowMultipleComponent]
public class World_Item : MonoBehaviour, IWorld_Interactable
{
    [Header("Vật phẩm")]
    [SerializeField] private Item_Data item;
    [SerializeField, Min(1)] private int count = 1;

    [Tooltip("Khoảng cách (mét) tới vật phẩm thì nhặt được.")]
    [SerializeField, Min(0.5f)] private float pickupRange = 1.6f;

    [Header("Thẻ tên")]
    [SerializeField] private bool showLabel = true;
    [Tooltip("Độ cao thẻ tên so với chân vật phẩm (mét).")]
    [SerializeField, Min(0f)] private float labelHeight = 1.1f;
    [Tooltip("Cỡ chữ thẻ tên (camera nhìn từ xa nên cần khá lớn).")]
    [SerializeField, Min(1f)] private float labelSize = 5f;

    private TextMeshPro label;
    private Camera cam;
    private bool setupDone;

    public Item_Data Item => item;
    public int Count => count;

    /// <summary>Món trang bị cụ thể (null với vật phẩm xếp chồng).</summary>
    public Item_Instance Instance { get; private set; }

    public Vector3 Position => transform.position;
    public float InteractRange => pickupRange;

    private float pickupReadyTime;

    /// <summary>False trong khoảng ngắn sau khi vừa thả xuống: click không nhắm trúng vật phẩm (tránh tự chạy lại nhặt).</summary>
    public bool IsPickupReady => Time.time >= pickupReadyTime;

    /// <summary>Player đã tới đủ gần: nhặt vào Inventory.</summary>
    public void Interact(World_Item_Picker interactor)
    {
        if (interactor != null) TryPickup(interactor.Inventory);
    }

    // ------------------------------------------------------------------
    // TẠO LÚC CHƠI
    // ------------------------------------------------------------------

    /// <summary>Đặt một vật phẩm xuống đất. instance = món trang bị cụ thể (null thì trang bị sẽ có món mặc định).</summary>
    /// <param name="pickupDelay">Giây vật phẩm chưa nhặt được bằng click (thả từ Inventory: tránh click thừa nhặt lại ngay).</param>
    public static World_Item Spawn(Item_Data item, int count, Item_Instance instance, Vector3 position, float pickupDelay = 0f)
    {
        if (item == null || count <= 0) return null;

        var go = new GameObject("World_Item_" + item.DisplayName);
        go.transform.position = position;

        // Awake/Start của component chạy sau khi gán dữ liệu bên dưới vì EnsureSetup được gọi thủ công.
        var worldItem = go.AddComponent<World_Item>();
        worldItem.item = item;
        worldItem.count = count;
        worldItem.Instance = instance;
        worldItem.pickupReadyTime = Time.time + Mathf.Max(0f, pickupDelay);
        worldItem.EnsureSetup();
        return worldItem;
    }

    // ------------------------------------------------------------------
    // NHẶT
    // ------------------------------------------------------------------

    /// <summary>
    /// Nhặt vào kho. True nếu đã nhặt hết (vật phẩm biến mất). Xếp chồng mà kho chỉ nhận được một phần thì phần dư ở lại trên đất.
    /// </summary>
    public bool TryPickup(Inventory inventory)
    {
        EnsureSetup();
        if (inventory == null || item == null) return false;

        if (Instance != null)
        {
            if (!inventory.AddInstance(Instance))
            {
                Debug.Log($"[{nameof(World_Item)}] Inventory hết chỗ cho '{item.DisplayName}'.", this);
                return false;
            }

            Destroy(gameObject);
            return true;
        }

        int leftover = inventory.Add(item, count);
        if (leftover <= 0)
        {
            Destroy(gameObject);
            return true;
        }

        if (leftover < count) SetCount(leftover);
        Debug.Log($"[{nameof(World_Item)}] Inventory hết chỗ cho '{item.DisplayName}' (còn {leftover} trên đất).", this);
        return false;
    }

    private void SetCount(int newCount)
    {
        count = Mathf.Max(1, newCount);
        RefreshLabel();
    }

    // ------------------------------------------------------------------
    // DỰNG
    // ------------------------------------------------------------------

    private void Start()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        if (setupDone) return;
        setupDone = true;

        if (item == null)
        {
            Debug.LogWarning($"[{nameof(World_Item)}] Chưa chọn Item.", this);
            enabled = false;
            return;
        }

        if (item.UsesInstance)
        {
            if (Instance == null) Instance = Item_Instance.Create(item);
            count = 1;
        }

        // Collider TRƯỚC model (xem ghi chú ở Npc_Shop): tránh mất vùng bấm khi dùng khối tạm.
        BuildCollider();
        BuildVisual();
        BuildLabel();
    }

    private void BuildVisual()
    {
        if (GetComponentInChildren<Renderer>() != null) return; // người dùng tự đặt model làm con

        if (item.WorldModel != null)
        {
            GameObject model = Instantiate(item.WorldModel, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            return;
        }

        // Khối vuông tạm.
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Collider primitiveCollider = cube.GetComponent<Collider>();
        if (primitiveCollider != null) Destroy(primitiveCollider);

        cube.name = "Placeholder_Model";
        cube.transform.SetParent(transform, false);
        cube.transform.localScale = Vector3.one * 0.5f;
        cube.transform.localPosition = new Vector3(0f, 0.25f, 0f);
    }

    private void BuildCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        // Cao tới thẻ tên để bấm vào chữ cũng nhặt được. Là trigger nên không cản Player/NavMesh.
        float height = (showLabel ? labelHeight : 0.5f) + 0.4f;
        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1f, height, 1f);
        box.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private void BuildLabel()
    {
        if (!showLabel) return;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, labelHeight, 0f);

        ((RectTransform)labelObject.transform).sizeDelta = new Vector2(8f, 1f);

        label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = labelSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.outlineWidth = 0.25f;
        label.outlineColor = new Color32(0, 0, 0, 255);

        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (label == null || item == null) return;

        label.text = count > 1 ? item.DisplayName + " x" + count : item.DisplayName;
        int tier = Instance != null ? Instance.Tier : (item is Material_Item_Data material ? material.RarityTier : 1);
        label.color = TierColor(tier);
    }

    // Thẻ tên luôn quay mặt về camera.
    private void LateUpdate()
    {
        if (label == null) return;

        if (cam == null) cam = Camera.main;
        if (cam != null) label.transform.rotation = cam.transform.rotation;
    }

    private static Color TierColor(int tier)
    {
        switch (tier)
        {
            case 1: return Color.white;
            case 2: return new Color(0.5f, 0.7f, 1f);
            case 3: return new Color(0.77f, 0.55f, 1f);
            case 4: return new Color(1f, 0.7f, 0.28f);
            default: return new Color(1f, 0.42f, 0.42f);
        }
    }
}
