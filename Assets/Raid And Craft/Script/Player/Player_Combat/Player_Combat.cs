using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chiến đấu của Player theo kiểu ARPG: click vào Enemy -> tự đuổi theo -> vào tầm thì dừng và đánh liên tục
/// cho tới khi mục tiêu chết hoặc bạn click ra mặt đất để đi chỗ khác.
/// Gắn lên PlayerPrefab (cùng object với Character_Stats, Combat_Attacker, NavMeshAgent).
/// Player_Controller là nơi nhận click và gọi TryTargetEnemyAt / ClearTarget; script này lo phần đuổi + đánh.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Combat_Attacker))]
[RequireComponent(typeof(NavMeshAgent))]
public class Player_Combat : MonoBehaviour
{
    [Header("Chọn mục tiêu")]
    [Tooltip("Layer của Enemy. BẮT BUỘC chọn đúng (ví dụ tạo Layer 'Enemy'), nếu để Nothing sẽ không click trúng được ai.")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float maxRayDistance = 200f;

    [Header("Đuổi theo")]
    [Tooltip("Cứ mỗi khoảng này (giây) mới tính lại đường tới mục tiêu, tránh tính đường mỗi frame.")]
    [SerializeField] private float repathInterval = 0.2f;

    private Combat_Attacker attacker;
    private Character_Stats stats;
    private NavMeshAgent agent;
    private Camera mainCamera;
    private float nextRepathTime;

    /// <summary>Mục tiêu đang đuổi/đánh (null = không có).</summary>
    public Character_Stats Target { get; private set; }

    private void Awake()
    {
        attacker = GetComponent<Combat_Attacker>();
        stats = GetComponent<Character_Stats>();
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Kiểm tra dưới con trỏ có Enemy còn sống không. Có thì đặt làm mục tiêu và trả về true
    /// (Player_Controller sẽ không xử lý click di chuyển nữa).
    /// </summary>
    public bool TryTargetEnemyAt(Vector2 screenPosition)
    {
        if (stats.IsDead) return false;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, enemyMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Collider có thể nằm trên object con của Enemy nên tìm ngược lên cha.
        Character_Stats found = hit.collider.GetComponentInParent<Character_Stats>();
        if (found == null || found == stats || found.IsDead) return false;

        Target = found;
        return true;
    }

    public void ClearTarget()
    {
        Target = null;
    }

    private void Update()
    {
        if (stats.IsDead)
        {
            Target = null;
            return;
        }

        if (Target == null) return;

        // Mục tiêu chết (hoặc bị huỷ) thì thôi, Player đứng yên chờ lệnh mới.
        if (Target.IsDead)
        {
            Target = null;
            return;
        }

        if (attacker.IsInRange(Target))
        {
            // Vào tầm: dừng lại rồi đánh. TryAttack tự lo hồi chiêu nên gọi mỗi frame được.
            if (agent.isOnNavMesh && agent.hasPath) agent.ResetPath();
            attacker.TryAttack(Target);
        }
        else if (Time.time >= nextRepathTime && agent.isOnNavMesh)
        {
            agent.SetDestination(Target.transform.position);
            nextRepathTime = Time.time + repathInterval;
        }
    }
}
