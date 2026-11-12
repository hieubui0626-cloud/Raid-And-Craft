using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Overview: hiện chỉ số của Player. Mỗi Layer (Complex / Offence / Defence / Misc) có thể hiện ở một
/// TextMeshPro riêng (một canvas/panel riêng): điền danh sách Layer Views, mỗi phần tử = một Layer + một text.
/// Danh sách Layer Views trống thì dùng ô Stats Text cũ (tất cả Layer gộp chung một text) để scene cũ không hỏng.
/// </summary>
public class Player_Stats_UI : MonoBehaviour
{
    [Serializable]
    private struct Layer_View
    {
        public Player_Stat_Layer layer;
        public TextMeshProUGUI text;
    }

    [Header("References")]
    [SerializeField] private Character_Stats player_Stats;
    [SerializeField] private Character_Level player_Level;

    [Header("Character Overview - mỗi Layer một text riêng")]
    [Tooltip("Mỗi phần tử: chọn Layer và kéo TextMeshPro của canvas/panel hiển thị Layer đó vào. " +
             "Để trống thì dùng Stats Text bên dưới (gộp mọi Layer).")]
    [SerializeField] private List<Layer_View> layerViews = new List<Layer_View>();

    [Tooltip("Bật: mỗi text có dòng tiêu đề tên Layer (ví dụ OFFENCE). Tắt nếu panel đã có tiêu đề riêng.")]
    [SerializeField] private bool showLayerTitle = true;

    [Tooltip("Cách cũ: một text chứa mọi Layer. Chỉ dùng khi Layer Views trống.")]
    [SerializeField] private TextMeshProUGUI stats_Text;

    [Tooltip("Chacracter Overview")]
    [SerializeField] private TextMeshProUGUI CharacterName;
    [SerializeField] private TextMeshProUGUI CharacterLevel;

    [Header("Combat UI References")]
    [SerializeField] private TextMeshProUGUI Combat_UI_Text_Health;
    [SerializeField] private GameObject Combat_UI_Health_Bar;
    [SerializeField] private TextMeshProUGUI Combat_UI_Text_Mana;
    [SerializeField] private GameObject Combat_UI_Mana_Bar;

    // Tách "PhysicAttack" thành "Physic Attack" (không chèn khoảng trắng ở đầu chuỗi).
    private static readonly Regex CamelCaseSplitter = new Regex("(?<!^)([A-Z])", RegexOptions.Compiled);

    private readonly StringBuilder sb = new StringBuilder();

    private void OnEnable()
    {
        if (player_Stats == null) return;

        player_Stats.OnStatsChanged += Update_Overview_UI;
        player_Stats.OnStatsChanged += Update_Combat_UI;   // Max đổi
        player_Stats.OnVitalsChanged += Update_Combat_UI;

        Update_Overview_UI(); // Vẽ lại ngay: chỉ số có thể đã đổi lúc UI đang tắt.
        Update_Combat_UI();
    }

    private void OnDisable()
    {
        if (player_Stats == null) return;

        player_Stats.OnStatsChanged -= Update_Overview_UI;
        player_Stats.OnStatsChanged -= Update_Combat_UI;
        player_Stats.OnVitalsChanged -= Update_Combat_UI;
    }

    void Update_Combat_UI()
    {
        if (player_Stats == null) return;

        UpdateVital(Combat_UI_Text_Health, Combat_UI_Health_Bar, player_Stats.CurrentHealth, player_Stats.MaxHealth);
        UpdateVital(Combat_UI_Text_Mana, Combat_UI_Mana_Bar, player_Stats.CurrentMana, player_Stats.MaxMana);
    }

    // Thiếu text hoặc thanh nào thì bỏ qua phần đó, không lỗi; Max = 0 thì thanh về 0 thay vì chia cho 0.
    private static void UpdateVital(TextMeshProUGUI text, GameObject bar, float current, float max)
    {
        if (text != null) text.text = FormatValue((int)current);

        if (bar != null && bar.TryGetComponent(out Slider slider))
        {
            slider.value = max > 0f ? current / max : 0f;
        }
    }

    private void Update_Overview_UI()
    {
        if (player_Stats == null) return;

        if(player_Level != null)
        {
            CharacterLevel.text = "Level:" + player_Level.Level.ToString();
        }
        
        if (Save_Manager.Instance != null)
        {
            CharacterName.text = Save_Manager.Instance.CharacterName;

        }

        if (layerViews != null && layerViews.Count > 0)
        {
            // Mỗi Layer một text riêng.
            foreach (Layer_View view in layerViews)
            {
                if (view.text == null) continue;

                sb.Clear();
                AppendLayer(view.layer, showLayerTitle);
                view.text.text = sb.ToString();
            }
            return;
        }

        // Cách cũ: mọi Layer gộp chung một text.
        if (stats_Text == null) return;

        sb.Clear();
        foreach (Player_Stat_Layer layer in Enum.GetValues(typeof(Player_Stat_Layer)))
        {
            if (AppendLayer(layer, true)) sb.AppendLine(); // Dòng trống giữa các Layer
        }
        
        stats_Text.text = sb.ToString();
    }

    // Thêm các dòng chỉ số của một Layer vào sb. Trả về false nếu Layer không có chỉ số nào.
    private bool AppendLayer(Player_Stat_Layer layer, bool withTitle)
    {
        var statsInLayer = player_Stats.GetStatsInLayer(layer);
        if (statsInLayer.Count == 0) return false;

        if (withTitle) sb.AppendLine($"<color=orange><b> {layer.ToString().ToUpper()} </b></color>");

        foreach (Player_Stat stat in statsInLayer)
        {
            string line = BuildLine(stat);
            if (line != null) sb.AppendLine(line);
        }
        return true;
    }

    private string BuildLine(Player_Stat stat)
    {
        switch (stat.Type)
        {
            
            case Player_Stat_Type.PhysicDamageMin:
                return DamageRangeLine("Physic Damage", stat.Value, player_Stats.GetValue(Player_Stat_Type.PhysicDamageMax));
            case Player_Stat_Type.MagicDamageMin:
                return DamageRangeLine("Magic Damage", stat.Value, player_Stats.GetValue(Player_Stat_Type.MagicDamageMax));
            case Player_Stat_Type.PhysicDamageMax:
            case Player_Stat_Type.MagicDamageMax:
                return null; // đã hiện chung dòng Min
            case Player_Stat_Type.Strength:
                return $"  Strength: <color=Red>{FormatValue(stat.Value)}</color>";
            case Player_Stat_Type.Agility:
                return $"  Agility: <color=Green>{FormatValue(stat.Value)}</color>";
            case Player_Stat_Type.Intelligence:
                return $"  Intelligen:<color=Blue>{FormatValue(stat.Value)}</color>";
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

            case Player_Stat_Type.MagicResistance:
                return $"  Magic Resistance: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

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

            // Misc
            case Player_Stat_Type.ChainProjectile:
                return $"  Chain Projectile: <color=#FFFFFF>{FormatValue(stat.Value)}</color>";

            case Player_Stat_Type.AreaModifier:
                return $"  Area Modifier: <color=#FFFFFF>{FormatValue(stat.Value)}%</color>";

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

    private static string DamageRangeLine(string label, float a, float b)
    {
        return $"  {label}: <color=#FFFFFF>{FormatValue(Mathf.Min(a, b))} - {FormatValue(Mathf.Max(a, b))}</color>";
    }
}
