using UnityEngine;

/// <summary>
/// Điểm dịch chuyển nhanh (portal, kiểu waypoint của các ARPG): click chuột trái, Player đi tới, điểm này được MỞ (lưu vào save)
/// và bảng Map (Map_Panel_UI) hiện ra để chọn nơi cần tới. Chỉ dịch chuyển được tới các điểm ĐÃ mở.
/// Cách tạo: object trong Scene, Add Component Fast_Travel_Point, tạo thêm một object con rỗng gắn Scene_Spawn_Point
/// (chỗ Player đứng khi tới, nên đặt hơi lệch khỏi portal) và kéo nó vào ô Spawn Point. Spawn Id của điểm đó là tên duy nhất của portal trong Scene.
/// </summary>
[DisallowMultipleComponent]
public class Fast_Travel_Point : Travel_Point_Base
{
    [Tooltip("Chỗ Player xuất hiện khi dịch chuyển tới portal này. Spawn Id của nó phải duy nhất trong Scene.")]
    [SerializeField] private Scene_Spawn_Point spawnPoint;

    protected override void OnInteract(World_Item_Picker interactor)
    {
        if (spawnPoint == null)
        {
            Debug.LogError($"[{nameof(Fast_Travel_Point)}] '{name}': chưa gán Spawn Point.", this);
            return;
        }

        var travel = interactor.GetComponent<Player_Fast_Travel>();
        if (travel == null)
        {
            Debug.LogError($"[{nameof(Fast_Travel_Point)}] Player chưa có component Player_Fast_Travel.", this);
            return;
        }

        var entry = new Fast_Travel_Entry { sceneName = gameObject.scene.name, spawnId = spawnPoint.SpawnId, displayName = DisplayName };
        if (travel.Discover(entry))
        {
            Scene_Transition.ReportMessage($"Đã mở điểm dịch chuyển: {DisplayName}");
            if (Save_Manager.Instance != null) Save_Manager.Instance.SaveGame();
        }

        Map_Panel_UI.Instance.Open();
    }

    protected override Color LabelColor => new Color(0.55f, 1f, 0.8f);

    private void OnValidate()
    {
        if (spawnPoint == null) Debug.LogWarning($"[{nameof(Fast_Travel_Point)}] '{name}': chưa gán Spawn Point.", this);
    }
}
