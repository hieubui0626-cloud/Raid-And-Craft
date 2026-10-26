/// <summary>
/// Cầu nối giữa skill và vũ khí, để skill system KHÔNG phụ thuộc trực tiếp Character_Equipment/Weapon_Data.
/// Character_Equipment chỉ cần implement ISkill_Weapon_Source (xem guide mục 6).
/// </summary>
public interface ISkill_Weapon_Source
{
    /// <summary>Trả false nếu đang tay không (Damage_Effect sẽ dùng delivery của chính nó).</summary>
    bool TryGetWeaponAttack(out Skill_Weapon_Attack attack);
}

public readonly struct Skill_Weapon_Attack
{
    public readonly Skill_Delivery Delivery;
    public readonly Damage_Type[] DamageTypes;
    public readonly float DamageMultiplier;

    public Skill_Weapon_Attack(Skill_Delivery delivery, Damage_Type[] damageTypes, float damageMultiplier)
    {
        Delivery = delivery;
        DamageTypes = damageTypes;
        DamageMultiplier = damageMultiplier;
    }
}
