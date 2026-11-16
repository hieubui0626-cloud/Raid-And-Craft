using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Kéo (pan) và phóng to/thu nhỏ (zoom) nội dung của canvas World Map. Gắn lên Viewport (khung cắt, có Image để nhận chuột);
/// World_Map_UI tự thêm nếu thiếu. Content là object chứa ảnh nền + các nút node (World_Map_Node_View).
/// - Kéo: giữ chuột trái (hoặc giữa/phải) rồi kéo trên nền hoặc trên node. Nút node vẫn bấm được vì bấm không kéo thì không tính là kéo.
/// - Zoom: lăn chuột, phóng quanh vị trí con trỏ.
/// - Giới hạn: Content không bao giờ trượt khỏi Viewport (Content nhỏ hơn Viewport thì căn giữa).
/// </summary>
[DisallowMultipleComponent]
public class Map_Pan_Zoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [SerializeField] private RectTransform content;

    [Header("Zoom")]
    [SerializeField, Min(0.05f)] private float minZoom = 0.6f;
    [SerializeField, Min(0.05f)] private float maxZoom = 2.5f;

    [Tooltip("Mỗi nấc lăn chuột nhân thêm hệ số này (1.15 = 15%).")]
    [SerializeField, Min(1.01f)] private float zoomStep = 1.15f;

    private RectTransform viewport;

    public float Zoom => content != null ? content.localScale.x : 1f;

    private void Awake()
    {
        viewport = (RectTransform)transform;
    }

    /// <summary>World_Map_UI gọi khi dựng, nếu Content gán ở đây khác với ở World_Map_UI.</summary>
    public void Configure(RectTransform contentRect, float min, float max)
    {
        content = contentRect;
        minZoom = Mathf.Min(min, max);
        maxZoom = Mathf.Max(min, max);
        if (viewport == null) viewport = (RectTransform)transform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Không cần làm gì: phải có để Unity coi object này là bộ xử lý kéo (OnDrag mới gọi).
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (content == null) return;

        // delta tính theo pixel màn hình; chia cho scaleFactor của Canvas để ra đơn vị UI.
        Canvas canvas = content.GetComponentInParent<Canvas>();
        float factor = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / Mathf.Max(0.0001f, factor);

        content.localPosition += (Vector3)delta;
        Clamp();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (content == null || Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;

        // Chỉ xét hướng cuộn: giá trị scrollDelta khác nhau giữa các phiên bản Input System.
        float factor = eventData.scrollDelta.y > 0f ? zoomStep : 1f / zoomStep;
        ZoomAt(eventData.position, eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera, factor);
    }

    /// <summary>Phóng/thu theo hệ số, giữ nguyên điểm dưới con trỏ (screenPoint) đứng yên.</summary>
    public void ZoomAt(Vector2 screenPoint, Camera eventCamera, float factor)
    {
        float oldScale = content.localScale.x;
        float newScale = Mathf.Clamp(oldScale * factor, minZoom, maxZoom);
        if (Mathf.Approximately(oldScale, newScale)) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, eventCamera, out Vector2 pointInViewport)) return;

        // Điểm của Content (toạ độ riêng của Content) đang nằm dưới con trỏ.
        Vector2 pointInContent = ((Vector2)content.localPosition * -1f + pointInViewport) / oldScale;

        content.localScale = new Vector3(newScale, newScale, 1f);
        content.localPosition = pointInViewport - pointInContent * newScale;
        Clamp();
    }

    /// <summary>Đặt zoom (kẹp trong min..max) rồi căn giữa Viewport vào target (một RectTransform con của Content).</summary>
    public void FocusOn(RectTransform target, float zoom)
    {
        if (content == null || target == null) return;

        float scale = Mathf.Clamp(zoom, minZoom, maxZoom);
        content.localScale = new Vector3(scale, scale, 1f);

        // Vị trí của target trong toạ độ Content, rồi đưa nó về tâm Viewport.
        Vector3 inContent = content.InverseTransformPoint(target.position);
        Vector2 viewportCenter = viewport.rect.center;
        content.localPosition = viewportCenter - (Vector2)inContent * scale;
        Clamp();
    }

    /// <summary>Đặt zoom về mức cho trước và căn giữa Content vào Viewport.</summary>
    public void ResetView(float zoom = 1f)
    {
        if (content == null) return;

        float scale = Mathf.Clamp(zoom, minZoom, maxZoom);
        content.localScale = new Vector3(scale, scale, 1f);
        content.localPosition = Vector3.zero;
        Clamp();
    }

    // Giữ Content phủ kín Viewport; nhỏ hơn thì căn giữa.
    private void Clamp()
    {
        float scale = content.localScale.x;
        Rect view = viewport.rect;
        Rect local = content.rect; // toạ độ so với pivot của Content
        Vector3 pos = content.localPosition;

        pos.x = ClampAxis(pos.x, view.xMin, view.xMax, local.xMin * scale, local.xMax * scale);
        pos.y = ClampAxis(pos.y, view.yMin, view.yMax, local.yMin * scale, local.yMax * scale);
        content.localPosition = pos;
    }

    private static float ClampAxis(float position, float viewMin, float viewMax, float contentMin, float contentMax)
    {
        float contentSize = contentMax - contentMin;
        float viewSize = viewMax - viewMin;

        if (contentSize <= viewSize)
        {
            // Căn giữa.
            return (viewMin + viewMax) * 0.5f - (contentMin + contentMax) * 0.5f;
        }

        // Cạnh trái/dưới của Content không vượt quá cạnh trái/dưới của Viewport, và cạnh phải/trên không thụt vào trong.
        float low = viewMax - contentMax;
        float high = viewMin - contentMin;
        return Mathf.Clamp(position, low, high);
    }
}
