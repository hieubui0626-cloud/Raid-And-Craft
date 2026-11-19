using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiến trình World Map của Player (gắn lên Player prefab, thay Player_Fast_Travel). Gồm:
/// - Trạng thái node: CHỈ lưu danh sách Instance đã Complete; Unlocked/Locked được SUY RA mỗi lần (nên sửa Layout không làm hỏng save).
///   Luật mở khoá:
///     * Node xuất phát (isStart, Base hoặc Instance) mở sẵn.
///     * Instance mở khi nối với một Base đã Unlocked, hoặc nối với một Instance đã Complete.
///     * Base mở khi nối với một Instance đã Complete.
/// - Di chuyển (TryTravel): đứng ở Base (Map_Gate) hoặc Instance (Map_Exit_Gate) đều mở được map. Tới Base đã Unlocked (bất kể xa gần),
///   hoặc tới Instance Unlocked/Complete NỐI TRỰC TIẾP với node hiện tại.
/// - Hoàn thành Instance (ReportBossDefeated, do Map_Boss_Objective gọi khi boss cuối chết): đánh dấu Complete, lưu, báo node mới mở và MỞ KHOÁ cổng thoát
///   (Map_Exit_Gate khoá cho tới lúc này). Không tự về Base: người chơi bấm cổng thoát rồi chọn điểm đến trên map.
/// Node hiện tại = node có Scene Name trùng Scene đang chơi.
/// Player_Save lưu CompletedNodeIds và nạp bằng RestoreCompleted.
/// </summary>
[DisallowMultipleComponent]
public class Player_Map_Progress : MonoBehaviour
{
    /// <summary>Bắn khi trạng thái node đổi (hoàn thành Instance, nạp save). UI Map vẽ lại.</summary>
    public event Action OnChanged;

    [SerializeField] private World_Map_Layout layout;

    // Vừa chuyển sang Base khác: lưu lại ở Scene mới để save ghi đúng Scene base mới (Scene_Transition.Travel chỉ lưu ở Scene cũ).
    private static bool pendingArrivalSave;

    private readonly HashSet<string> completed = new HashSet<string>();
    private readonly HashSet<string> unlocked = new HashSet<string>();

    public World_Map_Layout Layout => layout;

    /// <summary>Id các Instance đã Complete (để Player_Save lưu).</summary>
    public IEnumerable<string> CompletedNodeIds => completed;

    /// <summary>
    /// Cổng thoát (Map_Exit_Gate) của Scene hiện tại đã mở chưa: Instance phải Complete mới mở.
    /// Scene không thuộc bản đồ hoặc không phải Instance thì không khoá.
    /// </summary>
    public bool IsExitOpen
    {
        get
        {
            Map_Node_Data node = CurrentNode;
            return node == null || node.type != Map_Node_Type.Instance || completed.Contains(node.id);
        }
    }

