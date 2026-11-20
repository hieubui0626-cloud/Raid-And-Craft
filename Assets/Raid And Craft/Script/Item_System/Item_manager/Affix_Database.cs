using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh sách MỌI Affix của game: tra theo affixId (load save, reroll) và bốc pool theo ô trang bị (craft).
/// Tạo asset: Create -> Raid And Craft -> Affix Database, rồi chuột phải component -> "Thu thập mọi Affix trong project".
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Affix Database", fileName = "Affix_Database")]
public class Affix_Database : ScriptableObject
{
    [SerializeField] private List<Affix_Data> affixes = new List<Affix_Data>();

    private Dictionary<string, Affix_Data> byId;

    public IReadOnlyList<Affix_Data> Affixes => affixes;

    /// <summary>Tìm Affix theo affixId. Null nếu không có.</summary>
    public Affix_Data Find(string affixId)
    {
        if (string.IsNullOrEmpty(affixId)) return null;

        EnsureBuilt();
        return byId.TryGetValue(affixId, out Affix_Data affix) ? affix : null;
    }

    /// <summary>
    /// Pool affix roll được trên một ô trang bị, bỏ các affix loại trừ với những affix món đã có (existing, có thể null).
    /// Ghi vào result (xoá trước). Trọng số lấy từ Affix_Data.Weight.
    /// </summary>
    public void GetPool(Equipment_Slot slot, IReadOnlyList<Affix_Line> existing, List<Affix_Data> result)
    {
        result.Clear();
        foreach (Affix_Data a in affixes)
        {
            if (a == null || !a.CanRollOn(slot) || a.Weight <= 0) continue;
            if (IsBlockedBy(a, existing)) continue;
            result.Add(a);
        }
    }

    private bool IsBlockedBy(Affix_Data candidate, IReadOnlyList<Affix_Line> existing)
    {
        if (existing == null) return false;

        foreach (Affix_Line line in existing)
        {
            // Cùng loại với một dòng đã có thì vẫn cho roll (ApplyAffix sẽ ghi đè); chỉ chặn khác loại cùng nhóm loại trừ.
            if (line.affixId == candidate.AffixId) continue;

            Affix_Data other = Find(line.affixId);
            if (other != null && candidate.ConflictsWith(other)) return true;
        }

        return false;
    }

    private void OnEnable()
    {
        byId = null;
    }

    private void EnsureBuilt()
    {
        if (byId != null) return;

        byId = new Dictionary<string, Affix_Data>(affixes.Count);
        foreach (Affix_Data affix in affixes)
        {
            if (affix == null) continue;

            if (!byId.TryAdd(affix.AffixId, affix))
            {
                Debug.LogWarning($"[{nameof(Affix_Database)}] Trùng AffixId '{affix.AffixId}' ({affix.name}).", this);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        byId = null;
    }

    [ContextMenu("Thu thập mọi Affix trong project")]
    private void CollectAll()
    {
        affixes.Clear();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Affix_Data"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var affix = UnityEditor.AssetDatabase.LoadAssetAtPath<Affix_Data>(path);
            if (affix != null) affixes.Add(affix);
        }

        byId = null;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[{nameof(Affix_Database)}] Đã thu thập {affixes.Count} Affix.", this);
    }
#endif
}
