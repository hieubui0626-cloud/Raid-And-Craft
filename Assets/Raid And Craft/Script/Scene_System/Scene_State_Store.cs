using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ nhớ trạng thái các Scene của NHÂN VẬT ĐANG CHỌN (quái đã hạ ở từng điểm spawn). Dữ liệu sống trong RAM trong lúc chơi,
/// nạp từ save khi cần (tự nạp lại khi đổi nhân vật) và được Player_Save ghi vào save (Player_Save_Data.sceneStates).
/// Quy tắc reset (Scene_Rules): Persist State tắt = luôn lượt mới; rời Scene quá Reset After Minutes = lượt mới;
/// đã hạ hết mọi điểm spawn (Reset When Cleared) rồi rời = lượt mới; ResetScene(...) = ép lượt mới (Travel_Gate.Fresh Start, UI sau này).
/// Reset được kiểm tra MỘT lần khi vào Scene, ngay lần đầu có điểm spawn hỏi trạng thái.
/// </summary>
public static class Scene_State_Store
{
    private static Dictionary<string, Scene_State_Entry> entries;
    private static string loadedSlot;
    private static bool enterPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries = null;
        loadedSlot = null;
        enterPending = false;
    }

    /// <summary>Scene_Info gọi khi Scene vừa vào: lần hỏi tiếp theo sẽ xét reset.</summary>
    public static void NotifySceneEntered()
    {
        enterPending = true;
    }

    /// <summary>Quên hết (đổi/xoá nhân vật). Lần dùng sau nạp lại từ save của nhân vật hiện tại.</summary>
    public static void Clear()
    {
        entries = null;
        loadedSlot = null;
    }

    /// <summary>Ép Scene này bắt đầu lượt mới (quái hồi sinh đầy đủ ở lần vào kế tiếp).</summary>
    public static void ResetScene(string sceneName)
    {
        Ensure();
        entries.Remove(sceneName);
    }

    public static bool IsDead(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return false;

        Ensure();
        return entries.TryGetValue(SceneManager.GetActiveScene().name, out Scene_State_Entry entry) && entry.deadSpawnIds.Contains(spawnId);
    }

    public static void MarkDead(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return;

        Ensure();
        string scene = SceneManager.GetActiveScene().name;
        if (!entries.TryGetValue(scene, out Scene_State_Entry entry))
        {
            entry = new Scene_State_Entry { sceneName = scene };
            entries[scene] = entry;
        }

        if (!entry.deadSpawnIds.Contains(spawnId)) entry.deadSpawnIds.Add(spawnId);
    }

    /// <summary>Player_Save gọi khi lưu: ghi trạng thái mọi Scene; Scene đang ở được đóng dấu "vừa ở đây" và cờ đã dọn sạch.</summary>
    public static void Capture(List<Scene_State_Entry> target)
    {
        Ensure();

        string scene = SceneManager.GetActiveScene().name;
        if (entries.TryGetValue(scene, out Scene_State_Entry current))
        {
            current.leftUtcTicks = DateTime.UtcNow.Ticks;
            current.cleared = Enemy_Spawn_Point.AllDead();
        }

        foreach (Scene_State_Entry entry in entries.Values)
        {
            if (entry.deadSpawnIds.Count == 0) continue;

            target.Add(new Scene_State_Entry
            {
                sceneName = entry.sceneName,
                leftUtcTicks = entry.leftUtcTicks,
                cleared = entry.cleared,
                deadSpawnIds = new List<string>(entry.deadSpawnIds),
            });
        }
    }

    private static void Ensure()
    {
        Save_Manager manager = Save_Manager.Instance;
        string slot = manager != null ? manager.SlotId : string.Empty;

        if (entries == null || loadedSlot != slot)
        {
            entries = new Dictionary<string, Scene_State_Entry>();
            loadedSlot = slot;

            if (manager != null)
            {
                foreach (Scene_State_Entry e in manager.ReadSceneStates())
                {
                    if (e != null && !string.IsNullOrEmpty(e.sceneName)) entries[e.sceneName] = e;
                }
            }
        }

        if (!enterPending) return;
        enterPending = false;
        EvaluateEntry();
    }

    private static void EvaluateEntry()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!entries.TryGetValue(scene, out Scene_State_Entry entry)) return;

        Scene_Rules rules = Scene_Info.Active != null ? Scene_Info.Active.Rules : null;
        bool fresh = rules == null || !rules.PersistState;

        if (!fresh && entry.leftUtcTicks > 0 && rules.ResetAfterMinutes > 0f)
        {
            double awayMinutes = (DateTime.UtcNow.Ticks - entry.leftUtcTicks) / (double)TimeSpan.TicksPerMinute;
            fresh = awayMinutes >= rules.ResetAfterMinutes;
        }

        if (!fresh && rules.ResetWhenCleared && entry.cleared) fresh = true;

        if (fresh) entries.Remove(scene);
    }
}
