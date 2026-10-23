using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI cơ bản của Enemy, 3 trạng thái:
/// - Idle: đứng yên ở chỗ xuất hiện, thấy Player trong aggroRange thì chuyển sang Chase.
/// - Chase: đuổi Player; vào tầm thì dừng và đánh; Player chết hoặc xa quá loseRange thì chuyển sang Return.
/// - Return: quay về chỗ xuất hiện (bỏ qua Player trên đường về), tới nơi thì hồi đầy máu rồi về Idle.
/// Gắn lên Enemy prefab cùng Character_Stats + Combat_Attacker + NavMeshAgent.
/// Cần Player có tag "Player" (hoặc gán Target thủ công trong Inspector).
/// Chỉ số (máu, sát thương, tốc độ chạy...) chỉnh ở Character_Stats của chính prefab Enemy.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
[RequireComponent(typeof(Combat_Attacker))]
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
    private Combat_Attacker attacker;
    private NavMeshAgent agent;

    private State state = State.Idle;
    private Vector3 spawnPosition;
    private float nextRepathTime;

    private const float ReturnArriveDistance = 0.5f;

    private void Awake()
    {
        stats = GetComponent<Character_Stats>();
        attacker = GetComponent<Combat_Attacker>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnValidate()
    {
        aggroRange = Mathf.Max(0f, aggroRange);
        loseRange = Mathf.Max(aggroRange, loseRange);
    }

    private void Start()
    {
        spawnPosition = transform.position;

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

    // Chết: dừng AI + tắt agent và collider (không chặn đường, không bị click trúng nữa) rồi xoá sau một lúc.
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

        float distance = Combat_Attacker.PlanarDistance(transform.position, target.transform.position);
        if (distance <= aggroRange) state = State.Chase;
    }

    private void UpdateChase()
    {
        if (!TargetAlive)
        {
            state = State.Return;
            return;
        }

        float distance = Combat_Attacker.PlanarDistance(transform.position, target.transform.position);
        if (distance > loseRange)
        {
            state = State.Return;
            return;
        }

        if (attacker.IsInRange(target))
        {
            if (agent.hasPath) agent.ResetPath();
            attacker.TryAttack(target);
        }
        else
        {
            MoveTo(target.transform.position);
        }
    }

    private void UpdateReturn()
    {
        float distance = Combat_Attacker.PlanarDistance(transform.position, spawnPosition);
        if (distance <= ReturnArriveDistance)
        {
            if (agent.hasPath) agent.ResetPath();
            if (restoreOnReturn) stats.RestoreFull();
            state = State.Idle;
            return;
        }

        MoveTo(spawnPosition);
    }

    private void MoveTo(Vector3 destination)
    {
        if (Time.time < nextRepathTime) return;

        agent.SetDestination(destination);
        nextRepathTime = Time.time + repathInterval;
    }
}
