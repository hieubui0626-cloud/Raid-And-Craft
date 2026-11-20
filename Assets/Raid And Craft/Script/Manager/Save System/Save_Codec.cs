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
        foreach (Affix_Line line in instance.AffixLines) save.affixLines.Add(ToSave(line));
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
        AddAffixLines(instance, save);

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

    private static Affix_Line_Save ToSave(Affix_Line line)
    {
        var save = new Affix_Line_Save { affixId = line.affixId, format = line.format, tier = line.tier };

        for (int i = 0; i < line.Modifiers.Count; i++)
        {
            Stat_Modifier_Entry m = line.Modifiers[i];
            save.components.Add(new Affix_Component_Save
            {
                stat = (int)m.stat,
                modifierType = (int)m.modifierType,
                value = m.value,
                minRoll = i < line.MinRolls.Count ? line.MinRolls[i] : m.value,
                maxRoll = i < line.MaxRolls.Count ? line.MaxRolls[i] : m.value,
            });
        }

        return save;
    }

    // Đọc dòng Affix mới (nhiều chỉ số) hoặc dòng P1 (một chỉ số); bản save cũ nhất (chỉ có affixModifiers) chuyển thành dòng không id.
    private static void AddAffixLines(Item_Instance instance, Item_Instance_Save save)
    {
        foreach (Affix_Line_Save a in save.affixLines)
        {
            if (instance.AffixLines.Count >= Item_Instance.MaxAffixLines) break;

            var entries = new List<Stat_Modifier_Entry>();
            var mins = new List<float>();
            var maxs = new List<float>();

            if (a.components.Count > 0)
            {
                foreach (Affix_Component_Save c in a.components)
                {
                    if (!IsValid(c.stat, c.modifierType, save.itemId)) continue;

                    entries.Add(new Stat_Modifier_Entry { stat = (Player_Stat_Type)c.stat, modifierType = (Player_Stat_Modifier_Type)c.modifierType, value = c.value });
                    mins.Add(c.minRoll);
                    maxs.Add(c.maxRoll);
                }
            }
            else if (IsValid(a.stat, a.modifierType, save.itemId))
            {
                entries.Add(new Stat_Modifier_Entry { stat = (Player_Stat_Type)a.stat, modifierType = (Player_Stat_Modifier_Type)a.modifierType, value = a.value });
                mins.Add(a.minRoll);
                maxs.Add(a.maxRoll);
            }

            if (entries.Count == 0) continue;
            instance.AffixLines.Add(new Affix_Line(entries, mins, maxs, a.affixId, a.tier, a.format));
        }

        if (save.affixLines.Count > 0 || save.affixModifiers.Count == 0) return;

        var legacy = new List<Stat_Modifier_Entry>();
        AddModifiers(legacy, save.affixModifiers, save.itemId);
        foreach (Stat_Modifier_Entry m in legacy)
        {
            if (instance.AffixLines.Count >= Item_Instance.MaxAffixLines) break;
            instance.AffixLines.Add(new Affix_Line(m, null, m.value, m.value, 1));
        }
    }

    private static bool IsValid(int stat, int modifierType, string itemId)
    {
        if (Enum.IsDefined(typeof(Player_Stat_Type), stat) && Enum.IsDefined(typeof(Player_Stat_Modifier_Type), modifierType)) return true;

        Debug.LogWarning($"[{nameof(Save_Codec)}] Bỏ qua dòng affix không còn hợp lệ trên '{itemId}' (stat {stat}, kiểu {modifierType}).");
        return false;
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
