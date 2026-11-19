using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gây sát thương: quạt cận chiến hoặc đạn. Có thể lấy cách đánh/loại sát thương từ vũ khí đang cầm.
/// Mục tiêu trúng đòn tức thì được ghi vào context.HitTargets để hiệu ứng sau dùng lại.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Effects/Damage", fileName = "Effect_Damage")]
public class Damage_Effect : Skill_Effect
{
    [Header("Sát thương")]
    [Tooltip("Hệ số riêng của skill, nhân thêm sau scaling profile.")]
    [Min(0f)] public float damageMultiplier = 1f;
    public Damage_Type[] damageTypes = { Damage_Type.Physic };

    [Header("Damage Scaling")]
    [Tooltip("Bật để lấy multiplier từ Damage_Balance_Config. Weapon Attack tự bật và tự chọn profile theo vũ khí.")]
    public bool useBalanceProfile;

    [SerializeField]
    private Damage_Scaling_Profile_Type scalingProfile =
        Damage_Scaling_Profile_Type.PureMagicSpell;

    [Tooltip("Dùng chung một asset cho nhiều skill để cân bằng toàn cục. Để trống sẽ dùng baseline mặc định trong code.")]
    [SerializeField] private Damage_Balance_Config balanceConfig;

    [Header("Cách ra đòn")]
    [Tooltip(
        "Bật: lấy delivery và profile Physical/Magic từ vũ khí đang cầm. " +
        "Attack Mode Alternate giữ fallback tương thích với hành vi cũ."
    )]
    public bool useWeaponAttack;

    public Skill_Delivery delivery = new Skill_Delivery();

    [Tooltip("Tầm đạn dùng cho AI/tooltip (≈ tốc độ × thời gian sống của đạn). Chỉ dùng khi delivery là Projectile.")]
    [Min(0f)] public float projectileReach = 10f;

    public override void Execute(Skill_Context context)
    {
        if (!useWeaponAttack ||
            context.Skill.attackMode == Skill_Attack_Mode.WeaponIndependent)
        {
            ExecuteIndependentEvent(context);
            return;
        }

        var sources = new List<Skill_Attack_Source>();

        Skill_Attack_Source_Resolver.Resolve(
            context.Caster.WeaponSource,
            context.Skill.attackMode,
            sources
        );

        if (sources.Count == 0)
        {
            if (context.Skill.attackMode == Skill_Attack_Mode.Alternate)
            {
                ExecuteLegacyWeaponFallback(context);
            }

            return;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            Skill_Attack_Source source = sources[i];

            float flatDamageScale =
                GetFlatDamageScale(
                    context.Skill.flatDamageDistribution,
                    source.Slot,
                    sources.Count
                );

            Damage_Info info = BuildWeaponDamageInfo(
                context,
                source.Attack,
                flatDamageScale
            );

            ExecuteDamageEvent(
                context,
                source.Attack.Delivery,
                info
            );
        }
    }

    private void ExecuteLegacyWeaponFallback(Skill_Context context)
    {
        Resolve(
            context.Caster,
            true,
            out Skill_Delivery resolvedDelivery,
            out Damage_Type[] resolvedTypes,
            out float resolvedMultiplier,
            out bool resolvedUseProfile,
            out Damage_Scaling_Profile_Type resolvedProfileType,
            out Damage_Type[] resolvedAttackSources,
            out object resolvedExcludedStatSource
        );

        Damage_Info info;

        if (context.Skill.useSkillDamageConfiguration)
        {
            info = Combat_Formulas.BuildSkillAttack(
                context.Stats,
                resolvedMultiplier,
                context.Skill.damageEntries,
                resolvedAttackSources,
                resolvedExcludedStatSource
            );
        }
        else
        {
            info = resolvedUseProfile
                ? Combat_Formulas.BuildAttack(
                    context.Stats,
                    resolvedMultiplier,
                    GetProfile(resolvedProfileType),
                    resolvedExcludedStatSource
                )
                : Combat_Formulas.BuildAttack(
                    context.Stats,
                    resolvedMultiplier,
                    resolvedTypes
                );
        }

        ExecuteDamageEvent(
            context,
            resolvedDelivery,
            info
        );
    }

