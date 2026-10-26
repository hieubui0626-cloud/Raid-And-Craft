using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một vùng hiệu ứng (zone) trên Magic Circuit: các node cùng zoneId thuộc vùng này.
/// Vùng giúp tăng hiệu ứng hoặc định hình build (ví dụ vùng "Chiến binh" làm rune mạnh hơn).
/// </summary>
[Serializable]
public class Circuit_Zone
{
    public string id = "Zone";
    public string displayName = "Zone";
    [Tooltip("Màu vùng khi vẽ UI sau này.")]
    public Color color = Color.white;

    [Tooltip("Rune lắp trong node thuộc vùng này mạnh thêm bao nhiêu. 0.25 = +25% giá trị chỉ số của rune.")]
    [Min(0f)] public float runeEffectBonus = 0f;
}

/// <summary>Một node của Magic Circuit: người chơi mở bằng điểm, mỗi node có một số ô (socket) để lắp vật phẩm.</summary>
[Serializable]
public class Circuit_Node
{
    [Tooltip("Khoá duy nhất của node, dùng để save/load và để node khác nối tới.")]
    public string id;

    [Tooltip("Vị trí vẽ trên bản đồ (UI sau này).")]
    public Vector2 position;

    [Tooltip("Các node nối với node này. Chỉ cần khai báo MỘT phía, hệ thống tự nối hai chiều.")]
    public List<string> neighborIds = new List<string>();

    [Tooltip("Số ô lắp vật phẩm của node này.")]
    [Min(0)] public int socketCount = 1;

    [Tooltip("Số điểm Magic Circuit cần để mở node.")]
    [Min(0)] public int unlockCost = 1;

    [Tooltip("Vùng hiệu ứng chứa node (khớp Circuit_Zone.id). Để trống = không thuộc vùng nào.")]
    public string zoneId;

    [Tooltip("Node xuất phát: tự mở miễn phí, không gỡ được. Cần đúng một node.")]
    public bool isStart;
}

/// <summary>
/// Bản đồ Magic Circuit (tạo asset: Create -> Raid And Craft -> Magic Circuit Layout).
/// Chứa toàn bộ node, đường nối và vùng hiệu ứng. Chỉ là dữ liệu; trạng thái (node nào đã mở, vật phẩm nào đã lắp) nằm ở Magic_Circuit.
/// Số node nên LỚN hơn số điểm tối đa của nhân vật, để người chơi phải chọn hướng đi.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Magic Circuit Layout", fileName = "Magic_Circuit_Layout")]
public class Magic_Circuit_Layout : ScriptableObject
{
    [SerializeField] private List<Circuit_Zone> zones = new List<Circuit_Zone>();
    [SerializeField] private List<Circuit_Node> nodes = new List<Circuit_Node>();

    private Dictionary<string, Circuit_Node> nodeById;
    private Dictionary<string, Circuit_Zone> zoneById;
    private Dictionary<string, List<string>> adjacency;
    private Circuit_Node startNode;

    public IReadOnlyList<Circuit_Node> Nodes => nodes;
    public IReadOnlyList<Circuit_Zone> Zones => zones;

    public Circuit_Node StartNode
    {
        get { EnsureBuilt(); return startNode; }
    }

    public bool TryGetNode(string id, out Circuit_Node node)
    {
        EnsureBuilt();
        node = null;
        return !string.IsNullOrEmpty(id) && nodeById.TryGetValue(id, out node);
    }

    /// <summary>Các node nối với node này (đã nối hai chiều).</summary>
    public IReadOnlyList<string> GetNeighbors(string id)
    {
        EnsureBuilt();
        if (!string.IsNullOrEmpty(id) && adjacency.TryGetValue(id, out List<string> list)) return list;
        return Array.Empty<string>();
    }

    /// <summary>Vùng theo id, null nếu không có.</summary>
    public Circuit_Zone GetZone(string zoneId)
    {
        EnsureBuilt();
        return !string.IsNullOrEmpty(zoneId) && zoneById.TryGetValue(zoneId, out Circuit_Zone zone) ? zone : null;
    }

    private void OnEnable()
    {
        Invalidate();
    }

    private void OnValidate()
    {
        Invalidate();
        EnsureBuilt(); // In cảnh báo ngay khi chỉnh dữ liệu sai (trùng id, nối tới node không có, thiếu node xuất phát).
    }

    private void Invalidate()
    {
        nodeById = null;
        zoneById = null;
        adjacency = null;
        startNode = null;
    }

    private void EnsureBuilt()
    {
        if (nodeById != null) return;

        nodeById = new Dictionary<string, Circuit_Node>(nodes.Count);
        zoneById = new Dictionary<string, Circuit_Zone>(zones.Count);
        adjacency = new Dictionary<string, List<string>>(nodes.Count);
        startNode = null;

        foreach (Circuit_Zone zone in zones)
        {
            if (zone == null || string.IsNullOrEmpty(zone.id)) continue;

            if (!zoneById.TryAdd(zone.id, zone))
            {
                Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Trùng zone id '{zone.id}'.", this);
            }
        }

        foreach (Circuit_Node node in nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id)) continue;

            if (!nodeById.TryAdd(node.id, node))
            {
                Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Trùng node id '{node.id}'.", this);
                continue;
            }

            adjacency[node.id] = new List<string>();

            if (node.isStart)
            {
                if (startNode != null) Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Có nhiều hơn một node xuất phát ('{startNode.id}', '{node.id}'). Dùng '{startNode.id}'.", this);
                else startNode = node;
            }

            if (!string.IsNullOrEmpty(node.zoneId) && !zoneById.ContainsKey(node.zoneId))
            {
                Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Node '{node.id}' thuộc zone '{node.zoneId}' chưa khai báo.", this);
            }
        }

        // Nối hai chiều: chỉ cần khai báo một phía.
        foreach (Circuit_Node node in nodeById.Values)
        {
            foreach (string neighborId in node.neighborIds)
            {
                if (string.IsNullOrEmpty(neighborId) || neighborId == node.id) continue;

                if (!nodeById.ContainsKey(neighborId))
                {
                    Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Node '{node.id}' nối tới '{neighborId}' không tồn tại.", this);
                    continue;
                }

                if (!adjacency[node.id].Contains(neighborId)) adjacency[node.id].Add(neighborId);
                if (!adjacency[neighborId].Contains(node.id)) adjacency[neighborId].Add(node.id);
            }
        }

        if (startNode == null && nodeById.Count > 0)
        {
            Debug.LogWarning($"[{nameof(Magic_Circuit_Layout)}] Chưa có node xuất phát (isStart).", this);
        }
    }
}
