using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ chỉ số của Player. Mỗi chỉ số là một Player_Stat (base + modifier).
/// - Layer của từng chỉ số lấy từ Player_Stat_Layer_Map nên các hàm chỉ cần truyền Player_Stat_Type.
/// - Chỉ số phát sinh (derived) khai báo bằng derivedRules: Target = Source * Multiplier (cộng vào Target dạng Flat).
///   Ví dụ mặc định: MaxHealth += Strength * 10, MaxMana += Intelligence * 10.
/// - Máu/Mana hiện tại (CurrentHealth/CurrentMana) nằm ở đây, luôn được giữ trong khoảng [0, Max].
/// </summary>
public class Player_Stats : MonoBehaviour
{
    [Serializable]
    public struct Default_Stat
    {
        public Player_Stat_Type type;
        public float baseValue;

        public Default_Stat(Player_Stat_Type type, float baseValue)
        {
            this.type = type;
            this.baseValue = baseValue;
        }
    }

    /// <summary>Quy tắc phát sinh: Target nhận thêm (giá trị cuối của Source * Multiplier).</summary>
    [Serializable]
    public struct Derived_Rule
    {
        public Player_Stat_Type source;
        public Player_Stat_Type target;
        public float multiplier;
        

        public Derived_Rule(Player_Stat_Type source, Player_Stat_Type target, float multiplier)
        {
            this.source = source;
            this.target = target;
            this.multiplier = multiplier;
        }
    }

    /// <summary>Bắn khi BẤT KỲ chỉ số nào đổi giá trị (UI vẽ lại toàn bộ).</summary>
    public event Action OnStatsChanged;

    /// <summary>Bắn khi MỘT chỉ số đổi giá trị (hệ thống chỉ quan tâm 1 stat).</summary>
    public event Action<Player_Stat> OnStatChanged;

    /// <summary>Bắn khi máu hoặc mana hiện tại đổi (kể cả bị kẹp lại do Max giảm).</summary>
    public event Action OnVitalsChanged;

    [Header("Chỉ số khởi tạo (chỉnh được trong Inspector)")]
    [SerializeField]
    private List<Default_Stat> defaultStats = new List<Default_Stat>
    {
        // Layer: Complex
        new Default_Stat(Player_Stat_Type.Strength, 10f),
        new Default_Stat(Player_Stat_Type.Agility, 10f),
        new Default_Stat(Player_Stat_Type.Intelligence, 10f),
        new Default_Stat(Player_Stat_Type.MaxHealth, 50f),
        new Default_Stat(Player_Stat_Type.MaxMana, 30f),
        new Default_Stat(Player_Stat_Type.LifeRegen, 1f),
        new Default_Stat(Player_Stat_Type.ManaRegen, 1f),
        new Default_Stat(Player_Stat_Type.MovementSpeed, 5f),
        new Default_Stat(Player_Stat_Type.CharacterLevel, 1f),

        // Layer: Offence
        new Default_Stat(Player_Stat_Type.PhysicDamage, 10f),
        new Default_Stat(Player_Stat_Type.MagicDamage, 10f),
        new Default_Stat(Player_Stat_Type.FireDamage, 0f),
        new Default_Stat(Player_Stat_Type.ColdDamage, 0f),
        new Default_Stat(Player_Stat_Type.LightningDamage, 0f),
        new Default_Stat(Player_Stat_Type.ChaosDamage, 0f),
        new Default_Stat(Player_Stat_Type.ActionSpeed, 1f),
        new Default_Stat(Player_Stat_Type.Accuracy, 5f),
        new Default_Stat(Player_Stat_Type.CriticalChance, 5f),
        new Default_Stat(Player_Stat_Type.CriticalDamage, 50f),


        // Layer: Defence
        new Default_Stat(Player_Stat_Type.Armor, 5f),
        new Default_Stat(Player_Stat_Type.Evade, 2f),
        new Default_Stat(Player_Stat_Type.MagicResistance, 5f),
        new Default_Stat(Player_Stat_Type.FireResistance, 0f),
        new Default_Stat(Player_Stat_Type.ColdResistance, 0f),
        new Default_Stat(Player_Stat_Type.LightningResistance, 0f),
        new Default_Stat(Player_Stat_Type.ChaosResistance, 0f),
        new Default_Stat(Player_Stat_Type.BlockChance, 0f),
        

        // Layer: Vitals (giá trị gốc, chưa cộng phần phát sinh từ Strength/Intelligence)
        
    };

