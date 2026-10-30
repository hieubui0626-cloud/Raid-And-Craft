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
///   ├─ Circuit_Item_Data    (lắp vào ô Magic Circuit: Rune_Data, Spell_Stone_Data)
///   ├─ Equippable_Item_Data (mặc lên người: Weapon_Data, sau này giáp, nhẫn...)
///   └─ Consumable_Item_Data (dùng một lần, đặt lên thanh quick use: Restore_Potion_Data...)
/// Công dụng (Item_Usage): mỗi tính năng hỏi CanBeUsedFor(...) trước khi nhận vật phẩm, xem Item_Usage.
/// Quy tắc: itemId là khoá duy nhất dùng để save/load, KHÔNG đổi sau khi đã có save.
/// Mọi loại vật phẩm tạo asset chung một menu: Create -> Raid And Craft -> Items -> (Weapon / Rune / Spell Stone...).
/// Thêm loại mới: kế thừa lớp này (hoặc Circuit_Item_Data / Equippable_Item_Data) và đặt menuName = "Raid And Craft/Items/&lt;Tên&gt;".
///
/// Phân loại xếp chồng: Stack Type = Stackable (tới Max Stack) hoặc NonStackable (luôn 1 vật phẩm mỗi chỗ).
/// Inventory dạng lưới (grid): Grid Size là số ô rộng x cao vật phẩm chiếm (mặc định 1x1). Vật phẩm không xoay.
/// Trang bị: IsEquippable / EquipSlot (chỉ Equippable_Item_Data mới có ô trang bị).
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

    [Header("Công dụng")]
    [Tooltip("CHẶN bớt công dụng của vật phẩm này (ví dụ chặn MagicCircuit cho một Rune nhiệm vụ). " +
             "Không bật thêm được công dụng mà loại vật phẩm không hỗ trợ.")]
    [SerializeField] private Item_Usage blockedUsage = Item_Usage.None;

    public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;

    // ------------------------------------------------------------------
    // XẾP CHỒNG
    // ------------------------------------------------------------------

    /// <summary>
    /// Mỗi món là một Item_Instance riêng (chỉ số roll riêng) thay vì chỉ "loại + số lượng". Equippable_Item_Data ghi đè true.
    /// Vật phẩm instance luôn NonStackable.
    /// </summary>
    public virtual bool UsesInstance => false;

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

    // ------------------------------------------------------------------
    // CÔNG DỤNG (tính năng nào được phép nhận vật phẩm này)
    // ------------------------------------------------------------------

    /// <summary>
    /// Công dụng mà LOẠI vật phẩm này hỗ trợ. Lớp con ghi đè (Equippable = Equip, Circuit = MagicCircuit, Consumable = QuickUse).
    /// Một loại có thể hỗ trợ nhiều công dụng: trả về Item_Usage.MagicCircuit | Item_Usage.QuickUse.
    /// </summary>
    public virtual Item_Usage SupportedUsage => Item_Usage.None;

    /// <summary>Công dụng bị chặn riêng cho asset này trong Inspector.</summary>
    public Item_Usage BlockedUsage => blockedUsage;

    /// <summary>Công dụng thực tế = loại hỗ trợ trừ phần bị chặn.</summary>
    public Item_Usage Usage => SupportedUsage & ~blockedUsage;

    /// <summary>Tính năng X gọi hàm này trước khi nhận vật phẩm. Cần có đủ mọi cờ được hỏi.</summary>
    public bool CanBeUsedFor(Item_Usage usage)
    {
        return usage != Item_Usage.None && (Usage & usage) == usage;
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
