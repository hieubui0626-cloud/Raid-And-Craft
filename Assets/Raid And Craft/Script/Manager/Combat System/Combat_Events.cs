using System;

/// <summary>
/// Cổng báo kết quả đòn đánh toàn game. Mọi thứ gây sát thương (Combat_Attacker, Skill_Caster, Skill_Projectile...)
/// gọi Report sau khi đòn chạm mục tiêu; UI số sát thương nổi, âm thanh trúng đòn... chỉ cần nghe MỘT chỗ này.
/// (event C# chỉ được bắn từ bên trong class khai báo nó, nên các nơi khác phải gọi Report.)
/// </summary>
public static class Combat_Events
{
    /// <summary>Bắn mỗi khi một đòn chạm một mục tiêu: (bên đánh, bên nhận, kết quả).</summary>
    public static event Action<Character_Stats, Character_Stats, Damage_Result> OnAnyAttackResolved;

    public static void Report(Character_Stats attacker, Character_Stats target, Damage_Result result)
    {
        OnAnyAttackResolved?.Invoke(attacker, target, result);
    }
}
