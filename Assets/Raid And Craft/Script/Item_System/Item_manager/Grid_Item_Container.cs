using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Vật phẩm cho sẵn lúc bắt đầu (để thử, chỉnh trong Inspector).</summary>
[Serializable]
public struct Inventory_Starting_Item
{
    public Item_Data item;
    [Min(1)] public int count;
}

/// <summary>
/// Lớp gốc của mọi kho dạng LƯỚI gắn lên object (Inventory của Player, sau này Stash...).
/// Bọc một Item_Grid: thêm sự kiện, vật phẩm khởi đầu và các hàm cho UI/hệ thống khác.
/// Vật phẩm xếp chồng lưu loại + số lượng; trang bị lưu từng Item_Instance riêng (xem IItem_Container).
/// Trang bị cho sẵn ở Starting Items tự có một Item_Instance mặc định (Base Modifier sao chép từ asset).
/// Muốn có kho mới (Stash, rương): kế thừa lớp này (không cần viết gì thêm), gắn lên object, chỉnh Width/Height.
/// Chuyển vật phẩm giữa hai kho: Item_Transfer.TryMove(kho_a, kho_b, vật_phẩm, số_lượng).
/// Chưa lưu vào Save_Data (làm sau, tra vật phẩm qua Item_Database).
/// </summary>
public abstract class Grid_Item_Container : MonoBehaviour, IItem_Container
{
    /// <summary>Bắn khi nội dung hoặc kích thước kho đổi (một lần cho mỗi thao tác).</summary>
    public event Action OnContainerChanged;

    [Header("Kích thước lưới")]
    [Tooltip("Số ô chiều ngang. Có thể mở rộng khi đang chơi bằng TryResize.")]
    [SerializeField, Min(1)] private int width = 10;

    [Tooltip("Số ô chiều dọc.")]
    [SerializeField, Min(1)] private int height = 4;

    [Tooltip("Vật phẩm có sẵn lúc bắt đầu (để thử khi chưa có rơi đồ).")]
    [SerializeField] private List<Inventory_Starting_Item> startingItems = new List<Inventory_Starting_Item>();

    private Item_Grid grid;

    public int Width { get { EnsureInitialized(); return grid.Width; } }
    public int Height { get { EnsureInitialized(); return grid.Height; } }

    /// <summary>Số chỗ (chồng) đang có vật phẩm, để UI duyệt qua GetPlacement.</summary>
    public int PlacementCount { get { EnsureInitialized(); return grid.PlacementCount; } }

