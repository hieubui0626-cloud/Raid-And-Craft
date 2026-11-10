using UnityEngine;

/// <summary>
/// Một "family" vật phẩm tiêu hao (ví dụ Health Potion, Mana Potion, Food...). Mọi Consumable_Item_Data trỏ cùng một family
/// dùng CHUNG một hồi chiêu: dùng một món thì cả family chờ hết Cooldown mới dùng tiếp được (kể cả món khác loại cùng family).
/// Tạo asset: Create -> Raid And Craft -> Items -> Consumable Family, rồi kéo vào ô Family của từng vật phẩm tiêu hao.
/// Vật phẩm không có Family thì không có hồi chiêu.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Consumable Family", fileName = "Family_Consumable_New")]
public class Consumable_Family_Data : ScriptableObject
{
    [SerializeField] private string displayName = "Potion";

    [Tooltip("Số giây chờ giữa hai lần dùng bất kỳ vật phẩm nào thuộc family này. 0 = không hồi chiêu.")]
    [SerializeField, Min(0f)] private float cooldown = 5f;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public float Cooldown => Mathf.Max(0f, cooldown);
}
