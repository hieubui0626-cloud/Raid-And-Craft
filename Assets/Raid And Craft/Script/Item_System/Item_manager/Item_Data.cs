using UnityEngine;

/// <summary>
/// Dữ liệu gốc của MỌI vật phẩm xếp được trong Inventory (ScriptableObject).
/// Chỉ chứa thông tin dùng chung; từng loại vật phẩm kế thừa lớp này để thêm dữ liệu riêng
/// (ví dụ Rune_Data, Spell_Stone_Data qua Circuit_Item_Data).
/// Quy tắc: itemId là khoá duy nhất dùng để save/load, KHÔNG đổi sau khi đã có save.
/// Lưu ý: Weapon_Data hiện chưa kế thừa lớp này (làm cùng lúc đưa vũ khí vào Inventory).
/// </summary>
public abstract class Item_Data : ScriptableObject
{
    [Header("Thông tin chung")]
    [Tooltip("Khoá duy nhất để save/load. Để trống thì tự lấy tên asset.")]
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Tooltip("Số lượng tối đa trong một ô Inventory (1 = không xếp chồng).")]
    [SerializeField, Min(1)] private int maxStack = 1;

    public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => Mathf.Max(1, maxStack);

#if UNITY_EDITOR
    // Tự điền itemId bằng tên asset nếu để trống, để khỏi quên.
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId)) itemId = name;
    }
#endif
}
