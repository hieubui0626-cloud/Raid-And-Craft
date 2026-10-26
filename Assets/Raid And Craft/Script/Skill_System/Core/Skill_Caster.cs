using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thi triển skill: kiểm tra hồi chiêu + mana, quay mặt, chạy lần lượt các Skill_Effect.
/// Script này KHÔNG biết skill làm gì: thêm hành vi mới chỉ cần viết Skill_Effect mới.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Skill_Caster : MonoBehaviour
{
    public event Action<Skill_Data, Vector3> OnSkillCast;

    [SerializeField] private Character_Stats stats;

    [Header("Va chạm")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Đạn")]
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 1f, 0.6f);

    [Header("Gỡ lỗi")]
    [SerializeField] private bool logCasts;

    private readonly Dictionary<Skill_Data, float> nextReadyTime = new Dictionary<Skill_Data, float>();

    public Character_Stats Stats => stats;
    public LayerMask TargetMask => targetMask;
    public LayerMask ObstacleMask => obstacleMask;
    public Vector3 MuzzleOffset => muzzleOffset;

    /// <summary>Nguồn vũ khí (Character_Equipment nếu có implement ISkill_Weapon_Source). Có thể null.</summary>
    public ISkill_Weapon_Source WeaponSource { get; private set; }

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        WeaponSource = GetComponent<ISkill_Weapon_Source>();
    }

    // ------------------------------------------------------------------
    // Hồi chiêu / điều kiện
    // ------------------------------------------------------------------

    public bool IsReady(Skill_Data skill)
    {
        if (skill == null) return false;
        return !nextReadyTime.TryGetValue(skill, out float readyAt) || Time.time >= readyAt;
    }

    public float GetCooldownRemaining(Skill_Data skill)
    {
        if (skill == null || !nextReadyTime.TryGetValue(skill, out float readyAt)) return 0f;
        return Mathf.Max(0f, readyAt - Time.time);
    }

    public float GetCooldownDuration(Skill_Data skill)
    {
        if (skill == null || stats == null) return 0f;
        return skill.usesActionSpeed ? Combat_Formulas.ActionInterval(stats) : skill.cooldown;
    }

    public bool CanCast(Skill_Data skill)
    {
        if (skill == null || stats == null || stats.IsDead || !skill.HasEffects || !IsReady(skill)) return false;
        return skill.manaCost <= 0f || stats.CurrentMana >= skill.manaCost;
    }

    // ------------------------------------------------------------------
    // Thi triển
    // ------------------------------------------------------------------

    public bool TryCast(Skill_Data skill, Vector3 aimPoint)
    {
        if (!CanCast(skill)) return false;
        if (skill.manaCost > 0f && !stats.TrySpendMana(skill.manaCost)) return false;

        // Đặt hồi chiêu TRƯỚC khi chạy hiệu ứng: hiệu ứng lỗi cũng không thể spam.
        nextReadyTime[skill] = Time.time + GetCooldownDuration(skill);

        var context = new Skill_Context(this, skill, aimPoint);

        if (skill.faceAimDirection) transform.rotation = Quaternion.LookRotation(context.Direction);

        Skill_Effect[] effects = skill.effects;
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] == null) continue;

            // Một hiệu ứng lỗi không làm hỏng các hiệu ứng còn lại.
            try
            {
                effects[i].Execute(context);
            }
            catch (Exception e)
            {
                Debug.LogException(e, effects[i]);
            }
        }

        if (logCasts) Debug.Log($"[{nameof(Skill_Caster)}] {name} cast '{skill.displayName}' ({effects.Length} effect, trúng {context.HitTargets.Count}).", this);

        OnSkillCast?.Invoke(skill, context.Direction);
        return true;
    }

    public Vector3 GetHorizontalDirection(Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        return direction.normalized;
    }

    // ------------------------------------------------------------------
    // Tầm và tooltip (cho AI / UI)
    // ------------------------------------------------------------------

    /// <summary>Tầm cần để skill có tác dụng = tầm dương nhỏ nhất trong các hiệu ứng (0 = không đòi tầm).</summary>
    public float GetReach(Skill_Data skill)
    {
        if (skill == null || skill.effects == null) return 0f;

        float reach = 0f;
        for (int i = 0; i < skill.effects.Length; i++)
        {
            if (skill.effects[i] == null) continue;

            float r = skill.effects[i].GetReach(this, skill);
            if (r > 0f && (reach <= 0f || r < reach)) reach = r;
        }
        return reach;
    }

    public bool TryGetPreview(Skill_Data skill, out Skill_Preview preview)
    {
        preview = BuildPreview(skill);
        return preview != null;
    }

    public Skill_Preview BuildPreview(Skill_Data skill)
    {
        if (skill == null || stats == null) return null;

        var preview = new Skill_Preview
        {
            Name = skill.displayName,
            Description = skill.description,
            Category = skill.category,
            ManaCost = skill.manaCost,
            Cooldown = GetCooldownDuration(skill),
            HasEnoughMana = skill.manaCost <= 0f || stats.CurrentMana >= skill.manaCost
        };

        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Length; i++)
            {
                if (skill.effects[i] != null) skill.effects[i].Describe(this, skill, preview);
            }
        }
        return preview;
    }
}
