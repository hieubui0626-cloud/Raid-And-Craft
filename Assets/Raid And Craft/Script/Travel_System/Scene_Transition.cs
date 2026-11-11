using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Nơi DUY NHẤT chuyển Scene trong game (cổng, portal, menu...), để luôn làm đủ các bước:
/// 1. Lưu game NGAY (đồng bộ) trước khi rời Scene. Scene mới nạp lại save lúc Player đăng ký, nên không lưu thì đồ/máu vừa đổi bị trả về lần lưu trước.
/// 2. Nhớ điểm xuất hiện (Scene_Spawn_Point) ở Scene mới.
/// 3. Nạp Scene (phải có trong Build Settings).
/// Scene mới: Player_Save đặt Player vào điểm xuất hiện đã nhớ sau khi nạp save.
/// </summary>
public static class Scene_Transition
{
    /// <summary>Thông báo ngắn cho người chơi (cổng đóng, điểm dịch chuyển mới mở...). UI nào muốn hiện thì đăng ký; mặc định chỉ in Console.</summary>
    public static event Action<string> OnMessage;

    private static bool hasPending;
    private static string pendingSpawnId = string.Empty;

    /// <summary>Đang chuyển Scene (chặn bấm hai lần).</summary>
    public static bool IsTransitioning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnMessage = null;
        hasPending = false;
        pendingSpawnId = string.Empty;
        IsTransitioning = false;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsTransitioning = false;
    }

    public static void ReportMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log($"[{nameof(Scene_Transition)}] {message}");
        OnMessage?.Invoke(message);
    }

    /// <summary>Mọi điều kiện đạt thì true. Điều kiện đầu tiên chưa đạt cho ra lý do. Danh sách rỗng/null = luôn đạt.</summary>
    public static bool CheckConditions(IReadOnlyList<Travel_Condition> conditions, GameObject traveler, out string reason)
    {
        reason = string.Empty;
        if (conditions == null) return true;

        foreach (Travel_Condition condition in conditions)
        {
            if (condition == null) continue;
            if (!condition.IsMet(traveler, out reason)) return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>Lưu rồi sang Scene khác, đứng ở điểm spawnId của Scene đó (rỗng = điểm mặc định). False nếu không đi được.</summary>
    public static bool Travel(string sceneName, string spawnId)
    {
        if (IsTransitioning) return false;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[{nameof(Scene_Transition)}] Chưa điền tên Scene đích.");
            ReportMessage("Không đi được: chưa có Scene đích.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[{nameof(Scene_Transition)}] Scene '{sceneName}' không có trong Build Settings (File -> Build Profiles/Settings -> Scene List).");
            ReportMessage("Không đi được: khu vực này chưa sẵn sàng.");
            return false;
        }

        if (Save_Manager.Instance != null) Save_Manager.Instance.SaveNow();

        hasPending = true;
        pendingSpawnId = spawnId ?? string.Empty;
        IsTransitioning = true;
        SceneManager.LoadScene(sceneName);
        return true;
    }

    /// <summary>Scene base để quay về: Scene base gần nhất đã ghi (Save_Manager.LastBaseScene), không có thì Fallback Base Scene của Save_Manager.</summary>
    public static string ResolveBaseScene()
    {
        Save_Manager manager = Save_Manager.Instance;
        if (manager == null) return string.Empty;

        return string.IsNullOrEmpty(manager.LastBaseScene) ? manager.FallbackBaseScene ?? string.Empty : manager.LastBaseScene;
    }

    /// <summary>Về Scene base gần nhất, đứng ở điểm xuất hiện spawnId (rỗng = mặc định). Dùng khi chết, hoặc nút "Về base".</summary>
    public static bool ReturnToBase(string spawnId = "")
    {
        return Travel(ResolveBaseScene(), spawnId);
    }

    /// <summary>Lưu rồi về Scene Main Menu (tên lấy từ Save_Manager.MainMenuScene). Trả lại tốc độ thời gian bình thường trước khi nạp.</summary>
    public static bool GoToMainMenu()
    {
        if (IsTransitioning) return false;

        Save_Manager manager = Save_Manager.Instance;
        string menu = manager != null ? manager.MainMenuScene : string.Empty;
        if (string.IsNullOrEmpty(menu) || !Application.CanStreamedLevelBeLoaded(menu))
        {
            Debug.LogError($"[{nameof(Scene_Transition)}] Scene Main Menu '{menu}' trống hoặc không có trong Build Settings (chỉnh ở Save_Manager).");
            ReportMessage("Không về được Main Menu.");
            return false;
        }

        Time.timeScale = 1f;
        manager.SaveNow();

        hasPending = false;
        IsTransitioning = true;
        SceneManager.LoadScene(menu);
        return true;
    }

    /// <summary>Player_Save gọi sau khi nạp save ở Scene mới: nếu có lần chuyển đang chờ thì đặt Player vào điểm xuất hiện đã nhớ (chỉ một lần).</summary>
    public static bool PlaceAtPendingSpawn(Transform player, NavMeshAgent agent)
    {
        if (!hasPending) return false;

        hasPending = false;
        return PlaceAt(pendingSpawnId, player, agent);
    }

    /// <summary>Đặt Player vào một điểm xuất hiện của Scene hiện tại (dùng cho dịch chuyển nhanh trong cùng Scene).</summary>
    public static bool PlaceAt(string spawnId, Transform player, NavMeshAgent agent)
    {
        Scene_Spawn_Point point = Scene_Spawn_Point.Find(spawnId);
        if (point == null)
        {
            Debug.LogWarning($"[{nameof(Scene_Transition)}] Scene này không có Scene_Spawn_Point nào (cần Id '{spawnId}' hoặc một điểm Is Default), Player giữ nguyên chỗ.");
            return false;
        }

        if (!string.IsNullOrEmpty(spawnId) && point.SpawnId != spawnId)
        {
            Debug.LogWarning($"[{nameof(Scene_Transition)}] Không có điểm xuất hiện '{spawnId}', dùng điểm mặc định '{point.SpawnId}'.");
        }

        // NavMeshAgent phải Warp, gán transform.position trực tiếp sẽ bị agent kéo lại.
        bool warped = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            warped = agent.Warp(point.transform.position);
        }

        if (!warped) player.position = point.transform.position;
        player.rotation = Quaternion.Euler(0f, point.transform.eulerAngles.y, 0f);
        return true;
    }
}
