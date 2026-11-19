using System.Collections.Generic;

/// <summary>
/// Một nguồn tấn công đã được resolve cho một Skill Activation.
/// Phase 4.2 chỉ xác định nguồn; Phase 4.3 mới tạo Damage Event từ dữ liệu này.
/// </summary>
public readonly struct Skill_Attack_Source
{
    public readonly Skill_Weapon_Source_Slot Slot;
    public readonly Skill_Weapon_Attack Attack;

    public Skill_Attack_Source(
        Skill_Weapon_Source_Slot slot,
        Skill_Weapon_Attack attack)
    {
        Slot = slot;
        Attack = attack;
    }
}

/// <summary>
/// Resolve Attack Mode thành danh sách nguồn vũ khí hợp lệ.
/// Không tạo Damage_Info và không thực thi delivery.
/// </summary>
public static class Skill_Attack_Source_Resolver
{
    public static void Resolve(
        ISkill_Weapon_Source weaponSource,
        Skill_Attack_Mode attackMode,
        List<Skill_Attack_Source> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (weaponSource == null ||
            attackMode == Skill_Attack_Mode.WeaponIndependent)
        {
            return;
        }

        switch (attackMode)
        {
            case Skill_Attack_Mode.MainHand:
                AddSpecificSource(
                    weaponSource,
                    Skill_Weapon_Source_Slot.MainHand,
                    results
                );
                break;

            case Skill_Attack_Mode.OffHand:
                AddSpecificSource(
                    weaponSource,
                    Skill_Weapon_Source_Slot.OffHand,
                    results
                );
                break;

            case Skill_Attack_Mode.DualSimultaneous:
                AddSpecificSource(
                    weaponSource,
                    Skill_Weapon_Source_Slot.MainHand,
                    results
                );

                AddSpecificSource(
                    weaponSource,
                    Skill_Weapon_Source_Slot.OffHand,
                    results
                );
                break;

            case Skill_Attack_Mode.Alternate:
                if (weaponSource.TryGetAlternateWeaponAttack(
                    out Skill_Weapon_Attack alternateAttack))
                {
                    results.Add(
                        new Skill_Attack_Source(
                            alternateAttack.SourceSlot,
                            alternateAttack
                        )
                    );
                }
                break;
        }
    }

    private static void AddSpecificSource(
        ISkill_Weapon_Source weaponSource,
        Skill_Weapon_Source_Slot slot,
        List<Skill_Attack_Source> results)
    {
        if (!weaponSource.TryGetWeaponAttack(
            slot,
            out Skill_Weapon_Attack attack))
        {
            return;
        }

        results.Add(
            new Skill_Attack_Source(
                slot,
                attack
            )
        );
    }
}