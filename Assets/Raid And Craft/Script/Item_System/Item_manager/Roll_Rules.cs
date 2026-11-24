using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Luật roll của hệ craft (không giữ trạng thái; mọi hàm nhận System.Random để test lặp lại bằng seed):
/// - RollBase: roll lại giá trị Base của món trong khoảng theo Tier của Material (Tier món giữ nguyên).
/// - RollAffix: bốc một Affix từ pool của Material (theo Weight, đã lọc ô và nhóm loại trừ), roll giá trị theo Tier của Material,
///   rồi áp luật P0 (cùng loại ghi đè, còn chỗ thì thêm, đầy thì thay một dòng ngẫu nhiên).
/// - TryRollInflu: bốc một Influ trong Influ Options của Material (theo weight), roll trong khoảng của Material
///   (kẹp thêm trong Min/Max của chính Influ).
/// Chưa trừ nguyên liệu/Coin: việc đó là của Craft_Service (P5).
/// </summary>
public static class Roll_Rules
{
    // ------------------------------------------------------------------
    // TIỆN ÍCH
    // ------------------------------------------------------------------

    /// <summary>Khoảng roll của một Tier trong bảng: dòng đúng Tier, không thì Tier cao nhất không vượt quá, không thì thấp nhất.</summary>
    public static bool TryGetTierRange(IReadOnlyList<Affix_Data.Tier_Range> ranges, int tier, out float min, out float max)
    {
        min = max = 0f;
        if (ranges == null || ranges.Count == 0) return false;

        Affix_Data.Tier_Range atOrBelow = default;
        Affix_Data.Tier_Range lowest = ranges[0];
        bool found = false;

        for (int i = 0; i < ranges.Count; i++)
        {
            Affix_Data.Tier_Range r = ranges[i];
            if (r.tier < lowest.tier) lowest = r;
            if (r.tier <= tier && (!found || r.tier > atOrBelow.tier))
            {
                atOrBelow = r;
                found = true;
            }
        }

        Affix_Data.Tier_Range pick = found ? atOrBelow : lowest;
        min = Mathf.Min(pick.min, pick.max);
        max = Mathf.Max(pick.min, pick.max);
        return true;
    }

    /// <summary>Số ngẫu nhiên đều trong [min, max], làm tròn 3 chữ số (hoặc số nguyên nếu whole).</summary>
    public static float RollValue(float min, float max, bool whole, System.Random rng)
    {
        float t = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
        float value = Mathf.Lerp(min, max, t);
        return whole ? Mathf.Round(value) : (float)Math.Round(value, 3);
    }

    /// <summary>Bốc một phần tử theo trọng số (weightOf &lt;= 0 bị bỏ qua). Null nếu không có phần tử hợp lệ.</summary>
    public static T PickWeighted<T>(IReadOnlyList<T> items, Func<T, int> weightOf, System.Random rng) where T : class
    {
        long total = 0;
        for (int i = 0; i < items.Count; i++) total += Math.Max(0, weightOf(items[i]));
        if (total <= 0) return null;

        long pick = rng != null ? (long)(rng.NextDouble() * total) : (long)(UnityEngine.Random.value * total);
        if (pick >= total) pick = total - 1;

        for (int i = 0; i < items.Count; i++)
        {
            int w = Math.Max(0, weightOf(items[i]));
            if (pick < w) return items[i];
            pick -= w;
        }

        return null;
    }

    // ------------------------------------------------------------------
    // BASE
    // ------------------------------------------------------------------

    /// <summary>
    /// Roll lại BaseModifiers của món: mỗi dòng trong Stat Modifiers của asset có khai báo khoảng roll (Base Rolls) thì roll
    /// trong khoảng theo materialTier, dòng không khai báo giữ nguyên giá trị asset. Tier món giữ nguyên.
    /// False nếu món không phải Equippable_Item_Data.
    /// </summary>
    public static bool RollBase(Item_Instance item, int materialTier, System.Random rng)
    {
        if (item == null || !(item.Base is Equippable_Item_Data asset)) return false;

        var rolled = new List<Stat_Modifier_Entry>(asset.statModifiers.Count);

        foreach (Stat_Modifier_Entry m in asset.statModifiers)
        {
            Stat_Modifier_Entry result = m;

            Equippable_Item_Data.Base_Roll_Line? line = FindBaseRoll(asset, m);
            if (line.HasValue && TryGetTierRange(line.Value.ranges, materialTier, out float min, out float max))
            {
                result.value = RollValue(min, max, line.Value.wholeNumber, rng);
            }

            rolled.Add(result);
        }

        item.SetBaseModifiers(rolled);
        return true;
    }

