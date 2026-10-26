using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Magic Circuit của Player (gần giống passive tree của PoE, nhưng node chứa Ô TRỐNG để lắp vật phẩm).
/// - Mỗi cấp cho điểm (Player_Circuit_Points). Dùng điểm để mở node kề node đã mở; người chơi tự chọn hướng đi.
/// - Mỗi node có socketCount ô; MỌI ô nhận mọi Circuit_Item_Data (Rune cộng chỉ số, Spell Stone thêm skill...), lấy từ Inventory.
/// - Node thuộc vùng (zone) có thể làm Rune mạnh hơn (runeEffectBonus).
/// - Gỡ vật phẩm: trả về Inventory, có thể tốn phí (removalCostItem x removalCostCount, hiện để 0 = miễn phí).
/// - Gỡ node (respec từng node): chỉ khi các node còn lại vẫn nối với node xuất phát; vật phẩm trong node trả về Inventory, hoàn điểm.
/// Chưa lưu vào Save_Data (cần lưu: node đã mở + vật phẩm theo ô). Chưa có UI.
/// Gắn lên PlayerPrefab (cùng Character_Stats, Player_Skill_Slots, Player_Circuit_Points, Inventory).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
[RequireComponent(typeof(Player_Skill_Slots))]
[RequireComponent(typeof(Player_Circuit_Points))]
public class Magic_Circuit : MonoBehaviour
{
    // Khoá nguồn riêng cho từng ô: modifier của ô nào gỡ đúng của ô đó.
    private sealed class Socket_Source
    {
        public readonly string NodeId;
        public readonly int Index;

        public Socket_Source(string nodeId, int index)
        {
            NodeId = nodeId;
            Index = index;
        }
    }

    /// <summary>Bắn khi bất kỳ điều gì đổi (mở/gỡ node, lắp/gỡ vật phẩm). UI vẽ lại.</summary>
    public event Action OnCircuitChanged;
    public event Action<string> OnNodeUnlocked;
    public event Action<string> OnNodeLocked;

    /// <summary>Bắn khi một ô đổi vật phẩm (nodeId, chỉ số ô).</summary>
    public event Action<string, int> OnSocketChanged;

    [SerializeField] private Magic_Circuit_Layout layout;

    [Tooltip("Bỏ trống thì tự tìm trên cùng object.")]
    [SerializeField] private Inventory inventory;

    [Header("Phí gỡ vật phẩm khỏi ô (chừa sẵn, hiện miễn phí)")]
    [Tooltip("Vật phẩm bị trừ mỗi lần gỡ một vật phẩm khỏi ô. Bỏ trống = miễn phí.")]
    [SerializeField] private Item_Data removalCostItem;

    [Tooltip("Số lượng trừ mỗi lần gỡ. 0 = miễn phí.")]
    [SerializeField, Min(0)] private int removalCostCount = 0;

    private Character_Stats stats;
    private Player_Skill_Slots skillSlots;
    private Player_Circuit_Points points;

    private readonly HashSet<string> unlocked = new HashSet<string>();
    private readonly Dictionary<string, Item_Data[]> sockets = new Dictionary<string, Item_Data[]>();
    private readonly Dictionary<string, Socket_Source[]> sources = new Dictionary<string, Socket_Source[]>();

    // Đếm tham chiếu skill: hai Spell Stone cùng skill thì chỉ mất skill khi gỡ cái cuối.
    private readonly Dictionary<Skill_Data, int> skillRefs = new Dictionary<Skill_Data, int>();
    // Chỉ những skill do Magic Circuit thêm mới bị gỡ (skill có sẵn từ nguồn khác thì giữ).
    private readonly HashSet<Skill_Data> learnedByCircuit = new HashSet<Skill_Data>();

    public Magic_Circuit_Layout Layout => layout;
    public Character_Stats Stats => stats;
    public int UnlockedCount => unlocked.Count;
    public int RemovalCostCount => removalCostCount;
    public Item_Data RemovalCostItem => removalCostItem;

    private void Awake()
    {
        stats = GetComponent<Character_Stats>();
        skillSlots = GetComponent<Player_Skill_Slots>();
        points = GetComponent<Player_Circuit_Points>();
        if (inventory == null) inventory = GetComponent<Inventory>();

        if (layout == null || inventory == null)
        {
            Debug.LogError($"[{nameof(Magic_Circuit)}] Thiếu Layout hoặc Inventory.", this);
            enabled = false;
            return;
        }

        Circuit_Node start = layout.StartNode;
        if (start == null)
        {
            Debug.LogError($"[{nameof(Magic_Circuit)}] Layout chưa có node xuất phát.", this);
            enabled = false;
            return;
        }

        UnlockInternal(start);
    }

