using System.Collections.Generic;
using UnityEngine;

/// <summary>Hồi máu tức thời (cố định + phần trăm máu tối đa) cho các mục tiêu được chọn.</summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Effects/Heal", fileName = "Effect_Heal")]
public class Heal_Effect : Skill_Effect
{
    public Skill_Target_Mode targetMode = Skill_Target_Mode.Self;
    [Min(0f)] public float radius = 3f;

    [Header("Lượng hồi")]
    [Min(0f)] public float flatAmount = 20f;
    [Tooltip("0.1 = hồi thêm 10% máu tối đa của mục tiêu.")]
    [Min(0f)] public float percentOfMaxHealth;

    public override void Execute(Skill_Context context)
    {
        List<Character_Stats> targets = Skill_Targeting.Rent();
        Skill_Targeting.Resolve(context, targetMode, radius, targets);

        for (int i = 0; i < targets.Count; i++)
        {
            Character_Stats target = targets[i];
            target.Heal(flatAmount + target.MaxHealth * percentOfMaxHealth);
        }

        Skill_Targeting.Release(targets);
    }

    public override void Describe(Skill_Caster caster, Skill_Data skill, Skill_Preview preview)
    {
        float amount = flatAmount + (caster.Stats != null ? caster.Stats.MaxHealth * percentOfMaxHealth : 0f);
        preview.Add("Heal", $"{amount:0.##}");
    }
}
