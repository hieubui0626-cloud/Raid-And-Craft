using UnityEngine;

/// <summary>Vật phẩm có xếp chồng được trong một ô Inventory hay không.</summary>
// QUY TẮC: giá trị số được serialize trong asset. Không đổi số mục đã có, chỉ thêm mục mới ở CUỐI enum.
public enum Item_Stack_Type
{
    /// <summary>Xếp chồng được tới Max Stack (nguyên liệu, rune, tiền tệ, thuốc...).</summary>
    Stackable = 0,
    /// <summary>Mỗi vật phẩm chiếm riêng một chỗ, không gộp (trang bị, đồ độc nhất).</summary>
    NonStackable = 1,
}

/// <summary>
/// Dữ liệu gốc của MỌI vật phẩm xếp được trong Inventory (ScriptableObject).
/// Chỉ chứa thông tin dùng chung; từng loại vật phẩm kế thừa lớp này để thêm dữ liệu riêng:
///   Item_Data
///   ├─ Circuit_Item_Data   (lắp vào ô Magic Circuit: Rune_Data, Spell_Stone_Data)
///   └─ Equippable_Item_Data (mặc lên người: Weapon_Data, sau này giáp, nhẫn...)
/// Quy tắc: itemId là khoá duy nhất dùng để save/load, KHÔNG đổi sau khi đã có save.
/// Mọi loại vật phẩm tạo asset chung một menu: Create -> Raid And Craft -> Items -> (Weapon / Rune / Spell Stone...).
/// Thêm loại mới: kế thừa lớp này (hoặc Circuit_Item_Data / Equippable_Item_Data) và đặt menuName = "Raid And Craft/Items/&lt;Tên&gt;".
///
/// Phân loại xếp chồng: Stack Type = Stackable (tới Max Stack) hoặc NonStackable (luôn 1 vật phẩm mỗi chỗ).
/// Chuẩn bị cho Inventory dạng lưới (grid): Grid Size là số ô rộng x cao vật phẩm chiếm (mặc định 1x1).
/// Chuẩn bị cho trang bị: IsEquippable / EquipSlot (chỉ Equippable_Item_Data mới có ô trang bị).
/// Inventory hiện tại (danh sách ô) vẫn chạy bình thường, chỉ đọc MaxStack; Grid Size sẽ dùng khi làm grid Inventory.
/// </summary>
public abstract class Item_Data : ScriptableObject
{
    [Header("Thông tin chung")]
    [Tooltip("Khoá duy nhất để save/load. Để trống thì tự lấy tên asset.")]
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Xếp chồng")]
    [Tooltip("Stackable: xếp chồng tới Max Stack. NonStackable: mỗi vật phẩm một chỗ (Max Stack bị bỏ qua). " +
             "Trang bị (Equippable_Item_Data) luôn NonStackable.")]
    [SerializeField] private Item_Stack_Type stackType = Item_Stack_Type.Stackable;

    [Tooltip("Số lượng tối đa trong một chỗ khi Stackable (1 = thực tế không xếp chồng).")]
    [SerializeField, Min(1)] private int maxStack = 1;

    [Header("Lưới Inventory (grid)")]
    [Tooltip("Số ô vật phẩm chiếm trong Inventory dạng lưới: X = rộng, Y = cao. 1x1 = một ô.")]
    [SerializeField] private Vector2Int gridSize = Vector2Int.one;

    [Tooltip("Cho phép xoay vật phẩm 90 độ khi đặt vào lưới (đổi chỗ rộng và cao). Vô nghĩa với vật 1x1 hoặc vuông.")]
    [SerializeField] private bool canRotate = true;

    public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;

    // ------------------------------------------------------------------
    // XẾP CHỒNG
    // ------------------------------------------------------------------

    /// <summary>Loại đã cấu hình (Equippable_Item_Data ép NonStackable, xem CanStack).</summary>
    public Item_Stack_Type StackType => stackType;

    /// <summary>Lớp con trả true để ép vật phẩm không xếp chồng được, bất kể cấu hình trong Inspector.</summary>
    protected virtual bool ForceNonStackable => false;

    /// <summary>Có xếp chồng được không (tính cả ép buộc của lớp con).</summary>
    public bool CanStack => !ForceNonStackable && stackType == Item_Stack_Type.Stackable;

    /// <summary>Số lượng tối đa trong một chỗ: Max Stack nếu CanStack, ngược lại luôn 1.</summary>
    public int MaxStack => CanStack ? Mathf.Max(1, maxStack) : 1;

    // ------------------------------------------------------------------
    // LƯỚI INVENTORY
    // ------------------------------------------------------------------

    /// <summary>Kích thước chiếm trong lưới (rộng x cao), tối thiểu 1x1.</summary>
    public Vector2Int GridSize => new Vector2Int(Mathf.Max(1, gridSize.x), Mathf.Max(1, gridSize.y));
    public int GridWidth => GridSize.x;
    public int GridHeight => GridSize.y;

    /// <summary>Số ô lưới chiếm (rộng x cao).</summary>
    public int GridCellCount => GridWidth * GridHeight;

    /// <summary>Có xoay được không (chỉ có ý nghĩa khi rộng khác cao).</summary>
    public bool CanRotate => canRotate && GridWidth != GridHeight;

    /// <summary>Kích thước chiếm trong lưới khi xoay hoặc không xoay.</summary>
    public Vector2Int GetGridSize(bool rotated)
    {
        Vector2Int size = GridSize;
        return rotated && CanRotate ? new Vector2Int(size.y, size.x) : size;
    }

    // ------------------------------------------------------------------
    // TRANG BỊ
    // ------------------------------------------------------------------

    /// <summary>Mặc lên người được không. Equippable_Item_Data ghi đè thành true.</summary>
    public virtual bool IsEquippable => false;

    /// <summary>Ô trang bị vật phẩm này mặc vào (None nếu không phải trang bị).</summary>
    public virtual Equipment_Slot EquipSlot => Equipment_Slot.None;

#if UNITY_EDITOR
    // Tự điền itemId bằng tên asset nếu để trống, để khỏi quên; giữ kích thước lưới tối thiểu 1x1.
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId)) itemId = name;

        gridSize.x = Mathf.Max(1, gridSize.x);
        gridSize.y = Mathf.Max(1, gridSize.y);
    }
#endif
}
