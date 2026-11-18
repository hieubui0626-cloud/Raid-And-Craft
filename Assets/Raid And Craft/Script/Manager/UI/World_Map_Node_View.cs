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

    [Header("Hoặc đổi Sprite theo trạng thái (không bắt buộc)")]
    [Tooltip("Image sẽ đổi Sprite. Trống = Image trên chính nút này. Dùng khi ổ khoá/dấu hoàn thành nằm ngay trong Sprite của nút.")]
    [SerializeField] private Image stateImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite completeSprite;

    private World_Map_UI owner;
    private Button button;
    private bool checkedSetup;

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
        CheckSetupOnce();

        Sprite sprite = state == Map_Node_State.Complete ? completeSprite : state == Map_Node_State.Unlocked ? unlockedSprite : lockedSprite;
        if (sprite != null)
        {
            Image image = stateImage != null ? stateImage : GetComponent<Image>();
            if (image != null) image.sprite = sprite;
        }

        if (lockedVisual != null) lockedVisual.SetActive(state == Map_Node_State.Locked);
        if (unlockedVisual != null) unlockedVisual.SetActive(state == Map_Node_State.Unlocked);
        if (completeVisual != null) completeVisual.SetActive(state == Map_Node_State.Complete);
        if (currentVisual != null) currentVisual.SetActive(isCurrent);
    }

    // Báo lỗi cài đặt hay gặp (một lần mỗi nút) để khỏi phải đoán vì sao hình không đổi.
    private void CheckSetupOnce()
    {
        if (checkedSetup) return;
        checkedSetup = true;

        bool anyVisual = lockedVisual != null || unlockedVisual != null || completeVisual != null || currentVisual != null;
        bool anySprite = lockedSprite != null || unlockedSprite != null || completeSprite != null;
        if (!anyVisual && !anySprite)
        {
            Debug.LogWarning($"[{nameof(World_Map_Node_View)}] Nút '{name}' (id '{nodeId}') chưa gán Visual hay Sprite theo trạng thái nên hình sẽ không đổi (ổ khoá nằm sẵn trong Sprite của nút sẽ luôn hiện).", this);
        }

        CheckVisual(lockedVisual, "Locked Visual");
        CheckVisual(unlockedVisual, "Unlocked Visual");
        CheckVisual(completeVisual, "Complete Visual");
        CheckVisual(currentVisual, "Current Visual");

        if (lockedVisual != null && lockedVisual == unlockedVisual)
        {
            Debug.LogError($"[{nameof(World_Map_Node_View)}] Nút '{name}': Locked Visual và Unlocked Visual đang là cùng một object.", this);
        }
    }

    private void CheckVisual(GameObject visual, string field)
    {
        if (visual == null) return;

        if (!visual.scene.IsValid())
        {
            Debug.LogError($"[{nameof(World_Map_Node_View)}] Nút '{name}': {field} đang trỏ tới asset (Prefab) trong Project, không phải object trong Scene nên bật/tắt không có tác dụng. Kéo object con trong Hierarchy.", this);
        }
        else if (visual == gameObject || transform.IsChildOf(visual.transform))
        {
            Debug.LogError($"[{nameof(World_Map_Node_View)}] Nút '{name}': {field} là chính nút này hoặc object cha của nó. Phải là object CON (ổ khoá, dấu hoàn thành...).", this);
        }
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
