using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cổng chuyển giữa các Scene LIỀN KỀ (ví dụ thị trấn -> khu săn): click chuột trái, Player đi tới rồi sang Scene đích.
/// Có KHE ĐIỀU KIỆN: kéo các asset Travel_Condition vào Conditions, mọi điều kiện đạt thì cổng mới mở
/// (ví dụ sau này "đã giết boss X"). Danh sách trống = luôn mở.
/// Cách tạo: object trong Scene (kéo model làm con), Add Component Travel_Gate, điền Target Scene (đúng tên Scene, nằm trong Build Settings)
/// và Target Spawn Id (tên Scene_Spawn_Point ở Scene đích; trống = điểm mặc định). Chuyển Scene luôn lưu game trước (Scene_Transition).
/// </summary>
[DisallowMultipleComponent]
public class Travel_Gate : Travel_Point_Base
{
    [Header("Đích")]
    [Tooltip("Tên Scene đích (phải có trong Build Settings).")]
    [SerializeField] private string targetScene;

    [Tooltip("Id của Scene_Spawn_Point ở Scene đích. Trống hoặc không tìm thấy = điểm mặc định (Is Default).")]
    [SerializeField] private string targetSpawnId = "";

    [Header("Điều kiện mở cổng (tuỳ chọn)")]
    [Tooltip("Mọi điều kiện phải đạt mới đi qua được. Tạo asset: Create -> Raid And Craft -> Travel. Trống = luôn mở.")]
    [SerializeField] private List<Travel_Condition> conditions = new List<Travel_Condition>();

    [Tooltip("Câu báo khi cổng đóng mà điều kiện không nêu lý do.")]
    [SerializeField] private string lockedMessage = "Cổng này chưa mở.";

    public string TargetScene => targetScene;
    public string TargetSpawnId => targetSpawnId;

    /// <summary>Cổng đang mở với người này không. reason: lý do khi đóng.</summary>
    public bool IsOpen(GameObject traveler, out string reason)
    {
        if (Scene_Transition.CheckConditions(conditions, traveler, out reason)) return true;

        if (string.IsNullOrEmpty(reason)) reason = lockedMessage;
        return false;
    }

    protected override void OnInteract(World_Item_Picker interactor)
    {
        if (!IsOpen(interactor.gameObject, out string reason))
        {
            Scene_Transition.ReportMessage(reason);
            return;
        }

        Scene_Transition.Travel(targetScene, targetSpawnId);
    }

    protected override Color LabelColor => new Color(1f, 0.82f, 0.5f);

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(targetScene)) Debug.LogWarning($"[{nameof(Travel_Gate)}] '{name}': chưa điền Target Scene.", this);
    }
}
