/// <summary>
/// Các giai đoạn của một lần thi triển skill (do Skill_Caster quản lý).
///
///   Idle -> [Windup] -> Casting -> [Channeling] -> [Recovery] -> Idle
///
/// Giai đoạn trong [ ] chỉ có khi skill cấu hình (Windup Time > 0, effect gọi BeginChannel, Recovery Time > 0).
/// Skill tức thời đi Idle -> Casting -> Idle trong cùng một frame.
///
/// QUY TẮC: giá trị số có thể được lưu, chỉ thêm mục mới Ở CUỐI, không đổi số mục cũ.
/// Thêm trạng thái mới: xem guide mục "Thêm một trạng thái mới".
/// </summary>
public enum Skill_State
{
    Idle = 0,        // Rảnh, không thi triển gì
    Windup = 1,      // Gồng/chuẩn bị (cast time): hiệu ứng CHƯA chạy, còn có thể bị ngắt
    Casting = 2,     // Khoảnh khắc hiệu ứng chạy (tức thời, một frame)
    Channeling = 3,  // Có effect đang kéo dài (Blade Storm...): chờ tới khi effect báo xong
    Recovery = 4     // Hậu cast: khựng lại sau khi ra đòn

    // Thêm sau này (ví dụ Charging, Stunned...): thêm ở cuối.
}
