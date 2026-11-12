using UnityEngine;

/// <summary>
/// Tạo Player từ prefab khi vào Scene chơi (Base, Instance...). Đặt MỘT object trong mỗi Scene chơi, kéo Player prefab vào.
/// Nhờ vậy Player chỉ có MỘT prefab duy nhất: nhân vật mới bắt đầu từ prefab, nhân vật cũ nạp save lên chính prefab đó (Player_Save lo),
/// và sửa Player ở một chỗ là mọi Scene cùng đổi (không còn mỗi Scene một bản Player đặt tay).
/// - Chạy ở Awake (thứ tự -1000, trước mọi script khác): tạo Player ở điểm xuất hiện MẶC ĐỊNH (Is Default) của Scene.
///   Vị trí cuối do Player_Save quyết định sau đó: vừa qua cổng thì đứng ở điểm cổng chỉ định, Continue ở Base thì đứng chỗ đã lưu.
/// - Scene đã có sẵn Player (đặt tay để thử riêng Scene trong Editor) thì không tạo thêm.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class Player_Spawner : MonoBehaviour
{
    [Tooltip("Player prefab (có Player_Save, Player_Controller...). Camera KHÔNG nằm trong prefab: mỗi Scene có Main Camera riêng, Player_Controller tự tìm.")]
    [SerializeField] private GameObject playerPrefab;

    // Tạo ở AWAKE (chạy trước mọi script khác): UI trong Scene (Skill_Bar_UI tìm Player_Skill_Slots ở Start, Pause_Menu kiểm tra Player khi Scene vừa nạp...)
    // cần Player đã tồn tại. Tạo ở Start thì UI có thể chạy trước và không thấy Player.
    private void Awake()
    {
        if (FindFirstObjectByType<Player_Save>() != null) return; // đã có Player trong Scene (đặt tay)

        if (playerPrefab == null)
        {
            Debug.LogError($"[{nameof(Player_Spawner)}] Chưa gán Player Prefab.", this);
            return;
        }

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Ở Awake các Scene_Spawn_Point khác có thể chưa kịp tự đăng ký nên tìm thẳng trong Scene: ưu tiên điểm Is Default, không có thì điểm đầu tiên.
        Scene_Spawn_Point point = null;
        foreach (Scene_Spawn_Point candidate in FindObjectsByType<Scene_Spawn_Point>(FindObjectsSortMode.None))
        {
            if (candidate.IsDefault) { point = candidate; break; }
            if (point == null) point = candidate;
        }

        if (point != null)
        {
            position = point.transform.position;
            rotation = Quaternion.Euler(0f, point.transform.eulerAngles.y, 0f);
        }
        else
        {
            Debug.LogWarning($"[{nameof(Player_Spawner)}] Scene không có Scene_Spawn_Point nào nên Player xuất hiện tại vị trí của Spawner.", this);
        }

        Instantiate(playerPrefab, position, rotation);
    }
}
