using UnityEngine;

/// <summary>
/// Bình hồi máu/mana tức thời (tạo asset: Create -> Raid And Craft -> Items -> Restore Potion).
/// Không dùng được khi đã chết, hoặc khi cả máu và mana (phần mà bình hồi) đều đã đầy.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Restore Potion", fileName = "Potion_New")]
public class Restore_Potion_Data : Consumable_Item_Data
{
    [Header("Lượng hồi")]
    [Min(0f)] [SerializeField] private float healthRestore = 50f;
    [Min(0f)] [SerializeField] private float manaRestore = 0f;

    public float HealthRestore => healthRestore;
    public float ManaRestore => manaRestore;

    public override bool TryUse(Character_Stats user)
    {
        if (user == null || user.IsDead) return false;

        bool canHeal = healthRestore > 0f && user.CurrentHealth < user.MaxHealth;
        bool canRestoreMana = manaRestore > 0f && user.CurrentMana < user.MaxMana;
        if (!canHeal && !canRestoreMana) return false;

        if (canHeal) user.Heal(healthRestore);
        if (canRestoreMana) user.ModifyMana(manaRestore);
        return true;
    }
}
