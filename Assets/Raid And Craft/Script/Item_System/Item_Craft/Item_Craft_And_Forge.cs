using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Item_Craft_And_Forge : MonoBehaviour
{
    [Header("Nguồn test")]
    [SerializeField] private Character_Equipment characterEquipment;

    [SerializeField] private int BaseRarityTier;

    [SerializeField] private Material_Item_Data BaseVAffix_material;
    [SerializeField] private Affix_Database affixDatabase;

    [SerializeField] private Material_Item_Data Influ_material;

    [SerializeField] private Equipment_Slot targetSlot = Equipment_Slot.MainHand;

    [Header("Blueprint Craft")]
    [SerializeField] private Item_Data blueprint;

    [ContextMenu("Debug: Roll Base")]
    private void DebugRollBase()
    {
        if (!TryGetTargetItem(out Item_Instance item))
        {
            return;
        }

        bool success = Roll_Rules.RollBase(
            item,
            BaseVAffix_material != null ? BaseVAffix_material.RarityTier : BaseRarityTier,
            new System.Random());

        if (!success)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Không thể roll Base.",
                this);

            return;
        }

        for (int i = 0; i < item.BaseModifiers.Count; i++)
        {
            Stat_Modifier_Entry modifier = item.BaseModifiers[i];

            Debug.Log(
                $"Base [{i}] {modifier.stat}: " +
                $"{modifier.value} ({modifier.modifierType})",
                this);
        }
    }

    [ContextMenu("Debug: Roll One Affix")]
    private void DebugRollAffix()
    {
        if (!TryGetTargetItem(out Item_Instance item))
        {
            return;
        }

        if (BaseVAffix_material == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Material.",
                this);

            return;
        }

        if (affixDatabase == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Affix Database.",
                this);

            return;
        }

        bool success = Roll_Rules.ApplyAffixRoll(
            BaseVAffix_material,
            item,
            affixDatabase,
            new System.Random(),
            out Item_Instance.Affix_Apply_Result result);

        if (!success)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Không thể roll Affix.",
                this);

            return;
        }

        Debug.Log(
            $"[{nameof(Item_Craft_And_Forge)}] Roll một dòng Affix. " +
            $"Kết quả: {result}. " +
            $"Tổng số dòng: {item.AffixLines.Count}.",
            this);
    }

    [ContextMenu("Debug: Roll Influ")]
    private void DebugRollInflu()
    {
        if (!TryGetTargetItem(out Item_Instance item))
        {
            return;
        }

        if (Influ_material == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Material.",
                this);

            return;
        }

        bool success = Roll_Rules.TryRollInflu(
            Influ_material,
            new System.Random(),
            out Influ_Data influ,
            out float roll,
            out int variantIndex);

        if (!success || influ == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Không thể roll Influ.",
                this);

            return;
        }

        item.SetInflu(
            influ,
            roll,
            variantIndex);

        Debug.Log(
            $"[{nameof(Item_Craft_And_Forge)}] Đã roll Influ " +
            $"'{influ.DisplayName}' với giá trị {roll:0.##}.",
            this);
    }

    private bool TryGetTargetItem(out Item_Instance item)
    {
        item = null;

        if (characterEquipment == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Character Equipment.",
                this);

            return false;
        }

        item = characterEquipment.GetEquipped(targetSlot);

        if (item == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Không có item ở ô " +
                $"{targetSlot}.",
                this);

            return false;
        }

        return true;
    }

    [ContextMenu("Debug: Use Blueprint")]
    private void DebugUseBlueprint()
    {
        if (blueprint == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Blueprint.",
                this);

            return;
        }

        if (BaseVAffix_material == null)
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Chưa chọn Material.",
                this);

            return;
        }

        if (!blueprint.CanBeUsedFor(Item_Usage.Crafting))
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Blueprint " +
                $"'{blueprint.DisplayName}' không được phép dùng cho Crafting.",
                this);

            return;
        }

        if (!BaseVAffix_material.CanBeUsedFor(Item_Usage.Crafting))
        {
            Debug.LogWarning(
                $"[{nameof(Item_Craft_And_Forge)}] Material " +
                $"'{BaseVAffix_material.DisplayName}' không được phép dùng cho Crafting.",
                this);

            return;
        }

        Debug.Log(
            $"[{nameof(Item_Craft_And_Forge)}] Blueprint và Material hợp lệ cho Crafting. " +
            $"Blueprint: '{blueprint.DisplayName}', " +
            $"Material: '{BaseVAffix_material.DisplayName}'.",
            this);
    }
}