    private static Equippable_Item_Data.Base_Roll_Line? FindBaseRoll(Equippable_Item_Data asset, Stat_Modifier_Entry m)
    {
        foreach (Equippable_Item_Data.Base_Roll_Line line in asset.BaseRolls)
        {
            if (line.stat == m.stat && line.modifierType == m.modifierType) return line;
        }

        return null;
    }

    // ------------------------------------------------------------------
    // AFFIX
    // ------------------------------------------------------------------

    /// <summary>Kết quả bốc một Affix (chưa áp lên món).</summary>
    public readonly struct Affix_Roll
    {
        public readonly Affix_Data affix;
        public readonly Affix_Line line;
        /// <summary>Dòng bị thay nếu món đã đủ dòng và loại này mới (truyền cho ApplyAffix).</summary>
        public readonly int replaceIndex;

        public Affix_Roll(Affix_Data affix, Affix_Line line, int replaceIndex)
        {
            this.affix = affix;
            this.line = line;
            this.replaceIndex = replaceIndex;
        }
    }

    /// <summary>
    /// Bốc và roll một Affix cho món từ pool của Material (ô trang bị lấy từ món). False nếu Material không có vai trò Affix
    /// hoặc pool rỗng (hết affix hợp ô/hợp nhóm). database dùng để áp nhóm loại trừ với các dòng món đã có.
    /// </summary>
    public static bool TryRollAffix(Material_Item_Data material, Item_Instance item, Affix_Database database, System.Random rng, out Affix_Roll roll)
    {
        roll = default;
        if (material == null || item == null) return false;

        var pool = new List<Affix_Data>();
        material.GetAffixPool(item.Base.EquipSlot, item.AffixLines, database, pool);

        Affix_Data chosen = PickWeighted(pool, a => a.Weight, rng);
        if (chosen == null) return false;

        Affix_Line line = chosen.Roll(material.RarityTier, rng);
        int count = Mathf.Max(1, item.AffixLines.Count);
        int replaceIndex = rng != null ? rng.Next(0, count) : UnityEngine.Random.Range(0, count);

        roll = new Affix_Roll(chosen, line, replaceIndex);
        return true;
    }

    /// <summary>TryRollAffix rồi áp lên món bằng ApplyAffix (luật P0). False (món không đổi) nếu không bốc được.</summary>
    public static bool ApplyAffixRoll(Material_Item_Data material, Item_Instance item, Affix_Database database, System.Random rng,
        out Item_Instance.Affix_Apply_Result result)
    {
        result = default;
        if (!TryRollAffix(material, item, database, rng, out Affix_Roll roll)) return false;

        result = item.ApplyAffix(roll.line, roll.replaceIndex);
        return true;
    }

    // ------------------------------------------------------------------
    // INFLU
    // ------------------------------------------------------------------

    /// <summary>Bốc một Influ trong Influ Options của Material và roll trong khoảng của Material (kẹp trong Min/Max của Influ). False nếu không có.</summary>
    public static bool TryRollInflu(Material_Item_Data material, System.Random rng, out Influ_Data influ, out float roll)
    {
        influ = null;
        roll = 1f;
        if (material == null) return false;

        var options = new List<Material_Item_Data.Influ_Option>();
        material.GetInfluOptions(options);
        if (options.Count == 0) return false;

        // PickWeighted cần kiểu tham chiếu: bốc theo chỉ số.
        long total = 0;
        foreach (Material_Item_Data.Influ_Option o in options) total += Math.Max(0, o.weight);
        if (total <= 0) return false;

        long pick = rng != null ? (long)(rng.NextDouble() * total) : (long)(UnityEngine.Random.value * total);
        if (pick >= total) pick = total - 1;

        Material_Item_Data.Influ_Option chosen = options[options.Count - 1];
        foreach (Material_Item_Data.Influ_Option o in options)
        {
            int w = Math.Max(0, o.weight);
            if (pick < w) { chosen = o; break; }
            pick -= w;
        }

        float min = Mathf.Max(chosen.minRoll, chosen.influ.MinRoll);
        float max = Mathf.Min(chosen.maxRoll, chosen.influ.MaxRoll);
        if (min > max) { min = chosen.minRoll; max = chosen.maxRoll; }

        influ = chosen.influ;
        roll = RollValue(min, max, false, rng);
        return true;
    }
}
