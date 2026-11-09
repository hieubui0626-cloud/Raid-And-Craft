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

    [Header("Kinh nghiệm (EXP)")]
    [Tooltip("EXP cần để lên từ cấp 1 lên cấp 2. Cấp N cần: Base x N ^ Exponent (làm tròn). " +
             "Ví dụ Base 50, Exponent 1.6: cấp 1 -> 50, cấp 10 -> ~1990, cấp 50 -> ~26000.")]
    [Min(1)] public int baseExpRequired = 50;

    [Tooltip("Độ dốc đường cong EXP: 1 = tăng đều, càng cao càng khó lên cấp ở cấp cao.")]
    [Min(0f)] public float expExponent = 1.6f;

    /// <summary>EXP cần để lên từ cấp này lên cấp kế (tối thiểu 1).</summary>
    public int GetExpRequired(int level)
    {
        double required = baseExpRequired * System.Math.Pow(Mathf.Max(1, level), expExponent);
        return required >= int.MaxValue ? int.MaxValue : Mathf.Max(1, (int)System.Math.Round(required));
    }

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
