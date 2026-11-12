using System;
using UnityEngine;

/// <summary>
/// Dùng nhanh vật phẩm bằng phím của thanh kỹ năng (1-5, Q-W-E-R-T): mỗi ô có thể gán MỘT loại vật phẩm tiêu hao
/// (Consumable_Item_Data, công dụng QuickUse) lấy từ Inventory. Bấm phím ô đó thì dùng 1 món trong Inventory.
/// - Một ô chứa skill HOẶC vật phẩm: gán vật phẩm thì skill của ô bị bỏ, gán skill thì vật phẩm bị bỏ.
/// - Ô giữ loại vật phẩm; hết hàng trong Inventory thì ô vẫn nhớ (hiện số 0, mờ) và dùng được lại khi có hàng.
/// Skill_Bar_UI tự thêm component này lên Player (cùng object với Player_Skill_Slots) nếu chưa có.
/// Player_Save lưu loại vật phẩm của từng ô (itemId).
/// </summary>
[DisallowMultipleComponent]
public class Player_Quick_Use : MonoBehaviour
{
    public const int SlotCount = Player_Skill_Slots.SlotCount;

    /// <summary>Bắn khi một ô đổi vật phẩm (chỉ số ô, vật phẩm mới hoặc null).</summary>
    public event Action<int, Item_Data> OnSlotChanged;

    [Tooltip("Kho lấy đồ. Bỏ trống thì lấy Inventory cùng object.")]
    [SerializeField] private Inventory inventory;

    private readonly Item_Data[] slots = new Item_Data[SlotCount];
    private Player_Skill_Slots skillSlots;
    private Character_Stats stats;

    public Inventory Inventory => inventory;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        skillSlots = GetComponent<Player_Skill_Slots>();
        stats = GetComponent<Character_Stats>();
    }

    private void OnEnable()
    {
        if (skillSlots == null) skillSlots = GetComponent<Player_Skill_Slots>();
        if (skillSlots != null) skillSlots.OnSlotChanged += HandleSkillChanged;
    }

    private void OnDisable()
    {
        if (skillSlots != null) skillSlots.OnSlotChanged -= HandleSkillChanged;
    }

    private bool warnedMissing;

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        if (input == null || inventory == null || stats == null)
        {
            // Chỉ cảnh báo MỘT lần, và chỉ khi có ô đang gán vật phẩm (để biết vì sao bấm phím không dùng được).
            if (!warnedMissing && HasAnyItem())
            {
                warnedMissing = true;
                Debug.LogWarning($"[{nameof(Player_Quick_Use)}] Không dùng nhanh được vì thiếu: " +
                                 (input == null ? "Input_Manager " : string.Empty) +
                                 (inventory == null ? "Inventory (cùng object với Player_Quick_Use) " : string.Empty) +
                                 (stats == null ? "Character_Stats (cùng object)" : string.Empty), this);
            }
            return;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null || !input.IsSkillSlotPressed(i)) continue;

            if (!(slots[i] is Consumable_Item_Data consumable)) continue;

            if (!consumable.TryConsume(stats, inventory))
            {
                Debug.Log($"[{nameof(Player_Quick_Use)}] Ô {i + 1} ({slots[i].name}) không dùng được: " +
                          (inventory.CountOf(slots[i]) <= 0 ? "hết vật phẩm trong Inventory." : "TryUse trả về false (ví dụ đã đầy máu/mana, hoặc điều kiện của vật phẩm)."), this);
            }
        }
    }

    private bool HasAnyItem()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null) return true;
        }

        return false;
    }

    [ContextMenu("Debug: Báo cáo Quick Use")]
    private void DebugReport()
    {
        Debug.Log($"[{nameof(Player_Quick_Use)}] Input_Manager: {(Input_Manager.Instance != null ? "có" : "KHÔNG CÓ")}; " +
                  $"Inventory: {(inventory != null ? "có" : "KHÔNG CÓ")}; Character_Stats: {(stats != null ? "có" : "KHÔNG CÓ")}; " +
                  $"Player_Skill_Slots: {(skillSlots != null ? "có" : "KHÔNG CÓ")}; số Player_Quick_Use trong Scene: {FindObjectsByType<Player_Quick_Use>(FindObjectsSortMode.None).Length}", this);

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null) Debug.Log($"  Ô {i + 1}: {slots[i].name}, trong kho: {CountOf(slots[i])}", this);
        }
    }

    public Item_Data GetSlot(int index)
    {
        return index >= 0 && index < SlotCount ? slots[index] : null;
    }

    /// <summary>Số món loại này đang có trong Inventory (để hiện trên ô).</summary>
    public int CountOf(Item_Data item)
    {
        return item != null && inventory != null ? inventory.CountOf(item) : 0;
    }

    /// <summary>
    /// Gán vật phẩm vào ô (null = bỏ trống). Chỉ nhận vật phẩm tiêu hao có công dụng QuickUse.
    /// Loại này đang ở ô khác thì hai ô đổi chỗ. Gán vào ô thì skill của ô đó bị bỏ.
    /// </summary>
    public bool Assign(int index, Item_Data item)
    {
        if (index < 0 || index >= SlotCount) return false;
        if (item != null && !(item is Consumable_Item_Data && item.CanBeUsedFor(Item_Usage.QuickUse))) return false;
        if (slots[index] == item) return true;

        Item_Data previous = slots[index];

        int other = FindSlot(item);
        if (other >= 0)
        {
            slots[other] = previous;
            OnSlotChanged?.Invoke(other, previous);
        }

        slots[index] = item;
        OnSlotChanged?.Invoke(index, item);

        if (item != null && skillSlots != null && skillSlots.GetSlot(index) != null) skillSlots.Clear(index);
        return true;
    }

    public void Clear(int index)
    {
        Assign(index, null);
    }

    /// <summary>Bỏ trống cả 10 ô (load save).</summary>
    public void ClearAll()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null) continue;

            slots[i] = null;
            OnSlotChanged?.Invoke(i, null);
        }
    }

    public int FindSlot(Item_Data item)
    {
        if (item == null) return -1;
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == item) return i;
        }
        return -1;
    }

    // Skill được gán vào ô thì vật phẩm của ô đó bị bỏ.
    private void HandleSkillChanged(int index, Skill_Data skill)
    {
        if (skill != null && slots[index] != null)
        {
            slots[index] = null;
            OnSlotChanged?.Invoke(index, null);
        }
    }
}
