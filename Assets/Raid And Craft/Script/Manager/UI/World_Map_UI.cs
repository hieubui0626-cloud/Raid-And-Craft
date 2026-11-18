using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Canvas World Map (thay Map_Panel_UI cũ). Bạn tự chuẩn bị hình nền và các nút node; script này lo mở/đóng, pan, zoom,
/// tooltip, đổi hình node theo trạng thái, đánh dấu vị trí Player và đi tới node khi bấm.
///
/// Cấu trúc trong Canvas (đặt World_Map_UI lên object LUÔN BẬT, ví dụ gốc của Canvas; Map_Gate tìm qua World_Map_UI.Instance):
///   World_Map_UI (script này)
///   - Panel Root: object con bật/tắt khi mở/đóng map (nền mờ, nút Đóng...). Kéo vào ô Panel Root.
///     - Viewport: khung cắt (Image + RectMask2D). Kéo vào ô Viewport. Map_Pan_Zoom tự thêm vào đây.
///       - Content: ảnh nền map + mọi nút node (mỗi nút gắn World_Map_Node_View). Kéo vào ô Content.
///         - Player Marker: hình mũi tên/Sprite chỉ vị trí Player (để trống Sprite, bạn tự gán sau). Kéo vào ô Player Marker.
///
/// Hai chế độ mở:
/// - Qua Map_Gate (Base) hoặc Map_Exit_Gate (Instance): chọn điểm đến, bấm node để đi.
/// - Phím M / nút Map_Button: chỉ XEM (có mũi tên vị trí hiện tại), bấm node không đi.
/// Đóng: phím M, Esc, hoặc đi tới node xong.
/// </summary>
[DisallowMultipleComponent]
public class World_Map_UI : MonoBehaviour
{
    private static World_Map_UI instance;

    /// <summary>World_Map_UI đang có trong Scene (null nếu Scene không có). Không tự tạo vì giao diện do bạn vẽ.</summary>
    public static World_Map_UI Instance => instance;

    /// <summary>Bản đồ đang mở, hoặc vừa đóng trong frame này (Esc đóng Map thì Pause_Menu không mở theo).</summary>
    public static bool IsOpenOrJustClosed => instance != null && (instance.IsOpen || instance.closedFrame == Time.frameCount);

    [Header("Giao diện (tự vẽ)")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Tooltip("Hình chỉ vị trí Player hiện tại (mũi tên hoặc Sprite bạn tự gán). Phải là con của Content. Trống = không đánh dấu.")]
    [SerializeField] private RectTransform playerMarker;

    [Tooltip("Dịch hình đánh dấu khỏi tâm node (đơn vị UI), ví dụ đặt mũi tên phía trên node.")]
    [SerializeField] private Vector2 markerOffset = Vector2.zero;

    [Tooltip("Chữ báo lý do không đi được (chưa mở, không nối với Base...). Không bắt buộc.")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("Nút Đóng (không bắt buộc, tự nối).")]
    [SerializeField] private Button closeButton;

    [Header("Pan / Zoom")]
    [SerializeField, Min(0.05f)] private float minZoom = 0.6f;
    [SerializeField, Min(0.05f)] private float maxZoom = 2.5f;
    [Tooltip("Mức zoom khi mở map (căn giữa vào vị trí Player).")]
    [SerializeField, Min(0.05f)] private float openZoom = 1f;

    private int closedFrame = -1;
    private bool diagnosed;
    private bool selectMode;
    private Player_Map_Progress progress;
    private Map_Pan_Zoom panZoom;
    private World_Map_Node_View[] views = System.Array.Empty<World_Map_Node_View>();

