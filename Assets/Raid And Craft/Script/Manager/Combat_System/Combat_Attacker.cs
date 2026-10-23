using System;
using UnityEngine;

/// <summary>
/// Khả năng RA ĐÒN dùng chung cho Player và Enemy (gắn cùng object với Character_Stats).
/// Chỉ làm 3 việc: kiểm tra đủ điều kiện đánh (còn sống, hết hồi chiêu, trong tầm) -> dựng đòn bằng
/// Combat_Formulas.BuildAttack -> đưa cho mục tiêu qua TakeDamage(Damage_Info).
/// KHÔNG tự chọn mục tiêu, KHÔNG tự di chuyển: việc đó do Player_Combat / Enemy_AI quyết định.
/// - Hồi chiêu giữa hai đòn = 1 / ActionSpeed (Combat_Formulas.ActionInterval), đổi ActionSpeed là đổi tốc độ đánh.
/// - Muốn hiện hiệu ứng/animation/số sát thương: nghe OnAttackPerformed (từng đối tượng) hoặc Combat_Events.OnAnyAttackResolved (toàn game).
/// - Đây là kiểu đánh CÓ mục tiêu (Enemy_AI dùng). Player dùng Skill_Caster (đánh không cần mục tiêu, theo hướng con trỏ).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Combat_Attacker : MonoBehaviour
{
    /// <summary>Bắn khi đối tượng NÀY ra đòn (target, kết quả). Dùng để chạy animation/âm thanh của riêng nó.</summary>
    public event Action<Character_Stats, Damage_Result> OnAttackPerformed;

    [SerializeField] private Character_Stats stats;

    [Header("Đòn đánh thường")]
    [Tooltip("Tầm đánh (mét, tính theo mặt phẳng ngang, từ tâm tới tâm). Cận chiến khoảng 2, đánh xa lớn hơn.")]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("Loại sát thương của đòn thường. Cận chiến = Physic, pháp sư = Magic.")]
    [SerializeField] private Damage_Type damageType = Damage_Type.Physic;

    [Tooltip("Hệ số nhân lên chỉ số sát thương (1 = đúng bằng chỉ số của nhân vật).")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Gỡ lỗi")]
    [SerializeField] private bool logResults;

    private float nextAttackTime;

    public Character_Stats Stats => stats;
    public float AttackRange => attackRange;

    /// <summary>Còn sống và đã hết hồi chiêu.</summary>
    public bool IsReady => stats != null && !stats.IsDead && Time.time >= nextAttackTime;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
    }

    /// <summary>Khoảng cách trên mặt phẳng ngang (bỏ qua chênh lệch độ cao).</summary>
    public static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public bool IsInRange(Character_Stats target)
    {
        return target != null && PlanarDistance(transform.position, target.transform.position) <= attackRange;
    }

    /// <summary>Đánh thường bằng cấu hình trong Inspector. Trả về true nếu đòn đã được tung ra (kể cả khi bị né/đỡ).</summary>
    public bool TryAttack(Character_Stats target)
    {
        return TryAttack(target, out _, damageMultiplier, damageType);
    }

    /// <summary>
    /// Đánh với hệ số/loại sát thương tuỳ ý (nền tảng cho kỹ năng sau này).
    /// Trả về false (không tốn hồi chiêu) nếu: mục tiêu rỗng/chính mình/đã chết, chưa hết hồi chiêu, hoặc ngoài tầm.
    /// </summary>
    public bool TryAttack(Character_Stats target, out Damage_Result result, float multiplier, params Damage_Type[] types)
    {
        result = Damage_Result.Ignored;

        if (target == null || target == stats || target.IsDead) return false;
        if (!IsReady || !IsInRange(target)) return false;

        FaceTarget(target);

        Damage_Info info = Combat_Formulas.BuildAttack(stats, multiplier, types);
        result = target.TakeDamage(info);

        // Hồi chiêu tính từ ActionSpeed TẠI THỜI ĐIỂM đánh.
        nextAttackTime = Time.time + Combat_Formulas.ActionInterval(stats);

        if (logResults)
        {
            Debug.Log($"[{nameof(Combat_Attacker)}] {name} -> {target.name}: {result.outcome}" +
                      $"{(result.isCritical ? " (CRIT)" : "")}, trừ {result.damageDealt:0.#} máu" +
                      $"{(result.wasKilled ? ", HẠ GỤC" : "")}", this);
        }

        OnAttackPerformed?.Invoke(target, result);
        Combat_Events.Report(stats, target, result);
        return true;
    }

    // Quay mặt về mục tiêu (chỉ xoay quanh trục Y).
    private void FaceTarget(Character_Stats target)
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
