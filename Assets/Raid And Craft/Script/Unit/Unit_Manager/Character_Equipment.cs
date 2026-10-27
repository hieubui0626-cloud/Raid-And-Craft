using System;
using UnityEngine;

/// <summary>
/// Trang bị đang mang của MỘT nhân vật (hiện chỉ có vũ khí). Gắn cùng object với Character_Stats.
/// - Khi đổi vũ khí: gỡ chỉ số của vũ khí cũ, cộng chỉ số của vũ khí mới (modifier có Source = vũ khí nên gỡ sạch được).
/// - Implement ISkill_Weapon_Source: Damage_Effect (bật Use Weapon Attack) lấy cách đánh/loại sát thương/hệ số từ vũ khí qua đây.
/// - Muốn đổi vũ khí khi đang chơi (nhặt đồ, đổi đồ từ Inventory): gọi Equip(). Đổi ô Weapon trong Inspector lúc đang Play
///   KHÔNG tự cập nhật chỉ số, hãy dùng Equip().
/// - Chưa được lưu trong Save_Data (làm cùng Inventory sau).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Character_Equipment : MonoBehaviour, ISkill_Weapon_Source
{
    /// <summary>Bắn khi đổi vũ khí (tham số là vũ khí mới, null = tháo vũ khí).</summary>
    public event Action<Weapon_Data> OnWeaponChanged;

    [SerializeField] private Character_Stats stats;

    [Tooltip("Vũ khí đang cầm lúc bắt đầu. Bỏ trống = tay không.")]
    [SerializeField] private Weapon_Data weapon;

    public Weapon_Data Weapon => weapon;

    /// <summary>Cho Damage_Effect (Use Weapon Attack). Tay không = false, effect dùng delivery của chính nó.</summary>
    public bool TryGetWeaponAttack(out Skill_Weapon_Attack attack)
    {
        if (weapon == null)
        {
            attack = default;
            return false;
        }

        attack = new Skill_Weapon_Attack(weapon.attackDelivery, weapon.damageTypes, weapon.damageMultiplier, weapon.attackAnimation);
        return true;
    }

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        ApplyModifiers(weapon);
    }

    /// <summary>Đổi vũ khí (null = tháo). Gỡ chỉ số vũ khí cũ rồi cộng chỉ số vũ khí mới.</summary>
    public void Equip(Weapon_Data newWeapon)
    {
        if (newWeapon == weapon) return;

        if (weapon != null) stats.RemoveModifiersFromSource(weapon);

        weapon = newWeapon;
        ApplyModifiers(weapon);

        OnWeaponChanged?.Invoke(weapon);
    }

    private void ApplyModifiers(Weapon_Data data)
    {
        if (data == null) return;

        foreach (Weapon_Stat_Modifier m in data.statModifiers)
        {
            stats.AddModifier(m.stat, new Player_Stat_Modifier(m.value, m.modifierType, data));
        }
    }
}
