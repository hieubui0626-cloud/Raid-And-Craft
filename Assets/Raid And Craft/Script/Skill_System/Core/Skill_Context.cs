using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thông tin của MỘT lần thi triển, truyền qua từng Skill_Effect theo thứ tự.
/// HitTargets cho phép hiệu ứng sau dùng kết quả của hiệu ứng trước
/// (ví dụ Damage trúng ai thì Làm chậm đúng những người đó).
/// </summary>
public sealed class Skill_Context
{
    public Skill_Caster Caster { get; }
    public Skill_Data Skill { get; }
    public Character_Stats Stats => Caster.Stats;

    /// <summary>Điểm nhắm trong không gian 3D.</summary>
    public Vector3 AimPoint { get; }

    /// <summary>Hướng ngang (đã chuẩn hoá) từ người cast tới điểm nhắm.</summary>
    public Vector3 Direction { get; }

    /// <summary>Các mục tiêu đã bị hiệu ứng trước đó trúng (đòn tức thời; đạn bay không tính).</summary>
    public List<Character_Stats> HitTargets { get; } = new List<Character_Stats>();

    public Skill_Context(Skill_Caster caster, Skill_Data skill, Vector3 aimPoint)
    {
        Caster = caster;
        Skill = skill;
        AimPoint = aimPoint;
        Direction = caster.GetHorizontalDirection(aimPoint);
    }

    public void AddHit(Character_Stats target)
    {
        if (target != null && !HitTargets.Contains(target)) HitTargets.Add(target);
    }
}
