/// <summary>
/// Loại animation của một skill hoặc đòn đánh vũ khí (danh sách CHUNG cho cả game, giống cách Warcraft chọn "Animation Names").
/// Skill_Animation_Driver gửi số của mục này sang Animator (tham số Int "Skill_Anim_Type"); unit nào có
/// transition cho loại đó thì chạy, không có thì không chạy gì, skill vẫn hoạt động bình thường.
///
/// QUY TẮC: số được lưu trong asset VÀ nằm trong điều kiện của Animator Controller.
/// Chỉ thêm mục mới Ở CUỐI, không đổi số, không chèn vào giữa, không xoá mục đang dùng.
/// </summary>
public enum Skill_Animation_Type
{
    None = 0,          // Không animation: Driver bỏ qua skill này

    // Đòn đánh
    Attack_Slash = 1,  // Chém (kiếm, rìu)
    Attack_Stab = 2,   // Đâm (dao, giáo)
    Attack_Bow = 3,    // Bắn cung

    // Phép
    Cast_Staff = 4,    // Vung trượng
    Cast_Spell = 5,    // Niệm phép tức thời
    Channel = 6,       // Kênh (giữ tay, dùng cùng Skill_Channeling)

    // Đặc biệt
    Spin = 7,          // Xoay vòng (Blade Storm)
    Slam = 8           // Đập xuống

    // Thêm sau này Ở CUỐI.
}
