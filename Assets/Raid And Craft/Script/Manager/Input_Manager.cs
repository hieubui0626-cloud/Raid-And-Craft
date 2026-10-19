using UnityEngine;
using UnityEngine.InputSystem;

public class Input_Manager : MonoBehaviour
{
    // Singleton để gọi nhanh từ các script khác: InputManager.Instance
    public static Input_Manager Instance { get; private set; }

    [Header("Mouse Inputs")]
    public bool LeftClickPressed { get; private set; }
    public bool LeftClickHeld { get; private set; }
    public bool RightClickPressed { get; private set; }
    // Cách lấy vị trí chuột mới
    public Vector3 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;

    [Header("Keyboard Inputs")]
    
    public bool Space { get; private set; }

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ lại khi chuyển Scene
    }

    private void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // 2. Đọc dữ liệu Chuột mới
        LeftClickPressed = Mouse.current.leftButton.wasPressedThisFrame;
        LeftClickHeld = Mouse.current.leftButton.isPressed;
        RightClickPressed = Mouse.current.rightButton.wasPressedThisFrame;

        // 2. Đọc dữ liệu Bàn phím (Hỗ trợ cả WASD hoặc Phím mũi tên)


        // Đọc nút Nhảy (Mặc định là Phím Cách / Space)
        Space = Keyboard.current.spaceKey.wasPressedThisFrame;
    }
}
