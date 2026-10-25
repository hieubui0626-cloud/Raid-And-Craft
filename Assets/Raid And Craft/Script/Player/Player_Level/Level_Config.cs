using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu hình tăng cấp (tạo asset: chuột phải Project -> Create -> Raid And Craft -> Level Config).
/// Một asset dùng chung được cho nhiều nhân vật; Player và từng loại Enemy có thể dùng asset khác nhau.
/// Cấp tối đa KHÔNG nằm ở đây mà là chỉ số MaxCharacterLevel của nhân vật (để Magic Circuit, trang bị, modifier... nâng trần được).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Level Config", fileName = "Level_Config")]
public class Level_Config : ScriptableObject
{
    /// <summary>Mỗi cấp (từ cấp 2 trở đi) cộng thêm perLevel vào chỉ số này (dạng Flat).</summary>
    [Serializable]
    public struct Growth
    {
        public Player_Stat_Type stat;
        public float perLevel;

        public Growth(Player_Stat_Type stat, float perLevel)
        {
            this.stat = stat;
            this.perLevel = perLevel;
        }
    }

    [Header("Chỉ số tăng theo cấp")]
    [Tooltip("Tổng cộng thêm = perLevel x (cấp - 1). Cấp 1 không được cộng gì. Mỗi chỉ số nên xuất hiện một lần. " +
             "Để trống nếu muốn chỉ số chỉ tăng qua Magic Circuit/trang bị.")]
    public List<Growth> growthPerLevel = new List<Growth>
    {
        new Growth(Player_Stat_Type.Strength, 1f),
        new Growth(Player_Stat_Type.Agility, 1f),
        new Growth(Player_Stat_Type.Intelligence, 1f),
    };

    [Tooltip("Tăng cấp xong thì hồi đầy máu/mana.")]
    public bool restoreOnLevelUp = true;

    [Header("Điểm Magic Circuit")]
    [Tooltip("Số điểm Magic Circuit có ngay ở cấp 1.")]
    [Min(0)] public int startingCircuitPoints = 1;

    [Tooltip("Mỗi cấp (từ cấp 2 trở đi) cho thêm bấy nhiêu điểm Magic Circuit.")]
    [Min(0)] public int circuitPointsPerLevel = 1;

    /// <summary>Tổng số điểm Magic Circuit mà một nhân vật đạt được khi ở cấp này (chưa trừ điểm đã tiêu).</summary>
    public int GetTotalCircuitPoints(int level)
    {
        return startingCircuitPoints + circuitPointsPerLevel * Mathf.Max(0, level - 1);
    }
}
