using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh sách MỌI vật phẩm của game, để tra theo itemId khi load save
/// (tạo asset: chuột phải Project -> Create -> Raid And Craft -> Item Database, rồi kéo hết Item_Data vào).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Item Database", fileName = "Item_Database")]
public class Item_Database : ScriptableObject
{
    [SerializeField] private List<Item_Data> items = new List<Item_Data>();

    private Dictionary<string, Item_Data> byId;

    public IReadOnlyList<Item_Data> Items => items;

    /// <summary>Tìm vật phẩm theo itemId. Trả về null nếu không có.</summary>
    public Item_Data Find(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        EnsureBuilt();
        return byId.TryGetValue(itemId, out Item_Data item) ? item : null;
    }

    private void OnEnable()
    {
        byId = null;
    }

    private void OnValidate()
    {
        byId = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Thu thập mọi vật phẩm trong project")]
    private void CollectAll()
    {
        items.Clear();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Item_Data"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<Item_Data>(path);
            if (item != null) items.Add(item);
        }

        byId = null;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[{nameof(Item_Database)}] Đã thu thập {items.Count} vật phẩm.", this);
    }
#endif

    private void EnsureBuilt()
    {
        if (byId != null) return;

        byId = new Dictionary<string, Item_Data>(items.Count);
        foreach (Item_Data item in items)
        {
            if (item == null) continue;

            if (byId.ContainsKey(item.ItemId))
            {
                Debug.LogWarning($"[{nameof(Item_Database)}] Trùng itemId '{item.ItemId}' ({item.name}).", this);
                continue;
            }
            byId[item.ItemId] = item;
        }
    }
}
