using System;
using UnityEngine;

/// <summary>Nhóm scaling được dùng bởi Weapon Attack và Spell.</summary>
public enum Damage_Scaling_Profile_Type
{
    PhysicalWeapon = 0,
    MagicWeapon = 1,
    PureMagicSpell = 2,
    ElementalSpell = 3,
    PhysicalConversionSpell = 4,
    HybridSpell = 5
}

/// <summary>
/// Hệ số cho từng nguồn damage. Đây là SOURCE STAT multiplier, không phải damage conversion.
/// Conversion (đổi output type) sẽ là một bước riêng trong Spell Damage Builder.
/// </summary>
[Serializable]
public struct Damage_Scaling_Profile
{
    [Min(0f)] public float physic;
    [Min(0f)] public float magic;
    [Min(0f)] public float fire;
    [Min(0f)] public float cold;
    [Min(0f)] public float lightning;
    [Min(0f)] public float chaos;

    public Damage_Scaling_Profile(float physic, float magic, float fire, float cold, float lightning, float chaos)
    {
        this.physic = physic;
        this.magic = magic;
        this.fire = fire;
        this.cold = cold;
        this.lightning = lightning;
        this.chaos = chaos;
    }

    public float GetMultiplier(Damage_Type type)
    {
        switch (type)
        {
            case Damage_Type.Physic: return physic;
            case Damage_Type.Magic: return magic;
            case Damage_Type.Fire: return fire;
            case Damage_Type.Cold: return cold;
            case Damage_Type.Lightning: return lightning;
            case Damage_Type.Chaos: return chaos;
            default: return 0f;
        }
    }
}

/// <summary>
/// Cấu hình balance trung tâm cho các nhóm attack. Có thể dùng chung một asset cho nhiều Damage_Effect.
/// Chỉnh các hệ số tại Inspector để cân bằng mà không sửa Combat_Formulas.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Combat/Damage Balance Config", fileName = "Damage_Balance_Config")]
public sealed class Damage_Balance_Config : ScriptableObject
{
    [Header("Weapon Attack")]
    public Damage_Scaling_Profile physicalWeapon =
        new Damage_Scaling_Profile(2.00f, 0f, 0.40f, 0.40f, 0.40f, 0.40f);
    public Damage_Scaling_Profile magicWeapon =
        new Damage_Scaling_Profile(0f, 1.10f, 0.80f, 0.80f, 0.80f, 0.80f);

    [Header("Spell")]
    public Damage_Scaling_Profile pureMagicSpell =
        new Damage_Scaling_Profile(0f, 1.80f, 0.40f, 0.40f, 0.40f, 0.40f);
    public Damage_Scaling_Profile elementalSpell =
        new Damage_Scaling_Profile(0f, 1.20f, 1.00f, 1.00f, 1.00f, 1.00f);
    public Damage_Scaling_Profile physicalConversionSpell =
        new Damage_Scaling_Profile(1.20f, 0f, 0.50f, 0.50f, 0.50f, 0.50f);
    public Damage_Scaling_Profile hybridSpell =
        new Damage_Scaling_Profile(0.75f, 1.20f, 0f, 0f, 0f, 0f);

    public Damage_Scaling_Profile GetProfile(Damage_Scaling_Profile_Type type)
    {
        switch (type)
        {
            case Damage_Scaling_Profile_Type.PhysicalWeapon: return physicalWeapon;
            case Damage_Scaling_Profile_Type.MagicWeapon: return magicWeapon;
            case Damage_Scaling_Profile_Type.PureMagicSpell: return pureMagicSpell;
            case Damage_Scaling_Profile_Type.ElementalSpell: return elementalSpell;
            case Damage_Scaling_Profile_Type.PhysicalConversionSpell: return physicalConversionSpell;
            case Damage_Scaling_Profile_Type.HybridSpell: return hybridSpell;
            default: return physicalWeapon;
        }
    }

    /// <summary>Baseline dự phòng nếu Damage_Effect chưa được gán config asset.</summary>
    public static Damage_Scaling_Profile GetBuiltInProfile(Damage_Scaling_Profile_Type type)
    {
        switch (type)
        {
            case Damage_Scaling_Profile_Type.MagicWeapon:
                return new Damage_Scaling_Profile(0f, 1.10f, 0.80f, 0.80f, 0.80f, 0.80f);
            case Damage_Scaling_Profile_Type.PureMagicSpell:
                return new Damage_Scaling_Profile(0f, 1.80f, 0.40f, 0.40f, 0.40f, 0.40f);
            case Damage_Scaling_Profile_Type.ElementalSpell:
                return new Damage_Scaling_Profile(0f, 1.20f, 1f, 1f, 1f, 1f);
            case Damage_Scaling_Profile_Type.PhysicalConversionSpell:
                return new Damage_Scaling_Profile(1.20f, 0f, 0.50f, 0.50f, 0.50f, 0.50f);
            case Damage_Scaling_Profile_Type.HybridSpell:
                return new Damage_Scaling_Profile(0.75f, 1.20f, 0f, 0f, 0f, 0f);
            default:
                return new Damage_Scaling_Profile(2f, 0f, 0.40f, 0.40f, 0.40f, 0.40f);
        }
    }
}

/// <summary>
/// Auto giữ tương thích với Weapon_Data cũ: suy ra Physical/Magic từ damageTypes[0].
/// Có thể chọn Physical hoặc Magic rõ ràng trong Inspector khi cần.
/// </summary>
public enum Weapon_Attack_Archetype
{
    Auto = 0,
    Physical = 1,
    Magic = 2
}
