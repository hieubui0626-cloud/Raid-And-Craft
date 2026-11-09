using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đổi qua lại giữa vật phẩm trong game (Item_Instance) và dữ liệu lưu (Item_Instance_Save).
/// Chỉ lưu id chuỗi (itemId, InfluId) và các số đã roll; asset tra lại qua Item_Database / Influ_Database lúc nạp.
/// </summary>
public static class Save_Codec
{
    public static Item_Instance_Save ToSave(Item_Instance instance)
    {
        var save = new Item_Instance_Save
        {
            instanceId = instance.InstanceId,
            itemId = instance.Base.ItemId,
            tier = instance.Tier,
            influId = instance.Influ != null ? instance.Influ.InfluId : string.Empty,
            influRoll = instance.InfluRoll,
        };

        foreach (Stat_Modifier_Entry m in instance.BaseModifiers) save.baseModifiers.Add(ToSave(m));
        foreach (Stat_Modifier_Entry m in instance.AffixModifiers) save.affixModifiers.Add(ToSave(m));
        return save;
    }

    /// <summary>
    /// Dựng lại món trang bị từ bản save, giữ nguyên InstanceId và mọi roll.
    /// Trả về null (và log lỗi) nếu loại vật phẩm không còn trong Item_Database hoặc không phải loại dùng instance.
    /// Influ không còn trong Influ_Database: vẫn dựng món nhưng không có Influ (log cảnh báo).
    /// </summary>
    public static Item_Instance FromSave(Item_Instance_Save save, Item_Database items, Influ_Database influs)
    {
        if (save == null || items == null) return null;

        Item_Data baseItem = items.Find(save.itemId);
        if (baseItem == null)
        {
            Debug.LogError($"[{nameof(Save_Codec)}] Không tìm thấy vật phẩm '{save.itemId}' trong Item_Database nên món này bị bỏ qua. " +
                           "Thêm asset đó vào Item_Database (chuột phải -> Thu thập mọi vật phẩm trong project).");
            return null;
        }

        if (!baseItem.UsesInstance)
        {
            Debug.LogError($"[{nameof(Save_Codec)}] '{save.itemId}' không phải trang bị (không dùng Item_Instance) nên bị bỏ qua.");
            return null;
        }

        var instance = new Item_Instance(baseItem, save.instanceId) { Tier = Mathf.Max(1, save.tier) };
        AddModifiers(instance.BaseModifiers, save.baseModifiers, save.itemId);
        AddModifiers(instance.AffixModifiers, save.affixModifiers, save.itemId);

        if (!string.IsNullOrEmpty(save.influId))
        {
            Influ_Data influ = influs != null ? influs.Find(save.influId) : null;
            if (influ == null)
            {
                Debug.LogError($"[{nameof(Save_Codec)}] Không tìm thấy Influ '{save.influId}' trong Influ_Database nên món '{save.itemId}' mất hiệu ứng này. " +
                               "Thêm asset Influ vào Influ_Database.");
            }
            else
            {
                instance.Influ = influ;
                instance.InfluRoll = save.influRoll;
            }
        }

        return instance;
    }

    private static Modifier_Save_Entry ToSave(Stat_Modifier_Entry m)
    {
        return new Modifier_Save_Entry { stat = (int)m.stat, modifierType = (int)m.modifierType, value = m.value };
    }

    private static void AddModifiers(List<Stat_Modifier_Entry> target, List<Modifier_Save_Entry> source, string itemId)
    {
        foreach (Modifier_Save_Entry m in source)
        {
            // Bỏ qua dòng có chỉ số/kiểu đã bị xoá khỏi enum ở phiên bản game mới.
            if (!Enum.IsDefined(typeof(Player_Stat_Type), m.stat) || !Enum.IsDefined(typeof(Player_Stat_Modifier_Type), m.modifierType))
            {
                Debug.LogWarning($"[{nameof(Save_Codec)}] Bỏ qua dòng chỉ số không còn hợp lệ trên '{itemId}' (stat {m.stat}, kiểu {m.modifierType}).");
                continue;
            }

            target.Add(new Stat_Modifier_Entry
            {
                stat = (Player_Stat_Type)m.stat,
                modifierType = (Player_Stat_Modifier_Type)m.modifierType,
                value = m.value,
            });
        }
    }
}
