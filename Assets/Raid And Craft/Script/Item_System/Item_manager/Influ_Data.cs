using System.Collections.Generic;
using UnityEngine;

/// <summary>Ngữ cảnh một hiệu ứng Influ được áp dụng: ai đang mặc món đó.</summary>
public readonly struct Influ_Context
{
    public readonly Character_Stats Stats;
    public readonly Character_Equipment Equipment;

    public Influ_Context(Character_Stats stats, Character_Equipment equipment)
    {
        Stats = stats;
        Equipment = equipment;
    }

    public GameObject Owner => Stats != null ? Stats.gameObject : null;
}

/// <summary>
/// Lớp gốc của một loại hiệu ứng Influ (hiệu ứng đặc biệt trên trang bị, thay thế đồ Unique).
/// Mỗi Influ là một asset ScriptableObject với code riêng: kế thừa lớp này, viết Apply và Remove
/// (ví dụ Influ Phượng Hoàng: khi mặc thì đăng ký nghe Combat_Events, đòn đánh có tỉ lệ cast skill lửa phượng hoàng;
/// khi tháo thì huỷ đăng ký). Trang bị và kho không cần biết Influ cụ thể.
/// - roll: số đã roll trên từng món (trong giới hạn của nguyên liệu Influ lúc craft), Influ tự diễn giải
///   (tỉ lệ kích hoạt, hệ số sức mạnh...).
/// - source: khoá riêng của món đang mặc, dùng làm Source cho modifier hoặc làm khoá lưu trạng thái để Remove gỡ sạch.
/// Thêm Influ mới: [CreateAssetMenu(menuName = "Raid And Craft/Influ/&lt;Tên&gt;")] và một class kế thừa.
/// </summary>
public abstract class Influ_Data : ScriptableObject
{
    [Header("Thông tin")]
    [SerializeField] private string influId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Header("Giới hạn roll (nguyên liệu Influ quyết định khoảng thực tế)")]
    [SerializeField] private float minRoll = 0f;
    [SerializeField] private float maxRoll = 1f;

    public string InfluId => string.IsNullOrEmpty(influId) ? name : influId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public float MinRoll => minRoll;
    public float MaxRoll => maxRoll;

    /// <summary>Áp dụng khi món trang bị có Influ này được mặc.</summary>
    public abstract void Apply(Influ_Context context, float roll, object source);

    /// <summary>Thu hồi mọi tác động đã áp dụng bằng Apply (cùng source) khi tháo.</summary>
    public abstract void Remove(Influ_Context context, float roll, object source);

    /// <summary>Dòng mô tả cho tooltip, có số đã roll.</summary>
    public virtual string Describe(float roll)
    {
        return DisplayName;
    }
}

/// <summary>
/// Influ đơn giản nhất: cộng chỉ số, giá trị nhân với số đã roll. Dùng để thử hệ thống Influ trước khi có Influ code riêng.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Influ/Stat Influ", fileName = "Influ_Stat_New")]
public class Stat_Influ_Data : Influ_Data
{
    [Header("Chỉ số cộng (giá trị x roll)")]
    [SerializeField] private List<Weapon_Stat_Modifier> statModifiers = new List<Weapon_Stat_Modifier>();

    public override void Apply(Influ_Context context, float roll, object source)
    {
        if (context.Stats == null) return;

        foreach (Weapon_Stat_Modifier m in statModifiers)
        {
            context.Stats.AddModifier(m.stat, new Player_Stat_Modifier(m.value * roll, m.modifierType, source));
        }
    }

    public override void Remove(Influ_Context context, float roll, object source)
    {
        if (context.Stats != null) context.Stats.RemoveModifiersFromSource(source);
    }
}
