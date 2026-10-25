// Chi tiết từng chỉ số. Layer của mỗi chỉ số được khai báo trong Player_Stat_Layer_Map.
// QUY TẮC: không đổi số của mục đã có, chỉ thêm mục mới ở CUỐI enum.
public enum Player_Stat_Type
{

    // Layer: Complex
    Strength = 0,
    Agility = 1,
    Intelligence = 2,
    MaxHealth = 3,
    MaxMana = 4, 
    LifeRegen = 14,
    ManaRegen = 15,
    MovementSpeed = 25,
    CharacterLevel = 26,
    MaxCharacterLevel = 27,

    // Layer: Offence
    PhysicDamage = 5,
    MagicDamage = 6,
    FireDamage = 21,
    ColdDamage = 22,
    LightningDamage = 23,
    ChaosDamage = 24,
    ActionSpeed = 7,
    Accuracy = 11,
    CriticalChance = 12,
    CriticalDamage = 13,

    // Layer: Defence
    Armor = 8,
    Evade = 9,
    MagicResistance = 16,
    FireResistance = 17,
    ColdResistance = 18,
    LightningResistance = 19,
    ChaosResistance = 20,
    BlockChance = 10,






}
