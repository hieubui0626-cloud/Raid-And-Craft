/// <summary>
/// Lớp gốc của vật phẩm TIÊU HAO dùng một lần (thuốc, thức ăn, cuộn giấy...). Công dụng: QuickUse (đặt lên thanh quick use).
/// Thêm loại mới: kế thừa lớp này, ghi đè TryUse, đặt [CreateAssetMenu(menuName = "Raid And Craft/Items/&lt;Tên&gt;")].
/// Nên đặt Stack Type = Stackable và Max Stack > 1 cho asset.
/// Chưa có hồi chiêu giữa hai lần dùng (làm cùng thanh quick use).
/// </summary>
public abstract class Consumable_Item_Data : Item_Data
{
    public override Item_Usage SupportedUsage => Item_Usage.QuickUse;

    /// <summary>
    /// Tác dụng lên người dùng. Trả về false nếu chưa dùng được (đã chết, máu/mana đã đầy...) để không mất vật phẩm.
    /// </summary>
    public abstract bool TryUse(Character_Stats user);

    /// <summary>Dùng một vật phẩm lấy từ kho: có trong kho, TryUse thành công thì trừ 1. Thanh quick use chỉ cần gọi hàm này khi bấm phím.</summary>
    public bool TryConsume(Character_Stats user, IItem_Container container)
    {
        if (user == null || container == null || !container.Has(this)) return false;
        if (!TryUse(user)) return false;

        return container.TryRemove(this, 1);
    }
}
