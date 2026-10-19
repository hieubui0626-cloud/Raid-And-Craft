// 1. Quản lý các Nhóm Chỉ Số (Layer)
public enum Player_StatLayer
{
    Stats,
    Attack,
    Defend
    // Dễ dàng thêm sau này: Speed, Resistance, Crafting...
}

// 2. Quản lý chi tiết từng Chỉ số
public enum Player_StatType
{
    // Layer: Stats
    Strength,
    Agility,
    Intelligence,

    // Layer: Attack
    PhysicAttack,
    MagicAttack,

    // Layer: Defend
    Armor,
    Evade
}
