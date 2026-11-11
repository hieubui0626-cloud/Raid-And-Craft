using System;
using UnityEngine;

/// <summary>
/// Dữ liệu một loại vũ khí (tạo asset: chuột phải Project -> Create -> Raid And Craft -> Items -> Weapon).
/// Là một vật phẩm trang bị (Equippable_Item_Data): nằm được trong Inventory, mặc vào ô Equip Slot (mặc định MainHand).
/// Tên, icon, Stat Modifiers, ô trang bị, Grid Size, Max Stack... nằm ở lớp cha (cùng tên field nên asset cũ không mất dữ liệu).
/// Vũ khí quyết định ĐÒN ĐÁNH MẶC ĐỊNH hoạt động thế nào (cận chiến quạt hay bắn đạn) và cộng chỉ số cho người cầm.
/// Sát thương KHÔNG nằm trong vũ khí mà đến từ chỉ số nhân vật (vũ khí cộng PhysicDamage/MagicDamage... qua statModifiers).
/// Đòn đánh chỉ lấy từ vũ khí ở ô MainHand.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Weapon", fileName = "Weapon_New")]
public class Weapon_Data : Equippable_Item_Data
{
    [Header("Weapon Classification")]
    [Tooltip("Họ vũ khí dùng để kiểm tra Skill Requirement. Unclassified không hợp lệ với Skill Restricted cho tới khi được phân loại.")]
    public Weapon_Family weaponFamily = Weapon_Family.Unclassified;

    [Header("Đòn đánh mặc định khi cầm vũ khí này")]
    [Tooltip("Auto suy ra từ damageTypes[0] để giữ tương thích asset cũ. Chọn rõ Physical/Magic nếu vũ khí có cấu hình đặc biệt.")]
    public Weapon_Attack_Archetype attackArchetype = Weapon_Attack_Archetype.Auto;

    [Tooltip("Các loại sát thương đòn mang theo. Kiếm = Physic, trượng = Magic.")]
    public Damage_Type[] damageTypes = { Damage_Type.Physic };

    [Tooltip("Hệ số nhân lên sát thương của đòn đánh mặc định (đòn nặng của rìu > 1, dao găm < 1).")]
    [Min(0f)] public float damageMultiplier = 1f;

    public Skill_Delivery attackDelivery = new Skill_Delivery();

    [Tooltip("Loại animation của đòn đánh mặc định khi cầm vũ khí này (kiếm = Attack_Slash, trượng = Cast_Staff). " +
             "Chỉ có tác dụng khi skill bật 'Use Weapon Animation'. None = dùng loại ghi trong skill.")]
    public Skill_Animation_Type attackAnimation = Skill_Animation_Type.None;
}
