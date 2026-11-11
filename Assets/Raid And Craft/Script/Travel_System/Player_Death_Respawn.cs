using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chết thì hồi sinh ở Scene base gần nhất (khép vòng lặp Instance -> chết -> về base). Gắn lên Player cùng Character_Stats.
/// Sau Respawn Delay giây: hồi sinh (Revive) với tỉ lệ máu đã chọn rồi
/// - đang ở Scene base: đặt Player vào điểm xuất hiện mặc định của Scene (không nạp lại Scene);
/// - đang ở Scene tạm: về Scene base gần nhất (Scene_Transition.ReturnToBase, có lưu game).
/// Chưa có hình phạt chết (mất đồ/EXP); thêm ở HandleRespawn khi cần.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Player_Death_Respawn : MonoBehaviour
{
    [Tooltip("Giây chờ sau khi chết trước khi hồi sinh (để xem hoạt ảnh chết).")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    [Tooltip("Tỉ lệ máu khi hồi sinh (0..1).")]
    [SerializeField, Range(0.05f, 1f)] private float reviveHealthPercent = 0.5f;

    private Character_Stats stats;
    private NavMeshAgent agent;
    private Coroutine routine;

    private void Awake()
    {
        stats = GetComponent<Character_Stats>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        stats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnDeath -= HandleDeath;
        routine = null;
    }

    private void HandleDeath()
    {
        if (routine == null) routine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        routine = null;

        // Hồi sinh TRƯỚC khi lưu/chuyển Scene để save không ghi lại trạng thái đang chết.
        stats.Revive(reviveHealthPercent);
        Scene_Transition.ReportMessage("Bạn đã gục ngã và được đưa về nơi an toàn.");

        Scene_Info info = Scene_Info.Active;
        bool inBase = info == null || info.Kind == Scene_Kind.Base;
        if (inBase)
        {
            Scene_Transition.PlaceAt(string.Empty, transform, agent);
            if (Save_Manager.Instance != null) Save_Manager.Instance.SaveNow();
        }
        else if (!Scene_Transition.ReturnToBase())
        {
            // Không có Scene base để về (chưa cấu hình): ở lại, ít nhất không kẹt trong trạng thái chết.
            Debug.LogWarning($"[{nameof(Player_Death_Respawn)}] Không về được Scene base (Save_Manager.Fallback Base Scene trống?). Player hồi sinh tại chỗ.", this);
        }
    }
}
