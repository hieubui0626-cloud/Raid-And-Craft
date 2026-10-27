using System;

/// <summary>
/// Những gì bị KHOÁ trên người cast trong một giai đoạn của skill. Chọn nhiều mục cùng lúc trong Inspector.
/// - Movement: đứng yên (NavMeshAgent bị dừng, lệnh đi mới được xếp hàng tới khi hết khoá).
/// - Rotation: script khác không được xoay nhân vật (Player_Combat, Enemy_AI, NavMeshAgent); effect của skill vẫn xoay được.
/// - Casting: không thi triển skill khác. Bỏ chọn = cho phép bấm skill khác để HUỶ skill đang chạy (animation cancel).
/// </summary>
[Flags]
public enum Skill_Lock
{
    None = 0,
    Movement = 1,
    Rotation = 2,
    Casting = 4
}
