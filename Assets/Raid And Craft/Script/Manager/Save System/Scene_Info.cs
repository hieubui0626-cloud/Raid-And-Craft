using UnityEngine;

/// <summary>Loại Scene, dùng để quyết định có lưu vị trí Player hay không.</summary>
public enum Scene_Kind
{
    /// <summary>Scene "base" (thị trấn/căn cứ): lưu vị trí Player, mở game lại sẽ vào đúng Scene này, đúng chỗ đã đứng.</summary>
    Base = 0,

    /// <summary>Scene tạm (Arena, map phiêu lưu, World Map...): KHÔNG lưu vị trí. Thoát game ở đây thì lần sau vào lại Scene base gần nhất.</summary>
    Instance = 1,
}

/// <summary>
/// Đặt MỘT object trong mỗi Scene có thể có Player (không bắt buộc, Scene không có component này được coi là Base).
/// Chỉ khai báo "Scene này là loại gì"; việc lưu/nạp nằm ở Player_Save và Save_Manager.
/// Mọi Scene dùng cho save (cả Base lẫn Instance) cần nằm trong Build Settings để nạp được bằng tên.
/// </summary>
[DisallowMultipleComponent]
public class Scene_Info : MonoBehaviour
{
    [SerializeField] private Scene_Kind kind = Scene_Kind.Base;

    [Tooltip("Cho phép dịch chuyển nhanh (Map) khi đang ở Scene này. Thường tắt ở Scene tạm (Arena, hầm ngục).")]
    [SerializeField] private bool allowFastTravel = true;

    public Scene_Kind Kind => kind;
    public bool AllowFastTravel => allowFastTravel;
}
