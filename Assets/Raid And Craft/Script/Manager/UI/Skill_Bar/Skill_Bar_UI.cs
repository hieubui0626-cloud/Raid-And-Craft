using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thanh 10 ô kỹ năng (1-5, Q-W-E-R-T). Nối dữ liệu (Player_Skill_Slots) với giao diện từng ô (Skill_Slot_UI):
/// - Hiện skill/vòng hồi chiêu/trạng thái thiếu mana của từng ô.
/// - Bấm chuột vào một ô thì mở popup LƯỚI skill (Skill_Picker_UI, mặc định 3 cột) để gán vào ô đó.
/// - Rê chuột vào ô skill (trên thanh hoặc trong popup) thì hiện tooltip (Skill_Tooltip_UI) gồm tên, mana, sát thương...
/// - Bấm phím tương ứng để thi triển do Player_Skill_Slots lo (script này không đọc phím).
///
/// Cách dùng: đặt script này lên object cha của 10 nút ô, gắn Skill_Slot_UI lên TỪNG nút, rồi kéo 10 nút vào 'Slot UIs'
/// theo đúng thứ tự 1,2,3,4,5,Q,W,E,R,T (bỏ trống mảng thì tự lấy các Skill_Slot_UI con theo thứ tự trong Hierarchy).
/// Bỏ trống 'Skill Slots' thì tự tìm Player_Skill_Slots trong Scene.
/// </summary>
public class Skill_Bar_UI : MonoBehaviour
{
    [Header("Dữ liệu")]
    [Tooltip("Bỏ trống thì tự tìm Player_Skill_Slots trong Scene.")]
    [SerializeField] private Player_Skill_Slots skillSlots;

    [Header("Các ô (thứ tự: 1,2,3,4,5,Q,W,E,R,T)")]
    [Tooltip("Bỏ trống thì tự lấy các Skill_Slot_UI con theo thứ tự trong Hierarchy.")]
    [SerializeField] private List<Skill_Slot_UI> slotUIs = new List<Skill_Slot_UI>();

    [Header("Popup chọn skill (dạng lưới)")]
    [Tooltip("Số cột của lưới ô skill.")]
    [Range(1, 8)] [SerializeField] private int pickerColumns = 3;

    [Tooltip("Số hàng hiển thị trước khi phải cuộn. Skill nhiều hơn thì cuộn bằng con lăn chuột.")]
    [Min(1)] [SerializeField] private int pickerVisibleRows = 3;

    [Tooltip("Cạnh mỗi ô vuông (pixel, theo độ phân giải 1080p).")]
    [Min(24f)] [SerializeField] private float pickerCellSize = 72f;

    private Skill_Caster caster;
    private Character_Stats stats;
    private Skill_Picker_UI picker;
    private Skill_Tooltip_UI tooltip;
    private Canvas parentCanvas;

    private void Start()
    {
        if (skillSlots == null) skillSlots = FindFirstObjectByType<Player_Skill_Slots>();
        if (skillSlots == null)
        {
            Debug.LogError($"[{nameof(Skill_Bar_UI)}] Không tìm thấy Player_Skill_Slots (gắn lên Player prefab).", this);
            enabled = false;
            return;
        }

        caster = skillSlots.GetComponent<Skill_Caster>();
        stats = skillSlots.GetComponent<Character_Stats>();
        if (caster == null) Debug.LogWarning($"[{nameof(Skill_Bar_UI)}] Player thiếu Skill_Caster: không hiện được hồi chiêu.", this);

        if (slotUIs.Count == 0) slotUIs.AddRange(GetComponentsInChildren<Skill_Slot_UI>(true));

        if (slotUIs.Count != Player_Skill_Slots.SlotCount)
        {
            Debug.LogWarning($"[{nameof(Skill_Bar_UI)}] Có {slotUIs.Count} ô, cần {Player_Skill_Slots.SlotCount} (1-5 và Q-W-E-R-T).", this);
        }

        parentCanvas = GetComponentInParent<Canvas>();

        picker = new GameObject("Skill_Picker_UI").AddComponent<Skill_Picker_UI>();
        picker.Configure(pickerColumns, pickerVisibleRows, pickerCellSize);
        picker.OnHoverBegin += ShowTooltip;
        picker.OnHoverEnd += HideTooltip;

        tooltip = new GameObject("Skill_Tooltip_UI").AddComponent<Skill_Tooltip_UI>();

        for (int i = 0; i < slotUIs.Count && i < Player_Skill_Slots.SlotCount; i++)
        {
            Skill_Slot_UI slotUI = slotUIs[i];
            if (slotUI == null) continue;

            slotUI.Init(i, Player_Skill_Slots.HotkeyLabels[i]);
            slotUI.SetSkill(skillSlots.GetSlot(i));
            slotUI.OnClicked += HandleSlotClicked;
            slotUI.OnHoverEnter += HandleSlotHoverEnter;
            slotUI.OnHoverExit += HandleSlotHoverExit;
        }

        skillSlots.OnSlotChanged += HandleSlotChanged;
    }

    private void OnDestroy()
    {
        if (skillSlots != null) skillSlots.OnSlotChanged -= HandleSlotChanged;

        foreach (Skill_Slot_UI slotUI in slotUIs)
        {
            if (slotUI == null) continue;

            slotUI.OnClicked -= HandleSlotClicked;
            slotUI.OnHoverEnter -= HandleSlotHoverEnter;
            slotUI.OnHoverExit -= HandleSlotHoverExit;
        }

        if (picker != null)
        {
            picker.OnHoverBegin -= ShowTooltip;
            picker.OnHoverEnd -= HideTooltip;
            Destroy(picker.gameObject);
        }

        if (tooltip != null) Destroy(tooltip.gameObject);
    }

    private void HandleSlotChanged(int index, Skill_Data skill)
    {
        if (index >= 0 && index < slotUIs.Count && slotUIs[index] != null) slotUIs[index].SetSkill(skill);
    }

    private void HandleSlotHoverEnter(Skill_Slot_UI slotUI)
    {
        ShowTooltip(slotUI.Skill);
    }

    private void HandleSlotHoverExit(Skill_Slot_UI slotUI)
    {
        HideTooltip();
    }

    // Tooltip: tên, mô tả, mana, hồi chiêu, sát thương tính theo chỉ số hiện tại của Player.
    private void ShowTooltip(Skill_Data skill)
    {
        if (skill == null || tooltip == null) return;

        Skill_Preview preview = default;
        bool hasPreview = caster != null && caster.TryGetPreview(skill, out preview);

        tooltip.Show(skill, hasPreview, preview, stats);
    }

    private void HideTooltip()
    {
        if (tooltip != null) tooltip.Hide();
    }

    private void HandleSlotClicked(Skill_Slot_UI slotUI)
    {
        HideTooltip();
        int index = slotUI.Index;

        picker.Open(
            Player_Skill_Slots.HotkeyLabels[index],
            slotUI.Rect,
            parentCanvas,
            skillSlots.AvailableSkills,
            skillSlots.GetSlot(index),
            picked => skillSlots.Assign(index, picked));
    }

    private void Update()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null) slotUIs[i].UpdateState(caster, stats);
        }
    }
}
