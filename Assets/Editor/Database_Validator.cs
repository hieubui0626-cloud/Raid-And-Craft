#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu Raid And Craft -> Kiểm tra Database: với Item_Database, Influ_Database, Affix_Database báo
/// (1) asset trong project chưa được thêm vào database, (2) ô trống, (3) id trùng nhau.
/// </summary>
public static class Database_Validator
{
    [MenuItem("Raid And Craft/Kiểm tra Database")]
    public static void ValidateAll()
    {
        int problems = 0;

        foreach (Item_Database db in LoadAll<Item_Database>())
            problems += Check(db, db.Items, "Item_Data", i => i.ItemId);

        foreach (Influ_Database db in LoadAll<Influ_Database>())
            problems += Check(db, db.Influs, "Influ_Data", i => i.InfluId);

        foreach (Affix_Database db in LoadAll<Affix_Database>())
            problems += Check(db, db.Affixes, "Affix_Data", a => a.AffixId);

        if (problems == 0) Debug.Log("[Database_Validator] Không có vấn đề nào.");
        else Debug.LogWarning($"[Database_Validator] {problems} vấn đề, xem các dòng ở trên. Mỗi database có menu chuột phải 'Thu thập mọi ...' để bổ sung.");
    }

    private static int Check<T>(Object database, IReadOnlyList<T> entries, string typeName, System.Func<T, string> getId) where T : Object
    {
        int problems = 0;
        var seen = new Dictionary<string, T>();

        foreach (T entry in entries)
        {
            if (entry == null)
            {
                Debug.LogWarning($"[Database_Validator] {database.name}: có ô trống.", database);
                problems++;
                continue;
            }

            string id = getId(entry);
            if (seen.TryGetValue(id, out T other) && other != entry)
            {
                Debug.LogWarning($"[Database_Validator] {database.name}: trùng id '{id}' giữa '{other.name}' và '{entry.name}'.", entry);
                problems++;
            }
            else seen[id] = entry;
        }

        var inDatabase = new HashSet<T>(entries);
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeName))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && !inDatabase.Contains(asset))
            {
                Debug.LogWarning($"[Database_Validator] {database.name}: thiếu '{asset.name}' ({AssetDatabase.GetAssetPath(asset)}).", asset);
                problems++;
            }
        }

        return problems;
    }

    private static List<T> LoadAll<T>() where T : Object
    {
        var list = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        return list;
    }
}
#endif
