using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ chỉ số của Player. Mỗi chỉ số là một Player_Stat (base + modifier).
/// Layer của từng chỉ số lấy từ Player_StatLayerMap nên các hàm chỉ cần truyền StatType.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Serializable]
    public struct StatDefault
    {
        public Player_StatType type;
        public float baseValue;

        public StatDefault(Player_StatType type, float baseValue)
        {
            this.type = type;
            this.baseValue = baseValue;
        }
    }

    /// <summary>Bắn khi BẤT KỲ chỉ số nào đổi giá trị (dùng cho UI vẽ lại toàn bộ).</summary>
    public event Action OnStatsChanged;

    /// <summary>Bắn khi MỘT chỉ số đổi giá trị (dùng cho hệ thống chỉ quan tâm 1 stat, ví dụ cập nhật máu).</summary>
    public event Action<Player_Stat> OnStatChanged;

    [Header("Chỉ số khởi tạo (chỉnh được trong Inspector)")]
    [SerializeField]
    private List<StatDefault> defaultStats = new List<StatDefault>
    {
        // Layer: Stats
        new StatDefault(Player_StatType.Strength, 10f),
        new StatDefault(Player_StatType.Agility, 10f),
        new StatDefault(Player_StatType.Intelligence, 10f),

        // Layer: Attack
        new StatDefault(Player_StatType.PhysicAttack, 15f),
        new StatDefault(Player_StatType.MagicAttack, 12f),

        // Layer: Defend
        new StatDefault(Player_StatType.Armor, 5f),
        new StatDefault(Player_StatType.Evade, 2f),
    };

    private readonly Dictionary<Player_StatType, Player_Stat> stats = new Dictionary<Player_StatType, Player_Stat>();
    private readonly Dictionary<Player_StatLayer, List<Player_Stat>> statsByLayer = new Dictionary<Player_StatLayer, List<Player_Stat>>();
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của PlayerStats vẫn an toàn.
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        var baseValues = new Dictionary<Player_StatType, float>();
        foreach (StatDefault d in defaultStats) baseValues[d.type] = d.baseValue;

        // Mọi StatType đã khai báo trong LayerMap đều có chỉ số (mặc định 0 nếu không có trong defaultStats).
        foreach (Player_StatType type in Player_StatLayerMap.AllTypes)
        {
            baseValues.TryGetValue(type, out float baseValue);

            var stat = new Player_Stat(type, baseValue);
            stat.OnValueChanged += HandleStatValueChanged;
            stats[type] = stat;

            Player_StatLayer layer = Player_StatLayerMap.GetLayer(type);
            if (!statsByLayer.TryGetValue(layer, out List<Player_Stat> list))
            {
                list = new List<Player_Stat>();
                statsByLayer[layer] = list;
            }
            list.Add(stat);
        }
    }

    private void HandleStatValueChanged(Player_Stat stat)
    {
        OnStatChanged?.Invoke(stat);
        OnStatsChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // ĐỌC
    // ------------------------------------------------------------------

    public bool TryGetStat(Player_StatType type, out Player_Stat stat)
    {
        EnsureInitialized();
        return stats.TryGetValue(type, out stat);
    }

    /// <summary>Giá trị cuối (đã tính modifier). Trả về 0 nếu chỉ số không tồn tại.</summary>
    public float GetValue(Player_StatType type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.Value : 0f;
    }

    public int GetIntValue(Player_StatType type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.IntValue : 0;
    }

    /// <summary>Giá trị gốc (chưa tính modifier).</summary>
    public float GetBaseValue(Player_StatType type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.BaseValue : 0f;
    }

    /// <summary>Danh sách chỉ số của một Layer theo thứ tự hiển thị (phục vụ vẽ UI tự động).</summary>
    public IReadOnlyList<Player_Stat> GetStatsInLayer(Player_StatLayer layer)
    {
        EnsureInitialized();
        return statsByLayer.TryGetValue(layer, out List<Player_Stat> list)
            ? list
            : (IReadOnlyList<Player_Stat>)Array.Empty<Player_Stat>();
    }

    // ------------------------------------------------------------------
    // GHI - giá trị gốc (level up, phân bổ điểm...)
    // ------------------------------------------------------------------

    public void SetBaseValue(Player_StatType type, float value)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.BaseValue = value;
    }

    public void AddBaseValue(Player_StatType type, float amount)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.BaseValue += amount;
    }

    // ------------------------------------------------------------------
    // GHI - modifier (trang bị, buff, passive...)
    // ------------------------------------------------------------------

    public void AddModifier(Player_StatType type, Player_StatModifier modifier)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.AddModifier(modifier);
    }

    public bool RemoveModifier(Player_StatType type, Player_StatModifier modifier)
    {
        return TryGetStat(type, out Player_Stat stat) && stat.RemoveModifier(modifier);
    }

    /// <summary>Gỡ mọi modifier của một nguồn trên tất cả chỉ số (ví dụ tháo trang bị).</summary>
    public void RemoveModifiersFromSource(object source)
    {
        EnsureInitialized();
        foreach (Player_Stat stat in stats.Values) stat.RemoveModifiersFromSource(source);
    }
}
