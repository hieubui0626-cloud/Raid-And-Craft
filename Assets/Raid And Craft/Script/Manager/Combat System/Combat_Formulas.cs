using UnityEngine;

/// <summary>
/// Nơi DUY NHẤT chứa công thức chiến đấu và các hằng số cân bằng (balance). Muốn chỉnh độ khó/cảm giác game: sửa ở đây.
///
/// Quy ước đơn vị chỉ số (khớp với Player_Stats_UI):
/// - CriticalChance, CriticalDamage, BlockChance, *Resistance, MagicResistance: PHẦN TRĂM (5 = 5%).
///   CriticalDamage là phần cộng thêm: 50 nghĩa là chí mạng gây x1.5 sát thương.
/// - Accuracy, Evade, Armor: điểm thường (không phải %).
/// - Player_Stat kẹp giá trị cuối tối thiểu 0 nên hiện chưa có kháng âm (debuff giảm kháng).
///
/// Thứ tự xử lý một đòn: (bên đánh) dựng đòn + roll chí mạng -> (bên nhận) né -> đỡ -> giảm sát thương từng loại -> trừ máu.
/// </summary>
public static class Combat_Formulas
{
    // ------------------------------------------------------------------
    // HẰNG SỐ CÂN BẰNG
    // ------------------------------------------------------------------

    /// <summary>Tỉ lệ trúng tối thiểu/tối đa: luôn có cơ hội trúng và luôn có cơ hội trượt.</summary>
    public const float MinHitChance = 0.05f;
    public const float MaxHitChance = 0.95f;

    /// <summary>Trần tỉ lệ đỡ đòn.</summary>
    public const float MaxBlockChance = 0.75f;

    /// <summary>Trần kháng (Magic/Fire/Cold/Lightning/Chaos).</summary>
    public const float MaxResistance = 0.75f;

    /// <summary>Trần giảm sát thương vật lý từ Armor.</summary>
    public const float MaxArmorReduction = 0.90f;

    /// <summary>
    /// Armor giảm sát thương vật lý theo Armor / (Armor + ArmorFactor * Sát thương đòn).
    /// Armor càng cao so với đòn đánh càng giảm nhiều; đòn càng mạnh thì Armor càng kém hiệu quả.
    /// </summary>
    public const float ArmorFactor = 10f;

    // ------------------------------------------------------------------
    // CÔNG THỨC THÀNH PHẦN (thuần, dễ test)
    // ------------------------------------------------------------------

    /// <summary>Tỉ lệ trúng (0..1) = Accuracy / (Accuracy + Evade), kẹp trong [MinHitChance, MaxHitChance].</summary>
    public static float HitChance(float accuracy, float evade)
    {
        float total = accuracy + evade;
        if (total <= 0f) return MaxHitChance;

        return Mathf.Clamp(accuracy / total, MinHitChance, MaxHitChance);
    }

    /// <summary>Tỉ lệ đỡ đòn (0..MaxBlockChance) từ chỉ số BlockChance (%).</summary>
    public static float BlockProbability(float blockChancePercent)
    {
        return Mathf.Clamp(blockChancePercent / 100f, 0f, MaxBlockChance);
    }

    /// <summary>Tỉ lệ chí mạng (0..1) từ chỉ số CriticalChance (%).</summary>
    public static float CritProbability(float critChancePercent)
    {
        return Mathf.Clamp01(critChancePercent / 100f);
    }

    /// <summary>Hệ số nhân khi chí mạng: 1 + CriticalDamage/100 (50 -> x1.5).</summary>
    public static float CritMultiplier(float critDamagePercent)
    {
        return 1f + Mathf.Max(0f, critDamagePercent) / 100f;
    }

    /// <summary>Phần trăm (0..MaxArmorReduction) sát thương vật lý bị Armor chặn.</summary>
    public static float ArmorReduction(float armor, float physicalDamage)
    {
        if (physicalDamage <= 0f || armor <= 0f) return 0f;

        float reduction = armor / (armor + ArmorFactor * physicalDamage);
        return Mathf.Min(reduction, MaxArmorReduction);
    }

    /// <summary>Phần trăm (0..MaxResistance) sát thương bị kháng chặn từ chỉ số kháng (%).</summary>
    public static float ResistanceReduction(float resistancePercent)
    {
        return Mathf.Clamp(resistancePercent / 100f, 0f, MaxResistance);
    }

    /// <summary>Chỉ số tấn công tương ứng với một loại sát thương.</summary>
    public static Player_Stat_Type GetOffenceStat(Damage_Type type)
    {
        switch (type)
        {
            case Damage_Type.Physic: return Player_Stat_Type.PhysicDamage;
            case Damage_Type.Magic: return Player_Stat_Type.MagicDamage;
            case Damage_Type.Fire: return Player_Stat_Type.FireDamage;
            case Damage_Type.Cold: return Player_Stat_Type.ColdDamage;
            case Damage_Type.Lightning: return Player_Stat_Type.LightningDamage;
            default: return Player_Stat_Type.ChaosDamage;
        }
    }

