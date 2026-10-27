using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thông tin của MỘT lần thi triển, truyền qua từng Skill_Effect theo thứ tự (cũng là "phiên" của lần cast đó).
///
/// - HitTargets: effect sau dùng kết quả effect trước (Damage trúng ai thì Làm chậm đúng những người đó).
/// - Effect KÉO DÀI (xoay, kênh...) phải: BeginChannel() lúc bắt đầu, EndChannel() lúc xong, và kiểm tra IsAlive mỗi vòng.
///   Skill_Caster dựa vào đó để vào trạng thái Channeling, khoá di chuyển và chờ tới khi xong.
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

    /// <summary>Số effect đang kéo dài (đã BeginChannel nhưng chưa EndChannel).</summary>
    public int ChannelCount { get; private set; }

    /// <summary>Lần thi triển đã bị ngắt (chết, bị choáng, đổi skill...).</summary>
    public bool IsCancelled { get; private set; }

    private bool finished;

    /// <summary>
    /// Effect kéo dài kiểm tra mỗi vòng: false = phải dừng ngay (người cast chết/bị huỷ/bị ngắt).
    /// Chỉ dùng cho effect kéo dài; effect trễ kiểu "nổ sau 1 giây" thì chỉ cần kiểm tra IsDead của mình.
    /// </summary>
    public bool IsAlive => !IsCancelled && Caster != null && Stats != null && !Stats.IsDead;

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

    /// <summary>Báo "tôi sẽ chạy kéo dài". Gọi ĐỒNG BỘ trong Execute, trước khi StartCoroutine.</summary>
    public void BeginChannel()
    {
        if (finished || IsCancelled) return;
        ChannelCount++;
    }

    /// <summary>Báo "tôi đã xong". Gọi đúng một lần cho mỗi BeginChannel (nên để trong finally).</summary>
    public void EndChannel()
    {
        if (ChannelCount > 0) ChannelCount--;
    }

    // Chỉ Skill_Caster được gọi.
    internal void Cancel()
    {
        IsCancelled = true;
        ChannelCount = 0;
    }

    internal void Finish()
    {
        finished = true;
        ChannelCount = 0;
    }
}
