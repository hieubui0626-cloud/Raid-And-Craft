using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Cách Enemy chọn skill tiếp theo trong danh sách.</summary>
public enum Enemy_Skill_Pick_Mode
{
    /// <summary>Chọn ngẫu nhiên trong các skill đang dùng được (hết hồi chiêu, đủ mana).</summary>
    Random = 0,
    /// <summary>Lần lượt theo thứ tự danh sách (vòng lại từ đầu); skill nào chưa hết hồi chiêu thì bỏ qua sang skill kế.</summary>
    Sequential = 1,
}

/// <summary>
/// AI cơ bản của Enemy, 3 trạng thái:
/// - Idle: đứng yên ở chỗ xuất hiện, thấy Player trong aggroRange thì chuyển sang Chase.
/// - Chase: chọn một skill (trong danh sách Skills) rồi đuổi Player tới khi vào TẦM của skill đó thì dừng và thi triển.
///   Player chết hoặc xa quá loseRange thì chuyển sang Return.
/// - Return: quay về chỗ xuất hiện (bỏ qua Player trên đường về), tới nơi thì hồi đầy máu rồi về Idle.
///
/// Skill: Enemy dùng Skill_Caster như Player, nên mọi thứ của Skill_Data đều có tác dụng: hồi chiêu riêng từng skill
/// (đòn đánh mặc định hồi theo ActionSpeed), mana, kiểu ra đòn (quạt/đạn), loại sát thương.
/// - Danh sách Skills: Enemy chỉ dùng skill nào có trong danh sách. Cho Enemy đánh thường thì thêm Skill_Default_Attack
///   (lấy theo vũ khí trong Character_Equipment, không có thì đánh tay không).
/// - Không skill nào dùng được (đang hồi chiêu hết): Enemy tiến tới tầm skill ngắn nhất rồi đứng nhìn Player chờ.
///
/// Gắn lên Enemy prefab cùng Character_Stats + Skill_Caster + NavMeshAgent.
/// Skill_Caster của Enemy: đặt Target Mask = Layer của Player (nên tạo riêng Layer 'Player').
/// Cần Player có tag "Player" (hoặc gán Target thủ công trong Inspector).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
[RequireComponent(typeof(Skill_Caster))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy_AI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Chase,
        Return,
    }

    [Header("Mục tiêu")]
    [Tooltip("Bỏ trống thì tự tìm object có tag 'Player'.")]
    [SerializeField] private Character_Stats target;

    [Header("Kỹ năng")]
    [Tooltip("Các skill Enemy có thể dùng. Hồi chiêu và mana của từng skill vẫn có tác dụng.")]
    [SerializeField] private List<Skill_Data> skills = new List<Skill_Data>();

    [Tooltip("Random: chọn ngẫu nhiên trong các skill đang dùng được. Sequential: lần lượt theo thứ tự danh sách.")]
    [SerializeField] private Enemy_Skill_Pick_Mode pickMode = Enemy_Skill_Pick_Mode.Random;

    [Tooltip("Nghỉ tối thiểu giữa hai lần thi triển BẤT KỲ skill nào (giây), để Enemy không tung nhiều skill liền một lúc.")]
    [SerializeField] private float minTimeBetweenCasts = 0.5f;

    [Header("Phạm vi")]
    [Tooltip("Player lọt vào khoảng này thì Enemy bắt đầu đuổi.")]
    [SerializeField] private float aggroRange = 8f;

    [Tooltip("Player ra xa hơn khoảng này thì Enemy bỏ cuộc. Nên lớn hơn aggroRange để không bị chập chờn.")]
    [SerializeField] private float loseRange = 14f;

    [Header("Hành vi")]
    [Tooltip("Cứ mỗi khoảng này (giây) mới tính lại đường đi, tránh tính đường mỗi frame.")]
    [SerializeField] private float repathInterval = 0.2f;

    [Tooltip("Về tới chỗ xuất hiện thì hồi đầy máu/mana (tránh người chơi kéo Enemy ra rồi bỏ chạy để Enemy yếu đi).")]
    [SerializeField] private bool restoreOnReturn = true;

    [Tooltip("Chết xong, sau bao nhiêu giây thì xoá Enemy khỏi Scene (chờ animation chết/rơi đồ).")]
    [SerializeField] private float destroyDelay = 3f;

    private Character_Stats stats;
    private Skill_Caster caster;
    private NavMeshAgent agent;

    private State state = State.Idle;
    private Vector3 spawnPosition;
    private float nextRepathTime;
    private float nextCastTime;

    // Skill đã chọn và đang đuổi theo để dùng (null = chưa chọn).
    private Skill_Data currentSkill;
    // Chế độ Sequential: vị trí bắt đầu tìm skill ở lần chọn kế tiếp.
    private int sequenceIndex;

    private readonly List<Skill_Data> candidates = new List<Skill_Data>();

    private const float ReturnArriveDistance = 0.5f;

    private void Awake()
    {
        stats = GetComponent<Character_Stats>();
        caster = GetComponent<Skill_Caster>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnValidate()
    {
        aggroRange = Mathf.Max(0f, aggroRange);
        loseRange = Mathf.Max(aggroRange, loseRange);
        minTimeBetweenCasts = Mathf.Max(0f, minTimeBetweenCasts);
    }

    private void Start()
    {
        spawnPosition = transform.position;

        if (caster == null)
        {
            Debug.LogError($"[{nameof(Enemy_AI)}] Thiếu Skill_Caster (hãy gỡ Combat_Attacker cũ và thêm Skill_Caster).", this);
            enabled = false;
            return;
        }

        if (!HasAnySkill())
        {
            Debug.LogError($"[{nameof(Enemy_AI)}] Danh sách Skills đang trống: Enemy sẽ đuổi theo nhưng không tấn công.", this);
        }

        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) target = playerObject.GetComponent<Character_Stats>();
        }

        if (target == null)
        {
            Debug.LogError($"[{nameof(Enemy_AI)}] Không tìm thấy Player (cần tag 'Player' và có Character_Stats), hoặc hãy gán Target.", this);
        }

        ApplyMovementSpeed();
        stats.OnStatChanged += HandleStatChanged;
        stats.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (stats == null) return;

        stats.OnStatChanged -= HandleStatChanged;
        stats.OnDeath -= HandleDeath;
    }

    private void HandleStatChanged(Player_Stat stat)
    {
        if (stat.Type == Player_Stat_Type.MovementSpeed) ApplyMovementSpeed();
    }

    private void ApplyMovementSpeed()
    {
        agent.speed = stats.GetValue(Player_Stat_Type.MovementSpeed);
    }

    // Chết: dừng AI + tắt agent và collider (không chặn đường, không bị trúng đòn nữa) rồi xoá sau một lúc.
    private void HandleDeath()
    {
        enabled = false;

        if (agent.enabled)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        if (stats.IsDead || !agent.isOnNavMesh) return;

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Return: UpdateReturn(); break;
        }
    }

    private bool TargetAlive => target != null && !target.IsDead;

    private void UpdateIdle()
    {
        if (!TargetAlive) return;

        float distance = PlanarDistance(transform.position, target.transform.position);
        if (distance <= aggroRange) state = State.Chase;
    }

    private void UpdateChase()
    {
        if (!TargetAlive)
        {
            EnterReturn();
            return;
        }

        float distance = PlanarDistance(transform.position, target.transform.position);
        if (distance > loseRange)
        {
            EnterReturn();
            return;
        }

        Skill_Data skill = SelectSkill();

        if (skill != null)
        {
            // Đã có skill định dùng: đuổi tới khi vào tầm của skill đó rồi mới thi triển.
            if (distance > caster.GetReach(skill))
            {
                MoveTo(target.transform.position);
                return;
            }

            StopMoving();
            FaceTarget();

            if (Time.time >= nextCastTime && caster.TryCast(skill, target.transform.position))
            {
                nextCastTime = Time.time + minTimeBetweenCasts;
                currentSkill = null; // Dùng xong: lần sau chọn lại.
            }
        }
        else
        {
            // Mọi skill đang hồi chiêu (hoặc hết mana): áp sát tới tầm skill ngắn nhất rồi đứng chờ.
            if (distance > ShortestReach())
            {
                MoveTo(target.transform.position);
            }
            else
            {
                StopMoving();
                FaceTarget();
            }
        }
    }

    private void EnterReturn()
    {
        currentSkill = null;
        state = State.Return;
    }

    private void UpdateReturn()
    {
        float distance = PlanarDistance(transform.position, spawnPosition);
        if (distance <= ReturnArriveDistance)
        {
            StopMoving();
            if (restoreOnReturn) stats.RestoreFull();
            state = State.Idle;
            return;
        }

        MoveTo(spawnPosition);
    }

    // ------------------------------------------------------------------
    // CHỌN SKILL
    // ------------------------------------------------------------------

    private bool HasAnySkill()
    {
        foreach (Skill_Data s in skills)
        {
            if (s != null) return true;
        }
        return false;
    }

    // Giữ skill đã chọn cho tới khi dùng được/đã dùng; chỉ chọn lại khi chưa có hoặc skill đó không còn dùng được.
    private Skill_Data SelectSkill()
    {
        if (currentSkill != null && caster.CanCast(currentSkill)) return currentSkill;

        currentSkill = PickSkill();
        return currentSkill;
    }

    private Skill_Data PickSkill()
    {
        if (skills.Count == 0) return null;

        if (pickMode == Enemy_Skill_Pick_Mode.Sequential)
        {
            // Từ vị trí hiện tại đi vòng danh sách, lấy skill đầu tiên dùng được.
            for (int i = 0; i < skills.Count; i++)
            {
                int index = (sequenceIndex + i) % skills.Count;
                if (caster.CanCast(skills[index]))
                {
                    sequenceIndex = (index + 1) % skills.Count;
                    return skills[index];
                }
            }
            return null;
        }

        candidates.Clear();
        foreach (Skill_Data s in skills)
        {
            if (caster.CanCast(s)) candidates.Add(s);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private float ShortestReach()
    {
        float shortest = float.PositiveInfinity;
        foreach (Skill_Data s in skills)
        {
            if (s == null) continue;
            shortest = Mathf.Min(shortest, caster.GetReach(s));
        }

        // Danh sách rỗng: không có tầm nào, cứ áp sát.
        return float.IsPositiveInfinity(shortest) ? 0f : shortest;
    }

    // ------------------------------------------------------------------
    // DI CHUYỂN
    // ------------------------------------------------------------------

    private void MoveTo(Vector3 destination)
    {
        if (Time.time < nextRepathTime) return;

        agent.SetDestination(destination);
        nextRepathTime = Time.time + repathInterval;
    }

    private void StopMoving()
    {
        if (agent.hasPath) agent.ResetPath();
    }

    // Khoảng cách trên mặt phẳng ngang (bỏ qua chênh lệch độ cao).
    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FaceTarget()
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    // Chọn Enemy trong Scene để thấy vòng phát hiện (vàng) và vòng bỏ cuộc (xám).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
