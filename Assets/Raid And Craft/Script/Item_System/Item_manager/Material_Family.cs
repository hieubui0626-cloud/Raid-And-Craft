using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một "họ" nguyên liệu: chuỗi nguyên liệu cùng loại theo bậc hiếm (ví dụ họ Iron: quặng sắt T1, sắt thô T2, sắt tinh chế T3).
/// Scene khai báo những họ nào quái ở đó có thể rơi (Scene_Rules), tier quyết định theo level Scene; asset này chỉ trả đúng món theo tier.
/// Tạo asset: Create -> Raid And Craft -> Items -> Material Family, kéo các Material_Item_Data của họ vào (mỗi bậc một món,
/// bậc lấy từ Rarity Tier của chính vật phẩm).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Material Family", fileName = "Family_New")]
public class Material_Family : ScriptableObject
{
    [SerializeField] private List<Material_Item_Data> items = new List<Material_Item_Data>();

    public IReadOnlyList<Material_Item_Data> Items => items;

    /// <summary>
    /// Món của bậc tier: đúng bậc nếu có, không thì bậc cao nhất không vượt tier, không thì bậc thấp nhất của họ
    /// (họ chỉ có T3 mà Scene đòi T1 thì vẫn rơi T3 thay vì không rơi gì). Null nếu họ trống.
    /// </summary>
    public Material_Item_Data GetForTier(int tier)
    {
        Material_Item_Data atOrBelow = null;
        Material_Item_Data lowest = null;

        foreach (Material_Item_Data item in items)
        {
            if (item == null) continue;

            if (lowest == null || item.RarityTier < lowest.RarityTier) lowest = item;
            if (item.RarityTier <= tier && (atOrBelow == null || item.RarityTier > atOrBelow.RarityTier)) atOrBelow = item;
        }

        return atOrBelow != null ? atOrBelow : lowest;
    }
}
