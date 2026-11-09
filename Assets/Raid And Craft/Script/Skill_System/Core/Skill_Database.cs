using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh sách MỌI skill của game, để tra theo SkillId khi load save (kho skill và ô skill lưu id, không lưu asset).
/// Tạo asset: chuột phải Project -> Create -> Raid And Craft -> Skill Database, rồi chuột phải component -> "Thu thập mọi Skill".
/// Thêm skill mới thì nhớ thêm vào đây, nếu không skill đó sẽ biến mất khi load (có log cảnh báo).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Database", fileName = "Skill_Database")]
public class Skill_Database : ScriptableObject
{
    [SerializeField] private List<Skill_Data> skills = new List<Skill_Data>();

    private Dictionary<string, Skill_Data> byId;

    public IReadOnlyList<Skill_Data> Skills => skills;

    /// <summary>Tìm skill theo SkillId. Trả về null nếu không có.</summary>
    public Skill_Data Find(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;

        EnsureBuilt();
        return byId.TryGetValue(skillId, out Skill_Data skill) ? skill : null;
    }

    private void OnEnable()
    {
        byId = null;
    }

    private void EnsureBuilt()
    {
        if (byId != null) return;

        byId = new Dictionary<string, Skill_Data>(skills.Count);
        foreach (Skill_Data skill in skills)
        {
            if (skill == null) continue;

            if (!byId.TryAdd(skill.SkillId, skill))
            {
                Debug.LogWarning($"[{nameof(Skill_Database)}] Trùng SkillId '{skill.SkillId}' ({skill.name}).", this);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        byId = null;
    }

    [ContextMenu("Thu thập mọi Skill trong project")]
    private void CollectAll()
    {
        skills.Clear();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Skill_Data"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var skill = UnityEditor.AssetDatabase.LoadAssetAtPath<Skill_Data>(path);
            if (skill != null) skills.Add(skill);
        }

        byId = null;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[{nameof(Skill_Database)}] Đã thu thập {skills.Count} skill.", this);
    }
#endif
}
