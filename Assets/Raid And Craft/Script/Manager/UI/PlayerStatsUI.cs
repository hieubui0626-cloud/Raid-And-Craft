using UnityEngine;
using TMPro;
using System.Text;
using System;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshProUGUI statsText;

    private void OnEnable()
    {
        if (playerStats != null) playerStats.OnStatsChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.OnStatsChanged -= UpdateUI;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (statsText == null || playerStats == null) return;

        StringBuilder sb = new StringBuilder();

        // Duyệt qua tất cả các Layer có trong Enum Player_StatLayer
        foreach (Player_StatLayer layer in Enum.GetValues(typeof(Player_StatLayer)))
        {
            var statsInLayer = playerStats.GetStatsInLayer(layer);
            if (statsInLayer == null || statsInLayer.Count == 0) continue;

            // Vẽ tiêu đề của Layer (Ví dụ: [ === STATS === ])
            sb.AppendLine($"<color=yellow><b>=== {layer.ToString().ToUpper()} ===</b></color>");

            // Duyệt và in ra các chỉ số thuộc Layer này
            foreach (var stat in statsInLayer)
            {
                // Format tên chỉ số cho đẹp mắt (Ví dụ: PhysicAttack -> Physic Attack)
                string statName = SplitCamelCase(stat.Key.ToString());
                sb.AppendLine($"  {statName}: <color=#FFFFFF>{stat.Value}</color>");
            }

            sb.AppendLine(); // Thêm 1 dòng trống giữa các Layer cho thoáng
        }

        statsText.text = sb.ToString();
    }

    // Hàm phụ trợ giúp biến "PhysicAttack" thành "Physic Attack" khi hiển thị trên UI
    private string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
    }
}
