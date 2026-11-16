using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên một Button trong thanh nút điều khiển để mở/đóng World Map ở chế độ XEM (cùng việc với phím M).
/// Không cần kéo gì: tự nối vào World_Map_UI khi chạy (Scene không có World_Map_UI thì bấm chỉ in cảnh báo).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class Map_Button : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (World_Map_UI.Instance != null) World_Map_UI.Instance.Toggle();
            else Debug.LogWarning($"[{nameof(Map_Button)}] Scene chưa có World_Map_UI.", this);
        });
    }
}
