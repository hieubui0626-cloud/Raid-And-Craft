using System;

/// <summary>
/// Cách một modifier tác động lên chỉ số.
/// Công thức: Final = (Base + tổng Flat) * (1 + tổng PercentAdd) * (tích các (1 + PercentMult))
/// </summary>
public enum Player_StatModifierType
{
    Flat,        // Cộng thẳng: +5
    PercentAdd,  // Cộng dồn phần trăm với nhau: 0.10 = +10% (nhiều cái cộng lại rồi mới nhân 1 lần)
    PercentMult  // Nhân riêng từng cái: 0.10 = x1.10 (mỗi cái nhân độc lập)
}

/// <summary>
/// Một thay đổi tạm thời/có nguồn gốc lên chỉ số (trang bị, buff, passive...).
/// Source dùng để gỡ hàng loạt: ví dụ tháo trang bị -> RemoveModifiersFromSource(item).
/// </summary>
public readonly struct Player_StatModifier : IEquatable<Player_StatModifier>
{
    public readonly float Value;
    public readonly Player_StatModifierType Type;
    public readonly object Source;

    public Player_StatModifier(float value, Player_StatModifierType type = Player_StatModifierType.Flat, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }

    public bool Equals(Player_StatModifier other)
    {
        return Value == other.Value && Type == other.Type && Equals(Source, other.Source);
    }

    public override bool Equals(object obj) => obj is Player_StatModifier other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Type, Source);
}
