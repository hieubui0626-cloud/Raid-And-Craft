using UnityEngine;

/// <summary>
/// Lớp gốc của MỌI hành vi skill (gây sát thương, hồi máu, làm chậm, dịch chuyển...).
/// Mỗi hiệu ứng là một asset ScriptableObject; Skill_Data chỉ ghép chúng lại thành danh sách.
///
/// Thêm hành vi mới:
///   1. Tạo class kế thừa Skill_Effect, ghi đè Execute.
///   2. Thêm [CreateAssetMenu] để tạo asset.
///   3. Kéo asset vào danh sách Effects của Skill_Data.
/// Không cần sửa Skill_Data, Skill_Caster, UI hay AI.
/// </summary>
public abstract class Skill_Effect : ScriptableObject
{
    /// <summary>Chạy hiệu ứng. Dùng context để biết ai cast, nhắm đâu, đã trúng ai.</summary>
    public abstract void Execute(Skill_Context context);

    /// <summary>
    /// Tầm tối thiểu cần để hiệu ứng có tác dụng (AI dùng để biết đuổi tới đâu thì dừng).
    /// 0 = không đòi hỏi tầm (ví dụ tự buff).
    /// </summary>
    public virtual float GetReach(Skill_Caster caster, Skill_Data skill) => 0f;

    /// <summary>Thêm các dòng mô tả vào tooltip (không bắt buộc).</summary>
    public virtual void Describe(Skill_Caster caster, Skill_Data skill, Skill_Preview preview) { }
}
