using UnityEngine;

/// <summary>
/// Lớp gốc của vật phẩm TIÊU HAO dùng một lần (thuốc, thức ăn, cuộn giấy...). Công dụng: QuickUse (đặt lên thanh quick use).
/// Thêm loại mới: kế thừa lớp này, ghi đè TryUse, đặt [CreateAssetMenu(menuName = "Raid And Craft/Items/&lt;Tên&gt;")].
/// Nên đặt Stack Type = Stackable và Max Stack > 1 cho asset.
/// Hồi chiêu: kéo một Consumable_Family_Data vào ô Family; mọi vật phẩm cùng family dùng chung thời gian hồi (chỉnh trong asset family).
/// Không có Family = không hồi chiêu.
/// </summary>
public abstract class Consumable_Item_Data : Item_Data
{
    [Header("Hồi chiêu dùng chung")]
    [Tooltip("Family của vật phẩm. Cùng family thì chung hồi chiêu; thời gian hồi chỉnh trong asset Family. Bỏ trống = không hồi chiêu.")]
    [SerializeField] private Consumable_Family_Data family;

    public Consumable_Family_Data Family => family;

    public override Item_Usage SupportedUsage => Item_Usage.QuickUse;

    /// <summary>
    /// Tác dụng lên người dùng. Trả về false nếu chưa dùng được (đã chết, máu/mana đã đầy...) để không mất vật phẩm.
    /// </summary>
    public abstract bool TryUse(Character_Stats user);

    /// <summary>Số giây hồi chiêu còn lại của family cho người dùng này (0 = sẵn sàng hoặc không có family).</summary>
    public float GetCooldownRemaining(Character_Stats user)
    {
        if (family == null) return 0f;

        Consumable_Cooldown_Tracker tracker = Consumable_Cooldown_Tracker.Find(user);
        return tracker != null ? tracker.GetRemaining(family) : 0f;
    }

    /// <summary>Phần hồi chiêu còn lại 0..1 để vẽ lớp phủ (1 = vừa dùng, 0 = sẵn sàng).</summary>
    public float GetCooldownFraction(Character_Stats user)
    {
        if (family == null) return 0f;

        Consumable_Cooldown_Tracker tracker = Consumable_Cooldown_Tracker.Find(user);
        return tracker != null ? tracker.GetFraction(family) : 0f;
    }

    public bool IsOnCooldown(Character_Stats user)
    {
        return GetCooldownRemaining(user) > 0f;
    }

    /// <summary>
    /// Dùng một vật phẩm lấy từ kho: có trong kho, không đang hồi chiêu, TryUse thành công thì trừ 1 và bắt đầu hồi chiêu của family.
    /// Thanh quick use chỉ cần gọi hàm này khi bấm phím.
    /// </summary>
    public bool TryConsume(Character_Stats user, IItem_Container container)
    {
        if (user == null || container == null || !container.Has(this)) return false;
        if (IsOnCooldown(user)) return false;
        if (!TryUse(user)) return false;
        if (!container.TryRemove(this, 1)) return false;

        if (family != null) Consumable_Cooldown_Tracker.GetOrAdd(user).StartCooldown(family);
        return true;
    }
}