    /// <summary>Node ứng với Scene đang chơi (null nếu Scene không nằm trong bản đồ).</summary>
    public Map_Node_Data CurrentNode => layout != null ? layout.FindBySceneName(gameObject.scene.name) : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pendingArrivalSave = false;
    }

    private void Awake()
    {
        if (layout == null)
        {
            Debug.LogError($"[{nameof(Player_Map_Progress)}] Chưa gán World_Map_Layout.", this);
            return;
        }

        Recompute();
    }

    private IEnumerator Start()
    {
        if (layout != null && CurrentNode == null)
        {
            Debug.LogWarning($"[{nameof(Player_Map_Progress)}] Scene '{gameObject.scene.name}' không có node nào trong World_Map_Layout (Scene Name phải trùng).", this);
        }

        if (!pendingArrivalSave) yield break;

        // Đợi Player_Save đăng ký và nạp xong rồi mới lưu, để file save ghi đúng Scene base mới.
        for (int i = 0; i < 3; i++) yield return null;

        pendingArrivalSave = false;
        if (Save_Manager.Instance != null) Save_Manager.Instance.SaveNow();
    }

    // ------------------------------------------------------------------
    // TRẠNG THÁI
    // ------------------------------------------------------------------

    public Map_Node_State GetState(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return Map_Node_State.Locked;
        if (completed.Contains(nodeId)) return Map_Node_State.Complete;
        return unlocked.Contains(nodeId) ? Map_Node_State.Unlocked : Map_Node_State.Locked;
    }

    /// <summary>Nạp save: thay toàn bộ danh sách Instance đã Complete (id không còn trong Layout hoặc không phải Instance bị bỏ).</summary>
    public void RestoreCompleted(IEnumerable<string> ids)
    {
        completed.Clear();

        if (ids != null && layout != null)
        {
            foreach (string id in ids)
            {
                if (layout.TryGetNode(id, out Map_Node_Data node) && node.type == Map_Node_Type.Instance) completed.Add(id);
            }
        }

        Recompute();
        OnChanged?.Invoke();
    }

    // Suy ra tập Unlocked từ node xuất phát + Instance đã Complete, lặp tới khi hết thay đổi (chỉ thêm nên luôn dừng).
    private void Recompute()
    {
        unlocked.Clear();
        if (layout == null) return;

        Map_Node_Data start = layout.StartNode;
        if (start != null) unlocked.Add(start.id);
        foreach (string id in completed) unlocked.Add(id);

        bool changed;
        do
        {
            changed = false;
            foreach (Map_Node_Data node in layout.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id) || unlocked.Contains(node.id)) continue;

                if (ShouldUnlock(node))
                {
                    unlocked.Add(node.id);
                    changed = true;
                }
            }
        }
        while (changed);
    }

    private bool ShouldUnlock(Map_Node_Data node)
    {
        foreach (string neighborId in layout.GetNeighbors(node.id))
        {
            if (!layout.TryGetNode(neighborId, out Map_Node_Data neighbor)) continue;

            bool neighborCompleteInstance = neighbor.type == Map_Node_Type.Instance && completed.Contains(neighborId);

            if (node.type == Map_Node_Type.Instance)
            {
                if (neighbor.type == Map_Node_Type.Base && unlocked.Contains(neighborId)) return true;
                if (neighborCompleteInstance) return true;
            }
            else if (neighborCompleteInstance)
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------
    // DI CHUYỂN
    // ------------------------------------------------------------------

    /// <summary>Đi tới node targetId được không (từ Scene hiện tại). reason: lý do khi không được.</summary>
    public bool CanTravel(string targetId, out string reason)
    {
        reason = string.Empty;

        if (layout == null || !layout.TryGetNode(targetId, out Map_Node_Data target))
        {
            reason = "This location does not exist on the map.";
            return false;
        }

        Map_Node_Data current = CurrentNode;
        if (current == null)
        {
            reason = "You are not on the world map.";
            return false;
        }

        if (current.id == target.id)
        {
            reason = "You are already here.";
            return false;
        }

        if (GetState(target.id) == Map_Node_State.Locked)
        {
            reason = "This location is locked.";
            return false;
        }

        if (target.type == Map_Node_Type.Instance && !layout.AreConnected(current.id, target.id))
        {
            reason = "This area is not connected to your current location.";
            return false;
        }

        if (Scene_Transition.IsTransitioning)
        {
            reason = "Already travelling.";
            return false;
        }

        return true;
    }

    /// <summary>Lưu rồi sang Scene của node (qua Scene_Transition). False kèm lý do nếu không đi được.</summary>
    public bool TryTravel(string targetId, out string reason)
    {
        if (!CanTravel(targetId, out reason)) return false;

        layout.TryGetNode(targetId, out Map_Node_Data target);

        if (target.type == Map_Node_Type.Base) pendingArrivalSave = true;
        if (!Scene_Transition.Travel(target.sceneName, target.spawnId))
        {
            pendingArrivalSave = false;
            reason = "Cannot travel (see Console).";
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // HOÀN THÀNH INSTANCE
    // ------------------------------------------------------------------

    /// <summary>
    /// Nhiệm vụ chung của Instance: boss cuối đã chết. Map_Boss_Objective gọi. Không làm gì nếu Scene hiện tại không phải Instance trong bản đồ.
    /// </summary>
    public void ReportBossDefeated()
    {
        Map_Node_Data node = CurrentNode;
        if (node == null || node.type != Map_Node_Type.Instance || completed.Contains(node.id)) return;

        var before = new HashSet<string>(unlocked);
        completed.Add(node.id);
        Recompute();

        var newlyUnlocked = new List<Map_Node_Data>();
        Map_Node_Data newBase = null;
        foreach (Map_Node_Data n in layout.Nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.id) || before.Contains(n.id) || !unlocked.Contains(n.id)) continue;

            newlyUnlocked.Add(n);
            if (newBase == null && n.type == Map_Node_Type.Base) newBase = n;
        }

        // Base vừa mở trở thành Base gần nhất: chết hay thoát giữa chừng thì lần sau cũng về đó.
        if (newBase != null && Save_Manager.Instance != null) Save_Manager.Instance.LastBaseScene = newBase.sceneName;
        if (Save_Manager.Instance != null) Save_Manager.Instance.SaveNow();

        string message = $"{node.DisplayNameOrId} complete! The exit gate is open.";
        if (newlyUnlocked.Count > 0)
        {
            var names = new List<string>();
            foreach (Map_Node_Data n in newlyUnlocked) names.Add(n.DisplayNameOrId);
            message += " Unlocked: " + string.Join(", ", names) + ".";
        }
        Scene_Transition.ReportMessage(message);

        OnChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // GỠ LỖI (chuột phải component khi đang Play)
    // ------------------------------------------------------------------

    [ContextMenu("Debug: Hoàn thành Instance hiện tại (như hạ boss cuối)")]
    private void DebugCompleteCurrent()
    {
        ReportBossDefeated();
    }

    [ContextMenu("Debug: Xoá toàn bộ tiến trình Map")]
    private void DebugResetProgress()
    {
        RestoreCompleted(null);
    }
}
