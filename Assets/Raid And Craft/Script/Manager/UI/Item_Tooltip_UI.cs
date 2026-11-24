using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tooltip vật phẩm: tên (màu theo Tier), loại, mô tả, chỉ số Base / Affix / Influ của từng món trang bị,
/// hoặc thông tin vật phẩm xếp chồng (Rune, Spell Stone, thuốc, nguyên liệu).
/// Dùng qua Item_Tooltip_UI.Instance.Show(...) / Hide(): tự tạo khi cần, tự dựng giao diện bằng code,
/// đi theo con trỏ và luôn nằm trong màn hình. Không nhận chuột (không chặn click).
/// </summary>
public class Item_Tooltip_UI : MonoBehaviour
{
    //private const float PanelWidth = 320f;
    private const float Padding = 12f;
    private const float CursorOffset = 18f;

    private const string BaseColor = "#FFFFFF";
    private const string AffixColor = "#7FB2FF";
    private const string InfluColor = "#C58BFF";
    private const string MutedColor = "#8FA8C8";

    private static Item_Tooltip_UI instance;

    /// <summary>Tooltip dùng chung, tự tạo ở lần gọi đầu tiên.</summary>
    public static Item_Tooltip_UI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<Item_Tooltip_UI>();
            if (instance == null) instance = new GameObject("Item_Tooltip_UI").AddComponent<Item_Tooltip_UI>();
            return instance;
        }
    }

    /// <summary>Ẩn tooltip nếu nó đã được tạo (không tự tạo mới chỉ để ẩn).</summary>
    public static void HideIfExists()
    {
        if (instance != null) instance.Hide();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private Canvas canvas;
    private RectTransform panel;
    private TextMeshProUGUI text;
    private readonly StringBuilder sb = new StringBuilder();

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        if (instance == null) instance = this;
        Build();
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        if (IsVisible) FollowCursor();
    }

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // HIỆN
    // ------------------------------------------------------------------

    public void Show(Grid_Placement placement)
    {
        if (placement.IsEmpty) return;

        if (placement.Instance != null) Show(placement.Instance);
        else Show(placement.Item, placement.Count);
    }

    /// <summary>Tooltip của một món trang bị cụ thể (chỉ số riêng của món đó).</summary>
    public void Show(Item_Instance instanceToShow, string footer = null)
    {
        if (instanceToShow == null) return;

        sb.Clear();
        AppendHeader(instanceToShow.Base, instanceToShow.Tier, instanceToShow.Base.EquipSlot != Equipment_Slot.None
            ? "Tier " + instanceToShow.Tier + "  |  " + Humanize(instanceToShow.Base.EquipSlot.ToString())
            : "Tier " + instanceToShow.Tier);

        AppendDescription(instanceToShow.Base);
        AppendWeaponInfo(instanceToShow.Base as Weapon_Data);

        AppendBaseModifiers(instanceToShow.BaseModifiers);
        foreach (Affix_Line line in instanceToShow.AffixLines) AppendAffixLine(line, AffixColor);

        if (instanceToShow.Influ != null)
        {
            sb.Append("<color=").Append(InfluColor).Append('>').Append(instanceToShow.Influ.Describe(instanceToShow.InfluRoll)).Append("</color>\n");
        }

        AppendFooter(footer);
        Display();
    }

    /// <summary>Tooltip của vật phẩm xếp chồng (không có instance): Rune, Spell Stone, thuốc, nguyên liệu.</summary>
    public void Show(Item_Data item, int count, string footer = null)
    {
        if (item == null) return;

        sb.Clear();
        AppendHeader(item, 1, null);
        AppendDescription(item);

        if (item is Rune_Data rune)
        {
            foreach (Stat_Modifier_Entry m in rune.StatModifiers) AppendModifier(m, AffixColor);
        }
        else if (item is Spell_Stone_Data stone)
        {
            if (stone.Skill != null) sb.Append("Skill: <color=").Append(AffixColor).Append('>').Append(stone.Skill.displayName).Append("</color>\n");
        }
        else if (item is Restore_Potion_Data potion)
        {
            if (potion.HealthRestore > 0f) sb.Append("Restore <color=#7FE08A>").Append(potion.HealthRestore.ToString("0.#")).Append("</color> Health\n");
            if (potion.ManaRestore > 0f) sb.Append("Restore <color=#7FB2FF>").Append(potion.ManaRestore.ToString("0.#")).Append("</color> Mana\n");
        }

        if (item is Consumable_Item_Data consumable && consumable.Family != null && consumable.Family.Cooldown > 0f)
        {
            sb.Append("Cooldown: <color=#FFD27F>").Append(consumable.Family.Cooldown.ToString("0.#")).Append("s</color> <color=")
              .Append(MutedColor).Append(">(").Append(consumable.Family.DisplayName).Append(")</color>\n");
        }

        if (item.CanStack)
        {
            sb.Append("<color=").Append(MutedColor).Append(">Stack: ").Append(count).Append('/').Append(item.MaxStack).Append("</color>\n");
        }

        AppendFooter(footer);
        Display();
    }

    // Dòng thêm cuối tooltip (ví dụ giá trong cửa hàng). Rich text được.
    private void AppendFooter(string footer)
    {
        if (string.IsNullOrEmpty(footer)) return;

        sb.Append('\n').Append(footer).Append('\n');
    }

    private void Display()
    {
        text.text = sb.ToString().TrimEnd();

        canvas.gameObject.SetActive(true);

        // Cần biết kích thước thật của bảng để đặt vị trí đúng ngay frame đầu.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        FollowCursor();
    }

    // ------------------------------------------------------------------
    // NỘI DUNG
    // ------------------------------------------------------------------

    private void AppendHeader(Item_Data item, int tier, string subtitle)
    {
        sb.Append("<size=130%><b><color=").Append(TierColor(tier)).Append('>').Append(item.DisplayName).Append("</color></b></size>\n");
        if (!string.IsNullOrEmpty(subtitle)) sb.Append("<color=").Append(MutedColor).Append('>').Append(subtitle).Append("</color>\n");
    }

    private void AppendDescription(Item_Data item)
    {
        if (string.IsNullOrWhiteSpace(item.Description)) return;

        sb.Append("<color=#B8B8B8><i>").Append(item.Description.Trim()).Append("</i></color>\n");
    }

    private void AppendWeaponInfo(Weapon_Data weapon)
    {
        if (weapon == null) return;

        if (weapon.damageTypes != null && weapon.damageTypes.Length > 0)
        {
            sb.Append("Damage: <color=#FFD27F>");
            for (int i = 0; i < weapon.damageTypes.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(weapon.damageTypes[i]);
            }
            sb.Append("</color>");
            if (!Mathf.Approximately(weapon.damageMultiplier, 1f)) sb.Append("  x").Append(weapon.damageMultiplier.ToString("0.##"));
            sb.Append('\n');
        }
    }

    // Flat: "+5 Stat" | PercentAdd: "+10% Stat" (giá trị x100) | PercentMult: "x1.1 Stat".
    // Một dòng Affix: có Format thì hiện MỘT dòng theo mẫu ({0}, {1}... là giá trị từng chỉ số), không thì mỗi chỉ số một dòng.
    private void AppendAffixLine(Affix_Line line, string color)
    {
        if (string.IsNullOrEmpty(line.format))
        {
            foreach (Stat_Modifier_Entry m in line.Modifiers) AppendModifier(m, color);
            return;
        }

        string text = line.format;
        for (int i = 0; i < line.Modifiers.Count; i++)
        {
            Stat_Modifier_Entry m = line.Modifiers[i];
            string value = m.modifierType == Player_Stat_Modifier_Type.Flat
                ? m.value.ToString("0.##")
                : (m.value * 100f).ToString("0.##") + "%";
            text = text.Replace("{" + i + "}", value);
        }

        sb.Append("<color=").Append(color).Append('>').Append(text).Append("</color>\n");
    }

    private void AppendModifier(Stat_Modifier_Entry m, string color)
    {
        string stat = Humanize(m.stat.ToString());
        string line;

        switch (m.modifierType)
        {
            case Player_Stat_Modifier_Type.PercentAdd:
                line = Signed(m.value * 100f) + "% " + stat;
                break;
            case Player_Stat_Modifier_Type.PercentMult:
                line = "x" + (1f + m.value).ToString("0.##") + " " + stat;
                break;
            default:
                line = Signed(m.value) + " " + stat;
                break;
        }

        sb.Append("<color=").Append(color).Append('>').Append(line).Append("</color>\n");
    }

    private static string Signed(float value)
    {
        return (value >= 0f ? "+" : "") + value.ToString("0.##");
    }

    // "PhysicDamage" -> "Physic Damage".
    private static string Humanize(string name)
    {
        return Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
    }

    private static string TierColor(int tier)
    {
        switch (tier)
        {
            case 1: return "#FFFFFF";
            case 2: return "#7FB2FF";
            case 3: return "#C58BFF";
            case 4: return "#FFB347";
            default: return "#FF6B6B";
        }
    }

    // ------------------------------------------------------------------
    // VỊ TRÍ
    // ------------------------------------------------------------------

    // Góc trên-trái của bảng đặt chếch dưới-phải con trỏ; không đủ chỗ thì lật sang trái/lên trên.
    private void FollowCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 cursor = mouse.position.ReadValue();
        float scale = canvas.scaleFactor;
        float width = panel.rect.width * scale;
        float height = panel.rect.height * scale;

        bool flipX = cursor.x + CursorOffset + width > Screen.width;
        bool flipY = cursor.y - CursorOffset - height < 0f;

        panel.pivot = new Vector2(flipX ? 1f : 0f, flipY ? 0f : 1f);
        panel.position = new Vector2(
            cursor.x + (flipX ? -CursorOffset : CursorOffset),
            cursor.y + (flipY ? CursorOffset : -CursorOffset));
    }

    // ------------------------------------------------------------------
    // DỰNG GIAO DIỆN
    // ------------------------------------------------------------------

    private void Build()
    {
        canvas = UI_Builder.CreateTooltip("Item_Tooltip_Canvas", transform, 115, 0f, Padding, out panel, out text); // Trên Inventory/Skill tooltip (110), dưới vật phẩm đang cầm (120).
    }

    private void AppendBaseModifiers(
    IReadOnlyList<Stat_Modifier_Entry> modifiers)
    {
        if (modifiers == null)
            return;

        bool[] consumed = new bool[modifiers.Count];

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (consumed[i])
                continue;

            Stat_Modifier_Entry minModifier = modifiers[i];

            if (TryFindDamageMaxModifier(
                    minModifier,
                    modifiers,
                    out int maxIndex,
                    out Stat_Modifier_Entry maxModifier))
            {
                string damageName = GetDamageName(minModifier.stat);

                string minValue = Mathf.Abs(minModifier.value).ToString("0.##");
                string maxValue = Mathf.Abs(maxModifier.value).ToString("0.##");

                string line =
                    "Add " +
                    minValue +
                    " to " +
                    maxValue +
                    " " +
                    damageName;

                sb.Append("<color=")
                    .Append(BaseColor)
                    .Append('>')
                    .Append(line)
                    .Append("</color>\n");

                consumed[i] = true;
                consumed[maxIndex] = true;
                continue;
            }

            AppendModifier(minModifier, BaseColor);
            consumed[i] = true;
        }
    }

    private static bool TryFindDamageMaxModifier(
        Stat_Modifier_Entry minModifier,
        IReadOnlyList<Stat_Modifier_Entry> modifiers,
        out int maxIndex,
        out Stat_Modifier_Entry maxModifier)
    {
        maxIndex = -1;
        maxModifier = default;

        Player_Stat_Type maxStat;

        switch (minModifier.stat)
        {
            case Player_Stat_Type.PhysicDamageMin:
                maxStat = Player_Stat_Type.PhysicDamageMax;
                break;

            case Player_Stat_Type.MagicDamageMin:
                maxStat = Player_Stat_Type.MagicDamageMax;
                break;

            default:
                return false;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            Stat_Modifier_Entry candidate = modifiers[i];

            if (candidate.stat != maxStat)
                continue;

            if (candidate.modifierType != minModifier.modifierType)
                continue;

            maxIndex = i;
            maxModifier = candidate;
            return true;
        }

        return false;
    }

    private static string GetDamageName(Player_Stat_Type stat)
    {
        switch (stat)
        {
            case Player_Stat_Type.PhysicDamageMin:
            case Player_Stat_Type.PhysicDamageMax:
                return "Physical Damage";

            case Player_Stat_Type.MagicDamageMin:
            case Player_Stat_Type.MagicDamageMax:
                return "Magic Damage";

            default:
                return stat.ToString();
        }
    }
}
