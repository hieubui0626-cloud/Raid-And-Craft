using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một vật phẩm (hoặc một chồng vật phẩm) đang nằm trong lưới. Chỉ để đọc, dữ liệu thật nằm trong Item_Grid.
/// Toạ độ: gốc (0,0) là ô TRÊN-TRÁI, x tăng sang phải, y tăng xuống dưới (khớp cách UI xếp ô).
/// Origin là ô trên-trái của vùng vật phẩm chiếm; Size đã tính xoay.
/// </summary>
public readonly struct Grid_Placement
{
    public readonly Item_Data Item;

    /// <summary>Món cụ thể nếu là trang bị (Item.UsesInstance); null với vật phẩm xếp chồng.</summary>
    public readonly Item_Instance Instance;

    public readonly int Count;
    public readonly Vector2Int Origin;
    public readonly bool Rotated;

    public Grid_Placement(Item_Data item, Item_Instance instance, int count, Vector2Int origin, bool rotated)
    {
        Item = item;
        Instance = instance;
        Count = count;
        Origin = origin;
        Rotated = rotated;
    }

    public bool IsEmpty => Item == null || Count <= 0;

    /// <summary>Số ô rộng x cao đang chiếm (đã tính xoay).</summary>
    public Vector2Int Size => Item == null ? Vector2Int.zero : Item.GetGridSize(Rotated);

    public bool Contains(Vector2Int cell)
    {
        Vector2Int size = Size;
        return cell.x >= Origin.x && cell.x < Origin.x + size.x
            && cell.y >= Origin.y && cell.y < Origin.y + size.y;
    }
}

/// <summary>
/// Lưới vật phẩm kiểu PoE: mỗi vật phẩm chiếm GridSize ô (rộng x cao), có thể xoay 90 độ nếu CanRotate.
/// Vật phẩm Stackable xếp chồng tới MaxStack trong MỘT chỗ (một chỗ chiếm đúng GridSize ô, không tính theo số lượng).
/// Đây là lớp thuần (không phải MonoBehaviour) nên Inventory, Stash... đều dùng lại được; Grid_Item_Container bọc nó.
/// Chỉ lo luật đặt/gỡ/di chuyển; không có sự kiện (container bắn sự kiện khi gọi hàm thành công).
/// </summary>
public class Item_Grid
{
    private struct Entry
    {
        public Item_Data item;
        public Item_Instance instance; // chỉ có với trang bị (count luôn 1)
        public int count;
        public Vector2Int origin;
        public bool rotated;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private int width;
    private int height;
    private int[] cells; // mỗi ô: chỉ số entry đang chiếm, -1 = trống

    public Item_Grid(int width, int height)
    {
        this.width = Mathf.Max(1, width);
        this.height = Mathf.Max(1, height);
        RebuildCells();
    }

    public int Width => width;
    public int Height => height;

    /// <summary>Số chỗ (chồng) đang có vật phẩm.</summary>
    public int PlacementCount => entries.Count;

