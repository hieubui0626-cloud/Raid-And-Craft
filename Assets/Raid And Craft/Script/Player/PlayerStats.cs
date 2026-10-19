using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action OnStatsChanged;

    // Cấu trúc lưu trữ đa tầng: Dictionary chứa Dictionary
    private Dictionary<Player_StatLayer, Dictionary<Player_StatType, int>> statLayers = new Dictionary<Player_StatLayer, Dictionary<Player_StatType, int>>();

    private void Awake()
    {
        // --- KHỞI TẠO CÁC LAYER VÀ CHỈ SỐ MẶC ĐỊNH ---

        // Layer: Stats
        InitializeStat(Player_StatLayer.Stats, Player_StatType.Strength, 10);
        InitializeStat(Player_StatLayer.Stats, Player_StatType.Agility, 10);
        InitializeStat(Player_StatLayer.Stats, Player_StatType.Intelligence, 10);

        // Layer: Attack
        InitializeStat(Player_StatLayer.Attack, Player_StatType.PhysicAttack, 15);
        InitializeStat(Player_StatLayer.Attack, Player_StatType.MagicAttack, 12);
        // Layer: Defend
        InitializeStat(Player_StatLayer.Defend, Player_StatType.Armor, 5);
        InitializeStat(Player_StatLayer.Defend, Player_StatType.Evade, 2);
    }

    private void InitializeStat(Player_StatLayer layer, Player_StatType type, int defaultValue)
    {
        // Nếu Layer chưa tồn tại, tạo Layer mới
        if (!statLayers.ContainsKey(layer))
        {
            statLayers[layer] = new Dictionary<Player_StatType, int>();
        }

        // Nếu Chỉ số chưa có trong Layer, thêm vào
        if (!statLayers[layer].ContainsKey(type))
        {
            statLayers[layer].Add(type, defaultValue);
        }
    }

    // Hàm lấy giá trị của một chỉ số dựa vào Layer và Type
    public int GetStat(Player_StatLayer layer, Player_StatType type)
    {
        if (statLayers.TryGetValue(layer, out var subDict))
        {
            if (subDict.TryGetValue(type, out int value))
            {
                return value;
            }
        }
        return 0; // Trả về 0 nếu không tìm thấy
    }

    // Hàm lấy toàn bộ danh sách chỉ số của một Layer (Phục vụ cho việc vẽ UI tự động)
    public Dictionary<Player_StatType, int> GetStatsInLayer(Player_StatLayer layer)
    {
        if (statLayers.TryGetValue(layer, out var subDict))
        {
            return subDict;
        }
        return null;
    }

    // Hàm thay đổi chỉ số
    public void ModifyStat(Player_StatLayer layer, Player_StatType type, int amount)
    {
        if (statLayers.ContainsKey(layer) && statLayers[layer].ContainsKey(type))
        {
            statLayers[layer][type] += amount;

            if (statLayers[layer][type] < 0) statLayers[layer][type] = 0;

            OnStatsChanged?.Invoke(); // Thông báo cập nhật UI
        }
    }
}
