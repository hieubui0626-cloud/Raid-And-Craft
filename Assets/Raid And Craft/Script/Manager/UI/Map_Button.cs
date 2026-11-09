using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên một Button trong thanh nút điều khiển để mở/đóng bảng Map (cùng việc với phím M).
/// Không cần kéo gì: tự nối vào Map_Panel_UI khi chạy.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class Map_Button : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Map_Panel_UI.Instance.Toggle());
    }
}
