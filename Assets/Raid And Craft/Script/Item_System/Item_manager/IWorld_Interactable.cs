using UnityEngine;

/// <summary>
/// Thứ nằm trong thế giới mà click chuột trái vào thì Player đi tới và tương tác (nhặt vật phẩm, nói chuyện với NPC bán hàng...).
/// World_Item_Picker trên Player lo việc đi tới và gọi Interact khi đủ gần. Cần có Collider (Is Trigger cũng được) để bấm trúng.
/// </summary>
public interface IWorld_Interactable
{
    /// <summary>Vị trí để Player đi tới.</summary>
    Vector3 Position { get; }

    /// <summary>Khoảng cách (mét) tối đa tới Position thì tương tác được.</summary>
    float InteractRange { get; }

    /// <summary>Gọi khi Player đã tới đủ gần.</summary>
    void Interact(World_Item_Picker interactor);
}
