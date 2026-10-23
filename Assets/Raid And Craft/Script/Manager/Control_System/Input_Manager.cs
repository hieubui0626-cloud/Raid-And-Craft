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
            return;
        }

        SpacePressed = keyboard.spaceKey.wasPressedThisFrame;
    }
}
