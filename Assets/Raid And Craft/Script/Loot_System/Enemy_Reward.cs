using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Vật phẩm rơi")]
    [Tooltip("Các bảng rơi; mỗi bảng tung riêng. Trống = không rơi vật phẩm.")]
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

        if (receiver != null)
        {
            if (exp > 0)
            {
                var experience = receiver.GetComponent<Player_Experience>();
                if (experience != null) experience.AddExp(exp);
            }

            int coin = Random.Range(Mathf.Min(coinMin, coinMax), Mathf.Max(coinMin, coinMax) + 1);
            if (coin > 0)
            {
                var wallet = receiver.GetComponent<Player_Wallet>();
                if (wallet != null) wallet.Add(coin);
            }
        }

        DropLoot();
    }

    private GameObject ResolveReceiver()
    {
        Character_Stats killer = stats.LastAttacker;
        if (killer != null) return killer.gameObject;

        return GameObject.FindGameObjectWithTag("Player");
    }

    private void DropLoot()
    {
        DropBuffer.Clear();
        foreach (Loot_Table table in lootTables)
        {
            if (table != null) table.Roll(DropBuffer);
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
