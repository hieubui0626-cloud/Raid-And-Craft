using System.Collections;
using UnityEngine;

/// <summary>
/// Giữ nhân vật xoay trong một khoảng thời gian và cứ mỗi nhịp thì chạy một Damage_Effect (Blade Storm, Whirlwind...).
/// Damage_Effect (Melee_Arc, Arc 360) quyết định sát thương MỖI NHỊP; effect này chỉ lo thời gian và xoay.
///
/// Đây là mẫu cho effect KÉO DÀI: BeginChannel() lúc bắt đầu, EndChannel() lúc xong (để trong finally), và dừng khi
/// context.IsAlive = false. Skill_Caster nhờ vậy tự vào trạng thái Channeling, áp khoá di chuyển/xoay theo
/// Skill_Data.timing.channelLock và chờ tới khi effect này báo xong.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Effects/Blade Storm", fileName = "Effect_Blade_Storm")]
public class Blade_Storm_Effect : Skill_Effect
{
    [Header("Mỗi nhịp gây sát thương")]
    [Tooltip("Kéo asset Damage_Effect (Melee_Arc, Arc 360) vào đây.")]
    public Damage_Effect tickDamage;

    [Header("Kéo dài")]
    [Min(0.1f)] public float duration = 3f;
    [Min(0.05f)] public float tickInterval = 0.4f;

    [Header("Xoay")]
    [Tooltip("Độ/giây. 0 = không xoay (chỉ đánh theo nhịp, dùng khi animation tự xoay).")]
    public float spinSpeed = 720f;

    public override void Execute(Skill_Context context)
    {
        if (tickDamage == null) return;

        context.BeginChannel(); // Gọi ĐỒNG BỘ, trước StartCoroutine.
        context.Caster.StartCoroutine(Routine(context));
    }

    private IEnumerator Routine(Skill_Context context)
    {
        try
        {
            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < duration && context.IsAlive)
            {
                if (spinSpeed != 0f) context.Caster.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);

                if (elapsed >= nextTick)
                {
                    tickDamage.Execute(context);
                    nextTick += tickInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            context.EndChannel();
        }
    }

    public override float GetReach(Skill_Caster caster, Skill_Data skill)
    {
        return tickDamage != null ? tickDamage.GetReach(caster, skill) : 0f;
    }

    public override void Describe(Skill_Caster caster, Skill_Data skill, Skill_Preview preview)
    {
        if (tickDamage != null) tickDamage.Describe(caster, skill, preview);
        preview.Add("Duration", $"{duration:0.#}s ({tickInterval:0.##}s / tick)");
    }
}
