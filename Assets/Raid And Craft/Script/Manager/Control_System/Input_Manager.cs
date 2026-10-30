using UnityEngine;
using UnityEngine.InputSystem;

public class Input_Manager : MonoBehaviour
{
    // Singleton để gọi nhanh từ các script khác: Input_Manager.Instance
    public static Input_Manager Instance { get; private set; }

    // --- Mouse ---
    public bool LeftClickPressed { get; private set; }
    public bool LeftClickHeld { get; private set; }
    public bool RightClickPressed { get; private set; }
    public bool RightClickHeld { get; private set; }
    public Vector2 MousePosition { get; private set; }

    // --- Keyboard ---
    public bool SpacePressed { get; private set; }

    /// <summary>Số ô kỹ năng: 1-5 và Q-W-E-R-T.</summary>
    public const int SkillSlotCount = 10;

    /// <summary>Phím của từng ô kỹ năng theo thứ tự ô 0..9 (1, 2, 3, 4, 5, Q, W, E, R, T).</summary>
    public static readonly Key[] SkillSlotKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Q, Key.W, Key.E, Key.R, Key.T,
    };

    /// <summary>Phím mở/đóng panel, theo thứ tự enum Panel_Hotkey: C, V, B, N, M.</summary>
    public static readonly Key[] PanelHotkeyKeys = { Key.C, Key.V, Key.B, Key.N, Key.M };

    private readonly bool[] panelHotkeyPressed = new bool[PanelHotkeyKeys.Length];

    /// <summary>Phím mở/đóng panel này vừa được nhấn trong frame này.</summary>
    public bool IsPanelHotkeyPressed(Panel_Hotkey hotkey)
    {
        int index = (int)hotkey;
        return index >= 0 && index < panelHotkeyPressed.Length && panelHotkeyPressed[index];
    }

    private readonly bool[] skillSlotPressed = new bool[SkillSlotCount];
    private readonly bool[] skillSlotHeld = new bool[SkillSlotCount];

    /// <summary>Phím của ô kỹ năng này vừa được nhấn trong frame này (index 0..9).</summary>
    public bool IsSkillSlotPressed(int index) => index >= 0 && index < SkillSlotCount && skillSlotPressed[index];

    /// <summary>Phím của ô kỹ năng này đang được giữ (index 0..9).</summary>
    public bool IsSkillSlotHeld(int index) => index >= 0 && index < SkillSlotCount && skillSlotHeld[index];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ lại khi chuyển Scene
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // Mỗi thiết bị đọc độc lập: thiếu chuột không làm mất input bàn phím và ngược lại.
        ReadMouse(Mouse.current);
        ReadKeyboard(Keyboard.current);
    }

    private void ReadMouse(Mouse mouse)
    {
        if (mouse == null)
        {
            LeftClickPressed = false;
            LeftClickHeld = false;
            RightClickPressed = false;
            RightClickHeld = false;
            MousePosition = Vector2.zero;
            return;
        }

        LeftClickPressed = mouse.leftButton.wasPressedThisFrame;
        LeftClickHeld = mouse.leftButton.isPressed;
        RightClickPressed = mouse.rightButton.wasPressedThisFrame;
        RightClickHeld = mouse.rightButton.isPressed;
        MousePosition = mouse.position.ReadValue();
    }

    private void ReadKeyboard(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            SpacePressed = false;
            for (int i = 0; i < SkillSlotCount; i++)
            {
                skillSlotPressed[i] = false;
                skillSlotHeld[i] = false;
            }
            for (int i = 0; i < panelHotkeyPressed.Length; i++) panelHotkeyPressed[i] = false;
            return;
        }

        SpacePressed = keyboard.spaceKey.wasPressedThisFrame;

        for (int i = 0; i < SkillSlotCount; i++)
        {
            var key = keyboard[SkillSlotKeys[i]];
            skillSlotPressed[i] = key.wasPressedThisFrame;
            skillSlotHeld[i] = key.isPressed;
        }

        for (int i = 0; i < panelHotkeyPressed.Length; i++)
        {
            panelHotkeyPressed[i] = keyboard[PanelHotkeyKeys[i]].wasPressedThisFrame;
        }
    }
}
