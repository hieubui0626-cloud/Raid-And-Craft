using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Một họ nguyên liệu mà Scene cho phép rơi, kèm trọng số (họ nào nhiều trọng số hơn thì rơi nhiều hơn).</summary>
[Serializable]
public struct Scene_Family_Entry
{
    public Material_Family family;

    [Tooltip("Trọng số tương đối. Hai họ cùng trọng số = rơi ngang nhau.")]
    [Min(0f)] public float weight;
}

/// <summary>
/// QUY CHẾ CỦA SCENE (tạo asset: Create -> Raid And Craft -> Scene Rules, kéo vào ô Rules của Scene_Info).
/// Scene quyết định cho quái ở đó: level, hệ số EXP, những họ nguyên liệu có thể rơi và tier của món rơi.
/// Nhờ vậy CÙNG MỘT loại quái rơi đồ khác nhau tuỳ Scene: quái chỉ khai báo "rơi nguyên liệu với xác suất/số lượng này" (Enemy_Reward),
/// Scene chọn cụ thể rơi họ nào, tier nào.
/// - Level quái = Scene Level (cố định mỗi Scene): Enemy_Scene_Level đặt level cho quái lúc xuất hiện.
/// - Tier chính của món rơi = 1 + (Scene Level - 1) / Levels Per Tier; mỗi lần rơi có thể lệch lên/xuống một bậc theo xác suất.
/// Một asset dùng chung được cho nhiều Scene cùng cấp.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Scene Rules", fileName = "Scene_Rules")]
public class Scene_Rules : ScriptableObject
{
    [Header("Cấp độ và EXP")]
    [Tooltip("Level của mọi quái trong Scene này, đồng thời là cơ sở tính tier của món rơi.")]
    [SerializeField, Min(1)] private int sceneLevel = 1;

    [Tooltip("Nhân với EXP gốc của quái (Enemy_Reward) khi hạ quái trong Scene này.")]
    [SerializeField, Min(0f)] private float expMultiplier = 1f;

    [Header("Nguyên liệu rơi")]
    [Tooltip("Các họ nguyên liệu quái trong Scene có thể rơi. Trống = Scene này không rơi nguyên liệu theo quy chế.")]
    [SerializeField] private List<Scene_Family_Entry> materialFamilies = new List<Scene_Family_Entry>();

    [Tooltip("Bao nhiêu level Scene thì tier chính tăng 1 bậc. Ví dụ 10: Lv 1-10 = T1, Lv 11-20 = T2...")]
    [SerializeField, Min(1)] private int levelsPerTier = 10;

    [Tooltip("Xác suất một món rơi cao hơn tier chính 1 bậc (rơi may).")]
    [SerializeField, Range(0f, 1f)] private float higherTierChance = 0.1f;

    [Tooltip("Xác suất một món rơi thấp hơn tier chính 1 bậc. Còn lại rơi đúng tier chính.")]
    [SerializeField, Range(0f, 1f)] private float lowerTierChance = 0.15f;

    public int SceneLevel => Mathf.Max(1, sceneLevel);
    public float ExpMultiplier => expMultiplier;
    public IReadOnlyList<Scene_Family_Entry> MaterialFamilies => materialFamilies;

    /// <summary>Tier chính của món rơi theo level Scene (tối thiểu 1).</summary>
    public int MainTier => 1 + (SceneLevel - 1) / Mathf.Max(1, levelsPerTier);

    /// <summary>EXP cuối cùng của một quái có EXP gốc baseExp trong Scene này.</summary>
    public int ScaleExp(int baseExp)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseExp * expMultiplier));
    }

    /// <summary>Tung tier cho một món rơi: có thể lệch 1 bậc so với tier chính (không xuống dưới 1).</summary>
    public int RollTier()
    {
        int main = MainTier;
        float roll = UnityEngine.Random.value;

        if (roll < higherTierChance) return main + 1;
        if (roll < higherTierChance + lowerTierChance) return Mathf.Max(1, main - 1);
        return main;
    }

    /// <summary>Chọn một họ theo trọng số rồi lấy món đúng tier tung được. Null nếu Scene không có họ nào dùng được.</summary>
    public Material_Item_Data RollMaterial()
    {
        float total = 0f;
        foreach (Scene_Family_Entry entry in materialFamilies)
        {
            if (entry.family != null && entry.weight > 0f) total += entry.weight;
        }

        if (total <= 0f) return null;

        float pick = UnityEngine.Random.value * total;
        Material_Family chosen = null;
        foreach (Scene_Family_Entry entry in materialFamilies)
        {
            if (entry.family == null || entry.weight <= 0f) continue;

            chosen = entry.family;
            pick -= entry.weight;
            if (pick < 0f) break;
        }

        return chosen != null ? chosen.GetForTier(RollTier()) : null;
    }
}
