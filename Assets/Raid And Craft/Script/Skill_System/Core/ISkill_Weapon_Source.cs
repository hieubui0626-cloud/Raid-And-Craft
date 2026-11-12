/// <summary>
/// Cầu nối giữa skill và vũ khí, để skill system KHÔNG phụ thuộc trực tiếp Character_Equipment/Weapon_Data.
/// Character_Equipment implement ISkill_Weapon_Source.
/// </summary>
public interface ISkill_Weapon_Source
{
    /// <summary>Trả false nếu đang tay không (Damage_Effect dùng delivery của chính nó, animation dùng loại của skill).</summary>
    bool TryGetWeaponAttack(out Skill_Weapon_Attack attack);
}

public readonly struct Skill_Weapon_Attack
{
    public readonly Skill_Delivery Delivery;
    public readonly Damage_Type[] DamageTypes;
    public readonly Damage_Type[] AttackSources;
    public readonly float DamageMultiplier;
    public readonly Weapon_Attack_Archetype Archetype;
    public readonly Weapon_Family Family;

    /// <summary>Loại animation đòn đánh của vũ khí (None = vũ khí chưa chọn, dùng loại của skill).</summary>
    public readonly Skill_Animation_Type Animation;

    public Skill_Weapon_Attack(Skill_Delivery delivery, Damage_Type[] damageTypes, float damageMultiplier,
                               Skill_Animation_Type animation = Skill_Animation_Type.None,
                               Weapon_Attack_Archetype archetype = Weapon_Attack_Archetype.Auto,
                               Weapon_Family family = Weapon_Family.Unclassified,
                               Damage_Type[] attackSources = null)
    {
        Delivery = delivery;
        DamageTypes = damageTypes;
        AttackSources = attackSources;
        DamageMultiplier = damageMultiplier;
        Animation = animation;
        Family = family;

        if (archetype == Weapon_Attack_Archetype.Auto)
        {
            Archetype = damageTypes != null && damageTypes.Length > 0 && damageTypes[0] == Damage_Type.Magic
                ? Weapon_Attack_Archetype.Magic
                : Weapon_Attack_Archetype.Physical;
        }
        else
        {
            Archetype = archetype;
        }
    }
}
