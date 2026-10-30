using UnityEngine;

/// <summary>
/// Bật/tắt một panel bằng phím tắt (C = Character Overview, B = Inventory, V = Magic Circuit...).
/// Gắn script này lên một object LUÔN BẬT (ví dụ Canvas), KHÔNG gắn lên chính panel: object đang tắt không chạy Update nên không bật lại được.
/// Mỗi panel một component: chọn Hotkey và kéo panel vào ô Panel.
/// Nút X trên panel có thể gọi Close() (OnClick).
/// </summary>
[DisallowMultipleComponent]
public class Panel_Hotkey_Toggle : MonoBehaviour
{
    [SerializeField] private Panel_Hotkey hotkey = Panel_Hotkey.Inventory;

    [Tooltip("Panel cần bật/tắt (kéo object panel vào, không phải object chứa script này).")]
    [SerializeField] private GameObject panel;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Update()
    {
        Input_Manager input = Input_Manager.Instance;
        if (input == null || panel == null) return;

        if (input.IsPanelHotkeyPressed(hotkey)) Toggle();
    }

    public void Toggle()
    {
        if (panel != null) panel.SetActive(!panel.activeSelf);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
}