    private void ExecuteIndependentEvent(Skill_Context context)
    {
        Damage_Info info;

        if (context.Skill.useSkillDamageConfiguration)
        {
            info = Combat_Formulas.BuildSkillAttack(
                context.Stats,
                damageMultiplier,
                context.Skill.damageEntries
            );
        }
        else
        {
            info = useBalanceProfile
                ? Combat_Formulas.BuildAttack(
                    context.Stats,
                    damageMultiplier,
                    GetProfile(scalingProfile)
                )
                : Combat_Formulas.BuildAttack(
                    context.Stats,
                    damageMultiplier,
                    damageTypes
                );
        }

        ExecuteDamageEvent(
            context,
            delivery,
            info
        );
    }

    private Damage_Info BuildWeaponDamageInfo(
        Skill_Context context,
        Skill_Weapon_Attack attack,
        float flatDamageScale)
    {
        float multiplier =
            damageMultiplier *
            attack.DamageMultiplier;

        if (context.Skill.useSkillDamageConfiguration)
        {
            return Combat_Formulas.BuildSkillAttack(
                context.Stats,
                multiplier,
                context.Skill.damageEntries,
                attack.AttackSources,
                attack.ExcludedStatSource,
                flatDamageScale
            );
        }

        Damage_Scaling_Profile_Type profileType =
            attack.Archetype == Weapon_Attack_Archetype.Magic
                ? Damage_Scaling_Profile_Type.MagicWeapon
                : Damage_Scaling_Profile_Type.PhysicalWeapon;

        return Combat_Formulas.BuildAttack(
            context.Stats,
            multiplier,
            GetProfile(profileType),
            attack.ExcludedStatSource
        );
    }

    private static float GetFlatDamageScale(
        Skill_Flat_Damage_Distribution distribution,
        Skill_Weapon_Source_Slot slot,
        int eventCount)
    {
        switch (distribution)
        {
            case Skill_Flat_Damage_Distribution.Split:
                return eventCount > 0
                    ? 1f / eventCount
                    : 0f;

            case Skill_Flat_Damage_Distribution.MainHandOnly:
                return slot == Skill_Weapon_Source_Slot.MainHand
                    ? 1f
                    : 0f;

            case Skill_Flat_Damage_Distribution.OffHandOnly:
                return slot == Skill_Weapon_Source_Slot.OffHand
                    ? 1f
                    : 0f;

            case Skill_Flat_Damage_Distribution.Each:
            default:
                return 1f;
        }
    }

    private static void ExecuteDamageEvent(
        Skill_Context context,
        Skill_Delivery damageDelivery,
        Damage_Info info)
    {
        switch (damageDelivery.type)
        {
            case Skill_Delivery_Type.Melee_Arc:
                PerformMeleeArc(
                    context,
                    damageDelivery,
                    info
                );
                break;

            case Skill_Delivery_Type.Projectile:
                FireProjectile(
                    context,
                    damageDelivery,
                    info
                );
                break;
        }
    }

    public override float GetReach(Skill_Caster caster, Skill_Data skill)
    {
        Resolve(
            caster,
            false,
            out Skill_Delivery d,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _
        );

        return d.type == Skill_Delivery_Type.Melee_Arc
            ? d.range
            : projectileReach * 0.9f;
    }

