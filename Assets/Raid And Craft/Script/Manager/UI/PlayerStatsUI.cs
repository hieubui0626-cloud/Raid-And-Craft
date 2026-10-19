using System;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshProUGUI statsText;

    // Tách "PhysicAttack" thành "Physic Attack" (không chèn khoảng trắng ở đầu chuỗi).
    private static readonly Regex CamelCaseSplitter = new Regex("(?<!^)([A-Z])", RegexOptions.Compiled);

    private readonly StringBuilder sb = new StringBuilder();

    private void OnEnable()
    {
        if (playerStats == null) return;

        playerStats.OnStatsChanged += UpdateUI;
        UpdateUI(); // Vẽ lại ngay: chỉ số có thể đã đổi lúc UI đang tắt.
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.OnStatsChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (statsText == null || playerStats == null) return;

        sb.Clear();

        // Duyệt qua tất cả các Layer có trong enum Player_StatLayer
        foreach (Player_StatLayer layer in Enum.GetValues(typeof(Player_StatLayer)))
        {
            var statsInLayer = playerStats.GetStatsInLayer(layer);
            if (statsInLayer.Count == 0) continue;

            // Tiêu đề Layer, ví dụ: === ATTACK ===
            sb.AppendLine($"<color=yellow><b>=== {layer.ToString().ToUpper()} ===</b></color>");

            foreach (Player_Stat stat in statsInLayer)
            {
                string statName = SplitCamelCase(stat.Type.ToString());
                sb.AppendLine($"  {statName}: <color=#FFFFFF>{FormatValue(stat.Value)}</color>");
            }

            sb.AppendLine(); // Dòng trống giữa các Layer
        }

        statsText.text = sb.ToString();
    }

    private static string SplitCamelCase(string input)
    {
        return CamelCaseSplitter.Replace(input, " $1");
    }

    // Số nguyên hiển thị gọn (15), số lẻ tối đa 2 chữ số thập phân (15.5).
    private static string FormatValue(float value)
    {
        return value.ToString("0.##");
    }
}
