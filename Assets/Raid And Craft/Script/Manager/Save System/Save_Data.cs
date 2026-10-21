using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toàn bộ dữ liệu một lần lưu (một slot). Chỉ chứa dữ liệu thuần, JsonUtility serialize được.
/// Mở rộng sau này: thêm field mới (inventory, quest, world...) và tăng CurrentVersion nếu đổi cấu trúc cũ.
/// </summary>
[Serializable]
public class Save_Data
{
    public const int CurrentVersion = 1;

    /// <summary>Phiên bản cấu trúc dữ liệu - dùng để migrate save cũ.</summary>
    public int version = CurrentVersion;

    /// <summary>
    /// Số thứ tự lần lưu, tăng dần. Khi có cloud: bản nào revision cao hơn là bản mới hơn
    /// (không dựa vào đồng hồ máy vì có thể lệch giờ).
    /// </summary>
    public long revision;

    /// <summary>Thời điểm lưu (UTC, ISO 8601) - chỉ để hiển thị/gỡ lỗi.</summary>
    public string saveTimeUtc;

    public Player_Save_Data player = new Player_Save_Data();
}

[Serializable]
public class Player_Save_Data
{
    public Vector3 position;
    public float rotationY;

    public float currentHealth;
    public float currentMana;

    /// <summary>
    /// Chỉ lưu GIÁ TRỊ GỐC của từng chỉ số (không lưu modifier).
    /// Modifier từ trang bị/buff sẽ được dựng lại từ inventory/buff khi các hệ thống đó có save riêng,
    /// nếu không sẽ bị cộng đôi.
    /// </summary>
    public List<Stat_Save_Entry> baseStats = new List<Stat_Save_Entry>();
}

/// <summary>Lưu type dạng số nguyên (giá trị enum không đổi theo quy tắc của Player_Stat_Type) để đổi tên enum không làm hỏng save.</summary>
[Serializable]
public struct Stat_Save_Entry
{
    public int type;
    public float baseValue;

    public Stat_Save_Entry(int type, float baseValue)
    {
        this.type = type;
        this.baseValue = baseValue;
    }
}
