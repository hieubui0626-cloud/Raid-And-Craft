using System;
using System.Collections.Generic;

/// <summary>
/// Một dòng Affix đã roll trên một Item_Instance. MỘT dòng có thể cộng NHIỀU chỉ số
/// (ví dụ "Adds 4 to 10 Physical Damage" = PhysicDamageMin + PhysicDamageMax; "+5 All Attributes" = 3 thuộc tính); vẫn tính là 1 dòng.
/// - affixId: id của Affix_Data (Affix_Database). Để trống = dòng cũ/thử nghiệm không thuộc affix nào.
/// - format: mẫu chữ cho tooltip, sao chép từ Affix_Data lúc roll ({0}, {1}... là giá trị từng chỉ số). Trống = hiện từng chỉ số một dòng.
/// - modifiers: các chỉ số đã roll (giá trị thật được cộng khi mặc). minRolls/maxRolls: khoảng roll lúc roll, cùng thứ tự.
/// - tier: bậc của dòng (theo Tier của nguyên liệu dùng lúc roll).
/// Hai dòng cùng LOẠI khi cùng affixId (nếu có), không thì cùng stat + modifierType của chỉ số đầu.
/// </summary>
[Serializable]
public struct Affix_Line
{
    private static readonly Stat_Modifier_Entry[] NoModifiers = new Stat_Modifier_Entry[0];
    private static readonly float[] NoFloats = new float[0];

    public string affixId;
    public string format;
    public Stat_Modifier_Entry[] modifiers;
    public float[] minRolls;
    public float[] maxRolls;
    public int tier;

    /// <summary>Dòng một chỉ số.</summary>
    public Affix_Line(Stat_Modifier_Entry modifier, string affixId = null, float minRoll = 0f, float maxRoll = 0f, int tier = 1, string format = null)
        : this(new[] { modifier }, new[] { minRoll }, new[] { maxRoll }, affixId, tier, format)
    {
    }

    /// <summary>Dòng nhiều chỉ số; minRolls/maxRolls cùng độ dài với modifiers (null = dùng giá trị làm khoảng).</summary>
    public Affix_Line(IList<Stat_Modifier_Entry> modifiers, IList<float> minRolls, IList<float> maxRolls, string affixId = null, int tier = 1, string format = null)
    {
        int n = modifiers != null ? modifiers.Count : 0;
        this.modifiers = new Stat_Modifier_Entry[n];
        this.minRolls = new float[n];
        this.maxRolls = new float[n];

        for (int i = 0; i < n; i++)
        {
            this.modifiers[i] = modifiers[i];
            this.minRolls[i] = minRolls != null && i < minRolls.Count ? minRolls[i] : modifiers[i].value;
            this.maxRolls[i] = maxRolls != null && i < maxRolls.Count ? maxRolls[i] : modifiers[i].value;
        }

        this.affixId = affixId ?? string.Empty;
        this.format = format ?? string.Empty;
        this.tier = tier < 1 ? 1 : tier;
    }

    public IReadOnlyList<Stat_Modifier_Entry> Modifiers => modifiers ?? NoModifiers;
    public IReadOnlyList<float> MinRolls => minRolls ?? NoFloats;
    public IReadOnlyList<float> MaxRolls => maxRolls ?? NoFloats;

    public bool IsSameKind(Affix_Line other)
    {
        if (!string.IsNullOrEmpty(affixId) || !string.IsNullOrEmpty(other.affixId))
        {
            return affixId == other.affixId;
        }

        if (Modifiers.Count == 0 || other.Modifiers.Count == 0) return false;

        return Modifiers[0].stat == other.Modifiers[0].stat && Modifiers[0].modifierType == other.Modifiers[0].modifierType;
    }
}
