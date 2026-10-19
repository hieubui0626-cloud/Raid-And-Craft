using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một chỉ số đơn lẻ: giá trị gốc (Base) + danh sách modifier -> giá trị cuối (Value).
/// Là class thuần C# (không phải MonoBehaviour) nên có thể tái sử dụng cho Enemy, NPC...
/// </summary>
public class Player_Stat
{
    public Player_StatType Type { get; }

    /// <summary>Bắn ra khi giá trị cuối (Value) thay đổi.</summary>
    public event Action<Player_Stat> OnValueChanged;

    private readonly List<Player_StatModifier> modifiers = new List<Player_StatModifier>();
    private float baseValue;
    private float finalValue;

    public Player_Stat(Player_StatType type, float baseValue)
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
            if (Mathf.Approximately(baseValue, value)) return;
            baseValue = value;
            Recalculate();
        }
    }

    /// <summary>Giá trị cuối sau khi tính modifier (tối thiểu 0).</summary>
    public float Value => finalValue;

    /// <summary>Giá trị cuối làm tròn, tiện cho hiển thị hoặc công thức dùng số nguyên.</summary>
    public int IntValue => Mathf.RoundToInt(finalValue);

    public IReadOnlyList<Player_StatModifier> Modifiers => modifiers;

    public void AddModifier(Player_StatModifier modifier)
    {
        modifiers.Add(modifier);
        Recalculate();
    }

    public bool RemoveModifier(Player_StatModifier modifier)
    {
        bool removed = modifiers.Remove(modifier);
        if (removed) Recalculate();
        return removed;
    }

    /// <summary>Gỡ mọi modifier đến từ một nguồn. Trả về số modifier đã gỡ.</summary>
    public int RemoveModifiersFromSource(object source)
    {
        if (source == null) return 0;

        int removedCount = modifiers.RemoveAll(m => Equals(m.Source, source));
        if (removedCount > 0) Recalculate();
        return removedCount;
    }

    private void Recalculate()
    {
        float oldValue = finalValue;
        finalValue = Calculate();

        if (!Mathf.Approximately(oldValue, finalValue))
        {
            OnValueChanged?.Invoke(this);
        }
    }

    private float Calculate()
    {
        float flat = 0f;
        float percentAdd = 0f;
        float percentMult = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            Player_StatModifier m = modifiers[i];
            switch (m.Type)
            {
                case Player_StatModifierType.Flat:
                    flat += m.Value;
                    break;
                case Player_StatModifierType.PercentAdd:
                    percentAdd += m.Value;
                    break;
                case Player_StatModifierType.PercentMult:
                    percentMult *= 1f + m.Value;
                    break;
            }
        }

        float result = (baseValue + flat) * (1f + percentAdd) * percentMult;
        return Mathf.Max(0f, result);
    }
}
