using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Cách chọn mục tiêu cho các hiệu ứng không tự có hình dạng riêng (hồi máu, buff, debuff...).</summary>
public enum Skill_Target_Mode
{
    Self,                // Chính người cast
    HitTargets,          // Những ai đã bị hiệu ứng trước trong cùng skill trúng
    RadiusAroundCaster,  // Mọi mục tiêu (theo Target Mask) quanh người cast
    RadiusAroundAim      // Mọi mục tiêu (theo Target Mask) quanh điểm nhắm
}

/// <summary>
/// Nơi DUY NHẤT quét mục tiêu: khử trùng lặp (nhân vật nhiều collider chỉ tính một lần),
/// sắp xếp gần nhất trước, giới hạn số mục tiêu, không cấp phát bộ nhớ mỗi lần quét.
/// Muốn thêm kiểu chọn mục tiêu mới: thêm vào enum + Resolve.
/// </summary>
public static class Skill_Targeting
{
    private const int BufferSize = 64;

    private struct Candidate
    {
        public Character_Stats Stats;
        public float SqrDistance;
    }

    private static readonly Collider[] Buffer = new Collider[BufferSize];
    private static readonly List<Candidate> Candidates = new List<Candidate>(BufferSize);
    private static readonly HashSet<Character_Stats> Seen = new HashSet<Character_Stats>();
    private static readonly Stack<List<Character_Stats>> Pool = new Stack<List<Character_Stats>>();
    private static readonly Comparison<Candidate> ByDistance = (a, b) => a.SqrDistance.CompareTo(b.SqrDistance);

    /// <summary>Mượn một danh sách tạm (nhớ Release sau khi dùng).</summary>
    public static List<Character_Stats> Rent() => Pool.Count > 0 ? Pool.Pop() : new List<Character_Stats>(16);

    public static void Release(List<Character_Stats> list)
    {
        if (list == null) return;
        list.Clear();
        Pool.Push(list);
    }

    /// <summary>
    /// Tìm mục tiêu trong quạt (arcAngle >= 360 = hình tròn). KẾT QUẢ ĐƯỢC THÊM VÀO `results` (không xoá sẵn).
    /// maxTargets &lt;= 0 = không giới hạn. Mục tiêu gần nhất được lấy trước.
    /// </summary>
    public static void FindInArc(Vector3 origin, Vector3 direction, float range, float arcAngle, int maxTargets,
                                 LayerMask mask, Character_Stats exclude, List<Character_Stats> results)
    {
        Candidates.Clear();
        Seen.Clear();

        int count = Physics.OverlapSphereNonAlloc(origin, range, Buffer, mask, QueryTriggerInteraction.Ignore);
        bool fullCircle = arcAngle >= 359.9f;
        float halfArc = arcAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Character_Stats target = Buffer[i].GetComponentInParent<Character_Stats>();
            if (target == null || target == exclude || target.IsDead || !Seen.Add(target)) continue;

            Vector3 toTarget = target.transform.position - origin;
            toTarget.y = 0f;

            if (!fullCircle && toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(direction, toTarget) > halfArc) continue;

            Candidates.Add(new Candidate { Stats = target, SqrDistance = toTarget.sqrMagnitude });
        }

        Candidates.Sort(ByDistance);

        int take = maxTargets > 0 ? Mathf.Min(maxTargets, Candidates.Count) : Candidates.Count;
        for (int i = 0; i < take; i++) results.Add(Candidates[i].Stats);
    }

    /// <summary>Chọn mục tiêu theo chế độ. KẾT QUẢ ĐƯỢC THÊM VÀO `results`.</summary>
    public static void Resolve(Skill_Context context, Skill_Target_Mode mode, float radius, List<Character_Stats> results)
    {
        switch (mode)
        {
            case Skill_Target_Mode.Self:
                if (context.Stats != null && !context.Stats.IsDead) results.Add(context.Stats);
                break;

            case Skill_Target_Mode.HitTargets:
                foreach (Character_Stats hit in context.HitTargets)
                {
                    if (hit != null && !hit.IsDead) results.Add(hit);
                }
                break;

            case Skill_Target_Mode.RadiusAroundCaster:
                FindInArc(context.Caster.transform.position, context.Direction, radius, 360f, 0,
                          context.Caster.TargetMask, context.Stats, results);
                break;

            case Skill_Target_Mode.RadiusAroundAim:
                FindInArc(context.AimPoint, context.Direction, radius, 360f, 0,
                          context.Caster.TargetMask, context.Stats, results);
                break;
        }
    }
}
