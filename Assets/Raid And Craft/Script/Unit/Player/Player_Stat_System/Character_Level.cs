using System;
using UnityEngine;

/// <summary>
/// Cấp độ và cấp tối đa của MỘT nhân vật (Player, và sau này Enemy theo cấp của Arena/World Map). Gắn cùng Character_Stats.
/// - Cấp lấy từ chỉ số CharacterLevel (giá trị gốc), cấp tối đa từ chỉ số MaxCharacterLevel (mặc định 100).
///   Cả hai là chỉ số bình thường nên save/load có sẵn và modifier có thể nâng trần cấp (trang bị, Magic Circuit, world modifier...).
/// - Cấp hiệu lực = CharacterLevel kẹp trong [1, MaxLevel]. Trần bị hạ xuống dưới cấp hiện tại thì cấp hiệu lực tụt theo
///   (giá trị gốc vẫn giữ, trần nâng lại thì cấp trở lại).
/// - Mỗi lần cấp đổi, chỉ số tăng theo Level_Config.growthPerLevel (gỡ và cộng lại modifier, nguồn = script này),
///   tăng cấp thì hồi đầy máu/mana (nếu config bật) và bắn OnLevelChanged.
/// - Chưa có kinh nghiệm (EXP): sẽ làm cùng Arena/World Map. Khi đó chỉ cần gọi TryLevelUp()/SetLevel().
/// - Điểm Magic Circuit suy ra từ cấp: xem Player_Circuit_Points.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Character_Level : MonoBehaviour
{
    /// <summary>Bắn khi cấp hiệu lực đổi (cấp cũ, cấp mới).</summary>
    public event Action<int, int> OnLevelChanged;

    /// <summary>Bắn khi cấp tối đa đổi.</summary>
    public event Action OnMaxLevelChanged;

    [SerializeField] private Character_Stats stats;

    [Tooltip("Cách chỉ số tăng theo cấp và số điểm Magic Circuit. Bỏ trống thì dùng giá trị mặc định (không lưu được thành asset).")]
    [SerializeField] private Level_Config config;

    private int lastLevel;
    private int lastMaxLevel;

    public Level_Config Config => config;

    /// <summary>Cấp tối đa hiện tại (tối thiểu 1).</summary>
    public int MaxLevel => Mathf.Max(1, stats.GetIntValue(Player_Stat_Type.MaxCharacterLevel));

    /// <summary>Cấp hiệu lực, luôn trong [1, MaxLevel].</summary>
    public int Level => Mathf.Clamp(stats.GetIntValue(Player_Stat_Type.CharacterLevel), 1, MaxLevel);

    public bool IsMaxLevel => Level >= MaxLevel;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();

        if (config == null)
        {
            Debug.LogWarning($"[{nameof(Character_Level)}] Chưa gán Level_Config, dùng giá trị mặc định. " +
                             "Tạo asset: Create -> Raid And Craft -> Level Config.", this);
            config = ScriptableObject.CreateInstance<Level_Config>();
        }

        lastLevel = Level;
        lastMaxLevel = MaxLevel;

        ApplyGrowth(lastLevel);

        // Chỉ số khởi tạo (máu/mana đầy) được tính trước khi cộng chỉ số theo cấp, nên đầy lại cho khớp.
        if (!stats.IsDead) stats.RestoreFull();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        stats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnStatChanged -= HandleStatChanged;
    }

    private void HandleStatChanged(Player_Stat stat)
    {
        if (stat.Type == Player_Stat_Type.MaxCharacterLevel)
        {
            int max = MaxLevel;
            if (max != lastMaxLevel)
            {
                lastMaxLevel = max;
                OnMaxLevelChanged?.Invoke();
            }

            Refresh(); // Trần đổi có thể làm cấp hiệu lực đổi theo.
        }
        else if (stat.Type == Player_Stat_Type.CharacterLevel)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        int newLevel = Level;
        if (newLevel == lastLevel) return;

        int oldLevel = lastLevel;
        lastLevel = newLevel;

        ApplyGrowth(newLevel);

        if (newLevel > oldLevel && config.restoreOnLevelUp && !stats.IsDead) stats.RestoreFull();

        OnLevelChanged?.Invoke(oldLevel, newLevel);
    }

    // Cộng chỉ số theo cấp: tổng = perLevel x (cấp - 1). Gỡ hết modifier cũ của script này rồi cộng lại.
    private void ApplyGrowth(int level)
    {
        stats.RemoveModifiersFromSource(this);

        int steps = level - 1;
        if (steps <= 0) return;

        foreach (Level_Config.Growth growth in config.growthPerLevel)
        {
            if (Mathf.Approximately(growth.perLevel, 0f)) continue;

            stats.AddModifier(growth.stat, new Player_Stat_Modifier(growth.perLevel * steps, Player_Stat_Modifier_Type.Flat, this));
        }
    }

    // ------------------------------------------------------------------
    // ĐỔI CẤP
    // ------------------------------------------------------------------

    /// <summary>Đặt cấp (kẹp trong [1, MaxLevel]). Trả về true nếu cấp thực sự đổi.</summary>
    public bool SetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        if (level == Level) return false;

        stats.SetBaseValue(Player_Stat_Type.CharacterLevel, level);
        return true;
    }

    /// <summary>Tăng cấp (mặc định 1 cấp). Trả về false nếu đã ở cấp tối đa hoặc amount không dương.</summary>
    public bool TryLevelUp(int amount = 1)
    {
        if (amount <= 0 || IsMaxLevel) return false;

        return SetLevel(Level + amount);
    }

    // Gỡ lỗi nhanh: chuột phải component khi đang Play.
    [ContextMenu("Debug: Level Up")]
    private void DebugLevelUp()
    {
        TryLevelUp();
    }

    [ContextMenu("Debug: Level Down")]
    private void DebugLevelDown()
    {
        SetLevel(Level - 1);
    }
}
