using UnityEngine;

/// <summary>
/// Cổng Map đặt ở BASE: click chuột trái, Player đi tới rồi mở canvas World Map ở chế độ CHỌN ĐIỂM ĐẾN (bấm node để đi).
/// Thay Travel_Gate (cổng một đích) và Fast_Travel_Point cũ. Cần World_Map_UI trong Scene (xem world_map_guide).
/// Tên hiển thị/thẻ tên chỉnh ở ô Display Name của Travel_Point_Base.
/// </summary>
[DisallowMultipleComponent]
public class Map_Gate : Travel_Point_Base
{
    protected override void OnInteract(World_Item_Picker interactor)
    {
        if (World_Map_UI.Instance == null)
        {
            Debug.LogError($"[{nameof(Map_Gate)}] Scene chưa có World_Map_UI (đặt script lên object luôn bật trong Canvas).", this);
            return;
        }

        World_Map_UI.Instance.Open(true);
    }

    protected override Color LabelColor => new Color(1f, 0.82f, 0.5f);
}
