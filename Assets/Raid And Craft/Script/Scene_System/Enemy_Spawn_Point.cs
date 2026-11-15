using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điểm spawn quái đặt tay trong Scene (Instance hoặc Base): tạo quái từ prefab khi vào Scene, trừ khi điểm này đã bị hạ trong lượt hiện tại
/// (Scene_State_Store nhớ theo nhân vật; reset theo Scene_Rules). Quái bị hạ KHÔNG hồi sinh trong cùng lượt.
/// - Vị trí luôn được kéo về điểm gần nhất TRÊN NAVMESH (Snap Distance). Điểm nằm ngoài NavMesh bị bỏ qua và báo lỗi (Gizmo đỏ trong Scene view).
/// - Scatter Radius > 0: quái xuất hiện ở một điểm ngẫu nhiên TRÊN NAVMESH trong bán kính đó (đàn quái).
/// - Boss: đặt Enemy_Reward.Rank = Boss trên prefab; điểm spawn không cần biết hạng.
/// - Spawn Id: để trống thì tự lấy theo tên + toạ độ (đủ duy nhất; di chuyển điểm sẽ coi là điểm mới). Muốn bền khi di chuyển thì điền Id riêng.
/// </summary>
public class Enemy_Spawn_Point : MonoBehaviour
{
    private static readonly List<Enemy_Spawn_Point> All = new List<Enemy_Spawn_Point>();

    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Id ổn định của điểm này (để lưu 'đã hạ'). Trống = tự tạo từ tên + toạ độ.")]
    [SerializeField] private string spawnId;

    [Tooltip("0 = đúng điểm này; > 0 = điểm ngẫu nhiên trên NavMesh trong bán kính (mét).")]
    [SerializeField, Min(0f)] private float scatterRadius;

    [Tooltip("Điểm phải cách NavMesh tối đa bao nhiêu mét (kéo về NavMesh gần nhất).")]
    [SerializeField, Min(0.1f)] private float snapDistance = 3f;

    private Character_Stats spawned;

    public string Id => string.IsNullOrEmpty(spawnId)
        ? $"{name}@{Mathf.RoundToInt(transform.position.x)},{Mathf.RoundToInt(transform.position.y)},{Mathf.RoundToInt(transform.position.z)}"
        : spawnId;

    /// <summary>Mọi điểm spawn trong Scene đều đã bị hạ (và Scene có ít nhất một điểm).</summary>
    public static bool AllDead()
    {
        if (All.Count == 0) return false;

        foreach (Enemy_Spawn_Point point in All)
        {
            if (!Scene_State_Store.IsDead(point.Id)) return false;
        }

        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
    }

    private void Awake()
    {
        All.Add(this);
    }

    private void OnDestroy()
    {
        All.Remove(this);
        if (spawned != null) spawned.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"[{nameof(Enemy_Spawn_Point)}] '{name}': chưa gán Enemy Prefab.", this);
            return;
        }

        if (Scene_State_Store.IsDead(Id)) return; // đã hạ ở lượt này, chưa reset

        if (!TryPickPosition(out Vector3 position))
        {
            Debug.LogError($"[{nameof(Enemy_Spawn_Point)}] '{name}' nằm ngoài NavMesh (cách > {snapDistance}m) nên không spawn. Bake lại NavMesh hoặc dời điểm vào vùng đi được.", this);
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh) agent.Warp(position);

        spawned = enemy.GetComponent<Character_Stats>();
        if (spawned != null) spawned.OnDeath += HandleDeath;
        else Debug.LogWarning($"[{nameof(Enemy_Spawn_Point)}] Prefab '{enemyPrefab.name}' không có Character_Stats nên không ghi nhớ được việc hạ quái.", this);
    }

    private void HandleDeath()
    {
        Scene_State_Store.MarkDead(Id);
    }

    private bool TryPickPosition(out Vector3 position)
    {
        Vector3 origin = transform.position;

        // Thử vài lần chọn điểm ngẫu nhiên trong vùng; không được thì dùng chính điểm đặt.
        if (scatterRadius > 0f)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit scatterHit, snapDistance, NavMesh.AllAreas))
                {
                    position = scatterHit.position;
                    return true;
                }
            }
        }

        if (NavMesh.SamplePosition(origin, out NavMeshHit hit, snapDistance, NavMesh.AllAreas))
        {
            position = hit.position;
            return true;
        }

        position = origin;
        return false;
    }

    private void OnDrawGizmos()
    {
        bool onMesh = NavMesh.SamplePosition(transform.position, out _, snapDistance, NavMesh.AllAreas);
        Gizmos.color = onMesh ? new Color(0.9f, 0.3f, 0.2f, 0.9f) : Color.magenta;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);

        if (scatterRadius > 0f)
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, scatterRadius);
        }
    }
}
