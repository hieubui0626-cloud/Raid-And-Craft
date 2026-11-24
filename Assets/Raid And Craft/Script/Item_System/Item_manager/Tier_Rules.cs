using System;
using System.Collections.Generic;

/// <summary>Một phần nguyên liệu đã/sẽ tiêu thụ: count đơn vị ở bậc tier.</summary>
public readonly struct Tier_Amount
{
    public readonly int tier;
    public readonly int count;

    public Tier_Amount(int tier, int count)
    {
        this.tier = tier;
        this.count = count;
    }
}

/// <summary>
/// Luật tính Tier (thuần C#, không phụ thuộc Unity) theo "Thiết kế Craft và Forge":
/// - Craft: Tier = trung bình theo trọng số (số lượng) của phần nguyên liệu ĐÃ TIÊU THỤ, làm tròn, kẹp bởi maxTier của recipe.
///   Phần tiêu thụ được trừ từ Tier thấp lên cao (PlanConsumption). Chỉ trộn trong cùng Material_Family (người gọi lọc trước).
/// - Nâng Tier (Forge): Tier mới = trung bình theo trọng số của Tier món (trọng số itemWeight) và Tier phần Material tiêu thụ, làm tròn;
///   không lớn hơn Tier hiện tại thì thao tác không hiện, nên Tier không bao giờ giảm.
/// Làm tròn: 0.5 làm tròn LÊN (2.5 -> 3). Cho craft, doc chưa nói rõ cách làm tròn: dùng chung với Forge.
/// </summary>
public static class Tier_Rules
{
    /// <summary>Tier món craft ra. Không có nguyên liệu thì trả 1.</summary>
    public static int CraftTier(IReadOnlyList<Tier_Amount> consumed, int maxTier)
    {
        long weightSum = 0;
        long total = 0;

        foreach (Tier_Amount a in consumed)
        {
            if (a.count <= 0) continue;
            weightSum += a.count;
            total += (long)a.tier * a.count;
        }

        if (weightSum == 0) return 1;

        int tier = Round(total / (double)weightSum);
        return Math.Max(1, Math.Min(tier, Math.Max(1, maxTier)));
    }

    /// <summary>
    /// Tier mới khi nâng Tier. Trả về true (và newTier) chỉ khi newTier LỚN HƠN itemTier; false thì thao tác không hiện.
    /// itemWeight: trọng số của chính món trong phép trung bình (mặc định 1).
    /// </summary>
    public static bool TryForgeTier(int itemTier, IReadOnlyList<Tier_Amount> consumed, out int newTier, int itemWeight = 1)
    {
        long weightSum = Math.Max(1, itemWeight);
        long total = (long)itemTier * weightSum;

        foreach (Tier_Amount a in consumed)
        {
            if (a.count <= 0) continue;
            weightSum += a.count;
            total += (long)a.tier * a.count;
        }

        newTier = Math.Max(1, Round(total / (double)weightSum));
        return newTier > itemTier;
    }

    /// <summary>
    /// Chọn phần nguyên liệu tiêu thụ để đủ need đơn vị, trừ từ Tier thấp lên cao.
    /// available: số có sẵn theo từng tier (thứ tự bất kỳ, tier trùng thì gộp được). Null nếu không đủ.
    /// </summary>
    public static List<Tier_Amount> PlanConsumption(IReadOnlyList<Tier_Amount> available, int need)
    {
        var sorted = new List<Tier_Amount>();
        long have = 0;

        foreach (Tier_Amount a in available)
        {
            if (a.count <= 0) continue;
            sorted.Add(a);
            have += a.count;
        }

        if (need <= 0 || have < need) return need <= 0 ? new List<Tier_Amount>() : null;

        sorted.Sort((x, y) => x.tier.CompareTo(y.tier));

        var plan = new List<Tier_Amount>();
        int left = need;

        foreach (Tier_Amount a in sorted)
        {
            if (left <= 0) break;

            int take = Math.Min(a.count, left);
            plan.Add(new Tier_Amount(a.tier, take));
            left -= take;
        }

        return plan;
    }

    private static int Round(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
