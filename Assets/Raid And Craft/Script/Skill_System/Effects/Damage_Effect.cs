using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gây sát thương: quạt cận chiến hoặc đạn. Có thể lấy cách đánh/loại sát thương từ vũ khí đang cầm.
/// Mục tiêu trúng đòn tức thời được ghi vào context.HitTargets để hiệu ứng sau (làm chậm...) dùng lại.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Effects/Damage", fileName = "Effect_Damage")]
public class Damage_Effect : Skill_Effect
{
    [Header("Sát thương")]
    [Tooltip("Hệ số nhân lên chỉ số sát thương của nhân vật (1 = bằng chỉ số).")]
    [Min(0f)] public float damageMultiplier = 1f;
    public Damage_Type[] damageTypes = { Damage_Type.Physic };

    [Header("Cách ra đòn")]
    [Tooltip("Bật: lấy delivery, loại sát thương, hệ số từ vũ khí đang cầm (hệ số nhân với hệ số bên trên). " +
             "Tay không hoặc không có vũ khí thì dùng cấu hình bên dưới.")]
    public bool useWeaponAttack;
    public Skill_Delivery delivery = new Skill_Delivery();

    [Tooltip("Tầm đạn dùng cho AI/tooltip (≈ tốc độ × thời gian sống của đạn). Chỉ dùng khi delivery là Projectile.")]
    [Min(0f)] public float projectileReach = 10f;

    // ------------------------------------------------------------------

    public override void Execute(Skill_Context context)
    {
        Skill_Caster caster = context.Caster;
        Resolve(caster, out Skill_Delivery d, out Damage_Type[] types, out float multiplier);

        Damage_Info info = Combat_Formulas.BuildAttack(context.Stats, multiplier, types);

        switch (d.type)
        {
            case Skill_Delivery_Type.Melee_Arc:
                PerformMeleeArc(context, d, info);
                break;
            case Skill_Delivery_Type.Projectile:
                FireProjectile(context, d, info);
                break;
        }
    }

    public override float GetReach(Skill_Caster caster, Skill_Data skill)
    {
        Resolve(caster, out Skill_Delivery d, out _, out _);
        return d.type == Skill_Delivery_Type.Melee_Arc ? d.range : projectileReach * 0.9f;
    }

    public override void Describe(Skill_Caster caster, Skill_Data skill, Skill_Preview preview)
    {
        Character_Stats stats = caster.Stats;
        if (stats == null) return;

        Resolve(caster, out Skill_Delivery d, out Damage_Type[] types, out float multiplier);

        float total = 0f;
        var parts = new List<string>();
        if (types != null)
        {
            foreach (Damage_Type type in types)
            {
                float value = GetDamageStat(stats, type) * multiplier;
                total += value;
                parts.Add($"{value:0.##} {type}");
            }
        }

        if (total <= 0f)
        {
            preview.Add("Damage", "0 (damage stat is 0)");
            return;
        }

        preview.Add("Damage", parts.Count > 1 ? $"{total:0.##} ({string.Join(" + ", parts)})" : $"{total:0.##}");

        float critMultiplier = 1f + stats.GetValue(Player_Stat_Type.CriticalDamage) / 100f;
        preview.Add("Critical", $"{total * critMultiplier:0.##}");

        if (preview.Cooldown > 0f) preview.Add("DPS", $"{total / preview.Cooldown:0.##}");

        preview.Add("Delivery", d.type == Skill_Delivery_Type.Melee_Arc
            ? $"Melee {d.range:0.#}m / {d.arcAngle:0}°"
            : "Projectile");
    }

    // ------------------------------------------------------------------

    private void Resolve(Skill_Caster caster, out Skill_Delivery d, out Damage_Type[] types, out float multiplier)
    {
        d = delivery;
        types = damageTypes;
        multiplier = damageMultiplier;

        if (!useWeaponAttack) return;

        ISkill_Weapon_Source source = caster.WeaponSource;
        if (source != null && source.TryGetWeaponAttack(out Skill_Weapon_Attack weapon))
        {
            d = weapon.Delivery;
            types = weapon.DamageTypes;
            multiplier = damageMultiplier * weapon.DamageMultiplier;
        }
    }

    // Damage_Type.Physic -> Player_Stat_Type.PhysicDamage, Fire -> FireDamage...
    private static float GetDamageStat(Character_Stats stats, Damage_Type type)
    {
        return Enum.TryParse(type + "Damage", out Player_Stat_Type statType) ? stats.GetValue(statType) : 0f;
    }

    private static void PerformMeleeArc(Skill_Context context, Skill_Delivery d, Damage_Info info)
    {
        Skill_Caster caster = context.Caster;
        List<Character_Stats> found = Skill_Targeting.Rent();

        Skill_Targeting.FindInArc(caster.transform.position, context.Direction, d.range, d.arcAngle,
                                  d.maxTargets, caster.TargetMask, context.Stats, found);

        for (int i = 0; i < found.Count; i++)
        {
            Character_Stats target = found[i];
            Damage_Result result = target.TakeDamage(info);
            Combat_Events.Report(context.Stats, target, result);
            context.AddHit(target);
        }

        Skill_Targeting.Release(found);
    }

    private static void FireProjectile(Skill_Context context, Skill_Delivery d, Damage_Info info)
    {
        Skill_Caster caster = context.Caster;
        Vector3 spawnPosition = caster.transform.TransformPoint(caster.MuzzleOffset);
        Quaternion rotation = Quaternion.LookRotation(context.Direction);

        Skill_Projectile projectile = d.projectilePrefab != null
            ? Instantiate(d.projectilePrefab, spawnPosition, rotation)
            : Skill_Projectile.Create_Default(spawnPosition, rotation);

        projectile.Launch(context.Stats, info, context.Direction, d, caster.TargetMask, caster.ObstacleMask);
    }
}
