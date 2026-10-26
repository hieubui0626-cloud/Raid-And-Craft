using System.Collections.Generic;
using UnityEngine;

/// <summary>Một dòng trong tooltip: nhãn + giá trị.</summary>
public readonly struct Skill_Preview_Line
{
    public readonly string Label;
    public readonly string Value;

    public Skill_Preview_Line(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

/// <summary>
/// Dữ liệu tooltip của một skill. Phần chung do Skill_Caster điền,
/// phần riêng do từng Skill_Effect tự thêm dòng (Describe) nên UI không cần biết skill làm gì.
/// </summary>
public sealed class Skill_Preview
{
    public string Name;
    public string Description;
    public Skill_Category Category;
    public float ManaCost;
    public float Cooldown;
    public bool HasEnoughMana = true;
    public readonly List<Skill_Preview_Line> Lines = new List<Skill_Preview_Line>();

    public void Add(string label, string value) => Lines.Add(new Skill_Preview_Line(label, value));
}
