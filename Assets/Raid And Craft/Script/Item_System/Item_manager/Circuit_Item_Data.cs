/// <summary>
/// Vật phẩm lắp được vào ô của Magic Circuit (Rune, Spell Stone, và các loại sau này).
/// Muốn thêm loại mới: kế thừa lớp này, viết Apply (tác động khi lắp) và Remove (thu hồi khi gỡ).
/// Magic_Circuit không cần biết loại cụ thể.
/// </summary>
public abstract class Circuit_Item_Data : Item_Data
{
    public override Item_Usage SupportedUsage => Item_Usage.MagicCircuit;

    /// <summary>
    /// Áp dụng tác động khi lắp vào ô.
    /// source: khoá riêng của ô này, dùng làm Source cho modifier để gỡ sạch được.
    /// effectMultiplier: hệ số hiệu ứng của vùng (zone) chứa ô, 1 = không đổi.
    /// </summary>
    public abstract void Apply(Magic_Circuit circuit, object source, float effectMultiplier);

    /// <summary>Thu hồi mọi tác động đã áp dụng bằng Apply (cùng source).</summary>
    public abstract void Remove(Magic_Circuit circuit, object source);
}
