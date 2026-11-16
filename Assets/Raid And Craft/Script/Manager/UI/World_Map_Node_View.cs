using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên MỖI nút node trong canvas World Map (nút bạn tự vẽ). Chỉ cần điền Node Id trùng với id trong World_Map_Layout.
/// World_Map_UI tự tìm mọi World_Map_Node_View trong Content, nối nút bấm, tooltip và đổi hình theo trạng thái.
/// Hình theo trạng thái (đều KHÔNG bắt buộc, kéo object con tương ứng nếu có):
/// - Locked Visual: ổ khoá (Locked). - Unlocked Visual: node mở. - Complete Visual: dấu hoàn thành (chỉ Instance).
/// Nút cần một Graphic (Image) để nhận chuột; không có Button thì tự thêm.
/// </summary>
[DisallowMultipleComponent]
public class World_Map_Node_View : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Trùng với Id của node trong World_Map_Layout.")]
    [SerializeField] private string nodeId;

    [Header("Hình theo trạng thái (không bắt buộc)")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private GameObject completeVisual;

    [Tooltip("Bật khi Player đang đứng ở node này (viền sáng, v.v.). Không bắt buộc.")]
    [SerializeField] private GameObject currentVisual;

    private World_Map_UI owner;
    private Button button;

    public string NodeId => nodeId;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();
    }

    /// <summary>World_Map_UI gọi khi dựng: nối nút bấm về World_Map_UI.</summary>
    public void Initialize(World_Map_UI map)
    {
        owner = map;

        if (button == null) button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    /// <summary>World_Map_UI gọi mỗi lần làm mới: đổi hình theo trạng thái. isCurrent = Player đang ở node này.</summary>
    public void Apply(Map_Node_State state, bool isCurrent)
    {
        if (lockedVisual != null) lockedVisual.SetActive(state == Map_Node_State.Locked);
        if (unlockedVisual != null) unlockedVisual.SetActive(state == Map_Node_State.Unlocked);
        if (completeVisual != null) completeVisual.SetActive(state == Map_Node_State.Complete);
        if (currentVisual != null) currentVisual.SetActive(isCurrent);
    }

    private void HandleClick()
    {
        if (owner != null) owner.OnNodeClicked(nodeId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.OnNodeHover(nodeId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.OnNodeHoverEnd();
    }

    private void OnDisable()
    {
        // Node bị ẩn khi chuột đang trên nó thì sẽ không nhận PointerExit.
        if (owner != null) owner.OnNodeHoverEnd();
    }
}
