using System;
using System.Collections.Generic;

/// <summary>
/// Giao diện chung của mọi "kho chứa vật phẩm" (Inventory, sau này Stash...).
/// Các tính năng chỉ cần kho (mặc trang bị, lắp Magic Circuit, dùng thuốc) nên làm việc qua giao diện này,
/// không cần biết kho bên trong là danh sách ô hay lưới.
///
/// Hai kiểu vật phẩm trong kho:
///   - Xếp chồng (nguyên liệu, thuốc, Rune...): chỉ có loại + số lượng, dùng các hàm theo Item_Data.
///   - Trang bị (Item_Data.UsesInstance): mỗi món là một Item_Instance riêng, dùng các hàm *Instance.
///     Các hàm theo Item_Data với loại trang bị vẫn chạy: Add tự tạo instance mặc định, CountOf đếm theo loại,
///     TryRemove gỡ món cuối cùng của loại đó (muốn gỡ đúng một món cụ thể thì dùng TryRemoveInstance).
/// </summary>
public interface IItem_Container
{
    /// <summary>Bắn khi nội dung kho đổi (một lần cho mỗi thao tác).</summary>
    event Action OnContainerChanged;

    // ---- Theo loại (Item_Data) ----

    /// <summary>Tổng số lượng một loại vật phẩm trong kho (trang bị: số món thuộc loại đó).</summary>
    int CountOf(Item_Data item);

    bool Has(Item_Data item, int count = 1);

    /// <summary>Nhét vừa tất cả count vật phẩm này không (không thay đổi gì).</summary>
    bool CanAdd(Item_Data item, int count = 1);

    /// <summary>Nhét vừa mỗi vật phẩm trong danh sách (mỗi cái 1 đơn vị) cùng lúc không.</summary>
    bool CanAddAll(IReadOnlyList<Item_Data> toAdd);

    /// <summary>Thêm vật phẩm. Trả về số lượng KHÔNG nhét được (0 = vào hết).</summary>
    int Add(Item_Data item, int count = 1);

    /// <summary>Lấy đi count vật phẩm. Trả về false (không lấy gì) nếu không đủ.</summary>
    bool TryRemove(Item_Data item, int count = 1);

    // ---- Theo món cụ thể (Item_Instance) ----

    /// <summary>Có đúng món này (so sánh tham chiếu) trong kho không.</summary>
    bool HasInstance(Item_Instance instance);

    bool CanAddInstance(Item_Instance instance);

    /// <summary>Thêm đúng món này vào kho. False nếu hết chỗ.</summary>
    bool AddInstance(Item_Instance instance);

    bool TryRemoveInstance(Item_Instance instance);

    /// <summary>
    /// Lấy món "take" ra rồi nhét món "give" vào có được không (không thay đổi gì).
    /// Khác với CanAddInstance(give) sau TryRemoveInstance(take): chỗ vừa trống của "take" được tính, nên đổi đồ khi kho đầy vẫn được.
    /// </summary>
    bool CanReplaceInstance(Item_Instance take, Item_Instance give);

    /// <summary>Đổi món "take" lấy món "give" (nguyên tử: hoặc xong cả hai, hoặc không đổi gì).</summary>
    bool TryReplaceInstance(Item_Instance take, Item_Instance give);
}

/// <summary>Tiện ích chuyển vật phẩm giữa hai kho (ví dụ Inventory và Stash).</summary>
public static class Item_Transfer
{
    /// <summary>
    /// Chuyển count vật phẩm xếp chồng từ kho này sang kho kia. Hoặc chuyển đủ, hoặc không đổi gì
    /// (false nếu nguồn thiếu hoặc đích hết chỗ). Với trang bị hãy dùng TryMoveInstance để giữ nguyên món.
    /// </summary>
    public static bool TryMove(IItem_Container from, IItem_Container to, Item_Data item, int count = 1)
    {
        if (from == null || to == null || item == null || count <= 0 || ReferenceEquals(from, to)) return false;
        if (item.UsesInstance) return false;
        if (!from.Has(item, count) || !to.CanAdd(item, count)) return false;

        if (!from.TryRemove(item, count)) return false;
        to.Add(item, count);
        return true;
    }

    /// <summary>Chuyển đúng một món trang bị (giữ nguyên chỉ số roll) giữa hai kho.</summary>
    public static bool TryMoveInstance(IItem_Container from, IItem_Container to, Item_Instance instance)
    {
        if (from == null || to == null || instance == null || ReferenceEquals(from, to)) return false;
        if (!from.HasInstance(instance) || !to.CanAddInstance(instance)) return false;

        if (!from.TryRemoveInstance(instance)) return false;
        return to.AddInstance(instance);
    }
}
