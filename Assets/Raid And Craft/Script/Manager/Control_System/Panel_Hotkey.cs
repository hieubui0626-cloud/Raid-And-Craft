/// <summary>
/// Phím tắt mở/đóng các panel lớn. Thứ tự khớp Input_Manager.PanelHotkeyKeys (C, V, B, N, M).
/// QUY TẮC: không đổi số mục đã có, chỉ thêm ở CUỐI enum (và thêm phím tương ứng ở CUỐI PanelHotkeyKeys).
/// </summary>
public enum Panel_Hotkey
{
    CharacterOverview = 0, // C
    MagicCircuit = 1,      // V
    Inventory = 2,         // B
    Undecided = 3,         // N (chưa quyết)
    WorldMap = 4,          // M
}