    public override void Describe(
        Skill_Caster caster,
        Skill_Data skill,
        Skill_Preview preview)
    {
        Character_Stats stats = caster.Stats;
        if (stats == null)
            return;

        Resolve(
            caster,
            false,
            out Skill_Delivery d,
            out Damage_Type[] types,
            out float multiplier,
            out bool useProfile,
            out Damage_Scaling_Profile_Type profileType,
            out Damage_Type[] attackSources,
            out object excludedStatSource
            );

        float totalMin = 0f;
        float totalMax = 0f;
        var parts = new List<string>();

        if (skill.useSkillDamageConfiguration)
        {
            if (skill.damageEntries != null)
            {
                foreach (Skill_Damage_Entry entry in skill.damageEntries)
                {
                    if (entry == null)
                        continue;

                    Combat_Formulas.GetDamageRange(
                        stats,
                        entry.damageType,
                        excludedStatSource,
                        out float lo,
                        out float hi
                    );

                    bool sourceAvailable =
                        !useWeaponAttack ||
                        attackSources == null ||
                        Array.IndexOf(
                            attackSources,
                            entry.damageType
                        ) >= 0;

                    lo =
                        (
                            lo *
                            (
                                sourceAvailable
                                    ? Mathf.Max(
                                        0f,
                                        entry.damageMultiplier
                                    )
                                    : 0f
                            )
                            + Mathf.Max(0f, entry.flatDamage)
                        )
                        * multiplier;

                    hi =
                        (
                            hi *
                            (
                                sourceAvailable
                                    ? Mathf.Max(
                                        0f,
                                        entry.damageMultiplier
                                    )
                                    : 0f
                            )
                            + Mathf.Max(0f, entry.flatDamage)
                        )
                        * multiplier;

                    totalMin += lo;
                    totalMax += hi;

                    if (lo > 0f || hi > 0f)
                    {
                        parts.Add(
                            Mathf.Approximately(lo, hi)
                                ? $"{lo:0.##} {entry.damageType}"
                                : $"{lo:0.##}-{hi:0.##} {entry.damageType}"
                        );
                    }
                }
            }
        }
        else if (useProfile)
        {
            Damage_Scaling_Profile profile = GetProfile(profileType);

            foreach (Damage_Type type in Damage_Info.AllTypes)
            {
                float typeMultiplier =
                    multiplier * profile.GetMultiplier(type);

                if (typeMultiplier <= 0f)
                    continue;

                Combat_Formulas.GetDamageRange(
                    stats,
                    type,
                    out float lo,
                    out float hi
                );

                lo *= typeMultiplier;
                hi *= typeMultiplier;

                totalMin += lo;
                totalMax += hi;

                parts.Add(
                    Mathf.Approximately(lo, hi)
                        ? $"{lo:0.##} {type}"
                        : $"{lo:0.##}-{hi:0.##} {type}"
                );
            }
        }
        else if (types != null)
        {
            foreach (Damage_Type type in types)
            {
                Combat_Formulas.GetDamageRange(
                    stats,
                    type,
                    excludedStatSource,
                    out float lo,
                    out float hi
                );

                lo *= multiplier;
                hi *= multiplier;

                totalMin += lo;
                totalMax += hi;

                parts.Add(
                    Mathf.Approximately(lo, hi)
                        ? $"{lo:0.##} {type}"
                        : $"{lo:0.##}-{hi:0.##} {type}"
                );
            }
        }

        if (totalMax <= 0f)
        {
            preview.Add("Damage", "0 (damage stat is 0)");
            return;
        }

        string range = Mathf.Approximately(totalMin, totalMax)
            ? $"{totalMax:0.##}"
            : $"{totalMin:0.##}-{totalMax:0.##}";

        preview.Add(
            "Damage",
            parts.Count > 1
                ? $"{range} ({string.Join(" + ", parts)})"
                : range
        );

        float crit =
            1f + stats.GetValue(Player_Stat_Type.CriticalDamage) / 100f;

        preview.Add(
            "Critical",
            $"{totalMin * crit:0.##}-{totalMax * crit:0.##}"
        );

        float average = (totalMin + totalMax) * 0.5f;

        if (preview.Cooldown > 0f)
        {
            preview.Add(
                "DPS",
                $"{average / preview.Cooldown:0.##}"
            );
        }

        preview.Add(
            "Delivery",
            d.type == Skill_Delivery_Type.Melee_Arc
                ? $"Melee {d.range:0.#}m / {d.arcAngle:0}°"
                : "Projectile"
        );
    }

    private Damage_Scaling_Profile GetProfile(
        Damage_Scaling_Profile_Type type)
    {
        return balanceConfig != null
            ? balanceConfig.GetProfile(type)
            : Damage_Balance_Config.GetBuiltInProfile(type);
    }

