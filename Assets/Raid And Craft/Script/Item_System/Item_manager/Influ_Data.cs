using System;
using System.Collections.Generic;
using System.Text;
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

/// <summary>Kết quả roll của một Influ.</summary>
[Serializable]
public struct Influ_Roll_Result
{
    public float value;
    public int variantIndex;

    public Influ_Roll_Result(float value, int variantIndex = -1)
    {
        this.value = value;
        this.variantIndex = variantIndex;
    }
}

/// <summary>
/// Lớp cơ sở cho toàn bộ Influ.
/// Lớp này xử lý roll và mô tả; class kế thừa chỉ cần cung cấp dữ liệu hoặc logic riêng.
/// </summary>
public abstract class Influ_Data : ScriptableObject
{
    [Header("Thông tin")]
    [SerializeField] private string influId;
    [SerializeField] private string displayName;

    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Header("Giới hạn roll")]
    [SerializeField] private float minRoll = 0f;
    [SerializeField] private float maxRoll = 1f;

    public string InfluId => string.IsNullOrEmpty(influId) ? name : influId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public float MinRoll => minRoll;
    public float MaxRoll => maxRoll;

    /// <summary>
    /// Roll giá trị Influ. Material sẽ giới hạn lại khoảng này bên ngoài class.
    /// </summary>
    public Influ_Roll_Result Roll(
        float actualMin,
        float actualMax,
        System.Random rng = null)
    {
        float min = Mathf.Max(actualMin, MinRoll);
        float max = Mathf.Min(actualMax, MaxRoll);

        if (min > max)
        {
            min = actualMin;
            max = actualMax;
        }

        float value = rng != null
            ? Mathf.Lerp(min, max, (float)rng.NextDouble())
            : UnityEngine.Random.Range(min, max);

        return new Influ_Roll_Result(value, RollVariant(rng));
    }

    /// <summary>
    /// Chọn biến thể hoặc cụm stat. Mặc định Influ không có biến thể.
    /// </summary>
    protected virtual int RollVariant(System.Random rng)
    {
        return -1;
    }

    /// <summary>Áp dụng Influ lên nhân vật.</summary>
    public abstract void Apply(
        Influ_Context context,
        float roll,
        int variantIndex,
        object source);

    /// <summary>Gỡ Influ khỏi nhân vật.</summary>
    public abstract void Remove(
        Influ_Context context,
        float roll,
        int variantIndex,
        object source);

    /// <summary>
    /// Tạo nội dung Description động dựa trên roll.
    /// </summary>
    public virtual string Describe(float roll, int variantIndex)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return DisplayName;
        }

        return DisplayName + "\n" + FormatDescription(description, roll);
    }

    protected string FormatDescription(string text, float roll)
    {
        return text
            .Replace("{roll}", roll.ToString("0.##"))
            .Replace("{rollPercent}", (roll * 100f).ToString("0.##") + "%");
    }

    protected static string FormatModifier(Stat_Modifier_Entry modifier)
    {
        string statName = Humanize(modifier.stat.ToString());

        switch (modifier.modifierType)
        {
            case Player_Stat_Modifier_Type.PercentAdd:
                return "+" + (modifier.value * 100f).ToString("0.##") + "% " + statName;

            case Player_Stat_Modifier_Type.PercentMult:
                return "x" + (1f + modifier.value).ToString("0.##") + " " + statName;

            default:
                return "+" + modifier.value.ToString("0.##") + " " + statName;
        }
    }

    protected static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var builder = new StringBuilder(value.Length + 8);
        builder.Append(value[0]);

        for (int i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(value[i]);
        }

        return builder.ToString();
    }
}

