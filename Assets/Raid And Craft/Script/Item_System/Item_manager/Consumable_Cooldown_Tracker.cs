using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Giữ hồi chiêu dùng vật phẩm tiêu hao của MỘT nhân vật, theo từng Consumable_Family_Data.
/// Tự thêm vào nhân vật ở lần dùng vật phẩm có family đầu tiên (GetOrAdd). Không lưu vào save (hồi chiêu ngắn, nạp lại là sẵn sàng).
/// </summary>
[DisallowMultipleComponent]
public class Consumable_Cooldown_Tracker : MonoBehaviour
{
    private readonly Dictionary<Consumable_Family_Data, float> readyAt = new Dictionary<Consumable_Family_Data, float>();

    /// <summary>Tracker của nhân vật này, null nếu chưa có (chưa dùng vật phẩm nào có family).</summary>
    public static Consumable_Cooldown_Tracker Find(Character_Stats user)
    {
        return user != null ? user.GetComponent<Consumable_Cooldown_Tracker>() : null;
    }

    public static Consumable_Cooldown_Tracker GetOrAdd(Character_Stats user)
    {
        var tracker = Find(user);
        return tracker != null ? tracker : user.gameObject.AddComponent<Consumable_Cooldown_Tracker>();
    }

    /// <summary>Số giây còn lại của family (0 = sẵn sàng).</summary>
    public float GetRemaining(Consumable_Family_Data family)
    {
        if (family == null || !readyAt.TryGetValue(family, out float time)) return 0f;
        return Mathf.Max(0f, time - Time.time);
    }

    public bool IsReady(Consumable_Family_Data family)
    {
        return GetRemaining(family) <= 0f;
    }

    /// <summary>Phần hồi chiêu còn lại 0..1 (1 = vừa dùng, 0 = sẵn sàng) để vẽ lớp phủ trên ô.</summary>
    public float GetFraction(Consumable_Family_Data family)
    {
        if (family == null || family.Cooldown <= 0f) return 0f;
        return Mathf.Clamp01(GetRemaining(family) / family.Cooldown);
    }

    public void StartCooldown(Consumable_Family_Data family)
    {
        if (family == null || family.Cooldown <= 0f) return;
        readyAt[family] = Time.time + family.Cooldown;
    }
}
