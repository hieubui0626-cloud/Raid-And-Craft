
using System.Collections.Generic;

/// <summary>
/// Kiểm tra tính tương thích giữa trang bị MainHand và OffHand.
/// Không tự thay đổi Equipment hoặc Inventory.
/// </summary>
public static class Equipment_Validator
{
    public static bool CanEquip(
        Equippable_Item_Data incoming,
        Equipment_Slot targetSlot,
        IReadOnlyDictionary<Equipment_Slot, Item_Instance> equipped,
        out string reason)
    {
        reason = string.Empty;

        if (incoming == null)
        {
            reason = "Trang bị không hợp lệ.";
            return false;
        }

        if (incoming is Weapon_Data weaponData)
        {
            if (!weaponData.CanEquipInSlot(targetSlot))
            {
                reason = "Vũ khí không tương thích với ô trang bị.";
                return false;
            }
        }
        else if (incoming.EquipSlot != targetSlot)
        {
            reason = "Trang bị không thuộc ô đang chọn.";
            return false;
        }

        if (targetSlot == Equipment_Slot.None)
        {
            reason = "Không thể trang bị vào ô None.";
            return false;
        }

        Weapon_Data incomingWeapon = incoming as Weapon_Data;

        bool incomingTwoHand =
            incomingWeapon != null &&
            incomingWeapon.handType == Weapon_Hand_Type.TwoHand;

        // Shield chỉ được trang bị ở OffHand.
        if (incomingWeapon != null &&
            incomingWeapon.weaponKind == Weapon_Kind.Shield &&
            targetSlot != Equipment_Slot.OffHand)
        {
            reason = "Shield chỉ có thể trang bị ở OffHand.";
            return false;
        }

        // Không cho phép vũ khí hai tay nằm ở OffHand.
        if (targetSlot == Equipment_Slot.OffHand &&
            incomingTwoHand)
        {
            reason = "Không thể trang bị vũ khí hai tay ở OffHand.";
            return false;
        }

        if (equipped == null)
            return true;

        equipped.TryGetValue(
            Equipment_Slot.MainHand,
            out Item_Instance mainHand);

        equipped.TryGetValue(
            Equipment_Slot.OffHand,
            out Item_Instance offHand);

        // Không được trang bị bất kỳ vật phẩm OffHand nào
        // khi MainHand đang giữ vũ khí hai tay.
        if (targetSlot == Equipment_Slot.OffHand &&
            mainHand != null)
        {
            Weapon_Data mainWeapon =
                mainHand.Base as Weapon_Data;

            if (mainWeapon != null &&
                mainWeapon.handType == Weapon_Hand_Type.TwoHand)
            {
                reason = "MainHand đang sử dụng vũ khí hai tay.";
                return false;
            }
        }

        // Không được đưa vũ khí hai tay vào MainHand
        // nếu OffHand đang có trang bị.
        if (targetSlot == Equipment_Slot.MainHand &&
            incomingTwoHand &&
            offHand != null)
        {
            reason = "Hãy tháo trang bị OffHand trước khi dùng vũ khí hai tay.";
            return false;
        }

        return true;
    }
}
