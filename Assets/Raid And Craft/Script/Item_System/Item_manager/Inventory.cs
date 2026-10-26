using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Một ô Inventory (chỉ để đọc, dữ liệu thật nằm trong Inventory).</summary>
public readonly struct Inventory_Slot
{
    public readonly Item_Data Item;
    public readonly int Count;

    public Inventory_Slot(Item_Data item, int count)
    {
        Item = item;
        Count = count;
    }

    public bool IsEmpty => Item == null || Count <= 0;
}

/// <summary>Vật phẩm cho sẵn lúc bắt đầu (để thử, chỉnh trong Inspector).</summary>
[Serializable]
public struct Inventory_Starting_Item
{
    public Item_Data item;
    [Min(1)] public int count;
}

/// <summary>
/// Inventory tối giản: số ô cố định, mỗi ô chứa một loại vật phẩm xếp chồng tới MaxStack.
/// Chưa có UI, kéo thả, sắp xếp, trọng lượng; chưa lưu vào Save_Data (làm sau, tra vật phẩm qua Item_Database).
/// Gắn lên PlayerPrefab.
/// </summary>
[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    /// <summary>Bắn khi nội dung Inventory đổi (một lần cho mỗi thao tác).</summary>
    public event Action OnInventoryChanged;

    [SerializeField, Min(1)] private int capacity = 40;

    [Tooltip("Vật phẩm có sẵn lúc bắt đầu (để thử Magic Circuit khi chưa có rơi đồ).")]
    [SerializeField] private List<Inventory_Starting_Item> startingItems = new List<Inventory_Starting_Item>();

    private Item_Data[] items;
    private int[] counts;
    private bool initialized;

    public int Capacity
    {
        get { EnsureInitialized(); return items.Length; }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của Inventory vẫn an toàn.
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        items = new Item_Data[Mathf.Max(1, capacity)];
        counts = new int[items.Length];

        foreach (Inventory_Starting_Item s in startingItems)
        {
            if (s.item == null) continue;

            int leftover = Place(items, counts, s.item, Mathf.Max(1, s.count));
            if (leftover > 0) Debug.LogWarning($"[{nameof(Inventory)}] Hết chỗ cho vật phẩm khởi tạo {s.item.DisplayName} (dư {leftover}).", this);
        }
    }

    public Inventory_Slot GetSlot(int index)
    {
        EnsureInitialized();
        if (index < 0 || index >= items.Length) return new Inventory_Slot(null, 0);
        return new Inventory_Slot(items[index], counts[index]);
    }

    /// <summary>Tổng số lượng một loại vật phẩm trên mọi ô.</summary>
    public int CountOf(Item_Data item)
    {
        EnsureInitialized();
        if (item == null) return 0;

        int total = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == item) total += counts[i];
        }
        return total;
    }

    public bool Has(Item_Data item, int count = 1)
    {
        return CountOf(item) >= count;
    }

    /// <summary>Có nhét vừa tất cả count vật phẩm này không (không thay đổi gì).</summary>
    public bool CanAdd(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        EnsureInitialized();

        var simItems = (Item_Data[])items.Clone();
        var simCounts = (int[])counts.Clone();
        return Place(simItems, simCounts, item, count) == 0;
    }

    /// <summary>Có nhét vừa mỗi vật phẩm trong danh sách (mỗi cái 1 đơn vị) cùng lúc không.</summary>
    public bool CanAddAll(IReadOnlyList<Item_Data> toAdd)
    {
        EnsureInitialized();

        var simItems = (Item_Data[])items.Clone();
        var simCounts = (int[])counts.Clone();
        foreach (Item_Data item in toAdd)
        {
            if (item == null) continue;
            if (Place(simItems, simCounts, item, 1) > 0) return false;
        }
        return true;
    }

    /// <summary>Thêm vật phẩm. Trả về số lượng KHÔNG nhét được (0 = vào hết).</summary>
    public int Add(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return count;
        EnsureInitialized();

        int leftover = Place(items, counts, item, count);
        if (leftover < count) OnInventoryChanged?.Invoke();
        return leftover;
    }

    /// <summary>Lấy đi count vật phẩm. Trả về false (không lấy gì) nếu không đủ.</summary>
    public bool TryRemove(Item_Data item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        EnsureInitialized();
        if (CountOf(item) < count) return false;

        int remaining = count;
        for (int i = items.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (items[i] != item) continue;

            int take = Mathf.Min(counts[i], remaining);
            counts[i] -= take;
            remaining -= take;
            if (counts[i] <= 0)
            {
                counts[i] = 0;
                items[i] = null;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    // Nhét vào mảng (thật hoặc bản sao để thử). Trả về số lượng dư không nhét được.
    private static int Place(Item_Data[] itemArray, int[] countArray, Item_Data item, int amount)
    {
        int max = item.MaxStack;

        for (int i = 0; i < itemArray.Length && amount > 0; i++)
        {
            if (itemArray[i] != item || countArray[i] >= max) continue;

            int add = Mathf.Min(max - countArray[i], amount);
            countArray[i] += add;
            amount -= add;
        }

        for (int i = 0; i < itemArray.Length && amount > 0; i++)
        {
            if (itemArray[i] != null) continue;

            int add = Mathf.Min(max, amount);
            itemArray[i] = item;
            countArray[i] = add;
            amount -= add;
        }

        return amount;
    }
}
