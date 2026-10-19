using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nơi DUY NHẤT khai báo mỗi Player_StatType thuộc Player_StatLayer nào.
/// Thứ tự khai báo ở đây cũng là thứ tự hiển thị trong UI.
/// Khi thêm StatType mới: thêm vào enum, rồi thêm 1 dòng vào mảng Entries bên dưới.
/// </summary>
public static class Player_StatLayerMap
{
    private static readonly KeyValuePair<Player_StatType, Player_StatLayer>[] Entries =
    {
        // Layer: Stats
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Strength,     Player_StatLayer.Stats),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Agility,      Player_StatLayer.Stats),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Intelligence, Player_StatLayer.Stats),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Health, Player_StatLayer.Stats),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Mana, Player_StatLayer.Stats),

        // Layer: Attack
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.PhysicAttack, Player_StatLayer.Attack),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.MagicAttack,  Player_StatLayer.Attack),

        // Layer: Defend
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Armor,        Player_StatLayer.Defend),
        new KeyValuePair<Player_StatType, Player_StatLayer>(Player_StatType.Evade,        Player_StatLayer.Defend),
    };

    private static readonly Dictionary<Player_StatType, Player_StatLayer> LayerByType = BuildLookup();
    private static readonly Player_StatType[] OrderedTypes = BuildOrderedTypes();

    /// <summary>Toàn bộ StatType đã khai báo, theo thứ tự hiển thị.</summary>
    public static IReadOnlyList<Player_StatType> AllTypes => OrderedTypes;

    public static Player_StatLayer GetLayer(Player_StatType type)
    {
        if (LayerByType.TryGetValue(type, out Player_StatLayer layer)) return layer;

        Debug.LogError($"[Player_StatLayerMap] {type} chưa được khai báo Layer. Hãy thêm vào mảng Entries.");
        return Player_StatLayer.Stats;
    }

    private static Dictionary<Player_StatType, Player_StatLayer> BuildLookup()
    {
        var lookup = new Dictionary<Player_StatType, Player_StatLayer>(Entries.Length);
        foreach (var entry in Entries) lookup[entry.Key] = entry.Value;
        return lookup;
    }

    private static Player_StatType[] BuildOrderedTypes()
    {
        var types = new Player_StatType[Entries.Length];
        for (int i = 0; i < Entries.Length; i++) types[i] = Entries[i].Key;
        return types;
    }
}
