using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Thi triển skill và quản lý TRẠNG THÁI: Idle -> [Windup] -> Casting -> [Channeling] -> [Recovery] -> Idle.
/// Script này KHÔNG biết skill làm gì (việc đó do Skill_Effect), chỉ lo thời gian, khoá và báo sự kiện cho
/// animation/UI. Skill tức thời (mọi thời gian = 0, không kéo dài) vẫn chạy đồng bộ trong TryCast như trước.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Skill_Caster : MonoBehaviour
{
    /// <summary>Hiệu ứng vừa chạy (đầu giai đoạn Casting). Tham số: skill, hướng.</summary>
    public event Action<Skill_Data, Vector3> OnSkillCast;

    /// <summary>Đổi trạng thái. Idle thì skill = null. Animation/UI nghe sự kiện này.</summary>
    public event Action<Skill_State, Skill_Data> OnStateChanged;

    /// <summary>Skill bị ngắt giữa chừng (chết, bị choáng, đổi skill). Bắn TRƯỚC khi về Idle.</summary>
    public event Action<Skill_Data> OnSkillInterrupted;

    // Phòng khi effect quên EndChannel: tự gỡ sau chừng này giây để nhân vật không bị khoá vĩnh viễn.
    private const float ChannelSafetyTimeout = 60f;

    [SerializeField] private Character_Stats stats;

    [Tooltip("Tuỳ chọn. Có thì Skill_Caster tự dừng/thả NavMeshAgent theo khoá Movement/Rotation. Bỏ trống = tự tìm trên object.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Va chạm")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Đạn")]
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 1f, 0.6f);

    [Header("Gỡ lỗi")]
    [SerializeField] private bool logCasts;

    private readonly Dictionary<Skill_Data, float> nextReadyTime = new Dictionary<Skill_Data, float>();

    private Skill_Context session;
    private Coroutine runRoutine;
    private bool animationFireReceived;
    private bool movementHeld;
    private bool rotationHeld;
    private bool defaultUpdateRotation = true;

    public Character_Stats Stats => stats;
    public LayerMask TargetMask => targetMask;
    public LayerMask ObstacleMask => obstacleMask;
    public Vector3 MuzzleOffset => muzzleOffset;

    /// <summary>Nguồn vũ khí (Character_Equipment nếu có implement ISkill_Weapon_Source). Có thể null.</summary>
    public ISkill_Weapon_Source WeaponSource { get; private set; }

    // ------------------------------------------------------------------
    // Trạng thái
    // ------------------------------------------------------------------

    public Skill_State State { get; private set; } = Skill_State.Idle;

    /// <summary>Skill đang thi triển (null khi Idle).</summary>
    public Skill_Data CurrentSkill { get; private set; }

    public bool IsBusy => State != Skill_State.Idle;

    /// <summary>Những gì đang bị khoá theo giai đoạn hiện tại của skill hiện tại.</summary>
    public Skill_Lock CurrentLock
    {
        get
        {
            if (CurrentSkill == null) return Skill_Lock.None;

            Skill_Timing timing = CurrentSkill.timing;
            switch (State)
            {
                case Skill_State.Windup: return timing.windupLock;
                case Skill_State.Channeling: return timing.channelLock;
                case Skill_State.Recovery: return timing.recoveryLock;
                // Casting chỉ kéo dài một frame: giữ khoá của Windup để không nhả rồi khoá lại giữa hai giai đoạn.
                case Skill_State.Casting: return timing.windupTime > 0f ? timing.windupLock : Skill_Lock.None;
                default: return Skill_Lock.None;
            }
        }
    }

    public bool IsMovementLocked => (CurrentLock & Skill_Lock.Movement) != 0;

    /// <summary>true = script khác (Player_Combat, Enemy_AI...) KHÔNG được xoay nhân vật.</summary>
    public bool IsRotationLocked => (CurrentLock & Skill_Lock.Rotation) != 0;

    public bool IsCastLocked => (CurrentLock & Skill_Lock.Casting) != 0;

    /// <summary>Hệ số tốc độ theo ActionSpeed (gốc = 1; tối thiểu 0.1 để tránh chia cho 0).</summary>
    public float ActionSpeedMultiplier
    {
        get
        {
            if (stats == null) return 1f;

            float interval = Combat_Formulas.ActionInterval(stats);
            return float.IsInfinity(interval) ? 0.1f : Mathf.Max(0.1f, 1f / interval);
        }
    }

    // ------------------------------------------------------------------
    // Vòng đời
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null) defaultUpdateRotation = agent.updateRotation;

        WeaponSource = GetComponent<ISkill_Weapon_Source>();

        // Chết thì ngắt skill đang chạy.
        if (stats != null) stats.OnDeath += Interrupt;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.OnDeath -= Interrupt;
    }

    private void OnDisable()
    {
        if (session != null) Interrupt();
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
        if (IsCastLocked) return false;
        return skill.manaCost <= 0f || stats.CurrentMana >= skill.manaCost;
    }

    // ------------------------------------------------------------------
    // Thi triển
    // ------------------------------------------------------------------

    public bool TryCast(Skill_Data skill, Vector3 aimPoint)
    {
        if (!CanCast(skill)) return false;
        if (skill.manaCost > 0f && !stats.TrySpendMana(skill.manaCost)) return false;

        // Đang trong skill khác nhưng skill đó không khoá Casting: bấm skill mới = huỷ skill cũ (animation cancel).
        if (session != null) Interrupt();

        // Đặt hồi chiêu TRƯỚC khi chạy hiệu ứng: hiệu ứng lỗi cũng không thể spam.
        nextReadyTime[skill] = Time.time + GetCooldownDuration(skill);

        var context = new Skill_Context(this, skill, aimPoint);
        session = context;

        if (skill.faceAimDirection) transform.rotation = Quaternion.LookRotation(context.Direction);

        // Coroutine chạy đoạn đầu ĐỒNG BỘ tới lần yield đầu tiên: skill tức thời xong ngay trong lệnh này.
        Coroutine started = StartCoroutine(Run(context));
        if (session == context) runRoutine = started;

        return true;
    }

    private IEnumerator Run(Skill_Context context)
    {
        Skill_Data skill = context.Skill;
        Skill_Timing timing = skill.timing;

        float speed = timing.scaleWithActionSpeed ? ActionSpeedMultiplier : 1f;
        float windup = timing.windupTime / speed;
        float recovery = timing.recoveryTime / speed;

        // 1. WINDUP: gồng, hiệu ứng chưa chạy.
        if (windup > 0f)
        {
            animationFireReceived = false;
            SetState(Skill_State.Windup, skill);

            float end = Time.time + windup;
            while (Time.time < end)
            {
                if (timing.fireOnAnimationEvent && animationFireReceived) break;
                yield return null;
            }
        }

        if (context.IsCancelled) yield break;

        // 2. CASTING: chạy các effect (một frame).
        SetState(Skill_State.Casting, skill);
        ExecuteEffects(context);

        // Effect có thể làm người cast chết ngay lúc chạy (Interrupt đã dọn phiên): dừng, không chạy tiếp.
        if (context.IsCancelled) yield break;

        OnSkillCast?.Invoke(skill, context.Direction);

        if (logCasts)
        {
            Debug.Log($"[{nameof(Skill_Caster)}] {name} cast '{skill.displayName}' ({skill.effects.Length} effect, " +
                      $"trúng {context.HitTargets.Count}, kéo dài {context.ChannelCount}).", this);
        }

        // 3. CHANNELING: có effect kéo dài thì chờ tới khi tất cả báo xong (hoặc bị ngắt).
        if (context.ChannelCount > 0)
        {
            SetState(Skill_State.Channeling, skill);

            float limit = Time.time + ChannelSafetyTimeout;
            while (context.ChannelCount > 0)
            {
                if (Time.time > limit)
                {
                    Debug.LogError($"[{nameof(Skill_Caster)}] '{skill.displayName}' kéo dài quá {ChannelSafetyTimeout}s: " +
                                   "có effect quên gọi context.EndChannel()? Tự gỡ khoá.", this);
                    break;
                }
                yield return null;
            }
        }

        // 4. RECOVERY: khựng sau khi ra đòn.
        if (recovery > 0f)
        {
            SetState(Skill_State.Recovery, skill);

            float end = Time.time + recovery;
            while (Time.time < end) yield return null;
        }

        EndSession(context);
    }

    private void ExecuteEffects(Skill_Context context)
    {
        Skill_Effect[] effects = context.Skill.effects;
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
    }

    // ------------------------------------------------------------------
    // Ngắt
    // ------------------------------------------------------------------

    /// <summary>Ngắt BẮT BUỘC skill đang chạy (chết, tắt object, đổi skill). Không làm gì nếu đang rảnh.</summary>
    public void Interrupt()
    {
        if (session == null) return;

        Skill_Context context = session;
        Skill_Data skill = context.Skill;

        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = null;

        context.Cancel(); // Effect kéo dài thấy IsAlive = false và tự dừng.

        OnSkillInterrupted?.Invoke(skill);
        EndSession(context);
    }

    /// <summary>Ngắt THEO YÊU CẦU bên ngoài (choáng, đẩy lùi...): chỉ ngắt nếu skill cho phép. Trả về true nếu đã ngắt.</summary>
    public bool TryInterrupt()
    {
        if (session == null || !session.Skill.timing.interruptible) return false;

        Interrupt();
        return true;
    }

    /// <summary>Skill_Animation_Driver gọi khi animation gửi event 'Skill_Fire' (cho skill có Fire On Animation Event).</summary>
    public void NotifyAnimationFire()
    {
        if (State == Skill_State.Windup) animationFireReceived = true;
    }

    // ------------------------------------------------------------------
    // Nội bộ
    // ------------------------------------------------------------------

    private void SetState(Skill_State newState, Skill_Data skill)
    {
        State = newState;
        CurrentSkill = skill;

        ApplyLocks();
        OnStateChanged?.Invoke(newState, skill);
    }

    private void EndSession(Skill_Context context)
    {
        context.Finish();
        if (session == context) session = null;
        runRoutine = null;

        SetState(Skill_State.Idle, null);
    }

    // Khoá Movement/Rotation được áp lên NavMeshAgent ở đây để Player_Controller/Enemy_AI không cần biết.
    private void ApplyLocks()
    {
        if (agent == null || !agent.enabled) return;

        bool moveLocked = IsMovementLocked;
        if (moveLocked != movementHeld && agent.isOnNavMesh)
        {
            movementHeld = moveLocked;

            if (moveLocked)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }
            else
            {
                agent.isStopped = false;
            }
        }

        bool rotationLocked = IsRotationLocked;
        if (rotationLocked != rotationHeld)
        {
            rotationHeld = rotationLocked;
            agent.updateRotation = rotationLocked ? false : defaultUpdateRotation;
        }
    }

    public Vector3 GetHorizontalDirection(Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        return direction.normalized;
    }

    // ------------------------------------------------------------------
    // Animation
    // ------------------------------------------------------------------

    /// <summary>
    /// Loại animation cuối cùng của skill: vũ khí đang cầm nếu skill bật Use Weapon Animation và vũ khí đã chọn loại,
    /// không thì loại ghi trong skill. Skill_Animation_Driver gọi hàm này.
    /// </summary>
    public Skill_Animation_Type ResolveAnimation(Skill_Data skill)
    {
        if (skill == null) return Skill_Animation_Type.None;

        Skill_Animation_Config config = skill.animationConfig;

        if (config.useWeaponAnimation && WeaponSource != null &&
            WeaponSource.TryGetWeaponAttack(out Skill_Weapon_Attack weapon) &&
            weapon.Animation != Skill_Animation_Type.None)
        {
            return weapon.Animation;
        }

        return config.animationType;
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

        if (skill.timing.windupTime > 0f)
        {
            float speed = skill.timing.scaleWithActionSpeed ? ActionSpeedMultiplier : 1f;
            preview.Add("Cast time", $"{skill.timing.windupTime / speed:0.##}s");
        }

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
