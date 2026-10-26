using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Một dòng cộng chỉ số của vũ khí (ví dụ +8 PhysicDamage, hoặc +20% ActionSpeed).</summary>
[Serializable]
public struct Weapon_Stat_Modifier
{
    public Player_Stat_Type stat;

    [Tooltip("Flat: cộng thẳng. PercentAdd: 0.1 = +10% (các PercentAdd cộng dồn). PercentMult: 0.1 = x1.1 (nhân riêng).")]
    public Player_Stat_Modifier_Type modifierType;

    public float value;
}

/// <summary>
/// Dữ liệu một loại vũ khí (tạo asset: chuột phải Project -> Create -> Raid And Craft -> Weapon Data).
/// Vũ khí quyết định ĐÒN ĐÁNH MẶC ĐỊNH hoạt động thế nào (cận chiến quạt hay bắn đạn) và cộng chỉ số cho người cầm.
/// Sát thương KHÔNG nằm trong vũ khí mà đến từ chỉ số nhân vật (vũ khí cộng PhysicDamage/MagicDamage... qua statModifiers).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Weapon Data", fileName = "Weapon_New")]
public class Weapon_Data : ScriptableObject
{
    public string displayName = "New Weapon";
    public Sprite icon;

    [Header("Đòn đánh mặc định khi cầm vũ khí này")]
    [Tooltip("Các loại sát thương đòn mang theo. Kiếm = Physic, trượng = Magic.")]
    public Damage_Type[] damageTypes = { Damage_Type.Physic };

    [Tooltip("Hệ số nhân lên sát thương của đòn đánh mặc định (đòn nặng của rìu > 1, dao găm < 1).")]
    [Min(0f)] public float damageMultiplier = 1f;

    public Skill_Delivery attackDelivery = new Skill_Delivery();

    [Header("Chỉ số cộng cho người cầm")]
    public List<Weapon_Stat_Modifier> statModifiers = new List<Weapon_Stat_Modifier>();
}
