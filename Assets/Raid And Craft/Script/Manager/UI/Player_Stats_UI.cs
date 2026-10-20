using System;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class Player_Stats_UI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player_Stats player_Stats;
    [SerializeField] private TextMeshProUGUI stats_Text;
    [SerializeField] private TextMeshProUGUI Combat_UI_Text_Health;
    [SerializeField] private TextMeshProUGUI Combat_UI_Text_Mana;

    // Tách "PhysicAttack" thành "Physic Attack" (không chèn khoảng trắng ở đầu chuỗi).
    private static readonly Regex CamelCaseSplitter = new Regex("(?<!^)([A-Z])", RegexOptions.Compiled);

    private readonly StringBuilder sb = new StringBuilder();

    private void LateUpdate()
    {
        Update_Combat_UI();
    }

    void Update_Combat_UI()
    {    
        if (Combat_UI_Text_Health != null || Combat_UI_Text_Mana != null)
        {
            Combat_UI_Text_Health.text = $"{FormatValue(player_Stats.CurrentHealth)}";
            Combat_UI_Text_Mana.text = $"{FormatValue(player_Stats.CurrentMana)}";
        }

        
    }

    private void OnEnable()
    {
        if (player_Stats == null) return;

        player_Stats.OnStatsChanged += Update_Overview_UI;
        player_Stats.OnVitalsChanged += Update_Overview_UI;
        Update_Overview_UI(); // Vẽ lại ngay: chỉ số có thể đã đổi lúc UI đang tắt.
    }

    private void OnDisable()
    {
        if (player_Stats == null) return;

        player_Stats.OnStatsChanged -= Update_Overview_UI;
        player_Stats.OnVitalsChanged -= Update_Overview_UI;
    }

    private void Update_Overview_UI()
    {
        if (stats_Text == null || player_Stats  == null) return;

        sb.Clear();

        // Duyệt qua tất cả các Layer có trong enum Player_Stat_Layer
        foreach (Player_Stat_Layer layer in Enum.GetValues(typeof(Player_Stat_Layer)))
        {
            var statsInLayer = player_Stats.GetStatsInLayer(layer);
            if (statsInLayer.Count == 0) continue;

            // Tiêu đề Layer, ví dụ: === ATTACK ===
            sb.AppendLine($"<color=orange><b> {layer.ToString().ToUpper()} </b></color>");

            foreach (Player_Stat stat in statsInLayer)
            {
                sb.AppendLine(BuildLine(stat));
            }

            sb.AppendLine(); // Dòng trống giữa các Layer
        }

        stats_Text.text = sb.ToString();
        
    }

    private string BuildLine(Player_Stat stat)
    {
        switch (stat.Type)
        {
            
            case Player_Stat_Type.LifeRegen:
                return $"  Life Regen: <color=#FFFFFF>{FormatValue(stat.Value)}</color> per second";

            case Player_Stat_Type.ManaRegen:
                return $"  Mana Regen: <color=#FFFFFF>{FormatValue(stat.Value)}</color> per second";

            case Player_Stat_Type.ActionSpeed:
                return $"  Action Speed: <color=#FFFFFF>{FormatValue(stat.Value)}</color> per second";

            case Player_Stat_Type.CriticalChance:
                return $"  Critical Chance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

            case Player_Stat_Type.CriticalDamage:
                return $"  Critical Damage: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";
            
            case Player_Stat_Type.FireResistance:
                return $"  Fire Resistance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

            case Player_Stat_Type.ColdResistance:
                return $"  Cold Resistance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

            case Player_Stat_Type.LightningResistance:
                return $"  Lightning Resistance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

            case Player_Stat_Type.ChaosResistance:
                return $"  Chaos Resistance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";
            case Player_Stat_Type.BlockChance:
                return $"  Block Chance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";
            default:
                return $"  {SplitCamelCase(stat.Type.ToString())}: <color=#FFFFFF>{FormatValue(stat.Value)}</color>";
        }
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
