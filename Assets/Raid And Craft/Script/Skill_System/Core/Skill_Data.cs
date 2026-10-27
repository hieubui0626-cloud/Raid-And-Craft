using UnityEngine;

/// <summary>
/// DỮ LIỆU một kỹ năng (Create -> Raid And Craft -> Skill Data).
/// Chỉ chứa thông tin chung: tên, icon, chi phí, hồi chiêu và DANH SÁCH HIỆU ỨNG.
/// Mọi hành vi (gây sát thương, hồi máu, làm chậm...) nằm trong các asset Skill_Effect.
/// Thêm hành vi mới = viết thêm một class kế thừa Skill_Effect, KHÔNG sửa file này.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Data", fileName = "Skill_New")]
public class Skill_Data : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    [Tooltip("Id ổn định dùng cho save. Để trống = tên asset (đổi tên asset sẽ đổi id).")]
    [SerializeField] private string skillId;
    public string displayName = "New Skill";
    public Sprite icon;
    [TextArea(1, 4)] public string description;

    [Tooltip("Loại skill (Attack / Spell). Chỉ để phân loại/lọc/hiển thị, không đổi cách skill chạy.")]
    public Skill_Category category = Skill_Category.Attack;

    [Header("Chi phí và hồi chiêu")]
    [Min(0f)] public float manaCost;
    [Tooltip("Bật: hồi chiêu = 1 / ActionSpeed. Tắt: dùng 'cooldown' bên dưới (giây).")]
    public bool usesActionSpeed = true;
    [Min(0f)] public float cooldown = 1f;

    [Header("Thi triển")]
    [Tooltip("Bật: nhân vật quay về phía điểm nhắm trước khi chạy hiệu ứng. Tắt cho skill tự buff.")]
    public bool faceAimDirection = true;

    [Tooltip("Gồng (cast time), hậu cast, kéo dài (channeling) và những gì bị khoá ở từng giai đoạn. Mặc định = tức thời.")]
    public Skill_Timing timing = new Skill_Timing();

    [Header("Animation (tuỳ chọn)")]
    [Tooltip("Cấu hình gửi cho Skill_Animation_Driver. Không có Driver hoặc loại = None thì skill không có animation.")]
    public Skill_Animation_Config animationConfig = new Skill_Animation_Config();

    [Header("Hiệu ứng (chạy lần lượt từ trên xuống)")]
    public Skill_Effect[] effects;

    public string SkillId => string.IsNullOrEmpty(skillId) ? name : skillId;

    public bool HasEffects
    {
        get
        {
            if (effects == null) return false;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null) return true;
            }
            return false;
        }
    }
}
