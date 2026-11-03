using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tooltip skill: tên, loại, mô tả, mana, hồi chiêu, rồi các dòng do từng Skill_Effect tự mô tả (Skill_Preview.Lines).
/// Skill_Bar_UI tạo và gọi Show/Hide; tooltip tự dựng giao diện bằng code, đi theo con trỏ chuột và luôn nằm trong màn hình.
/// Không nhận chuột (không chặn click).
/// </summary>
public class Skill_Tooltip_UI : MonoBehaviour
{
    private const float PanelWidth = 300f;
    private const float Padding = 12f;
    private const float CursorOffset = 18f;

    private Canvas canvas;
    private RectTransform panel;
    private TextMeshProUGUI text;
    private readonly StringBuilder sb = new StringBuilder();

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        Build();
        Hide();
    }

    private void LateUpdate()
    {
        if (IsVisible) FollowCursor();
    }

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    /// <summary>Hiện tooltip của skill. hasPreview = false (hoặc preview null) thì chỉ hiện phần thông tin cơ bản.</summary>
    public void Show(Skill_Data skill, bool hasPreview, Skill_Preview preview, Character_Stats owner)
    {
        if (skill == null) return;

        text.text = BuildText(skill, hasPreview, preview, owner);

        canvas.gameObject.SetActive(true);

        // Cần biết kích thước thật của bảng để đặt vị trí đúng ngay frame đầu.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        FollowCursor();
    }

    // ------------------------------------------------------------------
    // Nội dung
    // ------------------------------------------------------------------

    private string BuildText(Skill_Data skill, bool hasPreview, Skill_Preview preview, Character_Stats owner)
    {
        sb.Clear();

        sb.Append("<size=130%><b>").Append(skill.displayName).Append("</b></size>\n");
        sb.Append("<color=#8FA8C8>").Append(skill.category).Append("</color>\n");

        if (!string.IsNullOrWhiteSpace(skill.description))
        {
            sb.Append("<color=#B8B8B8><i>").Append(skill.description.Trim()).Append("</i></color>\n");
        }

        if (skill.manaCost > 0f)
        {
            bool lacksMana = owner != null && owner.CurrentMana < skill.manaCost;
            sb.Append("Mana: ").Append(lacksMana ? "<color=#FF6B6B>" : "<color=#7FB2FF>")
              .Append(skill.manaCost.ToString("0.#")).Append("</color>\n");
        }

        if (!hasPreview || preview == null) return sb.ToString().TrimEnd();

        AppendCooldown(preview);

        // Các dòng do từng Skill_Effect tự thêm (Damage, Critical, DPS, Heal, làm chậm...).
        bool hasDamage = false;
        foreach (Skill_Preview_Line line in preview.Lines)
        {
            sb.Append(line.Label).Append(": <color=").Append(LineColor(line.Label)).Append('>')
              .Append(line.Value).Append("</color>\n");

            if (line.Label == "Damage") hasDamage = true;
        }

        if (hasDamage) sb.Append("<size=80%><color=#8A8A8A>Per target, before enemy defence.</color></size>");
        return sb.ToString().TrimEnd();
    }

    private void AppendCooldown(Skill_Preview preview)
    {
        if (preview.Cooldown <= 0f || float.IsInfinity(preview.Cooldown)) return;

        sb.Append("Cooldown: <color=#FFFFFF>").Append(preview.Cooldown.ToString("0.##")).Append("s</color>\n");
    }

    // Màu theo nhãn; effect mới có nhãn lạ thì dùng màu trắng, không cần sửa file này.
    private static string LineColor(string label)
    {
        switch (label)
        {
            case "Damage": return "#FFD27F";
            case "Critical": return "#FFB347";
            case "Heal": return "#7FE08A";
            default: return "#FFFFFF";
        }
    }

    // ------------------------------------------------------------------
    // Vị trí
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
    // Dựng giao diện
    // ------------------------------------------------------------------

    private void Build()
    {
        canvas = UI_Builder.CreateTooltip("Skill_Tooltip_Canvas", transform, 110, PanelWidth, Padding, out panel, out text); // Trên popup chọn skill (100).
    }
}
