// Loại skill, dùng để lọc popup chọn skill, nhóm tooltip, Magic Circuit...
// Loại KHÔNG quyết định cách skill hoạt động (việc đó do danh sách Effects của Skill_Data).
// QUY TẮC: các giá trị số được serialize trong asset. Không đổi số của mục đã có,
// không chèn vào giữa. Chỉ thêm mục mới ở CUỐI enum.
public enum Skill_Category
{
    Attack = 0, // Đòn vật lý / theo vũ khí (Double Slash, Power Strike...)
    Spell = 1,  // Phép thuật (Fireball, Arc Lightning...)

    // Thêm sau này: Buff, Utility, Movement...
}
