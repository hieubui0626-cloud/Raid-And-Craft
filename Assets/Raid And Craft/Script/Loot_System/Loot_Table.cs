using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Một dòng của bảng rơi: vật phẩm, xác suất rơi (0..1) và số lượng ngẫu nhiên trong [minCount, maxCount].</summary>
[Serializable]
public struct Loot_Entry
{
    public Item_Data item;

    [Tooltip("Xác suất rơi mỗi lần quái chết: 0 = không bao giờ, 1 = luôn rơi.")]
    [Range(0f, 1f)] public float chance;

    [Tooltip("Số lượng nhỏ nhất khi rơi (vật phẩm không xếp chồng được luôn rơi 1).")]
    [Min(1)] public int minCount;

    [Tooltip("Số lượng lớn nhất khi rơi.")]
    [Min(1)] public int maxCount;
}

/// <summary>Kết quả một lần tung bảng rơi: rơi vật phẩm nào, bao nhiêu.</summary>
public struct Loot_Drop
{
    public Item_Data item;
    public int count;
}

/// <summary>
/// Bảng rơi vật phẩm (tạo asset: Create -> Raid And Craft -> Loot Table). Mỗi dòng tung xác suất RIÊNG.
/// Dùng chung một asset cho nhiều quái cùng hạng (rank): ví dụ Loot_Rank1 rơi Raw Material bậc 1, Loot_Rank2 rơi bậc 2...
/// Theo thiết kế craft, quái chỉ rơi NGUYÊN LIỆU (và Coin), không rơi trang bị.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Loot Table", fileName = "Loot_Table")]
public class Loot_Table : ScriptableObject
{
    [SerializeField] private List<Loot_Entry> entries = new List<Loot_Entry>();

    public IReadOnlyList<Loot_Entry> Entries => entries;

    /// <summary>Tung mọi dòng, thêm các món rơi vào results.</summary>
    public void Roll(List<Loot_Drop> results) => Roll(results, 1f, 1f);

    /// <summary>Tung có hệ số: chanceFactor nhân xác suất (kẹp 0..1), countFactor nhân số lượng (phần lẻ làm tròn ngẫu nhiên).</summary>
    public void Roll(List<Loot_Drop> results, float chanceFactor, float countFactor)
    {
        foreach (Loot_Entry entry in entries)
        {
            if (entry.item == null || entry.chance <= 0f) continue;
            if (UnityEngine.Random.value > Mathf.Clamp01(entry.chance * chanceFactor)) continue;

            int count = 1;
            if (entry.item.CanStack)
            {
                int min = Mathf.Max(1, entry.minCount);
                int max = Mathf.Max(min, entry.maxCount);
                count = UnityEngine.Random.Range(min, max + 1);
                if (!Mathf.Approximately(countFactor, 1f)) count = Scene_Rule_Values.RoundStochastic(count * countFactor);
                if (count <= 0) continue;
            }

            results.Add(new Loot_Drop { item = entry.item, count = count });
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (Loot_Entry entry in entries)
        {
            if (entry.item is Equippable_Item_Data)
            {
                Debug.LogWarning($"[{nameof(Loot_Table)}] '{name}': '{entry.item.DisplayName}' là trang bị. Theo thiết kế craft, quái chỉ rơi nguyên liệu.", this);
            }
        }
    }
#endif
}
