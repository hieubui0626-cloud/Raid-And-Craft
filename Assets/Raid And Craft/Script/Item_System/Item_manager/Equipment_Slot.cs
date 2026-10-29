// Ô trang bị trên người nhân vật. Dùng cho Equippable_Item_Data.EquipSlot và Character_Equipment (mỗi ô một món).
// QUY TẮC: các giá trị số được serialize trong asset. Không đổi số của mục đã có,
// không chèn vào giữa. Chỉ thêm mục mới ở CUỐI enum.
public enum Equipment_Slot
{
    None = 0,      // Không phải trang bị
    MainHand = 1,  // Vũ khí chính
    OffHand = 2,   // Khiên, vũ khí phụ
    Head = 3,
    Body = 4,
    Hands = 5,
    Legs = 6,
    Amulet = 7,
    Ring_Right = 8,
    Ring_Left = 9,
    Belr = 10,

    // Thêm sau này: Belt, Cloak, Trinket...
}
