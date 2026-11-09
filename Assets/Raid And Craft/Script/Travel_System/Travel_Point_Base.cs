using TMPro;
using UnityEngine;

/// <summary>
/// Gốc của các vật bấm được trong thế giới để đi/dịch chuyển (Travel_Gate, Fast_Travel_Point): click chuột trái, Player đi tới rồi gọi OnInteract.
/// Lo phần dùng chung: Collider (tự thêm trigger nếu chưa có) và thẻ tên nổi giống Npc_Shop.
/// </summary>
public abstract class Travel_Point_Base : MonoBehaviour, IWorld_Interactable
{
    [SerializeField] private string displayName = "Travel Point";

    [Tooltip("Khoảng cách (mét) Player phải tới để kích hoạt.")]
    [SerializeField, Min(0.5f)] private float interactRange = 2.5f;

    [Header("Thẻ tên")]
    [SerializeField] private bool showLabel = true;
    [SerializeField, Min(0f)] private float labelHeight = 2.4f;
    [SerializeField, Min(1f)] private float labelSize = 5f;

    private TextMeshPro label;
    private Camera cam;

    public string DisplayName => displayName;
    public Vector3 Position => transform.position;
    public float InteractRange => interactRange;

    protected virtual Color LabelColor => new Color(0.6f, 0.85f, 1f);

    public void Interact(World_Item_Picker interactor)
    {
        if (interactor == null) return;

        OnInteract(interactor);
    }

    protected abstract void OnInteract(World_Item_Picker interactor);

    protected virtual void Start()
    {
        BuildCollider();
        BuildLabel();
    }

    private void BuildCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        // Là trigger nên không cản Player/NavMesh. Cao tới thẻ tên để bấm vào chữ cũng được.
        float height = (showLabel ? labelHeight : 2f) + 0.4f;
        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2f, height, 2f);
        box.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private void BuildLabel()
    {
        if (!showLabel) return;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, labelHeight, 0f);
        ((RectTransform)labelObject.transform).sizeDelta = new Vector2(8f, 1f);

        label = labelObject.AddComponent<TextMeshPro>();
        label.text = displayName;
        label.fontSize = labelSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = LabelColor;
        label.outlineWidth = 0.25f;
        label.outlineColor = new Color32(0, 0, 0, 255);
    }

    // Thẻ tên luôn quay mặt về camera.
    private void LateUpdate()
    {
        if (label == null) return;

        if (cam == null) cam = Camera.main;
        if (cam != null) label.transform.rotation = cam.transform.rotation;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
