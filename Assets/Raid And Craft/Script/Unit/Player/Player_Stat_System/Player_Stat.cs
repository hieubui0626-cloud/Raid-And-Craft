using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một chỉ số đơn lẻ: giá trị gốc (Base) + danh sách modifier -> giá trị cuối (Value).
/// Là class thuần C# (không phải MonoBehaviour) nên có thể tái sử dụng cho Enemy, NPC...
/// </summary>
public class Player_Stat
{
    public Player_Stat_Type Type { get; }

    /// <summary>Bắn ra khi giá trị cuối (Value) thay đổi.</summary>
    public event Action<Player_Stat> OnValueChanged;

    private readonly List<Player_Stat_Modifier> modifiers =
        new List<Player_Stat_Modifier>();

    private float baseValue;
    private float finalValue;

    public Player_Stat(Player_Stat_Type type, float baseValue)
    {
        Type = type;
        this.baseValue = baseValue;
        finalValue = Calculate();
    }

    /// <summary>Giá trị gốc, chưa tính modifier.</summary>
    public float BaseValue
    {
        get => baseValue;
        set
        {
            if (Mathf.Approximately(baseValue, value))
                return;

            baseValue = value;
            Recalculate();
        }
    }

    /// <summary>Giá trị cuối sau khi tính modifier, tối thiểu là 0.</summary>
    public float Value => finalValue;

    /// <summary>Giá trị cuối làm tròn.</summary>
    public int IntValue => Mathf.RoundToInt(finalValue);

    public IReadOnlyList<Player_Stat_Modifier> Modifiers => modifiers;

    public void AddModifier(Player_Stat_Modifier modifier)
    {
        modifiers.Add(modifier);
        Recalculate();
    }

    public bool RemoveModifier(Player_Stat_Modifier modifier)
    {
        bool removed = modifiers.Remove(modifier);

        if (removed)
            Recalculate();

        return removed;
    }

    /// <summary>Gỡ mọi modifier đến từ một nguồn.</summary>
    public int RemoveModifiersFromSource(object source)
    {
        if (source == null)
            return 0;

        int removedCount =
            modifiers.RemoveAll(m => Equals(m.Source, source));

        if (removedCount > 0)
            Recalculate();

        return removedCount;
    }

    /// <summary>
    /// Thay toàn bộ modifier của một nguồn bằng đúng một modifier mới.
    /// </summary>
    public void ReplaceModifierFromSource(
        object source,
        float value,
        Player_Stat_Modifier_Type type =
            Player_Stat_Modifier_Type.Flat)
    {
        if (source == null)
            return;

        modifiers.RemoveAll(m => Equals(m.Source, source));
        modifiers.Add(new Player_Stat_Modifier(value, type, source));
        Recalculate();
    }

    /// <summary>
    /// Trả về giá trị stat nhưng bỏ qua modifier của một nguồn.
    /// </summary>
    public float GetValueExcludingSource(object excludedSource)
    {
        if (excludedSource == null)
            return Value;

        return Calculate(excludedSource);
    }

    private void Recalculate()
    {
        float oldValue = finalValue;
        finalValue = Calculate();

        if (!Mathf.Approximately(oldValue, finalValue))
            OnValueChanged?.Invoke(this);
    }

    private float Calculate(object excludedSource = null)
    {
        float flat = 0f;
        float percentAdd = 0f;
        float percentMult = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            Player_Stat_Modifier modifier = modifiers[i];

            if (excludedSource != null &&
                Equals(modifier.Source, excludedSource))
            {
                continue;
            }

            switch (modifier.Type)
            {
                case Player_Stat_Modifier_Type.Flat:
                    flat += modifier.Value;
                    break;

                case Player_Stat_Modifier_Type.PercentAdd:
                    percentAdd += modifier.Value;
                    break;

                case Player_Stat_Modifier_Type.PercentMult:
                    percentMult *= modifier.Value;
                    break;
            }
        }

        return Mathf.Max(
            0f,
            (baseValue + flat) *
            (1f + percentAdd) *
            percentMult
        );
    }
}