    public Grid_Placement GetPlacement(int index)
    {
        if (index < 0 || index >= entries.Count) return default;

        Entry e = entries[index];
        return new Grid_Placement(e.item, e.instance, e.count, e.origin, e.rotated);
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    /// <summary>Chỉ số chỗ đang chiếm ô này (-1 = ô trống hoặc ngoài lưới).</summary>
    public int IndexAt(Vector2Int cell)
    {
        return InBounds(cell) ? cells[cell.y * width + cell.x] : -1;
    }

    public bool TryGetPlacementAt(Vector2Int cell, out Grid_Placement placement)
    {
        int index = IndexAt(cell);
        placement = GetPlacement(index);
        return index >= 0;
    }

    // ------------------------------------------------------------------
    // TRUY VẤN
    // ------------------------------------------------------------------

    public int CountOf(Item_Data item)
    {
        if (item == null) return 0;

        int total = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].item == item) total += entries[i].count;
        }
        return total;
    }

    public bool Has(Item_Data item, int count = 1)
    {
        return CountOf(item) >= count;
    }

    /// <summary>Có đúng món (instance) này trong lưới không (so sánh tham chiếu).</summary>
    public bool HasInstance(Item_Instance instance)
    {
        return IndexOfInstance(instance) >= 0;
    }

    /// <summary>Đặt vật phẩm với góc trên-trái tại origin có vừa (trong lưới, không đè vật khác) không.</summary>
    public bool CanFit(Item_Data item, Vector2Int origin, bool rotated)
    {
        return CanFit(item, origin, rotated, -1);
    }

    // ignoreIndex: coi chỗ này như trống (dùng khi di chuyển chính vật phẩm đó).
    private bool CanFit(Item_Data item, Vector2Int origin, bool rotated, int ignoreIndex)
    {
        if (item == null) return false;

        Vector2Int size = item.GetGridSize(rotated);
        if (origin.x < 0 || origin.y < 0 || origin.x + size.x > width || origin.y + size.y > height) return false;

        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                int occupant = cells[(origin.y + dy) * width + origin.x + dx];
                if (occupant != -1 && occupant != ignoreIndex) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Tìm chỗ trống đầu tiên (duyệt từ trên xuống, trái sang phải; thử không xoay trước rồi mới xoay).
    /// </summary>
    public bool TryFindSpace(Item_Data item, out Vector2Int origin, out bool rotated)
    {
        origin = default;
        rotated = false;
        if (item == null) return false;

        bool canRotate = item.CanRotate;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var o = new Vector2Int(x, y);
                if (CanFit(item, o, false, -1))
                {
                    origin = o;
                    return true;
                }
                if (canRotate && CanFit(item, o, true, -1))
                {
                    origin = o;
                    rotated = true;
                    return true;
                }
            }
        }
        return false;
    }

    // ------------------------------------------------------------------
    // THÊM / GỠ TỰ ĐỘNG (không cần biết vị trí)
    // ------------------------------------------------------------------

    /// <summary>
    /// Thêm vật phẩm: gộp vào chồng còn chỗ trước, rồi mở chỗ mới. Trả về số lượng KHÔNG nhét được.
    /// Với loại trang bị (UsesInstance), mỗi cái tự tạo một Item_Instance mặc định (Item_Instance.Create).
    /// </summary>
    public int Add(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return count;

        if (item.UsesInstance)
        {
            int left = count;
            while (left > 0 && AddInstance(Item_Instance.Create(item))) left--;
            return left;
        }

        int max = item.MaxStack;
        int amount = count;

        if (item.CanStack)
        {
            for (int i = 0; i < entries.Count && amount > 0; i++)
            {
                Entry e = entries[i];
                if (e.item != item || e.count >= max) continue;

                int add = Mathf.Min(max - e.count, amount);
                e.count += add;
                entries[i] = e;
                amount -= add;
            }
        }

        while (amount > 0)
        {
            if (!TryFindSpace(item, out Vector2Int origin, out bool rotated)) break;

            int put = Mathf.Min(max, amount);
            AddEntry(item, null, put, origin, rotated);
            amount -= put;
        }

        return amount;
    }

    /// <summary>Thêm đúng món (instance) này vào chỗ trống đầu tiên. False nếu hết chỗ hoặc món đã có trong lưới.</summary>
    public bool AddInstance(Item_Instance instance)
    {
        if (instance == null || HasInstance(instance)) return false;
        if (!TryFindSpace(instance.Base, out Vector2Int origin, out bool rotated)) return false;

        AddEntry(instance.Base, instance, 1, origin, rotated);
        return true;
    }

    public bool CanAddInstance(Item_Instance instance)
    {
        if (instance == null || HasInstance(instance)) return false;
        return TryFindSpace(instance.Base, out _, out _);
    }

    public bool CanAdd(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        return Clone().Add(item, count) == 0;
    }

    public bool CanAddAll(IReadOnlyList<Item_Data> toAdd)
    {
        Item_Grid sim = Clone();
        foreach (Item_Data item in toAdd)
        {
            if (item == null) continue;
            if (sim.Add(item, 1) > 0) return false;
        }
        return true;
    }

    /// <summary>Lấy đi count vật phẩm (từ các chồng cuối lưới trước). False và không lấy gì nếu không đủ.</summary>
    public bool TryRemove(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0 || CountOf(item) < count) return false;

        int remaining = count;
        bool removedEntry = false;

        for (int i = entries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            Entry e = entries[i];
            if (e.item != item) continue;

            int take = Mathf.Min(e.count, remaining);
            remaining -= take;

            if (take >= e.count)
            {
                entries.RemoveAt(i);
                removedEntry = true;
            }
            else
            {
                e.count -= take;
                entries[i] = e;
            }
        }

        if (removedEntry) RebuildCells();
        return true;
    }

    // ------------------------------------------------------------------
    // ĐỔI CHỖ (đổi đồ khi kho đầy)
    // ------------------------------------------------------------------

    /// <summary>Gỡ đúng món (instance) này khỏi lưới. False nếu không có.</summary>
    public bool TryRemoveInstance(Item_Instance instance)
    {
        int index = IndexOfInstance(instance);
        if (index < 0) return false;

        entries.RemoveAt(index);
        RebuildCells();
        return true;
    }

    /// <summary>Lấy món "take" ra rồi nhét món "give" vào có được không (tính cả chỗ "take" vừa trống).</summary>
    public bool CanReplaceInstance(Item_Instance take, Item_Instance give)
    {
        if (take == null || give == null || take == give) return false;

        Item_Grid sim = Clone();
        return sim.TryRemoveInstance(take) && sim.AddInstance(give);
    }

    /// <summary>Đổi món "take" lấy món "give". "give" ưu tiên vào đúng chỗ "take" vừa trống. Hoặc xong cả hai, hoặc không đổi gì.</summary>
    public bool TryReplaceInstance(Item_Instance take, Item_Instance give)
    {
        if (!CanReplaceInstance(take, give)) return false;

        Entry old = entries[IndexOfInstance(take)];

        TryRemoveInstance(take);
        if (TryPlaceInstanceAt(give, old.origin, old.rotated)) return true;

        return AddInstance(give);
    }

    // ------------------------------------------------------------------
    // ĐẶT / DI CHUYỂN THEO VỊ TRÍ (cho UI kéo thả)
    // ------------------------------------------------------------------

    /// <summary>
    /// Đặt một chồng count vật phẩm đúng vị trí (không gộp). False nếu không vừa hoặc count vượt MaxStack.
    /// Loại trang bị (UsesInstance): chỉ count = 1, tự tạo instance mặc định; muốn giữ món cũ dùng TryPlaceInstanceAt/TryPlace.
    /// </summary>
    public bool TryPlaceAt(Item_Data item, int count, Vector2Int origin, bool rotated)
    {
        if (item == null || count <= 0 || count > item.MaxStack) return false;
        if (item.UsesInstance) return count == 1 && TryPlaceInstanceAt(Item_Instance.Create(item), origin, rotated);

        rotated = rotated && item.CanRotate;
        if (!CanFit(item, origin, rotated, -1)) return false;

        AddEntry(item, null, count, origin, rotated);
        return true;
    }

    /// <summary>Đặt đúng món (instance) vào vị trí. False nếu không vừa hoặc món đã có trong lưới.</summary>
    public bool TryPlaceInstanceAt(Item_Instance instance, Vector2Int origin, bool rotated)
    {
        if (instance == null || HasInstance(instance)) return false;

        rotated = rotated && instance.Base.CanRotate;
        if (!CanFit(instance.Base, origin, rotated, -1)) return false;

        AddEntry(instance.Base, instance, 1, origin, rotated);
        return true;
    }

    /// <summary>Đặt lại một chồng đã nhấc lên (Grid_Placement từ TryRemoveAt), giữ nguyên instance nếu là trang bị.</summary>
    public bool TryPlace(Grid_Placement placement, Vector2Int origin, bool rotated)
    {
        if (placement.IsEmpty) return false;

        return placement.Instance != null
            ? TryPlaceInstanceAt(placement.Instance, origin, rotated)
            : TryPlaceAt(placement.Item, placement.Count, origin, rotated);
    }

    /// <summary>Dời chồng đang chiếm ô fromCell tới góc trên-trái toOrigin (có thể xoay). Chỉ dời vào chỗ trống.</summary>
    public bool TryMove(Vector2Int fromCell, Vector2Int toOrigin, bool rotated)
    {
        int index = IndexAt(fromCell);
        if (index < 0) return false;

        Entry e = entries[index];
        rotated = rotated && e.item.CanRotate;
        if (!CanFit(e.item, toOrigin, rotated, index)) return false;

        e.origin = toOrigin;
        e.rotated = rotated;
        entries[index] = e;
        RebuildCells();
        return true;
    }

    /// <summary>Xoay chồng đang chiếm ô cell 90 độ, giữ nguyên góc trên-trái. False nếu không xoay được hoặc vướng.</summary>
    public bool TryRotate(Vector2Int cell)
    {
        int index = IndexAt(cell);
        if (index < 0) return false;

        Entry e = entries[index];
        if (!e.item.CanRotate) return false;

        return TryMove(cell, e.origin, !e.rotated);
    }

    /// <summary>Gỡ nguyên chồng đang chiếm ô cell (nhấc lên). Trả về chồng đó.</summary>
    public bool TryRemoveAt(Vector2Int cell, out Grid_Placement removed)
    {
        int index = IndexAt(cell);
        removed = GetPlacement(index);
        if (index < 0) return false;

        entries.RemoveAt(index);
        RebuildCells();
        return true;
    }

    // ------------------------------------------------------------------
    // ĐỔI KÍCH THƯỚC (mở rộng kho)
    // ------------------------------------------------------------------

    /// <summary>
    /// Đổi kích thước lưới. Mở rộng luôn được; thu nhỏ chỉ khi mọi vật phẩm vẫn nằm trọn trong lưới mới.
    /// Vị trí vật phẩm không đổi.
    /// </summary>
    public bool TryResize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        foreach (Entry e in entries)
        {
            Vector2Int size = e.item.GetGridSize(e.rotated);
            if (e.origin.x + size.x > newWidth || e.origin.y + size.y > newHeight) return false;
        }

        width = newWidth;
        height = newHeight;
        RebuildCells();
        return true;
    }

    public void Clear()
    {
        entries.Clear();
        RebuildCells();
    }

    public Item_Grid Clone()
    {
        var copy = new Item_Grid(width, height);
        copy.entries.AddRange(entries);
        copy.RebuildCells();
        return copy;
    }

    // ------------------------------------------------------------------
    // NỘI BỘ
    // ------------------------------------------------------------------

    private int IndexOfInstance(Item_Instance instance)
    {
        if (instance == null) return -1;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].instance == instance) return i;
        }
        return -1;
    }

    private void AddEntry(Item_Data item, Item_Instance instance, int count, Vector2Int origin, bool rotated)
    {
        entries.Add(new Entry { item = item, instance = instance, count = count, origin = origin, rotated = rotated });
        Paint(entries.Count - 1);
    }

    private void Paint(int index)
    {
        Entry e = entries[index];
        Vector2Int size = e.item.GetGridSize(e.rotated);

        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                cells[(e.origin.y + dy) * width + e.origin.x + dx] = index;
            }
        }
    }

    private void RebuildCells()
    {
        cells = new int[width * height];
        for (int i = 0; i < cells.Length; i++) cells[i] = -1;

        for (int i = 0; i < entries.Count; i++) Paint(i);
    }
}
