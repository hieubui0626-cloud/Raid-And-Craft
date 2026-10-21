using System;

/// <summary>
/// Mọi thứ có thể nhận sát thương (Player, Enemy, vật thể phá được...) đều implement interface này.
/// Combat system chỉ làm việc với IDamageable nên không cần biết đối tượng là ai.
/// Giai đoạn sau sẽ thêm overload nhận Damage_Info (loại sát thương, crit, nguồn gây sát thương).
/// </summary>
public interface IDamageable
{
    bool IsDead { get; }

    /// <summary>Bắn đúng một lần khi máu về 0. Hồi sinh xong có thể bắn lại lần nữa.</summary>
    event Action OnDeath;

    /// <summary>Nhận sát thương (amount dương). Trả về lượng máu thực sự bị trừ (0 nếu đã chết).</summary>
    float TakeDamage(float amount);
}
