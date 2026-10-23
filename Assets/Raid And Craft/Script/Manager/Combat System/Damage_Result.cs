/// <summary>Kết cục của một đòn đánh khi chạm vào bên nhận.</summary>
public enum Damage_Outcome
{
    /// <summary>Trúng, đã trừ máu (có thể bằng 0 nếu bị giảm hết).</summary>
    Hit = 0,
    /// <summary>Bị né (Accuracy thua Evade).</summary>
    Evaded = 1,
    /// <summary>Bị đỡ hoàn toàn (BlockChance).</summary>
    Blocked = 2,
    /// <summary>Bỏ qua: bên nhận đã chết hoặc đòn không có sát thương.</summary>
    Ignored = 3,
}

/// <summary>Kết quả trả về cho bên tấn công/UI: dùng để hiện số sát thương, chữ "Miss/Block", báo hạ gục...</summary>
public readonly struct Damage_Result
{
    public readonly Damage_Outcome outcome;
    public readonly bool isCritical;

    /// <summary>Lượng máu thực sự bị trừ (sau né/đỡ/giảm và sau khi kẹp theo máu còn lại).</summary>
    public readonly float damageDealt;

    /// <summary>Đòn này là đòn kết liễu.</summary>
    public readonly bool wasKilled;

    public Damage_Result(Damage_Outcome outcome, bool isCritical, float damageDealt, bool wasKilled)
    {
        this.outcome = outcome;
        this.isCritical = isCritical;
        this.damageDealt = damageDealt;
        this.wasKilled = wasKilled;
    }

    public static Damage_Result Ignored => new Damage_Result(Damage_Outcome.Ignored, false, 0f, false);
}
