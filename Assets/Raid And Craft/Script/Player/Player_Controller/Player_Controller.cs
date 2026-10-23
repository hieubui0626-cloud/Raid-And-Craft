using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class Player_Controller : MonoBehaviour
{
    [Header("Player")]
    [FormerlySerializedAs("Player")] // Giữ tham chiếu đã gán trong Scene/Prefab
    [SerializeField] private GameObject player;

    [Header("Di chuyển")]
    [Tooltip("Chỉ các Layer này mới nhận click di chuyển (mặc định: tất cả). Nên chọn Layer mặt đất.")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxRayDistance = 200f;

    [Header("Cấu hình Camera")]
    [Tooltip("Khoảng cách lệch của Camera so với Player (X, Y, Z)")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 15f, -10f);

    [Tooltip("Độ mượt khi Camera đuổi theo Player (càng nhỏ càng mượt, càng lớn càng nhanh)")]
    [SerializeField] private float cameraSmoothSpeed = 5f;

    [Header("Chỉ số (tuỳ chọn)")]
    [Tooltip("Bỏ trống thì tự lấy Character_Stats trên Player. Có thì tốc độ di chuyển lấy từ chỉ số MovementSpeed và Player đứng yên khi chết.")]
    [SerializeField] private Character_Stats stats;

    [Header("Chiến đấu (tuỳ chọn)")]
    [Tooltip("Bỏ trống thì tự lấy Player_Combat trên Player. Có thì click vào Enemy sẽ đuổi + đánh thay vì đi tới.")]
    [SerializeField] private Player_Combat combat;

    private NavMeshAgent agent;
    private Camera mainCamera;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError($"[{nameof(Player_Controller)}] Chưa gán Player.", this);
            enabled = false;
            return;
        }

        agent = player.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"[{nameof(Player_Controller)}] Player thiếu component NavMeshAgent.", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError($"[{nameof(Player_Controller)}] Không tìm thấy Main Camera (cần tag MainCamera).", this);
            enabled = false;
            return;
        }

        // Đặt camera vào đúng vị trí ngay từ đầu để không bị trượt từ xa vào.
        mainCamera.transform.position = player.transform.position + cameraOffset;

        if (combat == null) combat = player.GetComponent<Player_Combat>();

        if (stats == null) stats = player.GetComponent<Character_Stats>();
        if (stats != null)
        {
            ApplyMovementSpeed();
            stats.OnStatChanged += HandleStatChanged;
            stats.OnDeath += HandleDeath;
        }
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

    // Chết thì dừng ngay, không đi tiếp tới điểm đã click.
    private void HandleDeath()
    {
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }

    private void Update()
    {
        if (stats != null && stats.IsDead) return;

        Input_Manager input = Input_Manager.Instance;
        if (input == null) return;

        if (input.LeftClickPressed || input.LeftClickHeld)
        {
            // Đang trỏ vào UI thì không coi là click vào thế giới (cả đánh lẫn di chuyển).
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (combat != null)
            {
                // Click trúng Enemy: Player_Combat tự đuổi + đánh, không đi tới điểm click.
                if (combat.TryTargetEnemyAt(input.MousePosition)) return;

                // Click ra mặt đất: bỏ mục tiêu cũ rồi đi tới đó.
                combat.ClearTarget();
            }

            HandleMovementClick(input.MousePosition);
        }
    }

    // Camera chạy sau khi Player đã di chuyển trong frame để tránh giật.
    private void LateUpdate()
    {
        FollowPlayerWithCamera();
    }

    private void HandleMovementClick(Vector2 screenPosition)
    {
        // Đang trỏ vào UI thì không coi là click di chuyển.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            agent.SetDestination(hit.point);
        }
    }

    private void FollowPlayerWithCamera()
    {
        Vector3 targetPosition = player.transform.position + cameraOffset;

        // Làm mượt không phụ thuộc framerate (Lerp với speed * deltaTime thì phụ thuộc).
        float t = 1f - Mathf.Exp(-cameraSmoothSpeed * Time.deltaTime);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, t);
    }
}
