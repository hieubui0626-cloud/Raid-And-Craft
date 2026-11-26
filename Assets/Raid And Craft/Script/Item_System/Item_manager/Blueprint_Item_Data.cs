using UnityEngine;

/// <summary>
/// Bản thiết kế (Blueprint): vật phẩm tiêu hao xếp chồng, mua từ NPC (thêm vào Entries của Npc_Shop như mọi vật phẩm khác).
/// Bỏ Blueprint vào bàn Craft cùng nguyên liệu thì danh sách kết quả chỉ còn loại món mà Blueprint thiết kế.
/// Crafted Base: loại món (base) Blueprint này cho craft ra. P5 sẽ nối thêm Craft_Recipe (nguyên liệu bắt buộc, maxTier, coinCost).
/// Công dụng: Crafting. Bị trừ 1 mỗi lần craft thành công (do Craft_Service, P5).
/// Tạo asset: Create -> Raid And Craft -> Items -> Blueprint. Nên đặt Stack Type = Stackable và Max Stack lớn hơn 1.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Blueprint", fileName = "Blueprint_New")]
public class Blueprint_Item_Data : Item_Data
{
    [Header("Blueprint")]
    [Tooltip("Loại món trang bị (base) Blueprint này thiết kế.")]
    [SerializeField] private Equippable_Item_Data craftedBase;

    public Equippable_Item_Data CraftedBase => craftedBase;

    public override Item_Usage SupportedUsage => Item_Usage.Crafting;
}