    [Header("Chỉ số phát sinh: Target += Source * Multiplier")]
    [SerializeField]
    private List<Derived_Rule> derivedRules = new List<Derived_Rule>
    {
        new Derived_Rule(Player_Stat_Type.Strength, Player_Stat_Type.MaxHealth, 10f),
        new Derived_Rule(Player_Stat_Type.Intelligence, Player_Stat_Type.MaxMana, 10f),
    };

    private readonly Dictionary<Player_Stat_Type, Player_Stat> stats = new Dictionary<Player_Stat_Type, Player_Stat>();
    private readonly Dictionary<Player_Stat_Layer, List<Player_Stat>> statsByLayer = new Dictionary<Player_Stat_Layer, List<Player_Stat>>();
    private readonly Dictionary<Player_Stat_Type, List<Derived_Rule>> rulesBySource = new Dictionary<Player_Stat_Type, List<Derived_Rule>>();

    private bool initialized;

    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    public float MaxHealth => GetValue(Player_Stat_Type.MaxHealth);
    public float MaxMana => GetValue(Player_Stat_Type.MaxMana);

    private void Awake()
    {
        EnsureInitialized();
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của Player_Stats vẫn an toàn.
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        var baseValues = new Dictionary<Player_Stat_Type, float>();
        foreach (Default_Stat d in defaultStats) baseValues[d.type] = d.baseValue;

        // Mọi Stat_Type đã khai báo trong Layer_Map đều có chỉ số (mặc định 0 nếu không có trong defaultStats).
        foreach (Player_Stat_Type type in Player_Stat_Layer_Map.AllTypes)
        {
            baseValues.TryGetValue(type, out float baseValue);

            var stat = new Player_Stat(type, baseValue);
            stats[type] = stat;

            Player_Stat_Layer layer = Player_Stat_Layer_Map.GetLayer(type);
            if (!statsByLayer.TryGetValue(layer, out List<Player_Stat> list))
            {
                list = new List<Player_Stat>();
                statsByLayer[layer] = list;
            }
            list.Add(stat);
        }

        // Dựng và áp dụng các quy tắc phát sinh TRƯỚC khi gắn event ra ngoài.
        foreach (Derived_Rule rule in derivedRules)
        {
            if (!stats.ContainsKey(rule.source) || !stats.ContainsKey(rule.target))
            {
                Debug.LogError($"[{nameof(Player_Stats)}] Derived rule {rule.source} -> {rule.target} tham chiếu chỉ số chưa khai báo trong Layer_Map.", this);
                continue;
            }

            if (rule.source == rule.target)
            {
                Debug.LogError($"[{nameof(Player_Stats)}] Derived rule không được trỏ vào chính nó ({rule.source}).", this);
                continue;
            }

            if (!rulesBySource.TryGetValue(rule.source, out List<Derived_Rule> rules))
            {
                rules = new List<Derived_Rule>();
                rulesBySource[rule.source] = rules;
            }
            rules.Add(rule);

            ApplyDerivedRule(rule);
        }

        foreach (Player_Stat stat in stats.Values) stat.OnValueChanged += HandleStatValueChanged;

        // Bắt đầu với đầy máu/mana.
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
    }

    private void HandleStatValueChanged(Player_Stat stat)
    {
        // Chỉ số nguồn đổi -> cập nhật các chỉ số phát sinh từ nó.
        if (rulesBySource.TryGetValue(stat.Type, out List<Derived_Rule> rules))
        {
            foreach (Derived_Rule rule in rules) ApplyDerivedRule(rule);
        }

        // Max giảm (ví dụ tháo trang bị) -> kẹp máu/mana hiện tại.
        if (stat.Type == Player_Stat_Type.MaxHealth || stat.Type == Player_Stat_Type.MaxMana)
        {
            ClampVitals();
        }

        OnStatChanged?.Invoke(stat);
        OnStatsChanged?.Invoke();
    }

    private void ApplyDerivedRule(Derived_Rule rule)
    {
        float bonus = stats[rule.source].Value * rule.multiplier;
        stats[rule.target].ReplaceModifierFromSource(DerivedKey(rule), bonus);
    }

    // Mỗi rule một khoá nguồn riêng (tách khỏi trang bị/buff) để nhiều rule cùng trỏ vào một Target không ghi đè nhau.
    // Lưu ý: không tạo vòng phát sinh (A -> B -> A), sẽ lặp vô hạn.
    private readonly Dictionary<(Player_Stat_Type, Player_Stat_Type), object> ruleKeys =
        new Dictionary<(Player_Stat_Type, Player_Stat_Type), object>();

