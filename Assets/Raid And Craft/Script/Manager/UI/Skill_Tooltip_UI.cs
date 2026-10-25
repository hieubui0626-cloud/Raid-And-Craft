using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tooltip skill: tên, mô tả, mana, hồi chiêu, sát thương (thường/chí mạng/DPS) tính theo chỉ số hiện tại, cách ra đòn.
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

    /// <summary>Hiện tooltip của skill. hasPreview = false (không tính được sát thương) thì bỏ phần sát thương.</summary>
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

        if (!hasPreview) return sb.ToString().TrimEnd();

        AppendCooldown(preview);
        AppendDamage(preview);
        AppendDelivery(preview.delivery);

        sb.Append("<size=80%><color=#8A8A8A>Per target, before enemy defence.</color></size>");
        return sb.ToString();
    }

    private void AppendCooldown(Skill_Preview preview)
    {
        if (preview.cooldown <= 0f || float.IsInfinity(preview.cooldown)) return;

        sb.Append("Cooldown: <color=#FFFFFF>").Append(preview.cooldown.ToString("0.##")).Append("s</color>\n");
    }

    private void AppendDamage(Skill_Preview preview)
    {
        float hit = preview.HitDamage;

        if (hit <= 0f)
        {
            sb.Append("Damage: <color=#FF6B6B>0</color> (damage stat is 0)\n");
            return;
        }

        sb.Append("Damage: <color=#FFD27F>").Append(hit.ToString("0.#")).Append("</color>");

        // Chi tiết từng loại sát thương.
        sb.Append(" (");
        bool first = true;
        foreach (Damage_Type type in Damage_Info.AllTypes)
        {
            float amount = preview.hit.Get(type);
            if (amount <= 0f) continue;

            if (!first) sb.Append(" + ");
            sb.Append(amount.ToString("0.#")).Append(' ').Append(type);
            first = false;
        }
        sb.Append(")\n");

        sb.Append("Critical: <color=#FFB347>").Append(preview.CritDamage.ToString("0.#")).Append("</color>\n");

        if (preview.Dps > 0f)
        {
            sb.Append("DPS: <color=#FFFFFF>").Append(preview.Dps.ToString("0.#")).Append("</color>\n");
        }
    }

    private void AppendDelivery(Skill_Delivery delivery)
    {
        if (delivery.type == Skill_Delivery_Type.Melee_Arc)
        {
            sb.Append("Melee arc: ").Append(delivery.range.ToString("0.#")).Append(" m, ")
              .Append(delivery.arcAngle.ToString("0")).Append("°, up to ")
              .Append(delivery.maxTargets).Append(delivery.maxTargets == 1 ? " target\n" : " targets\n");
        }
        else
        {
            sb.Append("Projectile: speed ").Append(delivery.projectileSpeed.ToString("0.#"))
              .Append(", range ").Append((delivery.projectileSpeed * delivery.projectileLifetime).ToString("0.#")).Append(" m");

            if (delivery.pierce > 0) sb.Append(", pierces ").Append(delivery.pierce);
            sb.Append('\n');
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
        var canvasObject = new GameObject("Skill_Tooltip_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110; // Trên popup chọn skill (100).

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
