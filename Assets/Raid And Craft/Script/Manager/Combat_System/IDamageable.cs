using System;

/// <summary>
/// Mọi thứ có thể nhận sát thương (Player, Enemy, vật thể phá được...) đều implement interface này.
/// Combat system chỉ làm việc với IDamageable nên không cần biết đối tượng là ai.
/// - TakeDamage(float): trừ máu thẳng, bỏ qua né/đỡ/giảm (dùng cho DoT, môi trường, sát thương cố định).
/// - TakeDamage(Damage_Info): đường đi chuẩn của combat, tính né/đỡ/Armor/kháng bằng chỉ số bên nhận.
/// </summary>
public interface IDamageable
{
    bool IsDead { get; }

    /// <summary>Bắn đúng một lần khi máu về 0. Hồi sinh xong có thể bắn lại lần nữa.</summary>
    event Action OnDeath;

    /// <summary>Nhận sát thương (amount dương). Trả về lượng máu thực sự bị trừ (0 nếu đã chết).</summary>
    float TakeDamage(float amount);

    /// <summary>Nhận một đòn đầy đủ thông tin (loại sát thương, chí mạng, nguồn...). Trả về kết cục để UI/hiệu ứng dùng.</summary>
    Damage_Result TakeDamage(Damage_Info info);
}
