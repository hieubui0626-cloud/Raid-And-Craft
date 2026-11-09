using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quy chế Scene SAU KHI áp modifier (dữ liệu chạy, không lưu). Scene_Info dựng ra từ Scene_Rules gốc + danh sách Scene_Rule_Modifier.
/// Modifier chỉ cần chỉnh các field public ở đây; mọi hệ số cộng dồn bằng phép NHÂN, level/tier bằng phép CỘNG.
/// Enemy_Reward / Enemy_Scene_Level chỉ đọc lớp này, nên thêm modifier mới không phải sửa code quái.
/// </summary>
public class Scene_Rule_Values
{
    public Scene_Rules Base { get; }

    /// <summary>Cộng vào level Scene (âm = giảm). Level cuối tối thiểu 1.</summary>
    public int levelOffset;

    /// <summary>Cộng vào tier chính của món rơi (độc lập với level). Tier cuối tối thiểu 1.</summary>
    public int tierOffset;

    public float expMultiplier = 1f;
    public float coinMultiplier = 1f;
    public float dropChanceMultiplier = 1f;
    public float dropCountMultiplier = 1f;

    /// <summary>Họ nguyên liệu thêm vào (ví dụ modifier "Hầm mỏ" thêm họ quặng) - cộng vào danh sách của Scene_Rules.</summary>
    public readonly List<Scene_Family_Entry> extraFamilies = new List<Scene_Family_Entry>();

    public Scene_Rule_Values(Scene_Rules baseRules)
    {
        Base = baseRules;
        expMultiplier = baseRules.ExpMultiplier;
        coinMultiplier = baseRules.CoinMultiplier;
        dropChanceMultiplier = baseRules.DropChanceMultiplier;
        dropCountMultiplier = baseRules.DropCountMultiplier;
    }

    public int Level => Mathf.Max(1, Base.SceneLevel + levelOffset);

    public int MainTier => Mathf.Max(1, 1 + (Level - 1) / Base.LevelsPerTier + tierOffset);

    // ------------------------------------------------------------------
    // Thưởng theo hạng quái
    // ------------------------------------------------------------------

    public int ScaleExp(int baseExp, Enemy_Rank rank)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseExp * expMultiplier * Base.GetRankRule(rank).expMultiplier));
    }

    public int ScaleCoin(int baseCoin, Enemy_Rank rank)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseCoin * coinMultiplier * Base.GetRankRule(rank).coinMultiplier));
    }

    /// <summary>Xác suất rơi cuối của một ô: chance gốc x Scene x modifier x hạng, kẹp 0..1.</summary>
    public float ScaleDropChance(float chance, Enemy_Rank rank)
    {
        return Mathf.Clamp01(chance * dropChanceMultiplier * Base.GetRankRule(rank).dropChanceMultiplier);
    }

    /// <summary>Hệ số xác suất tổng (cho Loot_Table).</summary>
    public float DropChanceFactor(Enemy_Rank rank) => dropChanceMultiplier * Base.GetRankRule(rank).dropChanceMultiplier;

    /// <summary>Hệ số số lượng tổng (cho Loot_Table).</summary>
    public float DropCountFactor(Enemy_Rank rank) => dropCountMultiplier * Base.GetRankRule(rank).dropCountMultiplier;

    /// <summary>Nhân số lượng, phần lẻ làm tròn ngẫu nhiên (1.5 -> 1 hoặc 2, trung bình 1.5). Tối thiểu 1 nếu count >= 1 và hệ số > 0.</summary>
    public int ScaleCount(int count, Enemy_Rank rank)
    {
        return RoundStochastic(count * DropCountFactor(rank));
    }

    public static int RoundStochastic(float value)
    {
        if (value <= 0f) return 0;
        int whole = Mathf.FloorToInt(value);
        return Random.value < value - whole ? whole + 1 : whole;
    }

    // ------------------------------------------------------------------
    // Chọn nguyên liệu
    // ------------------------------------------------------------------

    /// <summary>Tier một lần rơi: thường là tier chính, có xác suất lệch lên/xuống một bậc (không dưới 1).</summary>
    public int RollTier()
    {
        int tier = MainTier;
        float roll = Random.value;
        if (roll < Base.HigherTierChance) return tier + 1;
        if (roll < Base.HigherTierChance + Base.LowerTierChance) return Mathf.Max(1, tier - 1);
        return tier;
    }

    /// <summary>Chọn họ theo trọng số (gồm họ do modifier thêm) rồi lấy nguyên liệu đúng tier. Null nếu không có họ nào dùng được.</summary>
    public Material_Item_Data RollMaterial()
    {
        float total = 0f;
        foreach (Scene_Family_Entry e in Base.MaterialFamilies) if (e.family != null && e.weight > 0f) total += e.weight;
        foreach (Scene_Family_Entry e in extraFamilies) if (e.family != null && e.weight > 0f) total += e.weight;
        if (total <= 0f) return null;

        float pick = Random.value * total;
        Material_Family chosen = null;
        foreach (Scene_Family_Entry e in Base.MaterialFamilies)
        {
            if (e.family == null || e.weight <= 0f) continue;
            chosen = e.family;
            pick -= e.weight;
            if (pick < 0f) return chosen.GetForTier(RollTier());
        }

        foreach (Scene_Family_Entry e in extraFamilies)
        {
            if (e.family == null || e.weight <= 0f) continue;
            chosen = e.family;
            pick -= e.weight;
            if (pick < 0f) return chosen.GetForTier(RollTier());
        }

        return chosen != null ? chosen.GetForTier(RollTier()) : null;
    }
}
