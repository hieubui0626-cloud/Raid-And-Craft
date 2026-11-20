using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một loại Affix (ví dụ "+% Physic Damage"): asset ScriptableObject, nằm trong Affix_Database.
/// Item_Instance chỉ lưu affixId + giá trị đã roll (Affix_Line); khoảng roll thật lấy từ bảng theo Tier ở đây.
/// - allowedSlots: ô trang bị roll được affix này (trống = mọi ô).
/// - exclusionGroup: các affix cùng nhóm không cùng xuất hiện trên một món (ví dụ hai affix tăng sát thương vật lý).
/// - weight: trọng số khi bốc trong pool (lớn = hay ra).
/// Tạo asset: Create -> Raid And Craft -> Affix.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Affix", fileName = "Affix_New")]
public class Affix_Data : ScriptableObject
{
    [Serializable]
    public struct Tier_Range
    {
        [Min(1)] public int tier;
        public float min;
        public float max;
    }

    [Header("Thông tin")]
    [SerializeField] private string affixId;
    [SerializeField] private string displayName;

    [Serializable]
    public class Affix_Component
    {
        public Player_Stat_Type stat;
        [Tooltip("PercentAdd: 0.1 = +10%. Flat: cộng thẳng.")]
        public Player_Stat_Modifier_Type modifierType = Player_Stat_Modifier_Type.PercentAdd;
        [Tooltip("Làm tròn giá trị roll thành số nguyên (sát thương, thuộc tính...).")]
        public bool wholeNumber;
        [Tooltip("Khoảng roll theo Tier. Tier không có dòng riêng dùng dòng Tier cao nhất không vượt quá, không thì dòng thấp nhất.")]
        public List<Tier_Range> ranges = new List<Tier_Range> { new Tier_Range { tier = 1, min = 0.05f, max = 0.1f } };
    }

    [Header("Chỉ số cộng (MỘT dòng có thể cộng nhiều chỉ số)")]
    [Tooltip("Ví dụ Adds X-Y Physical Damage: hai thành phần PhysicDamageMin và PhysicDamageMax. All Attributes: ba thành phần Strength/Agility/Intelligence.")]
    [SerializeField] private List<Affix_Component> components = new List<Affix_Component>();
    [Tooltip("Bật: MỘT lần roll dùng chung cho mọi thành phần (All Attributes ra cùng giá trị). Tắt: mỗi thành phần roll riêng (min-max damage).")]
    [SerializeField] private bool sharedRoll;
    [Tooltip("Mẫu chữ tooltip: {0} là giá trị thành phần 1, {1} thành phần 2... Ví dụ: 'Adds {0} to {1} Physical Damage', '+{0} to All Attributes', '{0} increased Action Speed'. Số % tự có dấu %. Trống = mỗi chỉ số một dòng.")]
    [SerializeField] private string format;

    [Header("Roll")]
    [Min(0)] [SerializeField] private int weight = 100;
    [Tooltip("Cùng nhóm thì không xuất hiện chung trên một món. Để trống = không thuộc nhóm nào.")]
    [SerializeField] private string exclusionGroup;

    [Header("Áp dụng cho")]
    [Tooltip("Ô trang bị roll được affix này. Trống = mọi ô.")]
    [SerializeField] private List<Equipment_Slot> allowedSlots = new List<Equipment_Slot>();

    // Dữ liệu cũ (bản P2 một chỉ số): tự chuyển thành một thành phần trong OnValidate.
    [SerializeField, HideInInspector] private Player_Stat_Type stat;
    [SerializeField, HideInInspector] private Player_Stat_Modifier_Type modifierType = Player_Stat_Modifier_Type.PercentAdd;
    [SerializeField, HideInInspector] private List<Tier_Range> ranges = new List<Tier_Range>();

    public string AffixId => string.IsNullOrEmpty(affixId) ? name : affixId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public IReadOnlyList<Affix_Component> Components => components;
    public bool SharedRoll => sharedRoll;
    public string Format => format;
    public int Weight => Mathf.Max(0, weight);
    public string ExclusionGroup => exclusionGroup;

    /// <summary>Roll được trên ô này không.</summary>
    public bool CanRollOn(Equipment_Slot slot)
    {
        return allowedSlots.Count == 0 || allowedSlots.Contains(slot);
    }

    /// <summary>Hai affix có loại trừ nhau không (cùng nhóm khác trống, hoặc chính nó).</summary>
    public bool ConflictsWith(Affix_Data other)
    {
        if (other == null) return false;
        if (other == this) return true;
        return !string.IsNullOrEmpty(exclusionGroup) && exclusionGroup == other.exclusionGroup;
    }

    /// <summary>Khoảng roll của một thành phần ở một Tier (xem ô ranges).</summary>
    public static bool TryGetRange(Affix_Component c, int tier, out float min, out float max)
    {
        min = max = 0f;
        List<Tier_Range> list = c != null ? c.ranges : null;
        if (list == null || list.Count == 0) return false;

        Tier_Range atOrBelow = default;
        Tier_Range lowest = list[0];
        bool foundAtOrBelow = false;

        foreach (Tier_Range r in list)
        {
            if (r.tier < lowest.tier) lowest = r;
            if (r.tier <= tier && (!foundAtOrBelow || r.tier > atOrBelow.tier))
            {
                atOrBelow = r;
                foundAtOrBelow = true;
            }
        }

        Tier_Range pick = foundAtOrBelow ? atOrBelow : lowest;
        min = Mathf.Min(pick.min, pick.max);
        max = Mathf.Max(pick.min, pick.max);
        return true;
    }

    /// <summary>
    /// Roll một dòng Affix ở Tier cho trước. SharedRoll bật: một số t dùng chung cho mọi thành phần; tắt: mỗi thành phần một t riêng.
    /// rng truyền vào (seed được) để test lặp lại. Giá trị làm tròn 3 chữ số (hoặc số nguyên nếu thành phần đặt wholeNumber).
    /// </summary>
    public Affix_Line Roll(int tier, System.Random rng)
    {
        int n = components.Count;
        var entries = new Stat_Modifier_Entry[n];
        var mins = new float[n];
        var maxs = new float[n];

        float shared = NextT(rng);

        for (int i = 0; i < n; i++)
        {
            Affix_Component c = components[i];
            TryGetRange(c, tier, out float min, out float max);
            float t = sharedRoll ? shared : NextT(rng);
            float value = Mathf.Lerp(min, max, t);
            value = c.wholeNumber ? Mathf.Round(value) : (float)Math.Round(value, 3);

            entries[i] = new Stat_Modifier_Entry { stat = c.stat, modifierType = c.modifierType, value = value };
            mins[i] = min;
            maxs[i] = max;
        }

        return new Affix_Line(entries, mins, maxs, AffixId, tier, format);
    }

    private static float NextT(System.Random rng)
    {
        return rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
    }

    private void OnValidate()
    {
        // Chuyển dữ liệu một chỉ số của bản cũ thành một thành phần.
        if (components.Count == 0 && ranges != null && ranges.Count > 0)
        {
            components.Add(new Affix_Component { stat = stat, modifierType = modifierType, ranges = new List<Tier_Range>(ranges) });
            ranges.Clear();
        }

        foreach (Affix_Component c in components)
        {
            if (c.ranges == null) continue;

            for (int i = 0; i < c.ranges.Count; i++)
            {
                Tier_Range r = c.ranges[i];
                if (r.min > r.max)
                {
                    (r.min, r.max) = (r.max, r.min);
                    c.ranges[i] = r;
                }
            }
        }
    }
}
