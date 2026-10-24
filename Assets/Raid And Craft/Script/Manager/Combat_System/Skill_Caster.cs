using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thi triển kỹ năng KHÔNG cần chọn mục tiêu: quay về hướng nhắm, rồi tuỳ cách ra đòn (Skill_Delivery) mà
/// - Melee_Arc: gây sát thương lên các mục tiêu trong quạt phía trước mặt (giới hạn số mục tiêu, gần nhất trước), hoặc
/// - Projectile: bắn một viên đạn theo hướng nhắm.
/// Đòn đánh mặc định cũng là một Skill_Data (bật useWeaponDelivery): cách ra đòn lấy từ vũ khí trong Character_Equipment.
/// Gắn lên Player (sau này Enemy dùng chung). Không đọc input: Player_Combat (hoặc AI) quyết định khi nào gọi TryCast.
/// - Một lần thi triển dựng MỘT Damage_Info (chí mạng roll một lần), mỗi mục tiêu tự roll né/đỡ riêng.
/// - Hồi chiêu tính riêng từng skill.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Skill_Caster : MonoBehaviour
{
    private static readonly Damage_Type[] PhysicOnly = { Damage_Type.Physic };

    /// <summary>Bắn mỗi lần thi triển thành công (skill, hướng ngang đã chuẩn hoá). Dùng để chạy animation/âm thanh.</summary>
    public event Action<Skill_Data, Vector3> OnSkillCast;

    [SerializeField] private Character_Stats stats;
    [SerializeField] private Character_Equipment equipment;

    [Header("Va chạm")]
    [Tooltip("Layer của đối tượng có thể bị đánh (Player chọn Enemy, Enemy chọn Player). Để Nothing = không đánh trúng ai.")]
    [SerializeField] private LayerMask targetMask;

    [Tooltip("Layer vật cản chặn đạn (tường, địa hình dựng thẳng). Mặt đất KHÔNG nên nằm trong đây.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Tay không")]
    [Tooltip("Cách ra đòn của đòn đánh mặc định khi không cầm vũ khí.")]
    [SerializeField] private Skill_Delivery unarmedDelivery = new Skill_Delivery
    {
        type = Skill_Delivery_Type.Melee_Arc,
        range = 1.5f,
        arcAngle = 90f,
        maxTargets = 1,
    };

    [Header("Đạn")]
    [Tooltip("Vị trí đạn xuất hiện, tính theo toạ độ cục bộ của nhân vật (x phải, y lên, z phía trước).")]
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 1f, 0.6f);

    [Header("Gỡ lỗi")]
    [SerializeField] private bool logResults;

    private readonly Dictionary<Skill_Data, float> nextReadyTime = new Dictionary<Skill_Data, float>();
    private readonly Collider[] overlapBuffer = new Collider[32];
    private readonly List<Character_Stats> hitList = new List<Character_Stats>();

    // Chỉ để vẽ Gizmo giúp chỉnh vùng cận chiến.
    private float debugRange;
    private float debugArc;
    private Vector3 debugDirection;
    private float debugUntil;

    public Character_Stats Stats => stats;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        if (equipment == null) equipment = GetComponent<Character_Equipment>();
    }

    /// <summary>Skill đã hết hồi chiêu chưa (skill chưa từng dùng thì luôn sẵn sàng).</summary>
    public bool IsReady(Skill_Data skill)
    {
        if (skill == null) return false;
        return !nextReadyTime.TryGetValue(skill, out float readyAt) || Time.time >= readyAt;
    }

    /// <summary>Có thể thi triển ngay bây giờ không: còn sống, hết hồi chiêu và đủ mana. (AI dùng để chọn skill.)</summary>
    public bool CanCast(Skill_Data skill)
    {
        if (skill == null || stats == null || stats.IsDead || !IsReady(skill)) return false;
        return skill.manaCost <= 0f || stats.CurrentMana >= skill.manaCost;
    }

    /// <summary>
    /// Tầm hiệu dụng của skill (mét): cận chiến = Range của quạt; đạn = 90% quãng bay (tốc độ x thời gian sống).
    /// Với đòn đánh mặc định thì lấy theo vũ khí đang cầm. AI dùng để biết cần tới gần mục tiêu bao nhiêu thì ra đòn.
    /// </summary>
    public float GetReach(Skill_Data skill)
    {
        if (skill == null) return 0f;

        ResolveDelivery(skill, out Skill_Delivery delivery, out _, out _);

        return delivery.type == Skill_Delivery_Type.Melee_Arc
            ? delivery.range
            : delivery.projectileSpeed * delivery.projectileLifetime * 0.9f;
    }

    /// <summary>
    /// Thi triển skill về phía aimPoint (điểm nhắm trong thế giới, thường là điểm dưới con trỏ chuột).
    /// Trả về false (không tốn mana, không tốn hồi chiêu) nếu: đã chết, chưa hết hồi chiêu, hoặc không đủ mana.
    /// Trả về true khi đã tung đòn, kể cả khi quạt không trúng ai.
    /// </summary>
    public bool TryCast(Skill_Data skill, Vector3 aimPoint)
    {
        if (skill == null || stats == null || stats.IsDead || !IsReady(skill)) return false;

        ResolveDelivery(skill, out Skill_Delivery delivery, out Damage_Type[] types, out float multiplier);

        if (skill.manaCost > 0f && !stats.TrySpendMana(skill.manaCost)) return false;

        Vector3 direction = HorizontalDirection(aimPoint);
        transform.rotation = Quaternion.LookRotation(direction);

        Damage_Info info = Combat_Formulas.BuildAttack(stats, multiplier, types);

        switch (delivery.type)
        {
            case Skill_Delivery_Type.Melee_Arc: PerformMeleeArc(delivery, direction, info); break;
            case Skill_Delivery_Type.Projectile: FireProjectile(delivery, direction, info); break;
        }

        float interval = skill.usesActionSpeed ? Combat_Formulas.ActionInterval(stats) : skill.cooldown;
        nextReadyTime[skill] = Time.time + interval;

        OnSkillCast?.Invoke(skill, direction);
        return true;
    }

    // Skill có useWeaponDelivery thì lấy cách ra đòn/loại sát thương/hệ số từ vũ khí (hoặc tay không), ngược lại dùng của skill.
    private void ResolveDelivery(Skill_Data skill, out Skill_Delivery delivery, out Damage_Type[] types, out float multiplier)
    {
        multiplier = skill.damageMultiplier;

        if (skill.useWeaponDelivery)
        {
            Weapon_Data weapon = equipment != null ? equipment.Weapon : null;
            if (weapon != null)
            {
                delivery = weapon.attackDelivery;
                types = weapon.damageTypes;
                multiplier *= weapon.damageMultiplier;
            }
            else
            {
                delivery = unarmedDelivery;
                types = PhysicOnly;
            }
        }
        else
        {
            delivery = skill.delivery;
            types = skill.damageTypes;
        }

        // BuildAttack với danh sách rỗng sẽ mang MỌI loại sát thương, nên luôn chốt một loại.
        if (types == null || types.Length == 0) types = PhysicOnly;
    }

    private Vector3 HorizontalDirection(Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        direction.y = 0f;
        return direction.normalized;
    }

    // ------------------------------------------------------------------
    // Cận chiến: quạt phía trước mặt
    // ------------------------------------------------------------------

    private void PerformMeleeArc(Skill_Delivery delivery, Vector3 direction, Damage_Info info)
    {
        debugRange = delivery.range;
        debugArc = delivery.arcAngle;
        debugDirection = direction;
        debugUntil = Time.time + 0.4f;

        int count = Physics.OverlapSphereNonAlloc(transform.position, delivery.range, overlapBuffer, targetMask,
                                                  QueryTriggerInteraction.Ignore);

        hitList.Clear();
        for (int i = 0; i < count; i++)
        {
            Character_Stats target = overlapBuffer[i].GetComponentInParent<Character_Stats>();
            if (target == null || target == stats || target.IsDead || hitList.Contains(target)) continue;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            // Đứng sát ngay trên người thì luôn trúng, ngược lại phải nằm trong góc quạt.
            if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(direction, toTarget) > delivery.arcAngle * 0.5f) continue;

            hitList.Add(target);
        }

        // Gần nhất trước, rồi cắt theo số mục tiêu tối đa.
        Vector3 origin = transform.position;
        hitList.Sort((a, b) => PlanarSqrDistance(origin, a.transform.position).CompareTo(PlanarSqrDistance(origin, b.transform.position)));

        int hits = Mathf.Min(hitList.Count, Mathf.Max(1, delivery.maxTargets));
        for (int i = 0; i < hits; i++)
        {
            Character_Stats target = hitList[i];
            Damage_Result result = target.TakeDamage(info);
            Combat_Events.Report(stats, target, result);

            if (logResults)
            {
                Debug.Log($"[{nameof(Skill_Caster)}] {name} -> {target.name}: {result.outcome}" +
                          $"{(result.isCritical ? " (CRIT)" : "")}, trừ {result.damageDealt:0.#} máu" +
                          $"{(result.wasKilled ? ", HẠ GỤC" : "")}", this);
            }
        }
    }

    private static float PlanarSqrDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return (a - b).sqrMagnitude;
    }

    // ------------------------------------------------------------------
    // Đạn
    // ------------------------------------------------------------------

    private void FireProjectile(Skill_Delivery delivery, Vector3 direction, Damage_Info info)
    {
        Vector3 spawnPosition = transform.TransformPoint(muzzleOffset);
        Quaternion rotation = Quaternion.LookRotation(direction);

        Skill_Projectile projectile = delivery.projectilePrefab != null
            ? Instantiate(delivery.projectilePrefab, spawnPosition, rotation)
            : Skill_Projectile.Create_Default(spawnPosition, rotation);

        projectile.Launch(stats, info, direction, delivery, targetMask, obstacleMask);

        if (logResults) Debug.Log($"[{nameof(Skill_Caster)}] {name} bắn đạn theo hướng {direction}.", this);
    }

    // Chọn Player trong Scene lúc đang Play, sau khi đánh sẽ thấy vùng quạt vừa dùng trong khoảng 0.4 giây.
    private void OnDrawGizmosSelected()
    {
        if (Time.time > debugUntil || debugRange <= 0f) return;

        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float half = debugArc * 0.5f;

        Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * debugDirection * debugRange;
        Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * debugDirection * debugRange;
        Gizmos.DrawLine(origin, origin + left);
        Gizmos.DrawLine(origin, origin + right);

        const int segments = 16;
        Vector3 previous = origin + left;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 point = origin + Quaternion.AngleAxis(angle, Vector3.up) * debugDirection * debugRange;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