    // ------------------------------------------------------------------
    // ĐỌC
    // ------------------------------------------------------------------

    public bool IsUnlocked(string nodeId)
    {
        return nodeId != null && unlocked.Contains(nodeId);
    }

    /// <summary>Vật phẩm đang lắp ở ô (null nếu trống hoặc ô/node không hợp lệ).</summary>
    public Item_Data GetSocketItem(string nodeId, int socketIndex)
    {
        if (nodeId == null || !sockets.TryGetValue(nodeId, out Item_Data[] arr)) return null;
        return socketIndex >= 0 && socketIndex < arr.Length ? arr[socketIndex] : null;
    }

    /// <summary>Hệ số hiệu ứng của vùng chứa node (1 = không có vùng/không bonus).</summary>
    public float GetEffectMultiplier(string nodeId)
    {
        if (!layout.TryGetNode(nodeId, out Circuit_Node node)) return 1f;

        Circuit_Zone zone = layout.GetZone(node.zoneId);
        return 1f + (zone != null ? zone.runeEffectBonus : 0f);
    }

    // ------------------------------------------------------------------
    // MỞ / GỠ NODE
    // ------------------------------------------------------------------

    /// <summary>Mở được khi: node tồn tại, chưa mở, kề một node đã mở, và đủ điểm.</summary>
    public bool CanUnlock(string nodeId)
    {
        if (!layout.TryGetNode(nodeId, out Circuit_Node node)) return false;
        if (unlocked.Contains(nodeId)) return false;
        if (points.AvailablePoints < node.unlockCost) return false;

        foreach (string neighborId in layout.GetNeighbors(nodeId))
        {
            if (unlocked.Contains(neighborId)) return true;
        }
        return false;
    }

    public bool TryUnlock(string nodeId)
    {
        if (!CanUnlock(nodeId)) return false;

        layout.TryGetNode(nodeId, out Circuit_Node node);
        if (node.unlockCost > 0 && !points.TrySpend(node.unlockCost)) return false;

        UnlockInternal(node);
        OnNodeUnlocked?.Invoke(nodeId);
        OnCircuitChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Gỡ node được khi: không phải node xuất phát, các node đã mở còn lại vẫn nối với node xuất phát,
    /// Inventory đủ chỗ nhận lại vật phẩm trong node, và đủ vật phẩm trả phí gỡ.
    /// </summary>
    public bool CanLock(string nodeId)
    {
        if (!layout.TryGetNode(nodeId, out Circuit_Node node)) return false;
        if (node.isStart || !unlocked.Contains(nodeId)) return false;
        if (!RemainsConnectedWithout(nodeId)) return false;

        List<Item_Data> filled = CollectSocketItems(nodeId);
        return CanPayRemovalCost(filled.Count) && inventory.CanAddAll(filled);
    }

    public bool TryLock(string nodeId)
    {
        if (!CanLock(nodeId)) return false;

        layout.TryGetNode(nodeId, out Circuit_Node node);

        Item_Data[] arr = sockets[nodeId];
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null) UnsocketInternal(nodeId, i);
        }

        unlocked.Remove(nodeId);
        sockets.Remove(nodeId);
        sources.Remove(nodeId);
        if (node.unlockCost > 0) points.Refund(node.unlockCost);