    /// <summary>Chỉ số kháng tương ứng (Physic dùng Armor nên không có kháng, trả về false).</summary>
    public static bool TryGetResistanceStat(Damage_Type type, out Player_Stat_Type stat)
    {
        switch (type)
        {
            case Damage_Type.Magic: stat = Player_Stat_Type.MagicResistance; return true;
            case Damage_Type.Fire: stat = Player_Stat_Type.FireResistance; return true;
            case Damage_Type.Cold: stat = Player_Stat_Type.ColdResistance; return true;
            case Damage_Type.Lightning: stat = Player_Stat_Type.LightningResistance; return true;
            case Damage_Type.Chaos: stat = Player_Stat_Type.ChaosResistance; return true;
            default: stat = default; return false;
        }
    }

    /// <summary>Số giây giữa hai đòn đánh = 1 / ActionSpeed (ActionSpeed tính theo số hành động mỗi giây).</summary>
    public static float ActionInterval(Character_Stats stats)
    {
        float speed = stats.GetValue(Player_Stat_Type.ActionSpeed);
        return speed > 0f ? 1f / speed : float.PositiveInfinity;
    }

    // ------------------------------------------------------------------
    // BÊN ĐÁNH: dựng đòn
    // ------------------------------------------------------------------

    /// <summary>
    /// Dựng một đòn đánh từ chỉ số của attacker.
    /// - multiplier: hệ số của kỹ năng (đòn thường = 1, kỹ năng mạnh > 1).
    /// - types: các loại sát thương đòn này mang theo. Bỏ trống = mang đủ mọi loại (chỉ loại có chỉ số > 0 mới có tác dụng).
    ///   Ví dụ đòn thường vật lý: BuildAttack(stats, 1f, Damage_Type.Physic); quả cầu lửa: BuildAttack(stats, 1.5f, Damage_Type.Fire).
    /// Chí mạng được roll MỘT lần cho cả đòn, sát thương đã được nhân sẵn hệ số chí mạng.
    /// </summary>
    public static Damage_Info BuildAttack(Character_Stats attacker, float multiplier = 1f, params Damage_Type[] types)
    {
        return BuildAttack(attacker, Random.value, multiplier, types);
    }

    /// <summary>Như trên nhưng truyền sẵn kết quả roll chí mạng (0..1) để test hoặc replay.</summary>
    public static Damage_Info BuildAttack(Character_Stats attacker, float critRoll, float multiplier, params Damage_Type[] types)
    {
        var info = new Damage_Info { source = attacker };
        if (attacker == null) return info;

        Damage_Type[] carried = (types == null || types.Length == 0) ? Damage_Info.AllTypes : types;
        foreach (Damage_Type type in carried)
        {
            info.Set(type, attacker.GetValue(GetOffenceStat(type)) * multiplier);
        }

        info.accuracy = attacker.GetValue(Player_Stat_Type.Accuracy);

        float critChance = CritProbability(attacker.GetValue(Player_Stat_Type.CriticalChance));
        if (critRoll < critChance)
        {
            info.isCritical = true;
            float critMultiplier = CritMultiplier(attacker.GetValue(Player_Stat_Type.CriticalDamage));
            info = info.Scaled(critMultiplier);
        }

        return info;
    }

    // ------------------------------------------------------------------
    // BÊN NHẬN: xử lý đòn
    // ------------------------------------------------------------------

    /// <summary>
    /// Xử lý một đòn lên defender và trả về kết cục. Nếu là Hit thì finalDamage là tổng sát thương sau giảm.
    /// Né/đỡ roll bằng Random.value.
    /// </summary>
    public static Damage_Outcome Resolve(Damage_Info info, Character_Stats defender, out float finalDamage)
    {
        return Resolve(info, defender, Random.value, Random.value, out finalDamage);
    }

    /// <summary>Như trên nhưng truyền sẵn hai kết quả roll (0..1) cho né và đỡ để test hoặc replay.</summary>
    public static Damage_Outcome Resolve(Damage_Info info, Character_Stats defender, float hitRoll, float blockRoll, out float finalDamage)
    {
        finalDamage = 0f;
        if (defender == null || info.TotalRaw <= 0f) return Damage_Outcome.Ignored;

        // 1) Né: Accuracy của đòn so với Evade của bên nhận.
        if (!info.cannotBeEvaded)
        {
            float hitChance = HitChance(info.accuracy, defender.GetValue(Player_Stat_Type.Evade));
            if (hitRoll >= hitChance) return Damage_Outcome.Evaded;
        }

        // 2) Đỡ: đỡ được thì không nhận sát thương nào.
        if (!info.cannotBeBlocked)
        {
            float blockChance = BlockProbability(defender.GetValue(Player_Stat_Type.BlockChance));
            if (blockRoll < blockChance) return Damage_Outcome.Blocked;
        }

        // 3) Giảm sát thương từng loại.
        finalDamage = MitigatedTotal(info, defender);
        return Damage_Outcome.Hit;
    }

    /// <summary>Tổng sát thương sau khi áp Armor (vật lý) và kháng (các loại còn lại).</summary>
    public static float MitigatedTotal(Damage_Info info, Character_Stats defender)
    {
        float total = 0f;

        foreach (Damage_Type type in Damage_Info.AllTypes)
        {
            float raw = info.Get(type);
            if (raw <= 0f) continue;

            float reduction;
            if (TryGetResistanceStat(type, out Player_Stat_Type resistStat))
            {
                reduction = ResistanceReduction(defender.GetValue(resistStat));
            }
            else
            {
                reduction = ArmorReduction(defender.GetValue(Player_Stat_Type.Armor), raw);
            }

            total += raw * (1f - reduction);
        }

        return total;
    }
}
