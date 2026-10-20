/// <summary>
/// Cách một modifier tác động lên chỉ số.
/// Công thức: Final = (Base + tổng Flat) * (1 + tổng PercentAdd) * (tích các (1 + PercentMult))
/// </summary>
public enum Player_Stat_Modifier_Type
{
    Flat,        // Cộng thẳng: +5
    PercentAdd,  // Cộng dồn phần trăm với nhau: 0.10 = +10% (nhiều cái cộng lại rồi mới nhân 1 lần)
    PercentMult  // Nhân riêng từng cái: 0.10 = x1.10 (mỗi cái nhân độc lập)
}
