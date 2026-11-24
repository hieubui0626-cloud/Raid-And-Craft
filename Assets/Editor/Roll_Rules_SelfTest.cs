#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu Raid And Craft -> Test Roll/Tier Rules: chạy các kiểm tra thuần C# của Tier_Rules và Roll_Rules, kết quả in ở Console.
/// (Phần cần asset thật như TryRollAffix thì thử bằng hàm debug trên món.)
/// </summary>
public static class Roll_Rules_SelfTest
{
    private static int failed;

    [MenuItem("Raid And Craft/Test Roll-Tier Rules")]
    public static void Run()
    {
        failed = 0;

        // CraftTier
        Check("Craft T1x2+T3x2 = 2", Tier_Rules.CraftTier(new[] { new Tier_Amount(1, 2), new Tier_Amount(3, 2) }, 5) == 2);
        Check("Craft 1.5 làm tròn lên = 2", Tier_Rules.CraftTier(new[] { new Tier_Amount(1, 1), new Tier_Amount(2, 1) }, 5) == 2);
        Check("Craft kẹp maxTier", Tier_Rules.CraftTier(new[] { new Tier_Amount(4, 3) }, 2) == 2);
        Check("Craft không nguyên liệu = 1", Tier_Rules.CraftTier(new Tier_Amount[0], 5) == 1);

        // ForgeTier
        Check("Forge item T2 + T3x1 = 3 (nâng)", Tier_Rules.TryForgeTier(2, new[] { new Tier_Amount(3, 1) }, out int t1) && t1 == 3);
        Check("Forge item T3 + T1x1 = 2 (không hiện)", !Tier_Rules.TryForgeTier(3, new[] { new Tier_Amount(1, 1) }, out _));
        Check("Forge item T2 + T2 = 2 (không hiện)", !Tier_Rules.TryForgeTier(2, new[] { new Tier_Amount(2, 3) }, out _));

        // PlanConsumption
        List<Tier_Amount> plan = Tier_Rules.PlanConsumption(new[] { new Tier_Amount(3, 5), new Tier_Amount(1, 2) }, 4);
        Check("Plan trừ Tier thấp trước", plan != null && plan.Count == 2 && plan[0].tier == 1 && plan[0].count == 2 && plan[1].tier == 3 && plan[1].count == 2);
        Check("Plan thiếu = null", Tier_Rules.PlanConsumption(new[] { new Tier_Amount(1, 2) }, 3) == null);

        // RollValue
        var rng = new System.Random(1234);
        bool inRange = true;
        for (int i = 0; i < 2000; i++)
        {
            float v = Roll_Rules.RollValue(3f, 5f, false, rng);
            if (v < 3f || v > 5f) inRange = false;
            float w = Roll_Rules.RollValue(3f, 5f, true, rng);
            if (w < 3f || w > 5f || w != Mathf.Round(w)) inRange = false;
        }
        Check("RollValue trong khoảng, whole là số nguyên", inRange);

        Check("RollValue cùng seed cùng kết quả",
            Roll_Rules.RollValue(0f, 100f, false, new System.Random(7)) == Roll_Rules.RollValue(0f, 100f, false, new System.Random(7)));

        // TryGetTierRange
        var ranges = new List<Affix_Data.Tier_Range>
        {
            new Affix_Data.Tier_Range { tier = 1, min = 1, max = 2 },
            new Affix_Data.Tier_Range { tier = 3, min = 5, max = 8 },
        };
        Roll_Rules.TryGetTierRange(ranges, 2, out float min2, out float max2);
        Check("Range Tier 2 dùng dòng Tier 1", min2 == 1 && max2 == 2);
        Roll_Rules.TryGetTierRange(ranges, 9, out float min9, out float max9);
        Check("Range Tier 9 dùng dòng Tier 3", min9 == 5 && max9 == 8);
        Roll_Rules.TryGetTierRange(ranges, 0, out float min0, out float max0);
        Check("Range Tier 0 dùng dòng thấp nhất", min0 == 1 && max0 == 2);

        // PickWeighted: không bao giờ chọn trọng số 0
        var items = new List<string> { "a", "b", "c" };
        var weights = new Dictionary<string, int> { { "a", 0 }, { "b", 10 }, { "c", 0 } };
        bool onlyB = true;
        for (int i = 0; i < 500; i++)
        {
            if (Roll_Rules.PickWeighted(items, x => weights[x], rng) != "b") onlyB = false;
        }
        Check("PickWeighted bỏ trọng số 0", onlyB);

        if (failed == 0) Debug.Log("[Roll_Rules_SelfTest] Tất cả kiểm tra đạt.");
        else Debug.LogError($"[Roll_Rules_SelfTest] {failed} kiểm tra lỗi.");
    }

    private static void Check(string name, bool ok)
    {
        if (ok) return;

        failed++;
        Debug.LogError($"[Roll_Rules_SelfTest] LỖI: {name}");
    }
}
#endif
