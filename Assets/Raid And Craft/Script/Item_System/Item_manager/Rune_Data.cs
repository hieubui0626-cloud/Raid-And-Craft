using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rune: lắp vào ô Magic Circuit thì cộng chỉ số cho nhân vật (tạo asset: Create -> Raid And Craft -> Items -> Rune).
/// Dùng lại Stat_Modifier_Entry (chỉ số + kiểu modifier + giá trị).
/// Giá trị được nhân với hệ số hiệu ứng của vùng (zone) chứa ô: ô nằm trong vùng +25% thì rune mạnh hơn 25%.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Rune", fileName = "Rune_New")]
public class Rune_Data : Circuit_Item_Data
{
    [Header("Chỉ số cộng khi lắp")]
    [SerializeField] private List<Stat_Modifier_Entry> statModifiers = new List<Stat_Modifier_Entry>();

    public IReadOnlyList<Stat_Modifier_Entry> StatModifiers => statModifiers;

    public override void Apply(Magic_Circuit circuit, object source, float effectMultiplier)
    {
        foreach (Stat_Modifier_Entry m in statModifiers)
        {
            circuit.Stats.AddModifier(m.stat, new Player_Stat_Modifier(m.value * effectMultiplier, m.modifierType, source));
        }
    }

    public override void Remove(Magic_Circuit circuit, object source)
    {
        circuit.Stats.RemoveModifiersFromSource(source);
    }
}
