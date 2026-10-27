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
    public readonly float DamageMultiplier;

    /// <summary>Loại animation đòn đánh của vũ khí (None = vũ khí chưa chọn, dùng loại của skill).</summary>
    public readonly Skill_Animation_Type Animation;

    public Skill_Weapon_Attack(Skill_Delivery delivery, Damage_Type[] damageTypes, float damageMultiplier,
                               Skill_Animation_Type animation = Skill_Animation_Type.None)
    {
        Delivery = delivery;
        DamageTypes = damageTypes;
        DamageMultiplier = damageMultiplier;
        Animation = animation;
    }
}
