using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

/// <summary>
/// Chiến đấu của Player kiểu ARPG không cần chọn mục tiêu:
/// - GIỮ chuột phải = dùng đòn đánh mặc định (một Skill_Data có useWeaponDelivery) về phía con trỏ chuột, lặp theo ActionSpeed.
///   Cách đòn hoạt động (quạt cận chiến / bắn đạn) do vũ khí đang cầm quyết định (Character_Equipment).
/// - Chuột trái vẫn là di chuyển (Player_Controller); khi đang giữ chuột phải thì Player đứng yên để đánh.
/// - CastAtMouse(skill) dùng cho các skill khác sau này (slot 1-5, Q-T): chỉ cần gọi với skill tương ứng.
/// Gắn lên PlayerPrefab cùng Character_Stats, Skill_Caster, NavMeshAgent.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Skill_Caster))]
[RequireComponent(typeof(NavMeshAgent))]
public class Player_Combat : MonoBehaviour
{
    [Header("Đòn đánh mặc định")]
    [Tooltip("Skill dùng khi giữ chuột phải. Tạo asset Skill_Data, bật 'Use Weapon Delivery'.")]
    [SerializeField] private Skill_Data defaultAttack;

    [SerializeField] private float maxRayDistance = 200f;

    private Skill_Caster caster;
    private Character_Stats stats;
    private NavMeshAgent agent;
    private Camera mainCamera;

    private void Awake()
    {
        caster = GetComponent<Skill_Caster>();
        stats = GetComponent<Character_Stats>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (stats.IsDead || defaultAttack == null) return;

        Input_Manager input = Input_Manager.Instance;
        if (input == null || !input.RightClickHeld) return;

        // Đang trỏ vào UI thì không đánh.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        CastAtMouse(defaultAttack);
    }

    /// <summary>
    /// Thi triển skill về phía con trỏ chuột: dừng di chuyển, quay mặt về con trỏ, rồi ra đòn khi đã hết hồi chiêu.
    /// Trả về true nếu đòn đã được tung ra.
    /// </summary>
    public bool CastAtMouse(Skill_Data skill)
    {
        Input_Manager input = Input_Manager.Instance;
        if (input == null || skill == null) return false;

        if (!TryGetAimPoint(input.MousePosition, out Vector3 aimPoint)) return false;

        // Đứng yên để đánh (đang chạy mà giữ chuột phải thì dừng lại).
        if (agent.isOnNavMesh && agent.hasPath) agent.ResetPath();

        FaceTowards(aimPoint);
        return caster.TryCast(skill, aimPoint);
    }

    /// <summary>
    /// Điểm trong thế giới dưới con trỏ, đo trên mặt phẳng ngang ngang tầm chân Player
    /// (không cần Layer mặt đất, đạn bay ngang nên đúng với camera nhìn từ trên xuống).
    /// </summary>
    public bool TryGetAimPoint(Vector2 screenPosition, out Vector3 aimPoint)
    {
        aimPoint = default;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.up, transform.position);

        if (!plane.Raycast(ray, out float distance) || distance > maxRayDistance) return false;

        aimPoint = ray.GetPoint(distance);
        return true;
    }

    // Quay mặt ngay cả khi đang chờ hồi chiêu, để Player luôn nhìn theo con trỏ lúc giữ chuột phải.
    private void FaceTowards(Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
