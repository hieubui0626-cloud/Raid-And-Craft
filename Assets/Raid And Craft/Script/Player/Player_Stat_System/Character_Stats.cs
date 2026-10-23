using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ chỉ số của MỘT nhân vật (Player hoặc Enemy). Mỗi chỉ số là một Player_Stat (base + modifier).
/// - Layer của từng chỉ số lấy từ Player_Stat_Layer_Map nên các hàm chỉ cần truyền Player_Stat_Type.
/// - Chỉ số phát sinh (derived) khai báo bằng derivedRules: Target = Source * Multiplier (cộng vào Target dạng Flat).
///   Ví dụ mặc định: MaxHealth += Strength * 10, MaxMana += Intelligence * 10.
/// - Máu/Mana hiện tại (CurrentHealth/CurrentMana) nằm ở đây, luôn được giữ trong khoảng [0, Max].
/// - Implement IDamageable: nhận sát thương, báo chết (OnDeath), hồi sinh (Revive).
/// - Dùng chung cho Player và Enemy: mỗi prefab tự chỉnh defaultStats/derivedRules trong Inspector.
/// </summary>
public class Character_Stats : MonoBehaviour, IDamageable
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

    /// <summary>Bắn đúng một lần khi máu về 0 (xem IDamageable).</summary>
    public event Action OnDeath;

    /// <summary>Bắn khi được hồi sinh (Revive/RestoreFull lúc đang chết).</summary>
    public event Action OnRevive;

    /// <summary>
    /// Chỉ số nền mặc định. Chỉ số nào KHÔNG có trong danh sách Inspector (ví dụ prefab cũ tạo trước khi thêm MovementSpeed)
    /// sẽ lấy giá trị ở đây thay vì 0. Muốn một chỉ số bằng 0 thật: thêm dòng của nó vào danh sách Inspector với giá trị 0.
    /// </summary>
    public static readonly Default_Stat[] BuiltInDefaultStats =
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
    };

    [Header("Chỉ số khởi tạo (chỉnh được trong Inspector)")]
    [Tooltip("Chỉ số không có trong danh sách này sẽ dùng giá trị mặc định có sẵn trong code (BuiltInDefaultStats).")]
    [SerializeField]
    private List<Default_Stat> defaultStats = new List<Default_Stat>(BuiltInDefaultStats);

    /// <summary>
    /// Bộ quy tắc phát sinh mặc định (mỗi 1 điểm thuộc tính cho bao nhiêu chỉ số phụ). Đây là nơi chỉnh cân bằng thuộc tính.
    /// - Strength: máu, sát thương vật lý, giáp.
    /// - Agility: độ chính xác, né, tốc độ hành động, tỉ lệ chí mạng (%).
    /// - Intelligence: mana, sát thương phép, hồi mana, kháng phép (%).
    /// Chỉ số nguyên tố (Fire/Cold/Lightning/Chaos), Block và kháng nguyên tố KHÔNG phát sinh: chúng đến từ trang bị/gem/passive.
    /// Chỉ số khởi tạo (defaultStats) là phần "nền", phần phát sinh cộng thêm lên trên.
    /// </summary>
    public static readonly Derived_Rule[] DefaultDerivedRules =
    {
        // Strength
        new Derived_Rule(Player_Stat_Type.Strength, Player_Stat_Type.MaxHealth, 10f),
        new Derived_Rule(Player_Stat_Type.Strength, Player_Stat_Type.PhysicDamage, 0.5f),
        new Derived_Rule(Player_Stat_Type.Strength, Player_Stat_Type.Armor, 0.5f),

        // Agility
        new Derived_Rule(Player_Stat_Type.Agility, Player_Stat_Type.Accuracy, 1f),
        new Derived_Rule(Player_Stat_Type.Agility, Player_Stat_Type.Evade, 0.5f),
        new Derived_Rule(Player_Stat_Type.Agility, Player_Stat_Type.ActionSpeed, 0.01f),
        new Derived_Rule(Player_Stat_Type.Agility, Player_Stat_Type.CriticalChance, 0.1f),

        // Intelligence
        new Derived_Rule(Player_Stat_Type.Intelligence, Player_Stat_Type.MaxMana, 10f),
        new Derived_Rule(Player_Stat_Type.Intelligence, Player_Stat_Type.MagicDamage, 0.5f),
        new Derived_Rule(Player_Stat_Type.Intelligence, Player_Stat_Type.ManaRegen, 0.05f),
        new Derived_Rule(Player_Stat_Type.Intelligence, Player_Stat_Type.MagicResistance, 0.2f),
    };

    [Header("Chỉ số phát sinh: Target += Source * Multiplier")]
    [SerializeField]
    private List<Derived_Rule> derivedRules = new List<Derived_Rule>(DefaultDerivedRules);

    // Component đã có sẵn trên prefab/scene giữ danh sách cũ trong Inspector (đổi code mặc định không tự cập nhật).
    // Menu này chỉ THÊM các quy tắc (source -> target) còn thiếu, không đụng tới quy tắc/giá trị bạn đã chỉnh.
    [ContextMenu("Thêm quy tắc phát sinh mặc định còn thiếu")]
    private void AddMissingDefaultDerivedRules()
    {
        int added = 0;
        foreach (Derived_Rule def in DefaultDerivedRules)
        {
            bool exists = false;
            foreach (Derived_Rule rule in derivedRules)
            {
                if (rule.source == def.source && rule.target == def.target) { exists = true; break; }
            }

            if (exists) continue;
            derivedRules.Add(def);
            added++;
        }

        Debug.Log($"[{nameof(Character_Stats)}] Đã thêm {added} quy tắc phát sinh mặc định còn thiếu.", this);
    }

    private readonly Dictionary<Player_Stat_Type, Player_Stat> stats = new Dictionary<Player_Stat_Type, Player_Stat>();
    private readonly Dictionary<Player_Stat_Layer, List<Player_Stat>> statsByLayer = new Dictionary<Player_Stat_Layer, List<Player_Stat>>();
    private readonly Dictionary<Player_Stat_Type, List<Derived_Rule>> rulesBySource = new Dictionary<Player_Stat_Type, List<Derived_Rule>>();

    private bool initialized;

    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    public float MaxHealth => GetValue(Player_Stat_Type.MaxHealth);
    public float MaxMana => GetValue(Player_Stat_Type.MaxMana);

    /// <summary>True khi máu đã về 0 và chưa được hồi sinh. Đang chết thì không nhận sát thương/hồi máu.</summary>
    public bool IsDead { get; private set; }

    [Header("Hồi phục")]
    [Tooltip("Tự hồi máu/mana mỗi giây theo LifeRegen/ManaRegen. Tắt cho vật thể không hồi (rương, bẫy...).")]
    [SerializeField] private bool enableRegen = true;

    private void Awake()
    {
        EnsureInitialized();
    }

    // Hồi máu/mana theo thời gian. Dừng khi chết, khi tạm dừng game (timeScale = 0) hoặc khi đã đầy.
    // Lưu ý: khi đang hồi, OnVitalsChanged bắn mỗi frame; UI chỉ nên vẽ lại phần thanh máu/mana, không vẽ lại cả bảng chỉ số.
    private void Update()
    {
        if (!enableRegen || !initialized || IsDead) return;

        float deltaTime = Time.deltaTime;

        if (CurrentHealth < MaxHealth)
        {
            ModifyHealth(GetValue(Player_Stat_Type.LifeRegen) * deltaTime);
        }

        if (CurrentMana < MaxMana)
        {
            ModifyMana(GetValue(Player_Stat_Type.ManaRegen) * deltaTime);
        }
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của Character_Stats vẫn an toàn.
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        // Nền là giá trị mặc định trong code, danh sách Inspector ghi đè lên (chỉ số thiếu trong Inspector không bị về 0).
        var baseValues = new Dictionary<Player_Stat_Type, float>();
        foreach (Default_Stat d in BuiltInDefaultStats) baseValues[d.type] = d.baseValue;
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
                Debug.LogError($"[{nameof(Character_Stats)}] Derived rule {rule.source} -> {rule.target} tham chiếu chỉ số chưa khai báo trong Layer_Map.", this);
                continue;
            }

            if (rule.source == rule.target)
            {
                Debug.LogError($"[{nameof(Character_Stats)}] Derived rule không được trỏ vào chính nó ({rule.source}).", this);
                continue;
            }

            if (CreatesCycle(rule))
            {
                Debug.LogError($"[{nameof(Character_Stats)}] Derived rule {rule.source} -> {rule.target} tạo vòng phát sinh (đi ngược về {rule.source}), đã bỏ qua.", this);
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

    // Thêm rule này có làm xuất hiện vòng không? Có nếu từ Target đi theo các rule đã nhận vẫn tới được Source.
    private bool CreatesCycle(Derived_Rule rule)
    {
        var visited = new HashSet<Player_Stat_Type>();
        var pending = new Stack<Player_Stat_Type>();
        pending.Push(rule.target);

        while (pending.Count > 0)
        {
            Player_Stat_Type current = pending.Pop();
            if (current == rule.source) return true;
            if (!visited.Add(current)) continue;

            if (rulesBySource.TryGetValue(current, out List<Derived_Rule> next))
            {
                foreach (Derived_Rule r in next) pending.Push(r.target);
            }
        }

        return false;
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

    /// <summary>
    /// IDamageable: trừ máu thẳng (amount dương), KHÔNG qua né/đỡ/Armor/kháng. Trả về lượng máu thực sự bị trừ.
    /// Dùng cho sát thương cố định (DoT, môi trường). Đòn đánh bình thường hãy dùng TakeDamage(Damage_Info).
    /// Đây là điểm cuối cùng trừ máu: mọi đường sát thương đều đi qua đây.
    /// </summary>
    public float TakeDamage(float amount)
    {
        EnsureInitialized();
        if (IsDead || amount <= 0f) return 0f;

        return -ModifyHealth(-amount);
    }

    /// <summary>
    /// IDamageable: nhận một đòn đầy đủ. Né -> đỡ -> giảm theo Armor/kháng của CHÍNH nhân vật này -> trừ máu.
    /// Kết quả cho biết trúng/né/đỡ, có chí mạng không, máu bị trừ bao nhiêu và có bị hạ gục không.
    /// </summary>
    public Damage_Result TakeDamage(Damage_Info info)
    {
        EnsureInitialized();
        if (IsDead) return Damage_Result.Ignored;

        Damage_Outcome outcome = Combat_Formulas.Resolve(info, this, out float finalDamage);
        if (outcome != Damage_Outcome.Hit)
        {
            return new Damage_Result(outcome, false, 0f, false);
        }

        float dealt = TakeDamage(finalDamage);
        return new Damage_Result(Damage_Outcome.Hit, info.isCritical, dealt, IsDead);
    }

    /// <summary>Hồi máu (amount dương). Trả về lượng máu thực sự hồi. Đang chết thì không hồi được, hãy dùng Revive.</summary>
    public float Heal(float amount)
    {
        EnsureInitialized();
        if (IsDead || amount <= 0f) return 0f;

        return ModifyHealth(amount);
    }

    public void SetVitals(float health, float mana)
    {
        EnsureInitialized();
        SetVital(health, MaxHealth, true);
        SetVital(mana, MaxMana, false);
    }

    /// <summary>
    /// Cộng/trừ máu (delta âm = nhận sát thương). Trả về lượng thay đổi thực tế sau khi kẹp.
    /// Đang chết thì bỏ qua (trả về 0).
    /// </summary>
    public float ModifyHealth(float delta)
    {
        EnsureInitialized();
        if (IsDead) return 0f;

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
        if (IsDead || cost < 0f || CurrentMana < cost) return false;

        ModifyMana(-cost);
        return true;
    }

    /// <summary>Hồi sinh với một tỉ lệ máu (0..1, tối thiểu 1% để không chết ngay). Không làm gì nếu chưa chết.</summary>
    public void Revive(float healthPercent = 1f)
    {
        EnsureInitialized();
        if (!IsDead) return;

        IsDead = false;
        float percent = Mathf.Clamp(healthPercent, 0.01f, 1f);
        SetVital(MaxHealth * percent, MaxHealth, true);
        OnRevive?.Invoke();
    }

    /// <summary>Hồi đầy máu/mana. Nếu đang chết thì hồi sinh luôn.</summary>
    public void RestoreFull()
    {
        EnsureInitialized();
        Revive(1f);
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

        // Báo chết SAU khi UI đã nhận OnVitalsChanged (thanh máu về 0 trước rồi mới xử lý chết).
        if (isHealth && clamped <= 0f && !IsDead)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }

        return clamped - old;
    }

    private void ClampVitals()
    {
        SetVital(CurrentHealth, MaxHealth, true);
        SetVital(CurrentMana, MaxMana, false);
    }
}
