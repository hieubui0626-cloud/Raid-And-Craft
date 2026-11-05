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

    private Player_Quick_Use quickUse;
    private Inventory inventory;
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

        // Dùng nhanh vật phẩm: tự thêm Player_Quick_Use nếu Player chưa có.
        inventory = skillSlots.GetComponent<Inventory>();
        quickUse = skillSlots.GetComponent<Player_Quick_Use>();
        if (quickUse == null) quickUse = skillSlots.gameObject.AddComponent<Player_Quick_Use>();

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
        picker.OnItemHoverBegin += ShowItemTooltip;

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
        quickUse.OnSlotChanged += HandleQuickItemChanged;
        if (inventory != null) inventory.OnContainerChanged += RefreshQuickCounts;
        RefreshAllQuick();
    }

    private void OnDestroy()
    {
        if (skillSlots != null) skillSlots.OnSlotChanged -= HandleSlotChanged;
        if (quickUse != null) quickUse.OnSlotChanged -= HandleQuickItemChanged;
        if (inventory != null) inventory.OnContainerChanged -= RefreshQuickCounts;

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
            picker.OnItemHoverBegin -= ShowItemTooltip;
            Destroy(picker.gameObject);
        }

        if (tooltip != null) Destroy(tooltip.gameObject);
    }

    private void HandleSlotChanged(int index, Skill_Data skill)
    {
        if (index >= 0 && index < slotUIs.Count && slotUIs[index] != null) slotUIs[index].SetSkill(skill);
    }

    private void HandleQuickItemChanged(int index, Item_Data item)
    {
        if (index < 0 || index >= slotUIs.Count || slotUIs[index] == null) return;

        // Gán vật phẩm thì ô bỏ skill (Player_Skill_Slots.Clear đã báo), bỏ vật phẩm thì giữ nguyên skill hiện có.
        slotUIs[index].SetQuickItem(item, quickUse.CountOf(item));
        if (item == null) slotUIs[index].SetSkill(skillSlots.GetSlot(index));
    }

    private void RefreshQuickCounts()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && slotUIs[i].QuickItem != null) slotUIs[i].SetQuickCount(quickUse.CountOf(slotUIs[i].QuickItem));
        }
    }

    private void RefreshAllQuick()
    {
        for (int i = 0; i < slotUIs.Count && i < Player_Quick_Use.SlotCount; i++)
        {
            Item_Data item = quickUse.GetSlot(i);
            if (slotUIs[i] != null && item != null) slotUIs[i].SetQuickItem(item, quickUse.CountOf(item));
        }
    }

    private void ShowItemTooltip(Item_Data item, int count)
    {
        if (item != null) Item_Tooltip_UI.Instance.Show(item, count, "Quick use");
    }

    private void HandleSlotHoverEnter(Skill_Slot_UI slotUI)
    {
        if (slotUI.Skill != null)
        {
            ShowTooltip(slotUI.Skill);
        }
        else if (slotUI.QuickItem != null)
        {
            ShowItemTooltip(slotUI.QuickItem, quickUse.CountOf(slotUI.QuickItem));
        }
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
        Item_Tooltip_UI.HideIfExists();
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
            picked =>
            {
                skillSlots.Assign(index, picked);
                if (picked == null) quickUse.Clear(index); // "Empty" bỏ trống cả vật phẩm
            },
            ScanQuickUseItems(),
            quickUse.GetSlot(index),
            item => quickUse.Assign(index, item),
            quickUse.CountOf);
    }

    // Quét Inventory: mỗi LOẠI vật phẩm tiêu hao có công dụng QuickUse một ô (không lặp khi nằm nhiều chồng).
    private List<Item_Data> ScanQuickUseItems()
    {
        var result = new List<Item_Data>();
        if (inventory == null) return result;

        for (int i = 0; i < inventory.PlacementCount; i++)
        {
            Item_Data item = inventory.GetPlacement(i).Item;
            if (item == null || result.Contains(item)) continue;
            if (!(item is Consumable_Item_Data) || !item.CanBeUsedFor(Item_Usage.QuickUse)) continue;

            result.Add(item);
        }

        // Ô đã gán nhưng hết hàng vẫn cho chọn lại (đổi chỗ giữa các ô).
        for (int i = 0; i < Player_Quick_Use.SlotCount; i++)
        {
            Item_Data assigned = quickUse.GetSlot(i);
            if (assigned != null && !result.Contains(assigned)) result.Add(assigned);
        }

        return result;
    }

    private void Update()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null) slotUIs[i].UpdateState(caster, stats);
        }
    }
}
