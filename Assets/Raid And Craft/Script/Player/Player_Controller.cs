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
    }

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        if (input == null) return;

        if (input.LeftClickPressed || input.LeftClickHeld)
        {
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
