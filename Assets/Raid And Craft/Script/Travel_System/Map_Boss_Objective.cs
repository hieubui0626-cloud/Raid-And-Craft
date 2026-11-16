using UnityEngine;

/// <summary>
/// Nhiệm vụ chung của Instance: gắn lên BOSS CUỐI của tầng (prefab quái có Character_Stats). Boss chết thì Instance hiện tại được tính Complete
/// (Player_Map_Progress.ReportBossDefeated). Nhiệm vụ phụ sau này: viết component khác gọi cùng hàm đó khi đủ điều kiện.
/// Boss spawn từ Enemy_Spawn_Point cũng chạy bình thường vì component nằm trên prefab.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Map_Boss_Objective : MonoBehaviour
{
    private Character_Stats stats;
    private bool reported;

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
        if (reported) return;
        reported = true;

        // Tìm Player_Map_Progress lúc chết (Player có thể được tạo sau khi boss spawn).
        Player_Map_Progress progress = FindFirstObjectByType<Player_Map_Progress>();
        if (progress == null)
        {
            Debug.LogWarning($"[{nameof(Map_Boss_Objective)}] Không có Player_Map_Progress trong Scene nên không ghi nhận hoàn thành.", this);
            return;
        }

        progress.ReportBossDefeated();
    }
}
