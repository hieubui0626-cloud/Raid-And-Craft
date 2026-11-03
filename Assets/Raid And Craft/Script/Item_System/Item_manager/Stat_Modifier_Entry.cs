using System;
using UnityEngine;

/// <summary>
/// Một dòng cộng chỉ số (ví dụ +8 PhysicDamage, hoặc +20% ActionSpeed).
/// Dùng chung cho trang bị, Rune, Influ và Item_Instance (affix).
/// Tên field (stat, modifierType, value) giữ nguyên bản cũ Stat_Modifier_Entry nên asset đã tạo không mất dữ liệu.
/// </summary>
[Serializable]
public struct Stat_Modifier_Entry
{
    public Player_Stat_Type stat;

    [Tooltip("Flat: cộng thẳng. PercentAdd: 0.1 = +10% (các PercentAdd cộng dồn). PercentMult: 0.1 = x1.1 (nhân riêng).")]
    public Player_Stat_Modifier_Type modifierType;

    public float value;
}