    private object DerivedKey(Derived_Rule rule)
    {
        var id = (rule.source, rule.target);
        if (!ruleKeys.TryGetValue(id, out object key))
        {
            key = new object();
            ruleKeys[id] = key;
        }
        return key;
    }

    // ------------------------------------------------------------------
    // ĐỌC
    // ------------------------------------------------------------------

    public bool TryGetStat(Player_Stat_Type type, out Player_Stat stat)
    {
        EnsureInitialized();
        return stats.TryGetValue(type, out stat);
    }

    /// <summary>Giá trị cuối (đã tính modifier). Trả về 0 nếu chỉ số không tồn tại.</summary>
    public float GetValue(Player_Stat_Type type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.Value : 0f;
    }

    public int GetIntValue(Player_Stat_Type type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.IntValue : 0;
    }

    /// <summary>Giá trị gốc (chưa tính modifier).</summary>
    public float GetBaseValue(Player_Stat_Type type)
    {
        return TryGetStat(type, out Player_Stat stat) ? stat.BaseValue : 0f;
    }

    /// <summary>Danh sách chỉ số của một Layer theo thứ tự hiển thị (phục vụ vẽ UI tự động).</summary>
    public IReadOnlyList<Player_Stat> GetStatsInLayer(Player_Stat_Layer layer)
    {
        EnsureInitialized();
        return statsByLayer.TryGetValue(layer, out List<Player_Stat> list)
            ? list
            : (IReadOnlyList<Player_Stat>)Array.Empty<Player_Stat>();
    }

    // ------------------------------------------------------------------
    // GHI - giá trị gốc (level up, phân bổ điểm...)
    // ------------------------------------------------------------------

    public void SetBaseValue(Player_Stat_Type type, float value)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.BaseValue = value;
    }

    public void AddBaseValue(Player_Stat_Type type, float amount)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.BaseValue += amount;
    }

    // ------------------------------------------------------------------
    // GHI - modifier (trang bị, buff, passive...)
    // ------------------------------------------------------------------

    public void AddModifier(Player_Stat_Type type, Player_Stat_Modifier modifier)
    {
        if (TryGetStat(type, out Player_Stat stat)) stat.AddModifier(modifier);
    }

    public bool RemoveModifier(Player_Stat_Type type, Player_Stat_Modifier modifier)
    {
        return TryGetStat(type, out Player_Stat stat) && stat.RemoveModifier(modifier);
    }

    /// <summary>Gỡ mọi modifier của một nguồn trên tất cả chỉ số (ví dụ tháo trang bị).</summary>
    public void RemoveModifiersFromSource(object source)
    {
        EnsureInitialized();
        foreach (Player_Stat stat in stats.Values) stat.RemoveModifiersFromSource(source);
    }

    // ------------------------------------------------------------------
    // MÁU / MANA hiện tại
    // ------------------------------------------------------------------

    /// <summary>Cộng/trừ máu (delta âm = nhận sát thương). Trả về lượng thay đổi thực tế sau khi kẹp.</summary>
    public float ModifyHealth(float delta)
    {
        EnsureInitialized();
        return SetVital(CurrentHealth + delta, MaxHealth, true);
    }

    /// <summary>Cộng/trừ mana. Trả về lượng thay đổi thực tế sau khi kẹp.</summary>
    public float ModifyMana(float delta)
    {
        EnsureInitialized();
        return SetVital(CurrentMana + delta, MaxMana, false);
    }

    /// <summary>Trừ mana nếu đủ. Trả về false (và không trừ) nếu không đủ.</summary>
    public bool TrySpendMana(float cost)
    {
        EnsureInitialized();
        if (cost < 0f || CurrentMana < cost) return false;

        ModifyMana(-cost);
        return true;
    }

    public void RestoreFull()
    {
        EnsureInitialized();
        ModifyHealth(MaxHealth);
        ModifyMana(MaxMana);
    }

    private float SetVital(float newValue, float max, bool isHealth)
    {
        float clamped = Mathf.Clamp(newValue, 0f, max);
        float old = isHealth ? CurrentHealth : CurrentMana;

        if (Mathf.Approximately(old, clamped)) return 0f;

        if (isHealth) CurrentHealth = clamped;
        else CurrentMana = clamped;

        OnVitalsChanged?.Invoke();
        return clamped - old;
    }

    private void ClampVitals()
    {
        SetVital(CurrentHealth, MaxHealth, true);
        SetVital(CurrentMana, MaxMana, false);
    }
}