        OnNodeLocked?.Invoke(nodeId);
        OnCircuitChanged?.Invoke();
        return true;
    }

    // ------------------------------------------------------------------
    // LẮP / GỠ VẬT PHẨM
    // ------------------------------------------------------------------

    public bool CanSocket(string nodeId, int socketIndex, Item_Data item)
    {
        if (!(item is Circuit_Item_Data)) return false;
        if (!sockets.TryGetValue(nodeId ?? string.Empty, out Item_Data[] arr)) return false;
        if (socketIndex < 0 || socketIndex >= arr.Length || arr[socketIndex] != null) return false;
        return inventory.Has(item);
    }

    /// <summary>Lấy 1 vật phẩm từ Inventory lắp vào ô trống. Chỉ số/skill áp dụng ngay.</summary>
    public bool TrySocket(string nodeId, int socketIndex, Item_Data item)
    {
        if (!CanSocket(nodeId, socketIndex, item)) return false;
        if (!inventory.TryRemove(item, 1)) return false;

        sockets[nodeId][socketIndex] = item;

        var source = new Socket_Source(nodeId, socketIndex);
        sources[nodeId][socketIndex] = source;
        ((Circuit_Item_Data)item).Apply(this, source, GetEffectMultiplier(nodeId));

        OnSocketChanged?.Invoke(nodeId, socketIndex);
        OnCircuitChanged?.Invoke();
        return true;
    }

    public bool CanUnsocket(string nodeId, int socketIndex)
    {
        Item_Data item = GetSocketItem(nodeId, socketIndex);
        return item != null && CanPayRemovalCost(1) && inventory.CanAdd(item, 1);
    }

    /// <summary>Gỡ vật phẩm khỏi ô, trả về Inventory (trừ phí gỡ nếu có). Chỉ số/skill thu hồi ngay.</summary>
    public bool TryUnsocket(string nodeId, int socketIndex)
    {
        if (!CanUnsocket(nodeId, socketIndex)) return false;

        UnsocketInternal(nodeId, socketIndex);
        OnCircuitChanged?.Invoke();
        return true;
    }

    // ------------------------------------------------------------------
    // DÀNH CHO Circuit_Item_Data (Rune/Spell Stone gọi)
    // ------------------------------------------------------------------

    public void AcquireSkill(Skill_Data skill)
    {
        if (skill == null) return;

        skillRefs.TryGetValue(skill, out int refs);
        skillRefs[skill] = refs + 1;

        if (refs == 0 && skillSlots.Learn(skill)) learnedByCircuit.Add(skill);
    }

    public void ReleaseSkill(Skill_Data skill)
    {
        if (skill == null || !skillRefs.TryGetValue(skill, out int refs)) return;

        if (refs > 1)
        {
            skillRefs[skill] = refs - 1;
            return;
        }

        skillRefs.Remove(skill);
        if (learnedByCircuit.Remove(skill)) skillSlots.Forget(skill);
    }

    // ------------------------------------------------------------------
    // NỘI BỘ
    // ------------------------------------------------------------------

    private void UnlockInternal(Circuit_Node node)
    {
        unlocked.Add(node.id);
        sockets[node.id] = new Item_Data[node.socketCount];
        sources[node.id] = new Socket_Source[node.socketCount];
    }

    // Gỡ không kiểm tra (đã kiểm ở CanUnsocket/CanLock): thu hồi tác động, trừ phí, trả vật phẩm về Inventory.
    private void UnsocketInternal(string nodeId, int socketIndex)
    {
        Item_Data item = sockets[nodeId][socketIndex];
        Socket_Source source = sources[nodeId][socketIndex];

        PayRemovalCost(1);
        ((Circuit_Item_Data)item).Remove(this, source);

        sockets[nodeId][socketIndex] = null;
        sources[nodeId][socketIndex] = null;
        inventory.Add(item, 1);

        OnSocketChanged?.Invoke(nodeId, socketIndex);
    }

    private List<Item_Data> CollectSocketItems(string nodeId)
    {
        var result = new List<Item_Data>();
        if (sockets.TryGetValue(nodeId, out Item_Data[] arr))
        {
            foreach (Item_Data item in arr)
            {
                if (item != null) result.Add(item);
            }
        }
        return result;
    }

    private bool CanPayRemovalCost(int times)
    {
        if (removalCostItem == null || removalCostCount <= 0 || times <= 0) return true;
        return inventory.Has(removalCostItem, removalCostCount * times);
    }

    private void PayRemovalCost(int times)
    {
        if (removalCostItem == null || removalCostCount <= 0 || times <= 0) return;
        inventory.TryRemove(removalCostItem, removalCostCount * times);
    }

    // Loại bỏ excludedId rồi duyệt từ node xuất phát: mọi node đã mở còn lại phải với tới được.
    private bool RemainsConnectedWithout(string excludedId)
    {
        Circuit_Node start = layout.StartNode;
        if (start == null) return false;

        var visited = new HashSet<string> { start.id };
        var queue = new Queue<string>();
        queue.Enqueue(start.id);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            foreach (string neighborId in layout.GetNeighbors(current))
            {
                if (neighborId == excludedId || !unlocked.Contains(neighborId)) continue;
                if (visited.Add(neighborId)) queue.Enqueue(neighborId);
            }
        }

        return visited.Count == unlocked.Count - 1;
    }

    [ContextMenu("Debug: Mở một node kề còn mở được")]
    private void DebugUnlockNextNode()
    {
        foreach (Circuit_Node node in layout.Nodes)
        {
            if (node != null && TryUnlock(node.id))
            {
                Debug.Log($"[{nameof(Magic_Circuit)}] Đã mở node '{node.id}'.", this);
                return;
            }
        }
        Debug.Log($"[{nameof(Magic_Circuit)}] Không có node nào mở được (hết điểm hoặc hết node kề).", this);
    }
}
