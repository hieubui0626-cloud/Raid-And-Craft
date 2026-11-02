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
    private const float PanelWidth = 320f;
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
    public void Show(Item_Instance instanceToShow)
    {
        if (instanceToShow == null) return;

        sb.Clear();
        AppendHeader(instanceToShow.Base, instanceToShow.Tier, instanceToShow.Base.EquipSlot != Equipment_Slot.None
            ? "Tier " + instanceToShow.Tier + "  |  " + Humanize(instanceToShow.Base.EquipSlot.ToString())
            : "Tier " + instanceToShow.Tier);

        AppendDescription(instanceToShow.Base);
        AppendWeaponInfo(instanceToShow.Base as Weapon_Data);

        foreach (Weapon_Stat_Modifier m in instanceToShow.BaseModifiers) AppendModifier(m, BaseColor);
        foreach (Weapon_Stat_Modifier m in instanceToShow.AffixModifiers) AppendModifier(m, AffixColor);

        if (instanceToShow.Influ != null)
        {
            sb.Append("<color=").Append(InfluColor).Append('>').Append(instanceToShow.Influ.Describe(instanceToShow.InfluRoll)).Append("</color>\n");
        }

        Display();
    }

    /// <summary>Tooltip của vật phẩm xếp chồng (không có instance): Rune, Spell Stone, thuốc, nguyên liệu.</summary>
    public void Show(Item_Data item, int count)
    {
        if (item == null) return;

        sb.Clear();
        AppendHeader(item, 1, null);
        AppendDescription(item);

        if (item is Rune_Data rune)
        {
            foreach (Weapon_Stat_Modifier m in rune.StatModifiers) AppendModifier(m, AffixColor);
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

        if (item.CanStack)
        {
            sb.Append("<color=").Append(MutedColor).Append(">Stack: ").Append(count).Append('/').Append(item.MaxStack).Append("</color>\n");
        }

        Display();
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
    private void AppendModifier(Weapon_Stat_Modifier m, string color)
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
        var canvasObject = new GameObject("Item_Tooltip_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 115; // Trên Inventory/Skill tooltip (110), dưới vật phẩm đang cầm (120).

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(canvasObject.transform, false);
        panel = (RectTransform)panelObject.transform;
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        background.raycastTarget = false;

        var layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)Padding, (int)Padding, (int)Padding, (int)Padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(panel, false);

        text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.richText = true;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }
}
