using System;

/// <summary>
/// Gói dữ liệu của MỘT đòn đánh, do bên tấn công dựng (Combat_Formulas.BuildAttack) rồi đưa cho
/// IDamageable.TakeDamage(Damage_Info). Bên nhận tự tính né/đỡ/giảm sát thương bằng chỉ số của nó.
/// Là struct nên truyền đi không tạo rác (GC).
/// </summary>
public struct Damage_Info
{
    /// <summary>Ai gây ra đòn này (có thể null: bẫy, môi trường...). Dùng cho aggro, kill credit, thorn...</summary>
    public Character_Stats source;

    // Sát thương THÔ theo từng loại, đã nhân hệ số kỹ năng và hệ số chí mạng (nếu có).
    public float physic;
    public float magic;
    public float fire;
    public float cold;
    public float lightning;
    public float chaos;

    /// <summary>Chỉ số Accuracy của bên tấn công, dùng để so với Evade bên nhận.</summary>
    public float accuracy;

    /// <summary>Đòn này đã được tung chí mạng (chỉ để hiển thị/hiệu ứng, sát thương đã nhân sẵn).</summary>
    public bool isCritical;

    /// <summary>Đòn không thể bị né (ví dụ vùng tác động, DoT...).</summary>
    public bool cannotBeEvaded;

    /// <summary>Đòn không thể bị đỡ.</summary>
    public bool cannotBeBlocked;

    /// <summary>
    /// Đòn này không được phép tạo Ailment mới. Dùng cho DoT tick, phản sát thương
    /// hoặc damage phụ để ngăn vòng lặp Ailment.
    /// Mặc định false để không làm thay đổi hành vi các đòn đánh hiện tại.
    /// </summary>
    public bool cannotApplyAilments;

    /// <summary>Đánh dấu damage tick theo thời gian; hiện chỉ là metadata, không tự đổi công thức damage.</summary>
    public bool isDamageOverTime;

    public float Get(Damage_Type type)
    {
        switch (type)
        {
            case Damage_Type.Physic: return physic;
            case Damage_Type.Magic: return magic;
            case Damage_Type.Fire: return fire;
            case Damage_Type.Cold: return cold;
            case Damage_Type.Lightning: return lightning;
            case Damage_Type.Chaos: return chaos;
            default: return 0f;
        }
    }

    /// <summary>Gán sát thương không âm; damage âm không được dùng để hồi máu.</summary>
    public void Set(Damage_Type type, float value)
    {
        float safeValue = Math.Max(0f, value);
        switch (type)
        {
            case Damage_Type.Physic: physic = safeValue; break;
            case Damage_Type.Magic: magic = safeValue; break;
            case Damage_Type.Fire: fire = safeValue; break;
            case Damage_Type.Cold: cold = safeValue; break;
            case Damage_Type.Lightning: lightning = safeValue; break;
            case Damage_Type.Chaos: chaos = safeValue; break;
        }
    }

    /// <summary>Tổng sát thương thô hợp lệ (chưa giảm), bỏ qua giá trị âm/NaN.</summary>
    public float TotalRaw =>
        Safe(physic) + Safe(magic) + Safe(fire) +
        Safe(cold) + Safe(lightning) + Safe(chaos);

    public bool HasDamage(Damage_Type type) => Safe(Get(type)) > 0f;

    /// <summary>Trả về bản sao với mọi loại sát thương nhân thêm một hệ số không âm.</summary>
    public Damage_Info Scaled(float multiplier)
    {
        Damage_Info copy = this;
        float safeMultiplier = Safe(multiplier);
        foreach (Damage_Type type in AllTypes)
            copy.Set(type, Safe(Get(type)) * safeMultiplier);
        return copy;
    }

    private static float Safe(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? 0f : value;
    }

    public static readonly Damage_Type[] AllTypes = (Damage_Type[])Enum.GetValues(typeof(Damage_Type));
}
