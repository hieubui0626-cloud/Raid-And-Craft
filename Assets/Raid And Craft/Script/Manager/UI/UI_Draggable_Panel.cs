using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Cho phép kéo một panel UI bằng cách GIỮ chuột trái và rê. Gắn lên CHÍNH panel (object có RectTransform, bật lúc mở bằng hotkey).
/// - Chỉ kéo khi bấm vào phần "trống" của panel; bấm vào nút, ô lưới, thanh cuộn... thì không kéo (các thành phần đó tự xử lý click).
/// - Header Height &gt; 0: chỉ kéo được ở dải đó (tính từ mép trên panel, đơn vị pixel canvas). 0 = kéo ở bất kỳ chỗ trống nào.
/// - Panel luôn bị giữ trong màn hình; bấm vào panel thì nó nổi lên trên các panel cùng Canvas (Bring To Front).
/// - Không kéo khi đang cầm vật phẩm trên con trỏ.
/// Dùng polling chuột thay vì IDragHandler để không cướp click của các nút/ô con.
/// </summary>
[DisallowMultipleComponent]
public class UI_Draggable_Panel : MonoBehaviour
{
    [Tooltip("Panel được kéo. Bỏ trống = chính object này.")]
    [SerializeField] private RectTransform target;

    [Tooltip("Chiều cao dải kéo ở mép trên (pixel canvas). 0 = kéo ở bất kỳ phần trống nào của panel.")]
    [SerializeField, Min(0f)] private float headerHeight = 0f;

    [SerializeField] private bool clampToScreen = true;
    [SerializeField] private bool bringToFront = true;

    private static readonly List<RaycastResult> Results = new List<RaycastResult>();

    private Canvas canvas;
    private bool dragging;
    private Vector2 lastMouse;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
    }

    private void OnDisable()
    {
        dragging = false;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || target == null) return;

        if (mouse.leftButton.wasPressedThisFrame) TryBegin(mouse.position.ReadValue());

        if (!dragging) return;

        if (!mouse.leftButton.isPressed)
        {
            dragging = false;
            return;
        }

        Vector2 now = mouse.position.ReadValue();
        Vector2 delta = now - lastMouse;
        lastMouse = now;
        if (delta == Vector2.zero) return;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? Mathf.Max(0.0001f, canvas.scaleFactor) : 1f;

        target.anchoredPosition += delta / scale;
        if (clampToScreen) Clamp(scale);
    }

    private void TryBegin(Vector2 screen)
    {
        if (Item_Drag_Controller.IsHoldingItem) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        Results.Clear();
        eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screen }, Results);
        if (Results.Count == 0) return;

        // Phần tử trên cùng dưới con trỏ phải thuộc panel này và không tự xử lý click/kéo.
        GameObject top = Results[0].gameObject;
        if (!top.transform.IsChildOf(target)) return;
        if (IsInteractive(top, target)) return;

        if (headerHeight > 0f)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // 1 = trên-trái
            if (screen.y < corners[1].y - headerHeight * scale) return;
        }

        if (bringToFront) target.SetAsLastSibling();

        dragging = true;
        lastMouse = screen;
    }

    private static bool IsInteractive(GameObject go, Transform root)
    {
        for (Transform t = go.transform; t != null; t = t.parent)
        {
            var behaviours = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour b = behaviours[i];
                if (b == null || b is UI_Draggable_Panel) continue;
                if (b is IPointerClickHandler || b is IPointerDownHandler || b is IDragHandler || b is IScrollHandler) return true;
            }
            if (t == root) break;
        }
        return false;
    }

    // Giữ panel trong màn hình (so sánh bằng toạ độ màn hình, đúng với mọi anchor/pivot).
    private void Clamp(float scale)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);

        float dx = 0f, dy = 0f;
        if (corners[0].x < 0f) dx = -corners[0].x;
        else if (corners[2].x > Screen.width) dx = Screen.width - corners[2].x;

        if (corners[0].y < 0f) dy = -corners[0].y;
        else if (corners[2].y > Screen.height) dy = Screen.height - corners[2].y;

        if (dx != 0f || dy != 0f) target.anchoredPosition += new Vector2(dx, dy) / scale;
    }
}
