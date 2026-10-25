using UnityEngine;

/// <summary>
/// Dữ liệu một kỹ năng (tạo asset: chuột phải Project -> Create -> Raid And Craft -> Skill Data).
/// ĐÒN ĐÁNH MẶC ĐỊNH cũng là một Skill_Data: bật useWeaponDelivery để cách ra đòn lấy từ vũ khí đang cầm.
/// Skill khác (Power Strike, Fireball...) tắt useWeaponDelivery và tự khai báo delivery/loại sát thương.
/// Ra đòn thật do Skill_Caster làm.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Skill Data", fileName = "Skill_New")]
public class Skill_Data : ScriptableObject
{
    public string displayName = "New Skill";
    public Sprite icon;

    [Tooltip("Mô tả ngắn hiện trong tooltip khi rê chuột vào skill (tuỳ chọn).")]
    [TextArea(1, 4)] public string description;

    [Header("Chi phí và hồi chiêu")]
    [Min(0f)] public float manaCost;

    [Tooltip("Bật: hồi chiêu = 1 / ActionSpeed (đòn đánh, tốc độ đánh tăng theo chỉ số). Tắt: dùng 'cooldown' bên dưới (giây).")]
    public bool usesActionSpeed = true;
    [Min(0f)] public float cooldown = 1f;

    [Header("Sát thương")]
    [Tooltip("Hệ số nhân lên chỉ số sát thương của nhân vật (1 = bằng chỉ số, 2 = gấp đôi).")]
    [Min(0f)] public float damageMultiplier = 1f;

    [Header("Cách ra đòn")]
    [Tooltip("Bật cho ĐÒN ĐÁNH MẶC ĐỊNH: cách ra đòn, loại sát thương và hệ số lấy từ vũ khí đang cầm (không cầm gì = tay không). Hai mục dưới sẽ bị bỏ qua.")]
    public bool useWeaponDelivery;

    public Damage_Type[] damageTypes = { Damage_Type.Physic };
    public Skill_Delivery delivery = new Skill_Delivery();
}
