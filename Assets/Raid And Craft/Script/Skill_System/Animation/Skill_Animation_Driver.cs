using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cầu nối TUỲ CHỌN giữa Skill_Caster và Animator. Chỉ chạy animation cho skill có loại animation khác None
/// (loại do skill hoặc vũ khí quyết định, xem Skill_Caster.ResolveAnimation). Không có Driver, hoặc loại = None,
/// hoặc Animator không có transition cho loại đó thì skill vẫn chạy bình thường, chỉ không có animation.
///
/// Animator Controller cần (tên đổi được ở Inspector; tham số nào không có thì Driver tự bỏ qua, không báo lỗi):
///   Int     Skill_Anim_Type      số của Skill_Animation_Type (dùng làm điều kiện chuyển trạng thái)
///   Float   Skill_Anim_Speed     tốc độ animation (tích "Multiplier -> Parameter" ở state)
///   Trigger Skill_Windup         bắt đầu gồng
///   Trigger Skill_Cast           ra đòn
///   Bool    Skill_Channeling     true suốt giai đoạn kéo dài
///   Trigger Skill_Recovery       hậu cast
///   Trigger Skill_Interrupt      bị ngắt giữa chừng
///
/// Animation Event: thêm event tên "Skill_Fire" vào đúng khung hình ra đòn của clip, và skill bật
/// "Fire On Animation Event" thì hiệu ứng chạy đúng khung đó. Driver PHẢI nằm cùng GameObject với Animator
/// thì Unity mới gọi được event này.
///
/// Muốn đổi cách làm animation (Timeline, Animancer...) sau này: viết Driver khác nghe cùng các sự kiện của
/// Skill_Caster, skill và effect không phải sửa.
/// </summary>
[DisallowMultipleComponent]
public class Skill_Animation_Driver : MonoBehaviour
{
    [SerializeField] private Skill_Caster caster;
    [SerializeField] private Animator animator;

    [Header("Tên tham số Animator")]
    [SerializeField] private string animationTypeParam = "Skill_Anim_Type";
    [SerializeField] private string speedParam = "Skill_Anim_Speed";
    [SerializeField] private string windupTrigger = "Skill_Windup";
    [SerializeField] private string castTrigger = "Skill_Cast";
    [SerializeField] private string channelingBool = "Skill_Channeling";
    [SerializeField] private string recoveryTrigger = "Skill_Recovery";
    [SerializeField] private string interruptTrigger = "Skill_Interrupt";

    private int typeHash, speedHash, windupHash, castHash, channelingHash, recoveryHash, interruptHash;
    private HashSet<int> available;
    private Skill_State lastState = Skill_State.Idle;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (caster == null) caster = GetComponentInParent<Skill_Caster>();

        typeHash = Animator.StringToHash(animationTypeParam);
        speedHash = Animator.StringToHash(speedParam);
        windupHash = Animator.StringToHash(windupTrigger);
        castHash = Animator.StringToHash(castTrigger);
        channelingHash = Animator.StringToHash(channelingBool);
        recoveryHash = Animator.StringToHash(recoveryTrigger);
        interruptHash = Animator.StringToHash(interruptTrigger);
    }

    private void OnEnable()
    {
        if (caster == null) return;

        caster.OnStateChanged += HandleStateChanged;
        caster.OnSkillInterrupted += HandleInterrupted;
    }

    private void OnDisable()
    {
        if (caster == null) return;

        caster.OnStateChanged -= HandleStateChanged;
        caster.OnSkillInterrupted -= HandleInterrupted;
    }

    // Gắn vào Animation Event tên "Skill_Fire" (xem mô tả class).
    public void Skill_Fire()
    {
        if (caster != null) caster.NotifyAnimationFire();
    }

    // ------------------------------------------------------------------

    private void HandleStateChanged(Skill_State state, Skill_Data skill)
    {
        Skill_State previous = lastState;
        lastState = state;

        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (state == Skill_State.Idle)
        {
            SetBool(channelingHash, false);
            return;
        }

        Skill_Animation_Type type = caster.ResolveAnimation(skill);
        if (type == Skill_Animation_Type.None) return;

        // Đầu một phiên cast mới (Windup, hoặc Casting đi thẳng từ Idle): xoá trigger còn treo của phiên trước.
        if (state == Skill_State.Windup || (state == Skill_State.Casting && previous == Skill_State.Idle)) ResetTriggers();

        switch (state)
        {
            case Skill_State.Windup:
                Prepare(skill, type);
                SetTrigger(windupHash);
                break;

            case Skill_State.Casting:
                Prepare(skill, type);
                SetTrigger(castHash);
                break;

            case Skill_State.Channeling:
                SetBool(channelingHash, true);
                break;

            case Skill_State.Recovery:
                SetBool(channelingHash, false);
                SetTrigger(recoveryHash);
                break;
        }
    }

    private void HandleInterrupted(Skill_Data skill)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (caster.ResolveAnimation(skill) == Skill_Animation_Type.None) return;

        ResetTriggers();
        SetBool(channelingHash, false);
        SetTrigger(interruptHash);
    }

    // Đặt loại + tốc độ cho skill sắp chạy.
    // Trigger còn treo (unit không có transition cho loại đó) được xoá ở đầu phiên mới, KHÔNG xoá khi về Idle:
    // skill tức thời về Idle ngay trong cùng frame, xoá lúc đó sẽ mất trigger trước khi Animator kịp dùng.
    private void Prepare(Skill_Data skill, Skill_Animation_Type type)
    {
        SetInt(typeHash, (int)type);
        SetFloat(speedHash, skill.animationConfig.scaleSpeedWithActionSpeed ? caster.ActionSpeedMultiplier : 1f);
    }

    private void ResetTriggers()
    {
        ResetTrigger(windupHash);
        ResetTrigger(castHash);
        ResetTrigger(recoveryHash);
        ResetTrigger(interruptHash);
    }

    // ------------------------------------------------------------------
    // Chỉ chạm tham số có thật trong Animator Controller.
    // ------------------------------------------------------------------

    private bool Has(int hash)
    {
        if (available == null)
        {
            available = new HashSet<int>();
            foreach (AnimatorControllerParameter p in animator.parameters) available.Add(p.nameHash);
        }
        return available.Contains(hash);
    }

    private void SetTrigger(int hash) { if (Has(hash)) animator.SetTrigger(hash); }
    private void ResetTrigger(int hash) { if (Has(hash)) animator.ResetTrigger(hash); }
    private void SetBool(int hash, bool value) { if (Has(hash)) animator.SetBool(hash, value); }
    private void SetInt(int hash, int value) { if (Has(hash)) animator.SetInteger(hash, value); }
    private void SetFloat(int hash, float value) { if (Has(hash)) animator.SetFloat(hash, value); }
}
