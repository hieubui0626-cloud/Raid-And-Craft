using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một vật phẩm (hoặc một chồng vật phẩm) đang nằm trong lưới, hoặc đang được cầm trên con trỏ.
/// Chỉ để đọc, dữ liệu thật nằm trong Item_Grid.
/// Toạ độ: gốc (0,0) là ô TRÊN-TRÁI, x tăng sang phải, y tăng xuống dưới (khớp cách UI xếp ô).
/// Origin là ô trên-trái của vùng vật phẩm chiếm (vô nghĩa khi đang cầm trên con trỏ).
/// </summary>
public readonly struct Grid_Placement
{
    public readonly Item_Data Item;

    /// <summary>Món cụ thể nếu là trang bị (Item.UsesInstance); null với vật phẩm xếp chồng.</summary>
    public readonly Item_Instance Instance;

    public readonly int Count;
    public readonly Vector2Int Origin;

    public Grid_Placement(Item_Data item, Item_Instance instance, int count, Vector2Int origin)
    {
        Item = item;
        Instance = instance;
        Count = count;
        Origin = origin;
    }

    public bool IsEmpty => Item == null || Count <= 0;

    /// <summary>Số ô rộng x cao đang chiếm.</summary>
    public Vector2Int Size => Item == null ? Vector2Int.zero : Item.GridSize;

    public bool Contains(Vector2Int cell)
    {
        Vector2Int size = Size;
        return cell.x >= Origin.x && cell.x < Origin.x + size.x
            && cell.y >= Origin.y && cell.y < Origin.y + size.y;
    }
}

