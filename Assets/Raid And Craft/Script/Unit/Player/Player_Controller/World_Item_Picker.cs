using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tương tác với thế giới của Player: nhặt vật phẩm trên đất, nói chuyện với NPC bán hàng (mọi IWorld_Interactable),
/// và thả vật phẩm xuống đất. Player_Controller tự thêm component này nếu chưa có.
/// - Click vào IWorld_Interactable (World_Item, Npc_Shop): Player đi tới, đủ gần thì tương tác (nhặt / mở cửa hàng).
/// - Đang cầm vật phẩm trên con trỏ (nhấc từ Inventory/ô trang bị) mà click ra thế giới: thả xuống đất,
///   tại điểm click nhưng không xa quá Max Drop Distance quanh Player.
/// Cũng tự thêm Player_Wallet (ví Coin) nếu Player chưa có.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class World_Item_Picker : MonoBehaviour
{
    private const int HitBufferSize = 16;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

    [Tooltip("Kho nhận vật phẩm. Bỏ trống thì lấy Inventory cùng object.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Thả vật phẩm xa Player tối đa bao nhiêu mét.")]
    [SerializeField, Min(0.5f)] private float maxDropDistance = 3f;

    [SerializeField] private float maxRayDistance = 200f;

    private NavMeshAgent agent;
    private Player_Wallet wallet;
    private IWorld_Interactable target;

    public Inventory Inventory => inventory;
    public Player_Wallet Wallet => wallet;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (inventory == null) inventory = GetComponent<Inventory>();

        wallet = GetComponent<Player_Wallet>();
        if (wallet == null) wallet = gameObject.AddComponent<Player_Wallet>();
    }

    private void Update()
    {
        if (target == null) return;

        // Mục tiêu đã bị huỷ (vật phẩm được nhặt, NPC bị xoá).
        if (target is Object unityObject && unityObject == null)
        {
            target = null;
            return;
        }

        float range = Mathf.Max(target.InteractRange, agent.stoppingDistance + 0.3f);
        Vector3 delta = target.Position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > range * range) return;

        IWorld_Interactable arrived = target;
        target = null;
        if (agent.isOnNavMesh) agent.ResetPath();
        arrived.Interact(this);
    }

    // ------------------------------------------------------------------
    // TƯƠNG TÁC
    // ------------------------------------------------------------------

    /// <summary>Tia chuột có trúng một vật tương tác được không (lấy cái gần nhất; bỏ qua các vật cản khác).</summary>
    public bool TryGetInteractable(Ray ray, out IWorld_Interactable interactable)
    {
        interactable = null;

        int hits = Physics.RaycastNonAlloc(ray, HitBuffer, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;

        for (int i = 0; i < hits; i++)
        {
            if (HitBuffer[i].distance >= best) continue;

            IWorld_Interactable candidate = HitBuffer[i].collider.GetComponentInParent<IWorld_Interactable>();
            if (candidate == null) continue;

            best = HitBuffer[i].distance;
            interactable = candidate;
        }

        return interactable != null;
    }

    /// <summary>Đi tới vật tương tác và tương tác khi tới nơi.</summary>
    public void SetTarget(IWorld_Interactable interactable)
    {
        if (interactable == null) return;
        target = interactable;

        if (!agent.isOnNavMesh) return;

        Vector3 destination = interactable.Position;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas)) destination = hit.position;
        agent.SetDestination(destination);
    }

    /// <summary>Huỷ việc đi tới (Player_Controller gọi khi bạn click đi chỗ khác).</summary>
    public void CancelTarget()
    {
        target = null;
    }

    // ------------------------------------------------------------------
    // THẢ
    // ------------------------------------------------------------------

    /// <summary>Thả thứ đang cầm trên con trỏ xuống đất theo hướng tia chuột. False nếu tay trống.</summary>
    public bool DropHeld(Ray ray)
    {
        Item_Drag_Controller drag = Item_Drag_Controller.Instance;
        Grid_Placement held = drag.Held;
        if (held.IsEmpty) return false;

        Vector3 position = transform.position + transform.forward * 1.2f;

        var plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float distance) && distance <= maxRayDistance)
        {
            Vector3 offset = ray.GetPoint(distance) - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > maxDropDistance * maxDropDistance) offset = offset.normalized * maxDropDistance;
            position = transform.position + offset;
        }

        // Đặt lên mặt NavMesh để vật phẩm nằm đúng mặt đất và Player tới nhặt được.
        position = NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : transform.position;

        World_Item.Spawn(held.Item, held.Count, held.Instance, position);
        drag.Clear();
        return true;
    }
}
