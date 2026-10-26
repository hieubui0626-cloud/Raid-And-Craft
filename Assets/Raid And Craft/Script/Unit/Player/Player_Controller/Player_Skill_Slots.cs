using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kho kỹ năng của Player và 10 ô kỹ năng (phím 1-5 và Q-W-E-R-T).
/// - Available Skills: các skill Player ĐÃ CÓ (danh sách để chọn gán vào ô). Magic Circuit sau này gọi Learn() khi mở khoá skill.
/// - Slots: skill đang gán vào từng ô. Giữ phím của ô nào thì Player thi triển skill của ô đó về phía con trỏ (Player_Combat.CastAtMouse).
/// - Đòn đánh mặc định (chuột phải) không nằm trong ô, vẫn do Player_Combat lo.
/// Chỉ lo DỮ LIỆU và nhận phím; hiển thị là việc của Skill_Bar_UI. Gắn lên PlayerPrefab.
/// Chưa lưu vào Save_Data (làm sau khi có danh sách skill để tra cứu theo id).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player_Combat))]
public class Player_Skill_Slots : MonoBehaviour
{
    public const int SlotCount = Input_Manager.SkillSlotCount;

    /// <summary>Nhãn phím của từng ô, theo thứ tự ô 0..9.</summary>
    public static readonly string[] HotkeyLabels = { "1", "2", "3", "4", "5", "Q", "W", "E", "R", "T" };

    /// <summary>Bắn khi một ô đổi skill (chỉ số ô, skill mới hoặc null).</summary>
    public event Action<int, Skill_Data> OnSlotChanged;

    /// <summary>Bắn khi Player có thêm skill mới (Learn).</summary>
    public event Action OnAvailableSkillsChanged;

    [Header("Kho kỹ năng")]
    [Tooltip("Các skill Player đang có. Danh sách này hiện ra khi bấm vào một ô để chọn skill gán vào.")]
    [SerializeField] private List<Skill_Data> availableSkills = new List<Skill_Data>();

    [Header("Gán sẵn lúc bắt đầu (tuỳ chọn)")]
    [Tooltip("10 phần tử theo thứ tự ô: 1,2,3,4,5,Q,W,E,R,T. Để trống ô nào thì ô đó trống. Skill gán sẵn tự được thêm vào kho.")]
    [SerializeField] private Skill_Data[] initialSlots = new Skill_Data[SlotCount];

    private readonly Skill_Data[] slots = new Skill_Data[SlotCount];
    private Player_Combat combat;

    public IReadOnlyList<Skill_Data> AvailableSkills => availableSkills;

    private void Awake()
    {
        combat = GetComponent<Player_Combat>();

        for (int i = 0; i < SlotCount && i < initialSlots.Length; i++)
        {
            Skill_Data skill = initialSlots[i];
            if (skill == null) continue;

            if (!availableSkills.Contains(skill)) availableSkills.Add(skill);
            slots[i] = skill;
        }
    }

    private void OnValidate()
    {
        // Giữ đúng 10 phần tử khi chỉnh trong Inspector.
        if (initialSlots == null || initialSlots.Length != SlotCount) Array.Resize(ref initialSlots, SlotCount);
    }

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        if (input == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            // Giữ phím thì lặp lại theo hồi chiêu của skill (TryCast tự bỏ qua khi chưa sẵn sàng).
            if (!input.IsSkillSlotHeld(i)) continue;

            Skill_Data skill = slots[i];
            if (skill != null) combat.CastAtMouse(skill);
        }
    }

    public Skill_Data GetSlot(int index)
    {
        return IsValidSlot(index) ? slots[index] : null;
    }

    /// <summary>Số ô mà skill này đang nằm (-1 nếu chưa gán).</summary>
    public int FindSlot(Skill_Data skill)
    {
        if (skill == null) return -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == skill) return i;
        }
        return -1;
    }

    /// <summary>
    /// Gán skill vào ô (null = bỏ trống ô). Skill phải nằm trong kho. Nếu skill đang ở ô khác thì hai ô ĐỔI CHỖ cho nhau
    /// (ô kia nhận skill cũ của ô này, có thể là trống). Trả về false nếu ô không hợp lệ hoặc skill chưa có trong kho.
    /// </summary>
    public bool Assign(int index, Skill_Data skill)
    {
        if (!IsValidSlot(index)) return false;
        if (skill != null && !availableSkills.Contains(skill)) return false;
        if (slots[index] == skill) return true;

        Skill_Data previous = slots[index];

        int otherIndex = FindSlot(skill);
        if (otherIndex >= 0)
        {
            slots[otherIndex] = previous;
            OnSlotChanged?.Invoke(otherIndex, previous);
        }

        slots[index] = skill;
        OnSlotChanged?.Invoke(index, skill);
        return true;
    }

    public void Clear(int index)
    {
        Assign(index, null);
    }

    /// <summary>Thêm skill vào kho (Magic Circuit gọi khi mở khoá). Có sẵn rồi thì bỏ qua. Trả về true nếu thêm mới.</summary>
    public bool Learn(Skill_Data skill)
    {
        if (skill == null || availableSkills.Contains(skill)) return false;

        availableSkills.Add(skill);
        OnAvailableSkillsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Gỡ skill khỏi kho (Magic Circuit gọi khi gỡ Spell Stone). Skill đang nằm ở ô nào thì ô đó bị bỏ trống.
    /// Trả về true nếu skill có trong kho và đã gỡ.
    /// </summary>
    public bool Forget(Skill_Data skill)
    {
        if (skill == null || !availableSkills.Remove(skill)) return false;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != skill) continue;

            slots[i] = null;
            OnSlotChanged?.Invoke(i, null);
        }

        OnAvailableSkillsChanged?.Invoke();
        return true;
    }

    private static bool IsValidSlot(int index)
    {
        return index >= 0 && index < SlotCount;
    }
}
