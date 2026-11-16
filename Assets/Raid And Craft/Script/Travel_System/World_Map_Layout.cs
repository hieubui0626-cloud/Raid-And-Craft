using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Loại node trên World Map. Thêm loại mới Ở CUỐI, không đổi số cũ.</summary>
public enum Map_Node_Type
{
    /// <summary>Căn cứ/thị trấn: Locked hoặc Unlocked. Có cổng Map để đi tới Base khác hoặc Instance nối với nó.</summary>
    Base = 0,

    /// <summary>Khu săn (roguelike mini nhiều tầng): Locked / Unlocked / Complete.</summary>
    Instance = 1,
}

/// <summary>Trạng thái node. Base chỉ có Locked/Unlocked; Instance có thêm Complete.</summary>
public enum Map_Node_State
{
    Locked = 0,
    Unlocked = 1,
    Complete = 2,
}

/// <summary>Một node trên World Map. Vị trí hiển thị do nút UI bạn tự đặt trong canvas (World_Map_Node_View.nodeId khớp id ở đây).</summary>
[Serializable]
public class Map_Node_Data
{
    [Tooltip("Khoá duy nhất. Nút UI (World_Map_Node_View) và save dùng id này.")]
    public string id;

    public string displayName = "Node";

    [TextArea(1, 3)] public string description;

    public Map_Node_Type type = Map_Node_Type.Instance;

    [Tooltip("Tên Scene của node (phải có trong Build Settings). Mỗi node một Scene riêng.")]
    public string sceneName;

    [Tooltip("Id của Scene_Spawn_Point nơi Player xuất hiện khi đi tới node này. Trống = điểm mặc định (Is Default).")]
    public string spawnId = "";

    [Tooltip("Node khởi đầu: mở sẵn từ đầu game. Cần đúng MỘT node, có thể là Base hoặc Instance.")]
    public bool isStart;

    [Tooltip("Các node nối với node này. Chỉ cần khai báo MỘT phía, hệ thống tự nối hai chiều.")]
    public List<string> neighborIds = new List<string>();

    [Header("Thông tin cho tooltip")]
    [Tooltip("Quy chế Scene của node (level, họ nguyên liệu). NÊN dùng đúng asset đã kéo vào Scene_Info của Scene đó. Trống = tooltip hiện Level '?'.")]
    public Scene_Rules rules;

    [Tooltip("Map modifier gắn vào node. Tooltip tính Level/Material đã áp modifier. (Việc áp modifier vào Scene lúc vào map làm sau.)")]
    public List<Scene_Rule_Modifier> modifiers = new List<Scene_Rule_Modifier>();

    public string DisplayNameOrId => string.IsNullOrEmpty(displayName) ? id : displayName;

    /// <summary>Quy chế đã áp modifier của node; null nếu node chưa có Scene_Rules.</summary>
    public Scene_Rule_Values BuildValues()
    {
        if (rules == null) return null;

        var values = new Scene_Rule_Values(rules);
        foreach (Scene_Rule_Modifier modifier in modifiers)
        {
            if (modifier != null) modifier.Apply(values);
        }
        return values;
    }
}

/// <summary>
/// Bản đồ thế giới (tạo asset: Create -> Raid And Craft -> World Map Layout). Chỉ là DỮ LIỆU: node, đường nối, loại.
/// <summary>Trạng thái node. Base chỉ có Locked/Unlocked; Instance có thêm Complete.</summary>
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/World Map Layout", fileName = "World_Map_Layout")]
public class World_Map_Layout : ScriptableObject
{
    [SerializeField] private List<Map_Node_Data> nodes = new List<Map_Node_Data>();

    private Dictionary<string, Map_Node_Data> nodeById;
    private Dictionary<string, List<string>> adjacency;
    private Map_Node_Data startNode;

    public IReadOnlyList<Map_Node_Data> Nodes => nodes;

    public Map_Node_Data StartNode
    {
        get { EnsureBuilt(); return startNode; }
    }

    public bool TryGetNode(string id, out Map_Node_Data node)
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

    public bool AreConnected(string a, string b)
    {
        EnsureBuilt();
        return !string.IsNullOrEmpty(a) && adjacency.TryGetValue(a, out List<string> list) && list.Contains(b);
    }

    /// <summary>Node ứng với một Scene (so tên Scene). Null nếu Scene không nằm trong bản đồ.</summary>
    public Map_Node_Data FindBySceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;

        foreach (Map_Node_Data node in nodes)
        {
            if (node != null && node.sceneName == sceneName) return node;
        }
        return null;
    }

    private void OnEnable()
    {
        Invalidate();
    }

    private void OnValidate()
    {
        Invalidate();
        EnsureBuilt(); // In cảnh báo ngay khi chỉnh dữ liệu sai.
    }

    private void Invalidate()
    {
        nodeById = null;
        adjacency = null;
        startNode = null;
    }

    private void EnsureBuilt()
    {
        if (nodeById != null) return;

        nodeById = new Dictionary<string, Map_Node_Data>(nodes.Count);
        adjacency = new Dictionary<string, List<string>>(nodes.Count);
        startNode = null;

        foreach (Map_Node_Data node in nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id)) continue;

            if (!nodeById.TryAdd(node.id, node))
            {
                Debug.LogWarning($"[{nameof(World_Map_Layout)}] Trùng node id '{node.id}'.", this);
                continue;
            }

            adjacency[node.id] = new List<string>();

            if (node.isStart)
            {
                if (startNode != null) Debug.LogWarning($"[{nameof(World_Map_Layout)}] Có nhiều hơn một node xuất phát ('{startNode.id}', '{node.id}'). Dùng '{startNode.id}'.", this);
                else startNode = node;
            }

            if (string.IsNullOrEmpty(node.sceneName))
            {
                Debug.LogWarning($"[{nameof(World_Map_Layout)}] Node '{node.id}' chưa điền Scene Name.", this);
            }
        }

        // Nối hai chiều: chỉ cần khai báo một phía.
        foreach (Map_Node_Data node in nodeById.Values)
        {
            foreach (string neighborId in node.neighborIds)
            {
                if (string.IsNullOrEmpty(neighborId) || neighborId == node.id) continue;

                if (!nodeById.ContainsKey(neighborId))
                {
                    Debug.LogWarning($"[{nameof(World_Map_Layout)}] Node '{node.id}' nối tới '{neighborId}' không tồn tại.", this);
                    continue;
                }

                if (!adjacency[node.id].Contains(neighborId)) adjacency[node.id].Add(neighborId);
                if (!adjacency[neighborId].Contains(node.id)) adjacency[neighborId].Add(node.id);
            }
        }   

        if (startNode == null && nodeById.Count > 0)
        {
            Debug.LogWarning($"[{nameof(World_Map_Layout)}] Chưa có node xuất phát (isStart).", this);
        }
        
    }
}
