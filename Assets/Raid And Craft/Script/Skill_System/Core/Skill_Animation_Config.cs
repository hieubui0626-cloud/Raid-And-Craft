using System;
using UnityEngine;

/// <summary>
/// Cấu hình animation của skill (trường con của Skill_Data.animationConfig).
/// Skill chỉ NÓI "dùng loại animation nào", việc chạy Animator nằm hết trong Skill_Animation_Driver,
/// nên sau này đổi cách làm animation (Timeline, Animancer...) chỉ phải thay Driver, không động vào skill.
/// </summary>
[Serializable]
public class Skill_Animation_Config
{
    [Tooltip("Loại animation của skill. None = skill không có animation (Driver bỏ qua).")]
    public Skill_Animation_Type animationType = Skill_Animation_Type.None;

    [Tooltip("Bật: lấy loại animation từ vũ khí đang cầm (Weapon_Data.attackAnimation). 'Animation Type' bên trên dùng khi tay không " +
             "hoặc vũ khí chưa chọn loại. Bật cho đòn đánh mặc định.")]
    public bool useWeaponAnimation;

    [Tooltip("Bật: tốc độ animation nhân theo ActionSpeed (tham số Float Skill_Anim_Speed của Animator).")]
    public bool scaleSpeedWithActionSpeed = true;
}
