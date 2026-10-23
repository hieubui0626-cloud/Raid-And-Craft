using System;
using UnityEngine;

/// <summary>Cách một đòn/kỹ năng đưa sát thương tới mục tiêu.</summary>
public enum Skill_Delivery_Type
{
    /// <summary>Vùng hình quạt phía trước mặt, chạm ngay lập tức, giới hạn số mục tiêu.</summary>
    Melee_Arc = 0,
    /// <summary>Bắn ra một viên đạn bay thẳng theo hướng nhắm.</summary>
    Projectile = 1,
}

/// <summary>
/// Cấu hình cách ra đòn. Dùng chung cho vũ khí (đòn đánh mặc định) và kỹ năng riêng (Skill_Data).
/// Chỉ các trường của kiểu đang chọn mới có tác dụng.
/// </summary>
[Serializable]
public class Skill_Delivery
{
    public Skill_Delivery_Type type = Skill_Delivery_Type.Melee_Arc;

    [Header("Melee_Arc")]
    [Tooltip("Bán kính vùng đánh (mét), tính từ chân nhân vật. Thực thể được tính là trúng nếu thân nó chạm vùng này.")]
    [Min(0.1f)] public float range = 2f;

    [Tooltip("Góc quạt (độ). 90 = quạt hẹp trước mặt, 180 = nửa vòng, 360 = xoay quanh người.")]
    [Range(10f, 360f)] public float arcAngle = 90f;

    [Tooltip("Số mục tiêu tối đa nhận sát thương trong một đòn (lấy các mục tiêu gần nhất trước).")]
    [Min(1)] public int maxTargets = 3;

    [Header("Projectile")]
    [Tooltip("Prefab đạn (cần script Skill_Projectile). Bỏ trống = tự tạo một quả cầu nhỏ để thử.")]
    public Skill_Projectile projectilePrefab;

    [Min(0.1f)] public float projectileSpeed = 15f;

    [Tooltip("Số giây tối đa đạn tồn tại (quãng bay = tốc độ x thời gian).")]
    [Min(0.1f)] public float projectileLifetime = 1.5f;

    [Tooltip("Số mục tiêu đạn xuyên qua thêm sau mục tiêu đầu. 0 = trúng ai đầu tiên là biến mất.")]
    [Min(0)] public int pierce;
}
