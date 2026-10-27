using System;
using UnityEngine;

/// <summary>
/// Thời gian và khoá của các giai đoạn thi triển (trường con của Skill_Data.timing).
/// Mặc định mọi thời gian = 0 nên skill cũ chạy tức thời y như trước, không phải cấu hình lại.
/// </summary>
[Serializable]
public class Skill_Timing
{
    [Header("Thời gian")]
    [Tooltip("Gồng (giây) từ lúc bấm tới lúc hiệu ứng chạy. 0 = chạy ngay.")]
    [Min(0f)] public float windupTime;

    [Tooltip("Bật: hiệu ứng chạy khi animation gửi Animation Event 'Skill_Fire' (khớp đúng khoảnh khắc vung kiếm). " +
             "Windup Time lúc này là thời gian chờ tối đa; hết giờ mà chưa có event thì vẫn chạy. Cần Skill_Animation_Driver.")]
    public bool fireOnAnimationEvent;

    [Tooltip("Khựng lại (giây) sau khi hiệu ứng chạy xong (và sau khi kéo dài xong nếu có).")]
    [Min(0f)] public float recoveryTime;

    [Tooltip("Bật: Windup và Recovery rút ngắn theo ActionSpeed (ActionSpeed 2 = nhanh gấp đôi; gốc = 1).")]
    public bool scaleWithActionSpeed = true;

    [Header("Khoá theo giai đoạn")]
    public Skill_Lock windupLock = Skill_Lock.Movement | Skill_Lock.Rotation | Skill_Lock.Casting;
    [Tooltip("Giai đoạn kéo dài (Channeling). Bỏ 'Movement' nếu muốn vừa đi vừa dùng; bỏ 'Casting' nếu cho phép bấm skill khác để huỷ.")]
    public Skill_Lock channelLock = Skill_Lock.Movement | Skill_Lock.Rotation | Skill_Lock.Casting;
    public Skill_Lock recoveryLock = Skill_Lock.Movement | Skill_Lock.Casting;

    [Header("Ngắt")]
    [Tooltip("Cho phép bị ngắt bởi hiệu ứng bên ngoài (choáng, đẩy lùi...) qua Skill_Caster.TryInterrupt(). " +
             "Chết và đổi skill luôn ngắt được, bất kể ô này.")]
    public bool interruptible = true;
}
