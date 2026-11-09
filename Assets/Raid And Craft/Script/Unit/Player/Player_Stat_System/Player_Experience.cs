using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kinh nghiệm (EXP) của Player: nhận EXP (từ Enemy_Reward khi hạ quái...), đủ EXP thì lên cấp qua Character_Level.TryLevelUp
/// (nhận nhiều EXP thì lên nhiều cấp liền, EXP dư chuyển sang cấp sau). Đường cong EXP nằm trong Level_Config (Base Exp Required, Exp Exponent).
/// Ở cấp tối đa không tích EXP nữa. Gắn lên PlayerPrefab cùng Character_Level. Player_Save lưu EXP hiện có (cấp độ lưu ở chỉ số gốc).
/// Hiển thị (tuỳ chọn): kéo một TextMeshPro vào Exp Text và/hoặc một Image (Image Type = Filled) vào Exp Fill.
/// Gỡ lỗi: chuột phải component khi Play -> Debug: Thêm EXP.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Level))]
public class Player_Experience : MonoBehaviour
{
    /// <summary>Bắn khi EXP hiện có hoặc cấp đổi (UI vẽ lại).</summary>
    public event Action OnExpChanged;

    /// <summary>Bắn mỗi lần nhận EXP (số EXP nhận được), để hiện "+25 EXP".</summary>
    public event Action<int> OnExpGained;

    [SerializeField] private Character_Level level;

    [Header("Hiển thị (tuỳ chọn)")]
    [Tooltip("{0} = cấp, {1} = EXP hiện có, {2} = EXP cần để lên cấp (cấp tối đa: MAX).")]
    [SerializeField] private TMP_Text expText;

    [SerializeField] private string expTextFormat = "Lv {0}   EXP {1} / {2}";

    [Tooltip("Image dạng Filled: độ đầy = tiến độ lên cấp.")]
    [SerializeField] private Image expFill;

    private int currentExp;
    private bool adding; // đang trong AddExp: bỏ qua việc kẹp EXP khi cấp đổi

    /// <summary>EXP hiện có trong cấp này.</summary>
    public int CurrentExp => currentExp;

    /// <summary>EXP cần để lên cấp kế; 0 ở cấp tối đa.</summary>
    public int ExpToNext => level.IsMaxLevel ? 0 : level.Config.GetExpRequired(level.Level);

    /// <summary>Tiến độ lên cấp 0..1 (cấp tối đa = 1).</summary>
    public float Progress => ExpToNext <= 0 ? 1f : Mathf.Clamp01(currentExp / (float)ExpToNext);

    private void Awake()
    {
        if (level == null) level = GetComponent<Character_Level>();
        RefreshView();
    }

    private void OnEnable()
    {
        if (level == null) level = GetComponent<Character_Level>();
        level.OnLevelChanged += HandleLevelChanged;
        level.OnMaxLevelChanged += HandleMaxLevelChanged;
    }

    private void OnDisable()
    {
        if (level == null) return;

        level.OnLevelChanged -= HandleLevelChanged;
        level.OnMaxLevelChanged -= HandleMaxLevelChanged;
    }

    private void HandleLevelChanged(int oldLevel, int newLevel)
    {
        // Cấp đổi từ nơi khác (Debug, nạp save, modifier): EXP không được vượt mức cần của cấp mới.
        if (!adding) ClampExp();
        RefreshView();
        OnExpChanged?.Invoke();
    }

    private void HandleMaxLevelChanged()
    {
        if (!adding) ClampExp();
        RefreshView();
        OnExpChanged?.Invoke();
    }

    /// <summary>Cộng EXP (amount dương), tự lên cấp khi đủ. Trả về số EXP thực sự nhận (0 ở cấp tối đa hoặc amount không dương).</summary>
    public int AddExp(int amount)
    {
        if (amount <= 0 || level.IsMaxLevel) return 0;

        adding = true;
        long total = (long)currentExp + amount;
        int levelsGained = 0;

        while (!level.IsMaxLevel)
        {
            int required = ExpToNext;
            if (total < required) break;

            total -= required;
            if (!level.TryLevelUp()) break;
            levelsGained++;
        }

        currentExp = level.IsMaxLevel ? 0 : (int)Math.Min(total, int.MaxValue);
        adding = false;

        if (levelsGained > 0) Debug.Log($"[{nameof(Player_Experience)}] Lên cấp {level.Level} (+{levelsGained}).", this);

        RefreshView();
        OnExpGained?.Invoke(amount);
        OnExpChanged?.Invoke();
        return amount;
    }

    /// <summary>Đặt EXP hiện có (kẹp trong [0, EXP cần - 1]), KHÔNG lên cấp. Dùng khi nạp save.</summary>
    public void SetExp(int exp)
    {
        currentExp = Mathf.Max(0, exp);
        ClampExp();
        RefreshView();
        OnExpChanged?.Invoke();
    }

    private void ClampExp()
    {
        int toNext = ExpToNext;
        currentExp = toNext <= 0 ? 0 : Mathf.Clamp(currentExp, 0, toNext - 1);
    }

    private void RefreshView()
    {
        if (expText != null)
        {
            int toNext = ExpToNext;
            expText.text = string.Format(expTextFormat, level.Level, currentExp, toNext > 0 ? toNext.ToString() : "MAX");
        }

        if (expFill != null) expFill.fillAmount = Progress;
    }

    [ContextMenu("Debug: Thêm EXP bằng đúng số cần để lên 1 cấp")]
    private void DebugAddOneLevel()
    {
        AddExp(Mathf.Max(1, ExpToNext - currentExp));
    }

    [ContextMenu("Debug: Thêm 100 EXP")]
    private void DebugAdd100()
    {
        AddExp(100);
    }
}
