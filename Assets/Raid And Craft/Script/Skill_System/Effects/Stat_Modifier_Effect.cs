using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff/debuff chỉ số có thời hạn (làm chậm, tăng giáp, giảm kháng...).
/// Làm chậm nên dùng PercentMult với Value âm (-0.3 = -30%): nhân độc lập nên nhiều debuff không bao giờ về 0.
/// Source của modifier chính là asset này, nên cùng một asset áp lại sẽ được xử lý theo Stack Mode.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Effects/Stat Modifier", fileName = "Effect_Stat_Modifier")]
public class Stat_Modifier_Effect : Skill_Effect
{
    public Skill_Target_Mode targetMode = Skill_Target_Mode.HitTargets;
    [Min(0f)] public float radius = 3f;

    [Header("Chỉ số bị tác động")]
    public Player_Stat_Type stat = Player_Stat_Type.MovementSpeed;
    public Player_Stat_Modifier_Type modifierType = Player_Stat_Modifier_Type.PercentMult;
    [Tooltip("PercentAdd/PercentMult: -0.3 = -30%. Flat: cộng thẳng.")]
    public float value = -0.3f;
    [Min(0.01f)] public float duration = 3f;

    [Tooltip("Áp lại lên mục tiêu đang chịu hiệu ứng này: Refresh = làm mới thời gian, Stack = chồng thêm, Ignore = bỏ qua.")]
    public Status_Stack_Mode stackMode = Status_Stack_Mode.Refresh;

    public override void Execute(Skill_Context context)
    {
        List<Character_Stats> targets = Skill_Targeting.Rent();
        Skill_Targeting.Resolve(context, targetMode, radius, targets);

        for (int i = 0; i < targets.Count; i++)
        {
            Status_Effect_Controller controller = Status_Effect_Controller.GetOrAdd(targets[i]);
            controller.Apply(this, stat, new Player_Stat_Modifier(value, modifierType, this), duration, stackMode);
        }

        Skill_Targeting.Release(targets);
    }

    public override float GetReach(Skill_Caster caster, Skill_Data skill)
    {
        return targetMode == Skill_Target_Mode.RadiusAroundCaster ? radius : 0f;
    }

    public override void Describe(Skill_Caster caster, Skill_Data skill, Skill_Preview preview)
    {
        string amount = modifierType == Player_Stat_Modifier_Type.Flat
            ? $"{value:+0.##;-0.##}"
            : $"{value * 100f:+0.##;-0.##}%";
        preview.Add(stat.ToString(), $"{amount} / {duration:0.#}s");
    }
}
