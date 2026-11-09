using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Một "ô rơi nguyên liệu" của quái: tung xác suất, trúng thì rơi một nguyên liệu do QUY CHẾ SCENE chọn (họ và tier), số lượng min..max.
/// Quái nhiều ô (hoặc ô xác suất cao) rơi nhiều hơn: ví dụ quái thường 1 ô 40%, elite 2 ô 100%.
/// </summary>
[System.Serializable]
public struct Material_Drop_Slot
{
    [Range(0f, 1f)] public float chance;
    [Min(1)] public int minCount;
    [Min(1)] public int maxCount;
}

/// <summary>
/// Phần thưởng khi quái chết: EXP, Coin và vật phẩm rơi (theo Loot_Table). Gắn lên Enemy cùng Character_Stats.
/// - Người nhận EXP/Coin: kẻ hạ gục (Character_Stats.LastAttacker) nếu có Player_Experience/Player_Wallet;
///   chết do DoT/môi trường (không có kẻ hạ gục) thì lấy Player (tag "Player").
/// - Coin cộng thẳng vào ví. Vật phẩm rơi xuống đất quanh xác (nhặt bằng click như vật phẩm thường).
/// - Mỗi Enemy prefab điền riêng EXP/Coin; dùng chung asset Loot_Table cho các quái cùng hạng.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Enemy_Reward : MonoBehaviour
{
    [Header("Kinh nghiệm")]
    [Tooltip("EXP cho người hạ gục.")]
    [SerializeField, Min(0)] private int exp = 10;

    [Header("Coin")]
    [Tooltip("Coin nhận được ngẫu nhiên trong [Min, Max]. Cả hai 0 = không có Coin.")]
    [SerializeField, Min(0)] private int coinMin;
    [SerializeField, Min(0)] private int coinMax;

    [Header("Nguyên liệu theo Scene")]
    [Tooltip("Mỗi ô tung riêng; nguyên liệu cụ thể (họ, tier) do Scene_Rules của Scene hiện tại chọn, nên cùng quái ở Scene khác rơi khác. " +
             "Scene không có Rules thì các ô này không rơi gì.")]
    [SerializeField] private List<Material_Drop_Slot> materialSlots = new List<Material_Drop_Slot>
    {
        new Material_Drop_Slot { chance = 0.4f, minCount = 1, maxCount = 2 },
    };

    [Header("Vật phẩm rơi cố định")]
    [Tooltip("Các bảng rơi cố định (không phụ thuộc Scene); mỗi bảng tung riêng. Trống = không rơi vật phẩm cố định.")]
    [SerializeField] private List<Loot_Table> lootTables = new List<Loot_Table>();

    [Tooltip("Vật phẩm rơi rải ngẫu nhiên trong bán kính này (mét) quanh xác quái.")]
    [SerializeField, Min(0f)] private float scatterRadius = 1.2f;

    private static readonly List<Loot_Drop> DropBuffer = new List<Loot_Drop>();

    private Character_Stats stats;

    private void Awake()
    {
        stats = GetComponent<Character_Stats>();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        stats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        GameObject receiver = ResolveReceiver();
        Scene_Rules rules = Scene_Info.Active != null ? Scene_Info.Active.Rules : null;

        if (receiver != null)
        {
            int finalExp = rules != null ? rules.ScaleExp(exp) : exp;
            if (finalExp > 0)
            {
                var experience = receiver.GetComponent<Player_Experience>();
                if (experience != null) experience.AddExp(finalExp);
            }

            int coin = Random.Range(Mathf.Min(coinMin, coinMax), Mathf.Max(coinMin, coinMax) + 1);
            if (coin > 0)
            {
                var wallet = receiver.GetComponent<Player_Wallet>();
                if (wallet != null) wallet.Add(coin);
            }
        }

        DropLoot(rules);
    }

    private GameObject ResolveReceiver()
    {
        Character_Stats killer = stats.LastAttacker;
        if (killer != null) return killer.gameObject;

        return GameObject.FindGameObjectWithTag("Player");
    }

    private void DropLoot(Scene_Rules rules)
    {
        DropBuffer.Clear();
        foreach (Loot_Table table in lootTables)
        {
            if (table != null) table.Roll(DropBuffer);
        }

        // Nguyên liệu do Scene chọn (họ + tier theo level Scene).
        if (rules != null)
        {
            foreach (Material_Drop_Slot slot in materialSlots)
            {
                if (slot.chance <= 0f || UnityEngine.Random.value > slot.chance) continue;

                Material_Item_Data material = rules.RollMaterial();
                if (material == null) continue;

                int count = 1;
                if (material.CanStack)
                {
                    int min = Mathf.Max(1, slot.minCount);
                    count = UnityEngine.Random.Range(min, Mathf.Max(min, slot.maxCount) + 1);
                }

                DropBuffer.Add(new Loot_Drop { item = material, count = count });
            }
        }

        foreach (Loot_Drop drop in DropBuffer)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);

            // Đặt lên mặt NavMesh để vật phẩm nằm đúng mặt đất và Player tới nhặt được.
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) position = hit.position;
            else position = transform.position;

            World_Item.Spawn(drop.item, drop.count, null, position);
        }

        DropBuffer.Clear();
    }

    private void OnValidate()
    {
        if (coinMax < coinMin) coinMax = coinMin;
    }
}
