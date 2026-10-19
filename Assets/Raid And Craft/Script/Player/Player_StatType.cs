// Nhóm chỉ số (Layer) - dùng để gom nhóm khi hiển thị UI.
// QUY TẮC: các giá trị số bên dưới được serialize trong Inspector (PlayerStats.defaultStats).
// Không đổi số của mục đã có, không chèn vào giữa. Chỉ thêm mục mới ở CUỐI enum.
public enum Player_StatLayer
{
    Stats = 0,
    Attack = 1,
    Defend = 2
    // Thêm sau này: Speed, Resistance, Crafting...
}

// Chi tiết từng chỉ số. Layer của mỗi chỉ số được khai báo trong Player_StatLayerMap.
public enum Player_StatType
{
    // Layer: Stats
    Strength = 0,
    Agility = 1,
    Intelligence = 2,
    Health = 3,
    Mana = 4,

    // Layer: Attack
    PhysicAttack = 5,
    MagicAttack = 6,

    // Layer: Defend
    Armor = 7,
    Evade = 8,
}
