
using System;
using UnityEngine;

/// <summary>
/// Phân loại chi tiết loại vũ khí.
/// Không thay thế Weapon_Family hiện tại.
/// </summary>
public enum Weapon_Kind
{
    Unclassified = 0,
    Sword = 1,
    Axe = 2,
    Dagger = 3,
    Bow = 4,
    Staff = 5,
    Shield = 6,
    Other = 7
}

/// <summary>
/// Phân loại số tay cần để sử dụng vũ khí.
/// </summary>
public enum Weapon_Hand_Type
{
    Unspecified = 0,
    OneHand = 1,
    TwoHand = 2
}

/// <summary>
/// Dữ liệu một loại vũ khí.
/// Kế thừa Equippable_Item_Data.
/// </summary>
[CreateAssetMenu(
    menuName = "Raid And Craft/Items/Weapon",
    fileName = "Weapon_New")]
public class Weapon_Data : Equippable_Item_Data
{
    [Header("Weapon Classification")]

    [Tooltip(
        "Họ vũ khí dùng để kiểm tra Skill Requirement. " +
        "Giữ tương thích với hệ thống cũ.")]
    public Weapon_Family weaponFamily =
        Weapon_Family.Unclassified;

    [Tooltip(
        "Phân loại chi tiết: Sword, Axe, Dagger, Bow, Staff...")]
    public Weapon_Kind weaponKind =
        Weapon_Kind.Unclassified;

    [Tooltip(
        "Số tay cần để sử dụng vũ khí.")]
    public Weapon_Hand_Type handType =
        Weapon_Hand_Type.Unspecified;


    [Header("Đòn đánh mặc định khi cầm vũ khí này")]

    [Tooltip(
        "Auto suy ra từ damageTypes[0] để giữ tương thích asset cũ. " +
        "Chọn rõ Physical/Magic nếu vũ khí có cấu hình đặc biệt.")]
    public Weapon_Attack_Archetype attackArchetype =
        Weapon_Attack_Archetype.Auto;

    [Tooltip(
        "Các Attack Source được skill đọc khi đánh bằng vũ khí này. " +
        "Để trống: tự suy ra theo Archetype cũ.")]
    public Damage_Type[] attackSources =
        new Damage_Type[0];

    [Tooltip(
        "Các loại sát thương đòn mang theo của cấu hình cũ.")]
    public Damage_Type[] damageTypes =
        { Damage_Type.Physic };

    [Tooltip(
        "Hệ số nhân sát thương của đòn đánh mặc định.")]
    [Min(0f)]
    public float damageMultiplier = 1f;

    public Skill_Delivery attackDelivery =
        new Skill_Delivery();

    [Tooltip(
        "Loại animation của đòn đánh mặc định khi cầm vũ khí này.")]
    public Skill_Animation_Type attackAnimation =
        Skill_Animation_Type.None;

    // =====================================================
    // WEAPON EQUIPMENT RULES
    // =====================================================

    /// <summary>
    /// Kiểm tra vũ khí có phải vũ khí hai tay hay không.
    /// </summary>
    public bool IsTwoHanded()
    {
        return handType == Weapon_Hand_Type.TwoHand;
    }

    /// <summary>
    /// Kiểm tra vũ khí có phải Shield hay không.
    /// </summary>
    public bool IsShield()
    {
        return weaponKind == Weapon_Kind.Shield;
    }

    /// <summary>
    /// Kiểm tra vũ khí có được phép trang bị vào slot chỉ định.
    ///
    /// EquipSlot là vị trí mặc định của Asset,
    /// không phải giới hạn duy nhất của vũ khí.
    /// </summary>
    public bool CanEquipInSlot(Equipment_Slot targetSlot)
    {
        // Chỉ xử lý MainHand và OffHand.
        if (targetSlot != Equipment_Slot.MainHand &&
            targetSlot != Equipment_Slot.OffHand)
        {
            return targetSlot == EquipSlot;
        }

        // Shield chỉ được phép ở OffHand.
        if (IsShield())
        {
            return targetSlot == Equipment_Slot.OffHand;
        }

        // Vũ khí hai tay chỉ được phép ở MainHand.
        if (IsTwoHanded())
        {
            return targetSlot == Equipment_Slot.MainHand;
        }

        // Vũ khí một tay được phép ở cả hai tay.
        if (handType == Weapon_Hand_Type.OneHand)
        {
            return true;
        }

        // Tương thích ngược:
        // Asset cũ chưa được phân loại chỉ sử dụng slot mặc định.
        return targetSlot == EquipSlot;
    }

    /// <summary>
    /// Kiểm tra vũ khí có thể trở thành nguồn tấn công
    /// từ slot đang sử dụng hay không.
    /// Shield không phải nguồn Default Attack.
    /// </summary>
    public bool CanAttackFromSlot(Equipment_Slot sourceSlot)
    {
        if (!CanEquipInSlot(sourceSlot))
            return false;

        if (IsShield())
            return false;

        return sourceSlot == Equipment_Slot.MainHand ||
               sourceSlot == Equipment_Slot.OffHand;
    }

}
