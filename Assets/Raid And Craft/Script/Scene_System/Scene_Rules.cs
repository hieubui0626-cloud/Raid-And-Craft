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
/// Hệ số theo HẠNG quái (thường, boss...) trong một Scene. Nhân với hệ số chung của Scene (và modifier) khi tính thưởng.
/// Hạng không có trong danh sách = mọi hệ số bằng 1.
/// </summary>
[Serializable]
public struct Rank_Rule
{
    public Enemy_Rank rank;

    [Tooltip("Nhân vào xác suất rơi của từng ô (kết quả kẹp tối đa 100%). Boss thường > 1.")]
    [Min(0f)] public float dropChanceMultiplier;

    [Tooltip("Nhân vào số lượng rơi (số lẻ được làm tròn ngẫu nhiên: 1.5 = 1 hoặc 2).")]
    [Min(0f)] public float dropCountMultiplier;

    [Tooltip("Nhân vào EXP gốc của quái.")]
    [Min(0f)] public float expMultiplier;

    [Tooltip("Nhân vào Coin.")]
    [Min(0f)] public float coinMultiplier;

    public static Rank_Rule Neutral(Enemy_Rank rank)
    {
        return new Rank_Rule { rank = rank, dropChanceMultiplier = 1f, dropCountMultiplier = 1f, expMultiplier = 1f, coinMultiplier = 1f };
    }
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

    [Tooltip("Nhân với Coin của quái trong Scene này.")]
    [SerializeField, Min(0f)] private float coinMultiplier = 1f;

    [Header("Hệ số rơi (không phải lúc nào quái chết cũng rớt đồ)")]
    [Tooltip("Nhân vào xác suất rơi của MỌI ô rơi trong Scene này (0.5 = rơi ít một nửa). Kết quả từng ô kẹp trong 0..100%.")]
    [SerializeField, Min(0f)] private float dropChanceMultiplier = 1f;

    [Tooltip("Nhân vào số lượng rơi trong Scene này.")]
    [SerializeField, Min(0f)] private float dropCountMultiplier = 1f;

    [Tooltip("Hệ số riêng cho từng HẠNG quái (nhân thêm vào hệ số chung ở trên). Mặc định: boss rơi chắc hơn, nhiều hơn, thưởng nhiều hơn.")]
    [SerializeField] private List<Rank_Rule> rankRules = new List<Rank_Rule>
    {
        new Rank_Rule { rank = Enemy_Rank.Normal, dropChanceMultiplier = 1f, dropCountMultiplier = 1f, expMultiplier = 1f, coinMultiplier = 1f },
        new Rank_Rule { rank = Enemy_Rank.Boss, dropChanceMultiplier = 3f, dropCountMultiplier = 2f, expMultiplier = 5f, coinMultiplier = 5f },
    };

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
    public float CoinMultiplier => coinMultiplier;
    public float DropChanceMultiplier => dropChanceMultiplier;
    public float DropCountMultiplier => dropCountMultiplier;
    public IReadOnlyList<Scene_Family_Entry> MaterialFamilies => materialFamilies;
    public int LevelsPerTier => Mathf.Max(1, levelsPerTier);
    public float HigherTierChance => higherTierChance;
    public float LowerTierChance => lowerTierChance;

    /// <summary>Hệ số của một hạng quái; hạng chưa khai báo thì mọi hệ số bằng 1.</summary>
    public Rank_Rule GetRankRule(Enemy_Rank rank)
    {
        foreach (Rank_Rule rule in rankRules)
        {
            if (rule.rank == rank) return rule;
        }

        return Rank_Rule.Neutral(rank);
    }

    /// <summary>Tier chính của món rơi theo level Scene (tối thiểu 1).</summary>
    public int MainTier => 1 + (SceneLevel - 1) / Mathf.Max(1, levelsPerTier);
}
