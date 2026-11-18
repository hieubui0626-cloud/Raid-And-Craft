/// <summary>
/// Nguồn vũ khí độc lập với Equipment_Slot.
/// </summary>
public enum Skill_Weapon_Source_Slot
{
    MainHand = 0,
    OffHand = 1
}

/// <summary>
/// Cầu nối giữa skill và vũ khí, để skill system KHÔNG phụ thuộc trực tiếp Character_Equipment/Weapon_Data.
/// Character_Equipment implement ISkill_Weapon_Source.
/// </summary>
public interface ISkill_Weapon_Source
{
    /// <summary>
    /// Trả false nếu đang tay không.
    /// </summary>
    bool TryGetWeaponAttack(out Skill_Weapon_Attack attack);

    /// <summary>
    /// Lấy dữ liệu tấn công từ một tay cụ thể.
    /// </summary>
    bool TryGetWeaponAttack(
        Skill_Weapon_Source_Slot sourceSlot,
        out Skill_Weapon_Attack attack);

    /// <summary>
    /// Lấy dữ liệu tấn công kế tiếp theo cơ chế Alternate.
    /// </summary>
    bool TryGetAlternateWeaponAttack(
        out Skill_Weapon_Attack attack);
}

public readonly struct Skill_Weapon_Attack
{
    public readonly Skill_Delivery Delivery;
    public readonly Damage_Type[] DamageTypes;
    public readonly Damage_Type[] AttackSources;
    public readonly float DamageMultiplier;
    public readonly Weapon_Attack_Archetype Archetype;
    public readonly Weapon_Family Family;

    /// <summary>
    /// Loại animation đòn đánh của vũ khí.
    /// </summary>
    public readonly Skill_Animation_Type Animation;

    /// <summary>
    /// Modifier source của vũ khí đối diện cần loại trừ.
    /// </summary>
    public readonly object ExcludedStatSource;

    public Skill_Weapon_Attack(
        Skill_Delivery delivery,
        Damage_Type[] damageTypes,
        float damageMultiplier,
        Skill_Animation_Type animation =
            Skill_Animation_Type.None,
        Weapon_Attack_Archetype archetype =
            Weapon_Attack_Archetype.Auto,
        Weapon_Family family =
            Weapon_Family.Unclassified,
        Damage_Type[] attackSources = null,
        object excludedStatSource = null)
    {
        Delivery = delivery;
        DamageTypes = damageTypes;
        AttackSources = attackSources;
        DamageMultiplier = damageMultiplier;
        Animation = animation;
        Family = family;
        ExcludedStatSource = excludedStatSource;

        if (archetype == Weapon_Attack_Archetype.Auto)
        {
            Archetype =
                damageTypes != null &&
                damageTypes.Length > 0 &&
                damageTypes[0] == Damage_Type.Magic
                    ? Weapon_Attack_Archetype.Magic
                    : Weapon_Attack_Archetype.Physical;
        }
        else
        {
            Archetype = archetype;
        }
    }
}
