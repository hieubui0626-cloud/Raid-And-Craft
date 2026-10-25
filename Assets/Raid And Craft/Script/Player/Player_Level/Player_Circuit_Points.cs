using System;
using UnityEngine;

/// <summary>
/// Điểm Magic Circuit của Player: cầu nối giữa CẤP ĐỘ và Magic Circuit (cây kỹ năng gem), làm sẵn trước khi có Magic Circuit.
/// - Tổng điểm = Level_Config.startingCircuitPoints + circuitPointsPerLevel x (cấp - 1): tăng cấp là có thêm điểm.
/// - Tổng điểm tối đa = tổng điểm ở cấp tối đa (MaxTotalPoints): cấp tối đa quyết định Magic Circuit lớn cỡ nào.
/// - Đã tiêu / còn lại: Magic Circuit gọi TrySpend khi mở khoá/nâng một nút, Refund khi gỡ, ResetSpent khi respec.
/// - Điểm còn lại âm (IsOverspent) có thể xảy ra nếu cấp tụt xuống (trần cấp bị hạ): Magic Circuit tự quyết cách xử lý.
/// Gắn lên PlayerPrefab cùng Character_Level. Số điểm đã tiêu CHƯA được lưu vào Save_Data (lưu cùng Magic Circuit).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Level))]
public class Player_Circuit_Points : MonoBehaviour
{
    /// <summary>Bắn khi tổng/đã tiêu/còn lại đổi (đổi cấp, đổi trần cấp, tiêu, hoàn, respec).</summary>
    public event Action OnPointsChanged;

    private Character_Level level;
    private int spent;

    /// <summary>Tổng điểm có được ở cấp hiện tại.</summary>
    public int TotalPoints => level.Config.GetTotalCircuitPoints(level.Level);

    /// <summary>Tổng điểm sẽ có khi đạt cấp tối đa hiện tại.</summary>
    public int MaxTotalPoints => level.Config.GetTotalCircuitPoints(level.MaxLevel);

    public int SpentPoints => spent;

    /// <summary>Điểm còn dùng được (có thể âm khi IsOverspent).</summary>
    public int AvailablePoints => TotalPoints - spent;

    public bool IsOverspent => AvailablePoints < 0;

    private void Awake()
    {
        level = GetComponent<Character_Level>();
    }

    private void OnEnable()
    {
        level.OnLevelChanged += HandleLevelChanged;
        level.OnMaxLevelChanged += NotifyChanged;
    }

    private void OnDisable()
    {
        if (level == null) return;

        level.OnLevelChanged -= HandleLevelChanged;
        level.OnMaxLevelChanged -= NotifyChanged;
    }

    private void HandleLevelChanged(int oldLevel, int newLevel)
    {
        NotifyChanged();
    }

    /// <summary>Tiêu amount điểm. Trả về false (không tiêu) nếu amount không dương hoặc không đủ điểm còn lại.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > AvailablePoints) return false;

        spent += amount;
        NotifyChanged();
        return true;
    }

    /// <summary>Hoàn lại amount điểm đã tiêu (không âm quá 0 điểm đã tiêu).</summary>
    public void Refund(int amount)
    {
        if (amount <= 0) return;

        spent = Mathf.Max(0, spent - amount);
        NotifyChanged();
    }

    /// <summary>Hoàn lại toàn bộ điểm (respec).</summary>
    public void ResetSpent()
    {
        spent = 0;
        NotifyChanged();
    }

    /// <summary>Đặt số điểm đã tiêu (dùng khi load save của Magic Circuit).</summary>
    public void SetSpent(int value)
    {
        spent = Mathf.Max(0, value);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnPointsChanged?.Invoke();
    }
}
