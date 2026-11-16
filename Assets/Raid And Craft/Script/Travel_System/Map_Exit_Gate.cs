using UnityEngine;

/// <summary>
/// Cổng thoát đặt trong INSTANCE: click chuột trái, Player đi tới rồi về Base gần nhất (không cần hoàn thành Instance).
/// Base gần nhất = Base Player đã vào Instance từ đó (hoặc Base vừa mở nếu Instance đã Complete). Chưa Complete thì tiến trình Instance không đổi.
/// </summary>
[DisallowMultipleComponent]
public class Map_Exit_Gate : Travel_Point_Base
{
    protected override void OnInteract(World_Item_Picker interactor)
    {
        var progress = interactor.GetComponent<Player_Map_Progress>();
        if (progress == null)
        {
            Debug.LogError($"[{nameof(Map_Exit_Gate)}] Player chưa có Player_Map_Progress.", this);
            return;
        }

        if (!progress.TryReturnToBase(out string reason)) Scene_Transition.ReportMessage(reason);
    }

    protected override Color LabelColor => new Color(0.6f, 0.85f, 1f);
}
