using UnityEngine;

/// <summary>
/// Spell Stone: lắp vào ô Magic Circuit thì nhân vật có thêm một skill (tạo asset: Create -> Raid And Craft -> Items -> Spell Stone).
/// Gỡ ra thì skill biến mất khỏi kho và khỏi ô kỹ năng đã gán.
/// Hai stone cùng skill: skill chỉ biến mất khi gỡ stone cuối cùng (Magic_Circuit đếm tham chiếu).
/// Hệ số hiệu ứng của vùng chưa tác động lên skill.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Spell Stone", fileName = "SpellStone_New")]
public class Spell_Stone_Data : Circuit_Item_Data
{
    [Header("Skill được thêm khi lắp")]
    [SerializeField] private Skill_Data skill;

    public Skill_Data Skill => skill;

    public override void Apply(Magic_Circuit circuit, object source, float effectMultiplier)
    {
        circuit.AcquireSkill(skill);
    }

    public override void Remove(Magic_Circuit circuit, object source)
    {
        circuit.ReleaseSkill(skill);
    }
}
