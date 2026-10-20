using System;

/// <summary>
/// Một thay đổi tạm thời/có nguồn gốc lên chỉ số (trang bị, buff, passive...).
/// Source dùng để gỡ hàng loạt: ví dụ tháo trang bị -> RemoveModifiersFromSource(item).
/// </summary>
public readonly struct Player_Stat_Modifier : IEquatable<Player_Stat_Modifier>
{
    public readonly float Value;
    public readonly Player_Stat_Modifier_Type Type;
    public readonly object Source;

    public Player_Stat_Modifier(float value, Player_Stat_Modifier_Type type = Player_Stat_Modifier_Type.Flat, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }

    public bool Equals(Player_Stat_Modifier other)
    {
        return Value == other.Value && Type == other.Type && Equals(Source, other.Source);
    }

    public override bool Equals(object obj) => obj is Player_Stat_Modifier other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Type, Source);
}
