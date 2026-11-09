using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điểm xuất hiện của Player trong một Scene: nơi Player đứng khi đi qua cổng (Travel_Gate) hoặc dịch chuyển nhanh tới đây
/// (Fast_Travel_Point). Đặt một object trống ở chỗ muốn, đặt Spawn Id (duy nhất trong Scene), hướng mũi tên xanh = hướng Player nhìn.
/// Đánh dấu Is Default ở một điểm để làm chỗ xuất hiện mặc định khi không chỉ định (hoặc chỉ định Id không tồn tại).
/// </summary>
[DisallowMultipleComponent]
public class Scene_Spawn_Point : MonoBehaviour
{
    private static readonly List<Scene_Spawn_Point> Active = new List<Scene_Spawn_Point>();

    [Tooltip("Tên điểm, duy nhất trong Scene (ví dụ From_Forest). Cổng/portal ở Scene khác trỏ tới Id này.")]
    [SerializeField] private string spawnId = "Default";

    [Tooltip("Điểm xuất hiện mặc định của Scene (dùng khi Id không có hoặc để trống).")]
    [SerializeField] private bool isDefault;

    public string SpawnId => spawnId;
    public bool IsDefault => isDefault;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    /// <summary>Tìm điểm theo Id; không thấy (hoặc Id rỗng) thì trả điểm mặc định; không có cả hai thì null.</summary>
    public static Scene_Spawn_Point Find(string id)
    {
        Scene_Spawn_Point fallback = null;
        foreach (Scene_Spawn_Point point in Active)
        {
            if (!string.IsNullOrEmpty(id) && point.spawnId == id) return point;
            if (point.isDefault && fallback == null) fallback = point;
        }

        return fallback;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isDefault ? new Color(0.3f, 1f, 0.4f) : new Color(0.3f, 0.7f, 1f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.4f);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * 1.2f);
    }
}
