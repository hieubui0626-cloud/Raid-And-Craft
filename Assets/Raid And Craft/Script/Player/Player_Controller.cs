using UnityEngine;
using UnityEngine.AI;


public class Player_Controller : MonoBehaviour
{
    [Header("Player")]
    public GameObject Player;

    [Header("Cấu hình Camera")]
    [Tooltip("Khoảng cách lệch của Camera so với Player (X, Y, Z)")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 15f, -10f);

    [Tooltip("Độ mượt mà khi Camera đuổi theo Player (Càng nhỏ càng mượt, càng lớn càng nhanh)")]
    [SerializeField] private float cameraSmoothSpeed = 5f;
    private NavMeshAgent agent;
    private Camera mainCamera;
    void Start()
    {
        agent = Player.GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = Player.transform.position + cameraOffset;
            // Ép Camera luôn nhìn thẳng vào Player
            
            
        }
    }

    
    void Update()
    {
        if (Input_Manager.Instance == null) return;

        // 1. Đọc dữ liệu click chuột từ InputManager
        if (Input_Manager.Instance.LeftClickPressed || Input_Manager.Instance.LeftClickHeld)
        {
            HandleMovementClick();
        }
        if (mainCamera != null)
        {
            FollowPlayerWithCamera();
        }
    }
    

    private void HandleMovementClick()
    {
        // Lấy vị trí chuột từ InputManager
        Ray ray = mainCamera.ScreenPointToRay(Input_Manager.Instance.MousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            agent.SetDestination(hit.point);
        }
        
    }
    private void FollowPlayerWithCamera()
    {
        // Tính toán vị trí lý tưởng mà Camera cần đến
        Vector3 targetCameraPosition = Player.transform.position + cameraOffset;

        // Di chuyển Camera từ vị trí hiện tại đến vị trí lý tưởng một cách mượt mà bằng Lerp
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPosition, cameraSmoothSpeed * Time.deltaTime);

        
    }
}
