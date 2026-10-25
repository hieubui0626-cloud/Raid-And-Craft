using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Giao diện MỘT ô kỹ năng. Gắn lên Button của ô (1-5, Q-T); Skill_Bar_UI điều khiển.
/// Các phần hiển thị (icon, nhãn phím, tên skill, vòng hồi chiêu) tự được tạo thành object con nếu bạn chưa gán,
/// nên chỉ cần có sẵn Button + hình nền của ô. Muốn tuỳ biến kiểu dáng: tự tạo các object đó rồi kéo vào ô tương ứng.
/// </summary>
[RequireComponent(typeof(Button))]
public class Skill_Slot_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>Bắn khi người chơi bấm chuột vào ô này.</summary>
    public event Action<Skill_Slot_UI> OnClicked;

    /// <summary>Bắn khi con trỏ đi vào/rời ô (dùng cho tooltip). Chỉ cần quan tâm khi ô đang có skill.</summary>
    public event Action<Skill_Slot_UI> OnHoverEnter;
    public event Action<Skill_Slot_UI> OnHoverExit;

    [Header("Tuỳ chọn (bỏ trống thì tự tạo)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI hotkeyText;
    [SerializeField] private TextMeshProUGUI nameText;
    [Tooltip("Lớp phủ tối thể hiện hồi chiêu (thu nhỏ dần về đáy ô khi sắp hết hồi chiêu). Bỏ trống = tự tạo.")]
    [SerializeField] private RectTransform cooldownOverlay;

    [Header("Màu")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Màu icon khi không đủ mana.")]
    [SerializeField] private Color noManaColor = new Color(0.4f, 0.4f, 0.6f);

    private Button button;
    private Skill_Data skill;
    private bool built;

    public int Index { get; private set; }

    /// <summary>Skill đang gán trong ô (null = trống).</summary>
    public Skill_Data Skill => skill;
    public RectTransform Rect => (RectTransform)transform;

    private void Awake()
    {
        Build();
    }

    // Idempotent: object ô có thể đang tắt (Awake chưa chạy) khi Skill_Bar_UI gọi Init, nên Init cũng gọi Build.
    private void Build()
    {
        if (built) return;
        built = true;

        button = GetComponent<Button>();
        button.onClick.AddListener(() => OnClicked?.Invoke(this));

        EnsureParts();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke(this);
    }

    /// <summary>Skill_Bar_UI gọi một lần: chỉ số ô (0..9) và nhãn phím.</summary>
    public void Init(int index, string hotkeyLabel)
    {
        Build();
        Index = index;
        if (hotkeyText != null) hotkeyText.text = hotkeyLabel;
    }

    /// <summary>Đổi skill hiển thị trong ô (null = ô trống).</summary>
    public void SetSkill(Skill_Data newSkill)
    {
        skill = newSkill;

        bool hasIcon = newSkill != null && newSkill.icon != null;
        iconImage.enabled = hasIcon;
        iconImage.sprite = hasIcon ? newSkill.icon : null;

        // Skill chưa có icon thì hiện tên cho dễ phân biệt.
        nameText.text = (newSkill != null && !hasIcon) ? newSkill.displayName : string.Empty;

        if (newSkill == null) SetCooldownFraction(0f);
    }

    /// <summary>Cập nhật vòng hồi chiêu và màu thiếu mana (gọi mỗi frame).</summary>
    public void UpdateState(Skill_Caster caster, Character_Stats stats)
    {
        if (skill == null || caster == null) return;

        float duration = caster.GetCooldownDuration(skill);
        float remaining = caster.GetCooldownRemaining(skill);

        float fraction;
        if (remaining <= 0f) fraction = 0f;
        else if (float.IsInfinity(duration) || float.IsInfinity(remaining) || duration <= 0f) fraction = 1f;
        else fraction = Mathf.Clamp01(remaining / duration);

        SetCooldownFraction(fraction);

        bool lackMana = stats != null && skill.manaCost > 0f && stats.CurrentMana < skill.manaCost;
        Color tint = lackMana ? noManaColor : normalColor;
        iconImage.color = tint;
        nameText.color = tint;
    }

    // Lớp phủ che từ đáy lên: anchorMax.y = phần hồi chiêu còn lại (1 = che kín, 0 = không che).
    private void SetCooldownFraction(float fraction)
    {
        bool visible = fraction > 0.001f;
        if (cooldownOverlay.gameObject.activeSelf != visible) cooldownOverlay.gameObject.SetActive(visible);
        if (visible) cooldownOverlay.anchorMax = new Vector2(1f, fraction);
    }

    // ------------------------------------------------------------------
    // Tự tạo các phần còn thiếu
    // ------------------------------------------------------------------

    private void EnsureParts()
    {
        if (iconImage == null)
        {
            RectTransform rect = CreateChild("Icon");
            Stretch(rect, 6f);

            iconImage = rect.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;
        }

        if (nameText == null)
        {
            nameText = CreateText("Skill_Name", 14f, TextAlignmentOptions.Center);
            Stretch(nameText.rectTransform, 4f);
        }

        if (hotkeyText == null)
        {
            hotkeyText = CreateText("Hotkey", 16f, TextAlignmentOptions.TopLeft);
            Stretch(hotkeyText.rectTransform, 4f);
            hotkeyText.fontStyle = FontStyles.Bold;
        }

        if (cooldownOverlay == null)
        {
            RectTransform rect = CreateChild("Cooldown_Overlay");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.65f);
            image.raycastTarget = false;

            cooldownOverlay = rect;
            cooldownOverlay.gameObject.SetActive(false);
        }
        else
        {
            // Overlay tự gán: cần anchor dạng co giãn theo chiều cao để thu nhỏ được.
            cooldownOverlay.anchorMin = Vector2.zero;
            cooldownOverlay.anchorMax = new Vector2(1f, 0f);
            cooldownOverlay.offsetMin = Vector2.zero;
            cooldownOverlay.offsetMax = Vector2.zero;
            cooldownOverlay.gameObject.SetActive(false);
        }

        // Hotkey và cooldown nằm trên cùng để luôn thấy.
        hotkeyText.transform.SetAsLastSibling();
    }

    private RectTransform CreateChild(string childName)
    {
        var childObject = new GameObject(childName, typeof(RectTransform));
        childObject.transform.SetParent(transform, false);
        return (RectTransform)childObject.transform;
    }

    private TextMeshProUGUI CreateText(string childName, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateChild(childName);

        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