    public bool IsOpen => panelRoot != null && panelRoot.gameObject.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[{nameof(World_Map_UI)}] Scene có hai World_Map_UI, bỏ bản '{name}'.", this);
            enabled = false;
            return;
        }

        instance = this;

        if (panelRoot == null || viewport == null || content == null)
        {
            Debug.LogError($"[{nameof(World_Map_UI)}] Chưa kéo đủ Panel Root / Viewport / Content.", this);
            enabled = false;
            return;
        }

        panZoom = viewport.GetComponent<Map_Pan_Zoom>();
        if (panZoom == null) panZoom = viewport.gameObject.AddComponent<Map_Pan_Zoom>();
        panZoom.Configure(content, minZoom, maxZoom);

        views = content.GetComponentsInChildren<World_Map_Node_View>(true);
        foreach (World_Map_Node_View view in views) view.Initialize(this);

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        panelRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Unsubscribe();
    }

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        bool mPressed = input != null && input.IsPanelHotkeyPressed(Panel_Hotkey.WorldMap);

        if (IsOpen)
        {
            bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (mPressed || escPressed) Close();
        }
        else if (mPressed)
        {
            Open(false);
        }
    }

    // ------------------------------------------------------------------
    // MỞ / ĐÓNG
    // ------------------------------------------------------------------

    /// <summary>Mở map. selectDestination = true: bấm node để đi (từ Map_Gate); false: chỉ xem.</summary>
    public void Open(bool selectDestination)
    {
        if (panelRoot == null) return;

        Unsubscribe();
        progress = FindFirstObjectByType<Player_Map_Progress>();
        if (progress == null || progress.Layout == null)
        {
            Scene_Transition.ReportMessage("Player chưa có Player_Map_Progress (hoặc chưa gán World_Map_Layout) nên chưa mở được Map.");
            return;
        }

        progress.OnChanged += Refresh;
        selectMode = selectDestination;

        SetMessage(string.Empty);
        diagnosed = false;
        panelRoot.gameObject.SetActive(true);
        Refresh();
        FocusOnPlayer();
    }

    /// <summary>Nút Map_Button gọi: đang mở thì đóng, không thì mở chế độ xem.</summary>
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open(false);
    }

    public void Close()
    {
        Unsubscribe();
        Map_Node_Tooltip_UI.HideIfExists();
        closedFrame = Time.frameCount;
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (progress != null) progress.OnChanged -= Refresh;
        progress = null;
    }

    // ------------------------------------------------------------------
    // LÀM MỚI
    // ------------------------------------------------------------------

    private void Refresh()
    {
        if (progress == null) return;

        Map_Node_Data current = progress.CurrentNode;
        World_Map_Node_View currentView = null;

        foreach (World_Map_Node_View view in views)
        {
            if (view == null) continue;

            if (!progress.Layout.TryGetNode(view.NodeId, out Map_Node_Data node))
            {
                Debug.LogWarning($"[{nameof(World_Map_UI)}] Nút '{view.name}' có Node Id '{view.NodeId}' không có trong World_Map_Layout, ẩn nút.", view);
                view.gameObject.SetActive(false);
                continue;
            }

            bool isCurrent = current != null && current.id == node.id;
            if (isCurrent) currentView = view;
            view.Apply(progress.GetState(node.id), isCurrent);
        }

        DiagnoseCurrent(current, currentView);

        // Hình chỉ vị trí Player nằm đè lên node hiện tại; Scene không thuộc bản đồ thì ẩn.
        if (playerMarker != null)
        {
            bool show = currentView != null;
            playerMarker.gameObject.SetActive(show);
            if (show)
            {
                playerMarker.position = currentView.transform.position;
                playerMarker.anchoredPosition += markerOffset;
            }
        }
    }

    // Báo (một lần mỗi lần mở) vì sao hình đánh dấu Player / tự căn giữa không chạy.
    private void DiagnoseCurrent(Map_Node_Data current, World_Map_Node_View currentView)
    {
        if (diagnosed) return;
        diagnosed = true;

        string scene = progress.gameObject.scene.name;

        if (views.Length == 0)
        {
            Debug.LogWarning($"[{nameof(World_Map_UI)}] Không tìm thấy World_Map_Node_View nào bên trong Content. Nút node phải là con (ở bất kỳ cấp nào) của object Content.", this);
        }
        else if (current == null)
        {
            Debug.LogWarning($"[{nameof(World_Map_UI)}] Scene đang chơi '{scene}' không có node nào trong World_Map_Layout (so sánh Scene Name, phân biệt hoa thường) nên không đánh dấu Player và không tự căn giữa.", this);
        }
        else if (currentView == null)
        {
            Debug.LogWarning($"[{nameof(World_Map_UI)}] Scene '{scene}' ứng với node '{current.id}' nhưng canvas không có nút World_Map_Node_View nào mang Node Id '{current.id}' (Base chưa vẽ nút?). Sẽ căn giữa theo các node kề.", this);
        }

        if (playerMarker != null && !playerMarker.IsChildOf(content))
        {
            Debug.LogWarning($"[{nameof(World_Map_UI)}] Player Marker không nằm trong Content nên sẽ không đi theo khi kéo/zoom map. Kéo nó vào làm con của Content.", this);
        }
    }

    private void FocusOnPlayer()
    {
        if (panZoom == null) return;

        // Layout phải xong trước khi đo vị trí node.
        Canvas.ForceUpdateCanvases();

        Map_Node_Data current = progress != null ? progress.CurrentNode : null;
        if (current == null)
        {
            panZoom.ResetView(openZoom);
            return;
        }

        // Nút của node hiện tại; chưa vẽ nút (ví dụ Base) thì lấy trung bình các nút node kề.
        World_Map_Node_View target = FindView(current.id);
        if (target != null)
        {
            panZoom.FocusOn((RectTransform)target.transform, openZoom);
            return;
        }

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (string neighborId in progress.Layout.GetNeighbors(current.id))
        {
            World_Map_Node_View neighbor = FindView(neighborId);
            if (neighbor == null) continue;

            sum += neighbor.transform.position;
            count++;
        }

        if (count > 0) panZoom.FocusOnWorldPoint(sum / count, openZoom);
        else panZoom.ResetView(openZoom);
    }

    private World_Map_Node_View FindView(string nodeId)
    {
        foreach (World_Map_Node_View view in views)
        {
            if (view != null && view.NodeId == nodeId) return view;
        }
        return null;
    }

    // ------------------------------------------------------------------
    // NODE
    // ------------------------------------------------------------------

    public void OnNodeHover(string nodeId)
    {
        if (progress == null || !progress.Layout.TryGetNode(nodeId, out Map_Node_Data node)) return;

        Map_Node_Tooltip_UI.Instance.Show(node, progress.GetState(nodeId));
    }

    public void OnNodeHoverEnd()
    {
        Map_Node_Tooltip_UI.HideIfExists();
    }

    public void OnNodeClicked(string nodeId)
    {
        if (progress == null) return;

        if (!selectMode)
        {
            SetMessage("Dùng Cổng Map ở Base để di chuyển.");
            return;
        }

        if (progress.TryTravel(nodeId, out string reason)) Close();
        else SetMessage(reason);
    }

    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }
}
