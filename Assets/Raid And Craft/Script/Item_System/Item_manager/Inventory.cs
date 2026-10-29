using System;
using UnityEngine;

/// <summary>
/// Inventory của nhân vật: kho dạng LƯỚI kiểu PoE (mỗi vật phẩm chiếm Grid Size ô, xoay được, xếp chồng tới Max Stack).
/// Toàn bộ logic nằm trong Grid_Item_Container / Item_Grid; lớp này chỉ là "kho chính của Player".
/// Kho phụ (Stash...) làm bằng cách kế thừa Grid_Item_Container giống lớp này.
/// Chưa có UI, chưa lưu vào Save_Data. Gắn lên PlayerPrefab.
/// </summary>
[DisallowMultipleComponent]
public class Inventory : Grid_Item_Container
{
    /// <summary>Tên cũ của OnContainerChanged, giữ lại để code UI/hệ thống đã viết không phải sửa.</summary>
    public event Action OnInventoryChanged
    {
        add { OnContainerChanged += value; }
        remove { OnContainerChanged -= value; }
    }
}
