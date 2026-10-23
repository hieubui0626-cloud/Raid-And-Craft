/// <summary>
/// Loại sát thương. Mỗi loại ứng với một chỉ số tấn công (xem Combat_Formulas.GetOffenceStat)
/// và một cách giảm sát thương bên phòng thủ (Physic -> Armor, còn lại -> Resistance tương ứng).
/// QUY TẮC: không đổi số của mục đã có, chỉ thêm mục mới ở CUỐI enum.
/// </summary>
public enum Damage_Type
{
    Physic = 0,
    Magic = 1,
    Fire = 2,
    Cold = 3,
    Lightning = 4,
    Chaos = 5,
}
