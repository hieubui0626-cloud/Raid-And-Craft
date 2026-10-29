using System;

/// <summary>
/// CÔNG DỤNG của vật phẩm: các tính năng trong game được phép nhận vật phẩm đó.
/// Mỗi tính năng (mặc trang bị, lắp Magic Circuit, thanh quick use...) hỏi item.CanBeUsedFor(Item_Usage.X) trước khi nhận,
/// nên tính năng không cần biết loại vật phẩm cụ thể.
///
/// Loại vật phẩm quyết định công dụng nó hỗ trợ (Item_Data.SupportedUsage); trong Inspector có ô Blocked Usage để CHẶN bớt.
/// Thêm công dụng mới (ví dụ Crafting): thêm vào CUỐI enum với giá trị là luỹ thừa của 2 tiếp theo (8, 16, 32...),
/// cho các loại vật phẩm liên quan trả cờ đó trong SupportedUsage, rồi tính năng mới gọi CanBeUsedFor.
/// QUY TẮC: giá trị số được serialize trong asset. Không đổi số của mục đã có, không chèn vào giữa.
/// </summary>
[Flags]
public enum Item_Usage
{
    None = 0,
    /// <summary>Mặc lên người (Character_Equipment).</summary>
    Equip = 1,
    /// <summary>Lắp vào ô Magic Circuit (Magic_Circuit).</summary>
    MagicCircuit = 2,
    /// <summary>Đặt lên thanh quick use để bấm phím dùng (làm sau).</summary>
    QuickUse = 4,
}
