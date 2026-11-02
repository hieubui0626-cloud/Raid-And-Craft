using UnityEngine;

/// <summary>
/// Nguyên liệu (Raw Material...): vật phẩm xếp chồng dùng cho hệ craft sau này (quặng, sắt thô...).
/// Chưa có công dụng nào (Item_Usage.None); sau này thêm cờ Crafting rồi trả về trong SupportedUsage.
/// Rarity Tier là bậc hiếm (1 = thường). Tooltip và thẻ tên trên đất tô màu theo bậc này.
/// Tạo asset: Create -> Raid And Craft -> Items -> Material. Nhớ đặt Stack Type = Stackable và Max Stack > 1.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Material", fileName = "Material_New")]
public class Material_Item_Data : Item_Data
{
    [Header("Nguyên liệu")]
    [Tooltip("Bậc hiếm: 1 thường, 2 hiếm, 3 rất hiếm... (quyết định bậc Tier của món craft sau này).")]
    [SerializeField, Min(1)] private int rarityTier = 1;

    public int RarityTier => Mathf.Max(1, rarityTier);
}
