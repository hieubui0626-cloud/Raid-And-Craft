using UnityEngine;

/// <summary>
/// Cổng ở INSTANCE. KHOÁ cho tới khi Instance hiện tại Complete (hạ boss cuối, xem Map_Boss_Objective): click khi khoá chỉ báo
/// "chưa mở". Mở rồi: click chuột trái, Player đi tới rồi mở canvas World Map ở chế độ CHỌN ĐIỂM ĐẾN (giống Map_Gate ở Base).
/// Đi được tới Base đã Unlocked, hoặc Instance Unlocked/Complete nối trực tiếp với Instance hiện tại. Cần World_Map_UI trong Scene.
/// Hoàn thành Instance không còn tự về Base: người chơi tự bấm cổng này. Chết vẫn về Base như cũ.
/// Scene không thuộc World_Map_Layout hoặc không phải Instance thì cổng không bao giờ khoá.
/// </summary>
[DisallowMultipleComponent]
public class Map_Exit_Gate : Travel_Point_Base
{
    private static readonly Color LockedColor = new Color(0.65f, 0.65f, 0.65f);

    private Player_Map_Progress progress;
    private bool? shownOpen;

    protected override Color LabelColor => new Color(0.6f, 0.85f, 1f);

    protected override void OnInteract(World_Item_Picker interactor)
    {
        var playerProgress = interactor.GetComponent<Player_Map_Progress>();
        if (playerProgress != null && !playerProgress.IsExitOpen)
        {
            Scene_Transition.ReportMessage("The exit gate is locked: defeat the final boss to open it.");
            return;
        }

        if (World_Map_UI.Instance == null)
        {
            Debug.LogError($"[{nameof(Map_Exit_Gate)}] Scene chưa có World_Map_UI (đặt script lên object luôn bật trong Canvas).", this);
            return;
        }

        World_Map_UI.Instance.Open(true);
    }

    // Thẻ tên đổi theo trạng thái khoá/mở (Player có thể xuất hiện sau cổng nên tìm lại cho tới khi thấy).
    private void Update()
    {
        if (progress == null) progress = FindFirstObjectByType<Player_Map_Progress>();
        if (progress == null) return;

        bool open = progress.IsExitOpen;
        if (shownOpen == open) return;

        shownOpen = open;
        SetLabel(open ? DisplayName : DisplayName + " (Locked)", open ? LabelColor : LockedColor);
    }
}