    private void Resolve(
        Skill_Caster caster,
        bool useAlternateAttack,
        out Skill_Delivery d,
        out Damage_Type[] types,
        out float multiplier,
        out bool useProfile,
        out Damage_Scaling_Profile_Type profileType,
        out Damage_Type[] attackSources,
        out object excludedStatSource)
    {
        d = delivery;
        types = damageTypes;
        multiplier = damageMultiplier;
        useProfile = useBalanceProfile;
        profileType = scalingProfile;
        attackSources = null;
        excludedStatSource = null;

        if (!useWeaponAttack)
            return;

        useProfile = true;
        profileType =
            Damage_Scaling_Profile_Type.PhysicalWeapon;

        attackSources = DefaultPhysicalAttackSources;

        ISkill_Weapon_Source source = caster.WeaponSource;
        Skill_Weapon_Attack weapon = default;
        bool hasWeaponAttack = false;

        if (source != null)
        {
            hasWeaponAttack = useAlternateAttack
                ? source.TryGetAlternateWeaponAttack(
                    out weapon
                )
                : source.TryGetWeaponAttack(
                    out weapon
                );
        }

        if (!hasWeaponAttack)
            return;

        d = weapon.Delivery;
        multiplier =
            damageMultiplier *
            weapon.DamageMultiplier;

        profileType =
            weapon.Archetype ==
            Weapon_Attack_Archetype.Magic
                ? Damage_Scaling_Profile_Type.MagicWeapon
                : Damage_Scaling_Profile_Type.PhysicalWeapon;

        attackSources =
            weapon.AttackSources != null &&
            weapon.AttackSources.Length > 0
                ? weapon.AttackSources
                : GetLegacyAttackSources(
                    weapon.Archetype
                );

        excludedStatSource =
            weapon.ExcludedStatSource;
    }

    private static readonly Damage_Type[] DefaultPhysicalAttackSources =
    {
        Damage_Type.Physic,
        Damage_Type.Fire,
        Damage_Type.Cold,
        Damage_Type.Lightning,
        Damage_Type.Chaos
    };

    private static readonly Damage_Type[] DefaultMagicAttackSources =
    {
        Damage_Type.Magic,
        Damage_Type.Fire,
        Damage_Type.Cold,
        Damage_Type.Lightning,
        Damage_Type.Chaos
    };

    private static Damage_Type[] GetLegacyAttackSources(
        Weapon_Attack_Archetype archetype)
    {
        return archetype == Weapon_Attack_Archetype.Magic
            ? DefaultMagicAttackSources
            : DefaultPhysicalAttackSources;
    }

    private static void PerformMeleeArc(
        Skill_Context context,
        Skill_Delivery d,
        Damage_Info info)
    {
        Skill_Caster caster = context.Caster;
        List<Character_Stats> found = Skill_Targeting.Rent();

        Skill_Targeting.FindInArc(
            caster.transform.position,
            context.Direction,
            d.range,
            d.arcAngle,
            d.maxTargets,
            caster.TargetMask,
            context.Stats,
            found
        );

        for (int i = 0; i < found.Count; i++)
        {
            Character_Stats target = found[i];
            Damage_Result result = target.TakeDamage(info);

            Combat_Events.Report(
                context.Stats,
                target,
                result
            );

            if (result.WasSuccessfulHit)
                context.AddHit(target);
        }

        Skill_Targeting.Release(found);
    }

    private static void FireProjectile(
        Skill_Context context,
        Skill_Delivery d,
        Damage_Info info)
    {
        Skill_Caster caster = context.Caster;

        Vector3 spawnPosition = caster.transform.TransformPoint(
            caster.MuzzleOffset
        );

        Quaternion rotation = Quaternion.LookRotation(
            context.Direction
        );

        Skill_Projectile projectile = d.projectilePrefab != null
            ? UnityEngine.Object.Instantiate(
                d.projectilePrefab,
                spawnPosition,
                rotation
            )
            : Skill_Projectile.Create_Default(
                spawnPosition,
                rotation
            );

        projectile.Launch(
            context.Stats,
            info,
            context.Direction,
            d,
            caster.TargetMask,
            caster.ObstacleMask
        );
    }
}