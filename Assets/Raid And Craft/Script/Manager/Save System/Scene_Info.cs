using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Loại Scene, dùng để quyết định có lưu vị trí Player hay không.</summary>
public enum Scene_Kind
{
    /// <summary>Scene "base" (thị trấn/căn cứ): lưu vị trí Player, mở game lại sẽ vào đúng Scene này, đúng chỗ đã đứng.</summary>
    Base = 0,

    /// <summary>Scene tạm (Arena, map phiêu lưu, World Map...): KHÔNG lưu vị trí. Thoát game ở đây thì lần sau vào lại Scene base gần nhất.</summary>
    Instance = 1,
}

/// <summary>
/// Đặt MỘT object trong mỗi Scene có thể có Player (không bắt buộc, Scene không có component này được coi là Base).
/// Chỉ khai báo "Scene này là loại gì"; việc lưu/nạp nằm ở Player_Save và Save_Manager.
/// Mọi Scene dùng cho save (cả Base lẫn Instance) cần nằm trong Build Settings để nạp được bằng tên.
/// </summary>
[DisallowMultipleComponent]
public class Scene_Info : MonoBehaviour
{
    [SerializeField] private Scene_Kind kind = Scene_Kind.Base;

    [Tooltip("Quy chế của Scene cho quái: level, hệ số EXP, họ nguyên liệu rơi và tier. Trống = quái giữ level prefab, EXP gốc, không rơi nguyên liệu theo quy chế.")]
    [SerializeField] private Scene_Rules rules;

    [Tooltip("Cho phép dịch chuyển nhanh (Map) khi đang ở Scene này. Thường tắt ở Scene tạm (Arena, hầm ngục).")]
    [SerializeField] private bool allowFastTravel = true;

    [Tooltip("Modifier đang bật ngay khi vào Scene (map modifier). Thêm/bớt lúc chạy bằng AddModifier/RemoveModifier. Không lưu vào save: Scene dựng lại quy chế mỗi lần nạp.")]
    [SerializeField] private List<Scene_Rule_Modifier> startingModifiers = new List<Scene_Rule_Modifier>();

    private static Scene_Info active;
    private readonly List<Scene_Rule_Modifier> modifiers = new List<Scene_Rule_Modifier>();
    private Scene_Rule_Values values;
    private bool modifiersInitialized;

    /// <summary>Bắn khi danh sách modifier đổi. Quái ĐÃ xuất hiện giữ level cũ; thưởng (EXP/rơi đồ) tính lúc chết nên dùng giá trị mới.</summary>
    public event Action OnRulesChanged;

    /// <summary>Scene_Info của Scene đang chơi (tự tìm lần đầu). Null nếu Scene không có.</summary>
    public static Scene_Info Active
    {
        get
        {
            if (active == null) active = FindFirstObjectByType<Scene_Info>();
            return active;
        }
    }

    public Scene_Kind Kind => kind;
    public Scene_Rules Rules => rules;

    /// <summary>Quy chế đã áp modifier; null nếu Scene không có Scene_Rules.</summary>
    public Scene_Rule_Values Values
    {
        get
        {
            EnsureModifiers();
            if (values == null && rules != null)
            {
                values = new Scene_Rule_Values(rules);
                foreach (Scene_Rule_Modifier m in modifiers) if (m != null) m.Apply(values);
            }

            return values;
        }
    }

    public IReadOnlyList<Scene_Rule_Modifier> Modifiers
    {
        get { EnsureModifiers(); return modifiers; }
    }

    public void AddModifier(Scene_Rule_Modifier modifier)
    {
        if (modifier == null) return;
        EnsureModifiers();
        modifiers.Add(modifier);
        Changed();
    }

    public bool RemoveModifier(Scene_Rule_Modifier modifier)
    {
        EnsureModifiers();
        if (!modifiers.Remove(modifier)) return false;
        Changed();
        return true;
    }

    /// <summary>Thay toàn bộ modifier (ví dụ khi vào Scene bằng một "bản đồ" mang modifier).</summary>
    public void SetModifiers(IEnumerable<Scene_Rule_Modifier> list)
    {
        EnsureModifiers();
        modifiers.Clear();
        if (list != null) modifiers.AddRange(list);
        Changed();
    }

    private void EnsureModifiers()
    {
        if (modifiersInitialized) return;
        modifiersInitialized = true;
        modifiers.AddRange(startingModifiers);
    }

    private void Changed()
    {
        values = null;
        OnRulesChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
    }

    private void OnEnable()
    {
        Scene_State_Store.NotifySceneEntered();
        active = this;
    }

    private void OnDisable()
    {
        if (active == this) active = null;
    }
    public bool AllowFastTravel => allowFastTravel;
}
