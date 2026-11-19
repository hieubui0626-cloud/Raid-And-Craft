using System.Collections.Generic;
using UnityEngine;

public enum Stat_Modifier_Duration_Mode
{
    Temporary,
    Permanent
}

[CreateAssetMenu(
    menuName = "Raid And Craft/Skill Effects/Stat Modifier",
    fileName = "Effect_Stat_Modifier")]
public class Stat_Modifier_Effect : Skill_Effect
{
    [Header("Target")]
    public Skill_Target_Mode targetMode = Skill_Target_Mode.HitTargets;

    [Min(0f)]
    public float radius = 3f;

    [Header("Chế độ hiệu ứng")]
    public Stat_Modifier_Duration_Mode durationMode =
        Stat_Modifier_Duration_Mode.Temporary;

    [Header("Các chỉ số bị tác động")]
    public List<Stat_Modifier_Entry> modifiers =
        new List<Stat_Modifier_Entry>
        {
            new Stat_Modifier_Entry
            {
                stat = Player_Stat_Type.MovementSpeed,
                modifierType = Player_Stat_Modifier_Type.PercentMult,
                value = -0.3f
            }
        };

    [Header("Temporary Settings")]
    [Min(0.01f)]
    public float duration = 3f;

    [Tooltip("Refresh = làm mới thời gian, Stack = chồng thêm, Ignore = bỏ qua.")]
    public Status_Stack_Mode stackMode = Status_Stack_Mode.Refresh;

    public override void Execute(Skill_Context context)
    {
        List<Character_Stats> targets = Skill_Targeting.Rent();

        Skill_Targeting.Resolve(context, targetMode, radius, targets);

        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
        {
            Character_Stats target = targets[targetIndex];

            if (target == null)
                continue;

            for (int modifierIndex = 0;
                 modifierIndex < modifiers.Count;
                 modifierIndex++)
            {
                Stat_Modifier_Entry entry = modifiers[modifierIndex];

                Player_Stat_Modifier modifier =
                    new Player_Stat_Modifier(
                        entry.value,
                        entry.modifierType,
                        this);

                if (durationMode == Stat_Modifier_Duration_Mode.Temporary)
                {
                    Status_Effect_Controller controller =
                        Status_Effect_Controller.GetOrAdd(target);

                    controller.Apply(
                        this,
                        entry.stat,
                        modifier,
                        duration,
                        stackMode);
                }
                else
                {
                    target.AddModifier(entry.stat, modifier);
                }
            }
        }

        Skill_Targeting.Release(targets);
    }

    public override float GetReach(Skill_Caster caster, Skill_Data skill)
    {
        return targetMode == Skill_Target_Mode.RadiusAroundCaster
            ? radius
            : 0f;
    }

    public override void Describe(
        Skill_Caster caster,
        Skill_Data skill,
        Skill_Preview preview)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            Stat_Modifier_Entry entry = modifiers[i];

            string amount = entry.modifierType ==
                Player_Stat_Modifier_Type.Flat
                ? $"{entry.value:+0.##;-0.##}"
                : $"{entry.value * 100f:+0.##;-0.##}%";

            string description = durationMode ==
                Stat_Modifier_Duration_Mode.Temporary
                ? $"{amount} / {duration:0.#}s"
                : amount;

            preview.Add(entry.stat.ToString(), description);
        }
    }
}