    protected virtual void Awake()
    {
        EnsureInitialized();
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của kho vẫn an toàn.
    private void EnsureInitialized()
    {
        if (grid != null) return;

        grid = new Item_Grid(width, height);

        foreach (Inventory_Starting_Item s in startingItems)
        {
            if (s.item == null) continue;

            int leftover = grid.Add(s.item, Mathf.Max(1, s.count));
            if (leftover > 0) Debug.LogWarning($"[{GetType().Name}] Hết chỗ cho vật phẩm khởi tạo {s.item.DisplayName} (dư {leftover}).", this);
        }
    }

    private void NotifyChanged()
    {
        OnContainerChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // ĐỌC (cho UI)
    // ------------------------------------------------------------------

    public Grid_Placement GetPlacement(int index)
    {
        EnsureInitialized();
        return grid.GetPlacement(index);
    }

    /// <summary>Chồng đang chiếm ô cell (false nếu ô trống hoặc ngoài lưới).</summary>
    public bool TryGetPlacementAt(Vector2Int cell, out Grid_Placement placement)
    {
        EnsureInitialized();
        return grid.TryGetPlacementAt(cell, out placement);
    }

    /// <summary>Đặt vật phẩm với góc trên-trái tại origin có vừa không (UI dùng để tô xanh/đỏ khi kéo).</summary>
    public bool CanFit(Item_Data item, Vector2Int origin)
    {
        EnsureInitialized();
        return grid.CanFit(item, origin);
    }

    // ------------------------------------------------------------------
    // IItem_Container
    // ------------------------------------------------------------------

    public int CountOf(Item_Data item)
    {
        EnsureInitialized();
        return grid.CountOf(item);
    }

    public bool Has(Item_Data item, int count = 1)
    {
        EnsureInitialized();
        return grid.Has(item, count);
    }

    public bool CanAdd(Item_Data item, int count = 1)
    {
        EnsureInitialized();
        return grid.CanAdd(item, count);
    }

    public bool CanAddAll(IReadOnlyList<Item_Data> toAdd)
    {
        EnsureInitialized();
        return grid.CanAddAll(toAdd);
    }

    public int Add(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return count;
        EnsureInitialized();

        int leftover = grid.Add(item, count);
        if (leftover < count) NotifyChanged();
        return leftover;
    }

    public bool TryRemove(Item_Data item, int count = 1)
    {
        EnsureInitialized();
        if (!grid.TryRemove(item, count)) return false;

        NotifyChanged();
        return true;
    }

    public bool HasInstance(Item_Instance instance)
    {
        EnsureInitialized();
        return grid.HasInstance(instance);
    }

    public bool CanAddInstance(Item_Instance instance)
    {
        EnsureInitialized();
        return grid.CanAddInstance(instance);
    }

    public bool AddInstance(Item_Instance instance)
    {
        EnsureInitialized();
        if (!grid.AddInstance(instance)) return false;

        NotifyChanged();
        return true;
    }

    public bool TryRemoveInstance(Item_Instance instance)
    {
        EnsureInitialized();
        if (!grid.TryRemoveInstance(instance)) return false;

        NotifyChanged();
        return true;
    }

    public bool CanReplaceInstance(Item_Instance take, Item_Instance give)
    {
        EnsureInitialized();
        return grid.CanReplaceInstance(take, give);
    }

    public bool TryReplaceInstance(Item_Instance take, Item_Instance give)
    {
        EnsureInitialized();
        if (!grid.TryReplaceInstance(take, give)) return false;

        NotifyChanged();
        return true;
    }

    // ------------------------------------------------------------------
    // THEO VỊ TRÍ (UI kéo thả)
    // ------------------------------------------------------------------

    /// <summary>Đặt một chồng vật phẩm đúng vị trí (ví dụ thả từ con trỏ xuống lưới).</summary>
    public bool TryPlaceAt(Item_Data item, int count, Vector2Int origin)
    {
        EnsureInitialized();
        if (!grid.TryPlaceAt(item, count, origin)) return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Đặt lại một chồng đã nhấc lên (Grid_Placement từ TryRemoveAt), giữ nguyên món trang bị. UI dùng để thả item đang cầm.</summary>
    public bool TryPlace(Grid_Placement placement, Vector2Int origin)
    {
        EnsureInitialized();
        if (!grid.TryPlace(placement, origin)) return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Dời chồng đang chiếm ô fromCell tới góc trên-trái toOrigin. Chỉ dời vào chỗ trống.</summary>
    public bool TryMove(Vector2Int fromCell, Vector2Int toOrigin)
    {
        EnsureInitialized();
        if (!grid.TryMove(fromCell, toOrigin)) return false;

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// THẢ một chồng/món đang cầm trên con trỏ (held, lấy từ TryRemoveAt hoặc từ ô trang bị) xuống lưới, góc trên-trái tại origin.
    /// Vùng thả trống: đặt vào. Chạm đúng một chồng cùng loại còn chỗ: gộp tới Max Stack, phần dư ở lại con trỏ.
    /// Chạm đúng một chồng khác: đổi chỗ, chồng bị đè lên con trỏ. Chạm từ hai chồng trở lên hoặc vượt ngoài lưới: không làm gì (false).
    /// leftover = thứ còn trên con trỏ sau khi thả (IsEmpty = con trỏ trống).
    /// </summary>
    public bool TryDrop(Grid_Placement held, Vector2Int origin, out Grid_Placement leftover)
    {
        EnsureInitialized();
        if (!grid.TryDrop(held, origin, out leftover)) return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Nhấc nguyên chồng đang chiếm ô cell ra khỏi kho lên con trỏ. Thả bằng TryDrop (UI không cho tương tác khác khi đang cầm, kẻo mất).</summary>
    public bool TryRemoveAt(Vector2Int cell, out Grid_Placement removed)
    {
        EnsureInitialized();
        if (!grid.TryRemoveAt(cell, out removed)) return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Đổi kích thước lưới (mở rộng kho). Thu nhỏ chỉ được khi mọi vật phẩm vẫn nằm trọn trong lưới mới.</summary>
    public bool TryResize(int newWidth, int newHeight)
    {
        EnsureInitialized();
        if (!grid.TryResize(newWidth, newHeight)) return false;

        NotifyChanged();
        return true;
    }
}