/// <summary>
/// Lưới vật phẩm kiểu PoE: mỗi vật phẩm chiếm GridSize ô (rộng x cao), không xoay.
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
        return new Grid_Placement(e.item, e.instance, e.count, e.origin);
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

    /// <summary>Vùng vật phẩm chiếm khi đặt góc trên-trái tại origin có nằm trọn trong lưới không (không xét vật khác).</summary>
    public bool FitsInBounds(Item_Data item, Vector2Int origin)
    {
        if (item == null) return false;

        Vector2Int size = item.GridSize;
        return origin.x >= 0 && origin.y >= 0 && origin.x + size.x <= width && origin.y + size.y <= height;
    }

    /// <summary>Đặt vật phẩm với góc trên-trái tại origin có vừa (trong lưới, không đè vật khác) không.</summary>
    public bool CanFit(Item_Data item, Vector2Int origin)
    {
        return CanFit(item, origin, -1);
    }

    // ignoreIndex: coi chỗ này như trống (dùng khi di chuyển chính vật phẩm đó).
    private bool CanFit(Item_Data item, Vector2Int origin, int ignoreIndex)
    {
        if (!FitsInBounds(item, origin)) return false;

        Vector2Int size = item.GridSize;
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

    /// <summary>Tìm chỗ trống đầu tiên (duyệt từ trên xuống, trái sang phải).</summary>
    public bool TryFindSpace(Item_Data item, out Vector2Int origin)
    {
        origin = default;
        if (item == null) return false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var o = new Vector2Int(x, y);
                if (CanFit(item, o, -1))
                {
                    origin = o;
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
            if (!TryFindSpace(item, out Vector2Int origin)) break;

            int put = Mathf.Min(max, amount);
            AddEntry(item, null, put, origin);
            amount -= put;
        }

        return amount;
    }

    /// <summary>Thêm đúng món (instance) này vào chỗ trống đầu tiên. False nếu hết chỗ hoặc món đã có trong lưới.</summary>
    public bool AddInstance(Item_Instance instance)
    {
        if (instance == null || HasInstance(instance)) return false;
        if (!TryFindSpace(instance.Base, out Vector2Int origin)) return false;

        AddEntry(instance.Base, instance, 1, origin);
        return true;
    }

    public bool CanAddInstance(Item_Instance instance)
    {
        if (instance == null || HasInstance(instance)) return false;
        return TryFindSpace(instance.Base, out _);
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
        if (TryPlaceInstanceAt(give, old.origin)) return true;

        return AddInstance(give);
    }

    // ------------------------------------------------------------------
    // ĐẶT / DI CHUYỂN / THẢ THEO VỊ TRÍ (cho UI kéo thả)
    // ------------------------------------------------------------------

    /// <summary>
    /// Đặt một chồng count vật phẩm đúng vị trí (không gộp). False nếu không vừa hoặc count vượt MaxStack.
    /// Loại trang bị (UsesInstance): chỉ count = 1, tự tạo instance mặc định; muốn giữ món cũ dùng TryPlaceInstanceAt/TryPlace.
    /// </summary>
    public bool TryPlaceAt(Item_Data item, int count, Vector2Int origin)
    {
        if (item == null || count <= 0 || count > item.MaxStack) return false;
        if (item.UsesInstance) return count == 1 && TryPlaceInstanceAt(Item_Instance.Create(item), origin);

        if (!CanFit(item, origin, -1)) return false;

        AddEntry(item, null, count, origin);
        return true;
    }

    /// <summary>Đặt đúng món (instance) vào vị trí. False nếu không vừa hoặc món đã có trong lưới.</summary>
    public bool TryPlaceInstanceAt(Item_Instance instance, Vector2Int origin)
    {
        if (instance == null || HasInstance(instance)) return false;
        if (!CanFit(instance.Base, origin, -1)) return false;

        AddEntry(instance.Base, instance, 1, origin);
        return true;
    }

    /// <summary>Đặt lại một chồng/món (Grid_Placement) vào vị trí trống, giữ nguyên instance nếu là trang bị.</summary>
    public bool TryPlace(Grid_Placement placement, Vector2Int origin)
    {
        if (placement.IsEmpty) return false;

        return placement.Instance != null
            ? TryPlaceInstanceAt(placement.Instance, origin)
            : TryPlaceAt(placement.Item, placement.Count, origin);
    }

    /// <summary>Dời chồng đang chiếm ô fromCell tới góc trên-trái toOrigin. Chỉ dời vào chỗ trống (kể cả chỗ chính nó đang chiếm).</summary>
    public bool TryMove(Vector2Int fromCell, Vector2Int toOrigin)
    {
        int index = IndexAt(fromCell);
        if (index < 0) return false;

        Entry e = entries[index];
        if (!CanFit(e.item, toOrigin, index)) return false;

        e.origin = toOrigin;
        entries[index] = e;
        RebuildCells();
        return true;
    }

    /// <summary>Gỡ nguyên chồng đang chiếm ô cell (nhấc lên con trỏ). Trả về chồng đó.</summary>
    public bool TryRemoveAt(Vector2Int cell, out Grid_Placement removed)
    {
        int index = IndexAt(cell);
        removed = GetPlacement(index);
        if (index < 0) return false;

        entries.RemoveAt(index);
        RebuildCells();
        return true;
    }

    /// <summary>
    /// THẢ một chồng/món đang cầm (held) xuống lưới, góc trên-trái tại origin. Luật:
    ///   - Vùng thả trống: đặt vào.
    ///   - Vùng thả chạm đúng MỘT chồng cùng loại xếp chồng được và còn chỗ: gộp tới Max Stack,
    ///     phần dư vẫn còn trên con trỏ (leftover).
    ///   - Vùng thả chạm đúng MỘT chồng khác (hoặc chồng cùng loại đã đầy): đổi chỗ, chồng bị đè được nhấc lên con trỏ (leftover).
    ///   - Chạm từ hai chồng trở lên, hoặc vùng thả vượt ngoài lưới: không làm gì (false), leftover = held.
    /// Trả về true nếu lưới có thay đổi. leftover.IsEmpty = con trỏ trống sau khi thả.
    /// </summary>
    public bool TryDrop(Grid_Placement held, Vector2Int origin, out Grid_Placement leftover)
    {
        leftover = held;
        if (held.IsEmpty || !FitsInBounds(held.Item, origin)) return false;

        // Tìm các chồng bị vùng thả chạm tới.
        Vector2Int size = held.Item.GridSize;
        int overlap = -1;
        for (int dy = 0; dy < size.y; dy++)
        {
            for (int dx = 0; dx < size.x; dx++)
            {
                int occupant = cells[(origin.y + dy) * width + origin.x + dx];
                if (occupant == -1) continue;
                if (overlap == -1) overlap = occupant;
                else if (overlap != occupant) return false;
            }
        }

        // Vùng trống: đặt vào.
        if (overlap == -1)
        {
            if (!TryPlace(held, origin)) return false;

            leftover = default;
            return true;
        }

        Entry target = entries[overlap];

        // Gộp chồng cùng loại.
        if (held.Instance == null && target.instance == null && target.item == held.Item
            && held.Item.CanStack && target.count < held.Item.MaxStack)
        {
            int moved = Mathf.Min(held.Item.MaxStack - target.count, held.Count);
            target.count += moved;
            entries[overlap] = target;

            int rest = held.Count - moved;
            leftover = rest > 0 ? new Grid_Placement(held.Item, null, rest, default) : default;
            return true;
        }

        // Đổi chỗ: nhấc chồng bị đè lên, đặt món đang cầm vào. Vùng thả chỉ chạm đúng chồng này nên chắc chắn vừa.
        Grid_Placement picked = GetPlacement(overlap);
        entries.RemoveAt(overlap);
        RebuildCells();

        if (!TryPlace(held, origin))
        {
            entries.Insert(overlap, target);
            RebuildCells();
            return false;
        }

        leftover = picked;
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
            Vector2Int size = e.item.GridSize;
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

    private void AddEntry(Item_Data item, Item_Instance instance, int count, Vector2Int origin)
    {
        entries.Add(new Entry { item = item, instance = instance, count = count, origin = origin });
        Paint(entries.Count - 1);
    }

    private void Paint(int index)
    {
        Entry e = entries[index];
        Vector2Int size = e.item.GridSize;

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
