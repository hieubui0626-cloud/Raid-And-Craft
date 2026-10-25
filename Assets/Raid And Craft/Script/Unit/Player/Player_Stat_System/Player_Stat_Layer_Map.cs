using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nơi DUY NHẤT khai báo mỗi Player_Stat_Type thuộc Player_Stat_Layer nào.
/// Thứ tự khai báo ở đây cũng là thứ tự hiển thị trong UI.
/// Khi thêm Stat_Type mới: thêm vào enum, rồi thêm 1 dòng vào mảng Entries bên dưới.
/// </summary>
public static class Player_Stat_Layer_Map
{
    private static readonly KeyValuePair<Player_Stat_Type, Player_Stat_Layer>[] Entries =
    {
        // Layer: Complex
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Strength,     Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Agility,      Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Intelligence, Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MaxHealth,    Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MaxMana,      Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.LifeRegen,     Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ManaRegen,     Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MovementSpeed,     Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.CharacterLevel,     Player_Stat_Layer.Complex),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MaxCharacterLevel,  Player_Stat_Layer.Complex),

        // Layer: Offence
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.PhysicDamage, Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MagicDamage,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.FireDamage,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ColdDamage,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.LightningDamage,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ChaosDamage,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ActionSpeed,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Accuracy,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.CriticalChance,  Player_Stat_Layer.Offence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.CriticalDamage,  Player_Stat_Layer.Offence),

        // Layer: Defence
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Armor,        Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.Evade,        Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.MagicResistance, Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.FireResistance, Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ColdResistance, Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.LightningResistance, Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.ChaosResistance, Player_Stat_Layer.Defence),
        new KeyValuePair<Player_Stat_Type, Player_Stat_Layer>(Player_Stat_Type.BlockChance, Player_Stat_Layer.Defence),
        

        // Layer: Vitals
       
    };

    private static readonly Dictionary<Player_Stat_Type, Player_Stat_Layer> LayerByType = BuildLookup();
    private static readonly Player_Stat_Type[] OrderedTypes = BuildOrderedTypes();

    /// <summary>Toàn bộ Stat_Type đã khai báo, theo thứ tự hiển thị.</summary>
    public static IReadOnlyList<Player_Stat_Type> AllTypes => OrderedTypes;

    public static Player_Stat_Layer GetLayer(Player_Stat_Type type)
    {
        if (LayerByType.TryGetValue(type, out Player_Stat_Layer layer)) return layer;

        Debug.LogError($"[Player_Stat_Layer_Map] {type} chưa được khai báo Layer. Hãy thêm vào mảng Entries.");
        return Player_Stat_Layer.Complex;
    }

    private static Dictionary<Player_Stat_Type, Player_Stat_Layer> BuildLookup()
    {
        var lookup = new Dictionary<Player_Stat_Type, Player_Stat_Layer>(Entries.Length);
        foreach (var entry in Entries) lookup[entry.Key] = entry.Value;
        return lookup;
    }

    private static Player_Stat_Type[] BuildOrderedTypes()
    {
        var types = new Player_Stat_Type[Entries.Length];
        for (int i = 0; i < Entries.Length; i++) types[i] = Entries[i].Key;
        return types;
    }
}
