using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Một điểm dịch chuyển nhanh đã mở: nằm ở Scene nào, tên điểm xuất hiện, tên hiển thị. Lưu thẳng vào save (JsonUtility).</summary>
[Serializable]
public struct Fast_Travel_Entry
{
    public string sceneName;
    public string spawnId;
    public string displayName;
}

/// <summary>
/// Gắn lên Player. Giữ danh sách điểm dịch chuyển nhanh (Fast_Travel_Point) đã mở và thực hiện dịch chuyển tới chúng
/// (cùng Scene: đặt thẳng Player; khác Scene: qua Scene_Transition). Player_Save lưu danh sách này.
/// Phím M (Panel_Hotkey.WorldMap) bật/tắt Map_Panel_UI, bảng liệt kê các điểm đã mở.
/// </summary>
[DisallowMultipleComponent]
public class Player_Fast_Travel : MonoBehaviour
{
    public event Action OnChanged;

    private readonly List<Fast_Travel_Entry> entries = new List<Fast_Travel_Entry>();

    public IReadOnlyList<Fast_Travel_Entry> Entries => entries;

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        if (input != null && input.IsPanelHotkeyPressed(Panel_Hotkey.WorldMap)) Map_Panel_UI.Instance.Toggle();
    }

    public bool IsDiscovered(string sceneName, string spawnId)
    {
        foreach (Fast_Travel_Entry e in entries)
        {
            if (e.sceneName == sceneName && e.spawnId == spawnId) return true;
        }

        return false;
    }

    /// <summary>Mở một điểm. True nếu là điểm MỚI (chưa mở trước đó).</summary>
    public bool Discover(Fast_Travel_Entry entry)
    {
        if (string.IsNullOrEmpty(entry.sceneName) || IsDiscovered(entry.sceneName, entry.spawnId)) return false;

        entries.Add(entry);
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Thay toàn bộ danh sách (dùng khi nạp save).</summary>
    public void RestoreAll(IEnumerable<Fast_Travel_Entry> saved)
    {
        entries.Clear();
        if (saved != null)
        {
            foreach (Fast_Travel_Entry e in saved)
            {
                if (!string.IsNullOrEmpty(e.sceneName) && !IsDiscovered(e.sceneName, e.spawnId)) entries.Add(e);
            }
        }

        OnChanged?.Invoke();
    }

    /// <summary>Dịch chuyển tới một điểm đã mở. False (kèm lý do) nếu không đi được.</summary>
    public bool TryTravel(Fast_Travel_Entry entry, out string reason)
    {
        reason = string.Empty;

        Scene_Info info = FindFirstObjectByType<Scene_Info>();
        if (info != null && !info.AllowFastTravel)
        {
            reason = "Không thể dịch chuyển nhanh ở khu vực này.";
            return false;
        }

        if (!IsDiscovered(entry.sceneName, entry.spawnId))
        {
            reason = "Điểm dịch chuyển này chưa được mở.";
            return false;
        }

        if (Scene_Transition.IsTransitioning)
        {
            reason = "Đang chuyển khu vực.";
            return false;
        }

        bool ok = entry.sceneName == gameObject.scene.name
            ? Scene_Transition.PlaceAt(entry.spawnId, transform, GetComponent<NavMeshAgent>())
            : Scene_Transition.Travel(entry.sceneName, entry.spawnId);

        if (!ok) reason = "Không dịch chuyển được tới điểm này.";
        return ok;
    }
}
