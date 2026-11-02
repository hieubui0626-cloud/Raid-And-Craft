using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Một mặt hàng của NPC: mua một lần được Count món với giá Price Coin.</summary>
[Serializable]
public struct Shop_Entry
{
    public Item_Data item;

    [Tooltip("Số lượng nhận được mỗi lần mua (nguyên liệu: 10 một lần...).")]
    [Min(1)] public int count;

    [Tooltip("Giá (Coin) cho mỗi lần mua.")]
    [Min(0)] public int price;
}

/// <summary>
/// NPC bán hàng đơn giản (để thử Inventory): click chuột trái vào NPC, Player đi tới và mở cửa hàng (Shop_UI).
/// Mua bằng Coin (Player_Wallet), hàng bán không giới hạn số lượng.
/// Cách tạo: GameObject trong Scene (kéo model của bạn làm con, hoặc để trống để dùng viên nang tạm), Add Component Npc_Shop,
/// điền tên và danh sách hàng (Entries). Chưa có Collider thì tự thêm BoxCollider (Is Trigger) bao cả model và thẻ tên.
/// Thẻ tên nổi phía trên NPC giống vật phẩm trên đất.
/// </summary>
[DisallowMultipleComponent]
public class Npc_Shop : MonoBehaviour, IWorld_Interactable
{
    [Header("NPC")]
    [SerializeField] private string npcName = "Merchant";

    [Tooltip("Khoảng cách (mét) Player phải tới để mở cửa hàng; đi xa hơn một chút thì cửa hàng tự đóng.")]
    [SerializeField, Min(0.5f)] private float interactRange = 2.5f;

    [Header("Hàng bán")]
    [SerializeField] private List<Shop_Entry> entries = new List<Shop_Entry>();

    [Header("Thẻ tên")]
    [SerializeField] private bool showLabel = true;
    [SerializeField, Min(0f)] private float labelHeight = 2.4f;
    [SerializeField, Min(1f)] private float labelSize = 5f;

    private TextMeshPro label;
    private Camera cam;

    public string NpcName => npcName;
    public IReadOnlyList<Shop_Entry> Entries => entries;
    public Vector3 Position => transform.position;
    public float InteractRange => interactRange;

    public void Interact(World_Item_Picker interactor)
    {
        if (interactor == null) return;

        Shop_UI.Instance.Open(this, interactor);
    }

    private void Start()
    {
        BuildVisual();
        BuildCollider();
        BuildLabel();
    }

    private void BuildVisual()
    {
        if (GetComponentInChildren<Renderer>() != null) return; // người dùng tự đặt model làm con

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Collider primitiveCollider = capsule.GetComponent<Collider>();
        if (primitiveCollider != null) Destroy(primitiveCollider);

        capsule.name = "Placeholder_Model";
        capsule.transform.SetParent(transform, false);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
    }

    private void BuildCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        // Cao tới thẻ tên để bấm vào chữ cũng mở được. Là trigger nên không cản Player/NavMesh.
        float height = (showLabel ? labelHeight : 2f) + 0.4f;
        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.2f, height, 1.2f);
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
        label.text = npcName;
        label.fontSize = labelSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.82f, 0.5f);
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
}
