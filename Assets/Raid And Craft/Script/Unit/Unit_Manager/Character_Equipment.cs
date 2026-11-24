using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Trang bị đang mang của MỘT nhân vật, theo từng ô (Equipment_Slot): mỗi ô một món (Item_Instance). Gắn cùng object với Character_Stats.
/// - Player mặc từ Inventory: TryEquip lấy món từ kho, món cũ ở ô đó về lại kho; TryUnequip trả món về kho.
/// - Enemy/thử nghiệm/load save mặc thẳng, không qua kho: EquipDirect / UnequipDirect.
/// - Món mặc vào cộng chỉ số Base và Affix của chính món đó (Source = instance) và áp hiệu ứng Influ của nó;
///   tháo ra gỡ sạch theo Source đó.
/// - Implement ISkill_Weapon_Source: Damage_Effect (bật Use Weapon Attack) lấy cách đánh/loại sát thương/hệ số từ vũ khí ở ô MainHand.
///   Ô MainHand trống = tay không.
/// - Mặc được hay không do Item_Usage.Equip quyết định (item.CanBeUsedFor).
/// - Đổi ô Weapon/Starting Equipment trong Inspector lúc đang Play KHÔNG cập nhật chỉ số, hãy dùng các hàm Equip/Unequip.
/// - Player_Save lưu từng món (cả roll Base/Affix/Influ) và nạp lại bằng UnequipAllDirect + EquipDirect.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Character_Equipment : MonoBehaviour, ISkill_Weapon_Source
{
    /// <summary>Bắn khi MỘT ô đổi món: (ô, món cũ hoặc null, món mới hoặc null). UI trang bị vẽ lại ô đó.</summary>
    public event Action<Equipment_Slot, Item_Instance, Item_Instance> OnEquipmentChanged;

    /// <summary>Bắn khi ô MainHand đổi (tham số là vũ khí mới, null = tay không hoặc món MainHand không phải vũ khí).</summary>
    public event Action<Weapon_Data> OnWeaponChanged;

    [SerializeField] private Character_Stats stats;

    [Tooltip("Kho để lấy món mặc vào / trả món tháo ra. Bỏ trống thì tự tìm Inventory trên cùng object (Enemy không cần).")]
    [SerializeField] private Inventory inventory;

    [Header("Trang bị khởi đầu (mặc thẳng lúc Awake, không lấy từ kho; mỗi cái thành một món mặc định từ asset)")]
    [Tooltip("Vũ khí cầm lúc bắt đầu, mặc vào ô MainHand. Bỏ trống = tay không.")]
    [SerializeField] private Weapon_Data weapon;

    [Tooltip("Các trang bị khác mặc sẵn (giáp, mũ, nhẫn...). Mỗi món vào ô Equip Slot của nó.")]
    [SerializeField] private List<Equippable_Item_Data> startingEquipment = new List<Equippable_Item_Data>();

    private readonly Dictionary<Equipment_Slot, Item_Instance> equipped = new Dictionary<Equipment_Slot, Item_Instance>();
    private bool initialized;
    private bool nextAlternateIsOffHand;

    [Header("Attack Source Debug")]
    [SerializeField] private bool showAttackSourceDebug = false;

    [Header("Material Affix Pool Debug")]
    [SerializeField] private Material_Item_Data debugMaterial;
    [SerializeField] private Affix_Database debugAffixDatabase;
    [SerializeField] private Equipment_Slot debugMaterialSlot = Equipment_Slot.MainHand;

    private int alternateAttackCount;
    private string lastAttackSource = "None";
    private string lastAttackWeapon = "None";
    private string lastAttackDamage = "None";

    /// <summary>Mọi món đang mặc (ô -> món). Ô trống không có trong danh sách.</summary>
    public IReadOnlyDictionary<Equipment_Slot, Item_Instance> Equipped => equipped;

    /// <summary>Vũ khí (loại) đang cầm ở ô MainHand (null nếu tay không).</summary>
    public Weapon_Data Weapon => GetEquipped(Equipment_Slot.MainHand)?.Base as Weapon_Data;

    public Item_Instance GetEquipped(Equipment_Slot slot)
    {
        EnsureInitialized();
        return equipped.TryGetValue(slot, out Item_Instance item) ? item : null;
    }

    private bool TryBuildWeaponAttack(
        Skill_Weapon_Source_Slot sourceSlot,
        out Skill_Weapon_Attack attack)
    {
        Weapon_Data w = null;

        switch (sourceSlot)
        {
            case Skill_Weapon_Source_Slot.MainHand:
                w = Weapon;
                break;

            case Skill_Weapon_Source_Slot.OffHand:
                w = GetEquipped(Equipment_Slot.OffHand)?.Base
                    as Weapon_Data;
                break;
        }

        if (w == null)
        {
            attack = default;
            return false;
        }

        Equipment_Slot equipmentSlot =
            sourceSlot == Skill_Weapon_Source_Slot.MainHand
                ? Equipment_Slot.MainHand
                : Equipment_Slot.OffHand;

        if (!w.CanAttackFromSlot(equipmentSlot))
        {
            attack = default;
            return false;
        }

        Equipment_Slot oppositeSlot =
            sourceSlot == Skill_Weapon_Source_Slot.MainHand
                ? Equipment_Slot.OffHand
                : Equipment_Slot.MainHand;

        Item_Instance oppositeWeapon = GetEquipped(oppositeSlot);

        attack = new Skill_Weapon_Attack(
            w.attackDelivery,
            w.damageTypes,
            w.damageMultiplier,
            w.attackAnimation,
            w.attackArchetype,
            w.weaponFamily,
            w.attackSources,
            oppositeWeapon,
            sourceSlot
);

        return true;
    }

    public bool TryGetWeaponAttack(out Skill_Weapon_Attack attack)
    {
        return TryBuildWeaponAttack(
            Skill_Weapon_Source_Slot.MainHand,
            out attack
        );
    }

    public bool TryGetWeaponAttack(
        Skill_Weapon_Source_Slot sourceSlot,
        out Skill_Weapon_Attack attack)
    {
        return TryBuildWeaponAttack(sourceSlot, out attack);
    }

    public bool TryGetAlternateWeaponAttack(
        out Skill_Weapon_Attack attack)
    {
        bool hasMain = TryBuildWeaponAttack(
            Skill_Weapon_Source_Slot.MainHand,
            out Skill_Weapon_Attack mainAttack
        );

        bool hasOff = TryBuildWeaponAttack(
            Skill_Weapon_Source_Slot.OffHand,
            out Skill_Weapon_Attack offAttack
        );

        if (!hasMain && !hasOff)
        {
            attack = default;
            nextAlternateIsOffHand = false;
            return false;
        }

        if (hasMain && !hasOff)
        {
            attack = mainAttack;
            lastAttackSource = "MAIN HAND";
            lastAttackWeapon = GetEquipped(Equipment_Slot.MainHand)
                ?.DisplayName ?? "Unknown";

            lastAttackDamage =
                attack.DamageTypes != null
                    ? string.Join(", ", attack.DamageTypes)
                    : "None";

            nextAlternateIsOffHand = false;
            alternateAttackCount++;
            return true;
        }

        if (!hasMain && hasOff)
        {
            attack = offAttack;
            lastAttackSource = "OFF HAND";
            lastAttackWeapon = GetEquipped(Equipment_Slot.OffHand)
                ?.DisplayName ?? "Unknown";

            lastAttackDamage =
                attack.DamageTypes != null
                    ? string.Join(", ", attack.DamageTypes)
                    : "None";

            nextAlternateIsOffHand = false;
            alternateAttackCount++;
            return true;
        }

        if (nextAlternateIsOffHand)
        {
            attack = offAttack;
            lastAttackSource = "OFF HAND";
            lastAttackWeapon = GetEquipped(Equipment_Slot.OffHand)
                ?.DisplayName ?? "Unknown";

            nextAlternateIsOffHand = false;
        }
        else
        {
            attack = mainAttack;
            lastAttackSource = "MAIN HAND";
            lastAttackWeapon = GetEquipped(Equipment_Slot.MainHand)
                ?.DisplayName ?? "Unknown";

            nextAlternateIsOffHand = true;
        }

        alternateAttackCount++;

        lastAttackDamage =
            attack.DamageTypes != null
                ? string.Join(", ", attack.DamageTypes)
                : "None";

        return true;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    // Khởi tạo lười: script khác gọi sớm hơn Awake của Character_Equipment vẫn an toàn.
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (stats == null) stats = GetComponent<Character_Stats>();
        if (inventory == null) inventory = GetComponent<Inventory>();

        if (weapon != null) EquipDirect(weapon);

        foreach (Equippable_Item_Data item in startingEquipment)
        {
            if (item == null) continue;
            if (!EquipDirect(item)) Debug.LogWarning($"[{nameof(Character_Equipment)}] Không mặc được trang bị khởi đầu '{item.name}' (Equip Slot = None hoặc bị chặn Equip).", this);
        }
    }

    // ------------------------------------------------------------------
    // MẶC / THÁO QUA KHO (Player)
    // ------------------------------------------------------------------

    /// <summary>Gợi ý cho UI làm xám nút: món này mặc được không (chưa đảm bảo TryEquip thành công: còn phụ thuộc kho).</summary>
    public bool CanEquip(Item_Data item)
    {
        if (item == null)
            return false;

        if (!(item is Equippable_Item_Data equippable))
            return false;

        return CanEquip(item, equippable.EquipSlot);
    }

    /// <summary>Loại vật phẩm này mặc được không (ô trang bị hợp lệ và công dụng Equip không bị chặn).</summary>
    public bool CanEquip(Item_Data item, Equipment_Slot targetSlot)
    {
        if (!(item is Equippable_Item_Data equippable))
            return false;

        if (targetSlot == Equipment_Slot.None)
            return false;

        if (!equippable.CanBeUsedFor(Item_Usage.Equip))
            return false;

        // Weapon: kiểm tra slot bằng Weapon Data.
        if (equippable is Weapon_Data weaponData)
        {
            if (!weaponData.CanEquipInSlot(targetSlot))
                return false;
        }
        else
        {
            // Các trang bị không phải Weapon vẫn sử dụng EquipSlot cũ.
            if (equippable.EquipSlot != targetSlot)
                return false;
        }

        return Equipment_Validator.CanEquip(
            equippable,
            targetSlot,
            equipped,
            out _);
    }

    /// <summary>
    /// Lấy đúng món này từ kho mặc vào ô của nó; món cũ ở ô đó về kho. False và không đổi gì nếu:
    /// không mặc được, không có kho, kho không có món đó, món đó đang mặc rồi, hoặc kho hết chỗ nhận món cũ.
    /// </summary>
    public bool TryEquip(Item_Instance item)
    {
        EnsureInitialized();
        if (!CanEquip(item.Base) || inventory == null || !inventory.HasInstance(item)) return false;

        Equipment_Slot slot = item.Base.EquipSlot;
        equipped.TryGetValue(slot, out Item_Instance oldItem);
        if (oldItem == item) return false;

        // Món mới ra khỏi kho trước, món cũ vào kho sau; đổi cùng lúc nên kho đầy vẫn đổi được nếu chỗ trống của món mới đủ cho món cũ.
        bool moved = oldItem == null
            ? inventory.TryRemoveInstance(item)
            : inventory.TryReplaceInstance(item, oldItem);
        if (!moved) return false;

        SetSlot(slot, item);
        return true;
    }

    /// <summary>Tháo món ở ô về kho. False nếu ô trống, không có kho, hoặc kho hết chỗ.</summary>
    public bool TryUnequip(Equipment_Slot slot)
    {
        EnsureInitialized();
        if (inventory == null || !equipped.TryGetValue(slot, out Item_Instance item)) return false;
        if (!inventory.CanAddInstance(item)) return false;

        SetSlot(slot, null);
        inventory.AddInstance(item);
        return true;
    }

    // ------------------------------------------------------------------
    // KÉO THẢ TỪ/ĐẾN CON TRỎ (UI): món đang cầm đã rời khỏi kho
    // ------------------------------------------------------------------

    /// <summary>
    /// Tháo món ở ô thả vào đúng ô lưới của kho (góc trên-trái tại origin). False nếu ô trang bị trống, không có kho,
    /// hoặc vùng đó không trống/không nằm trọn trong lưới (không đổi gì). Đổi chỗ với món khác thì dùng TryTakeEquipped rồi Inventory.TryDrop.
    /// </summary>
    public bool TryUnequipTo(Equipment_Slot slot, Vector2Int origin)
    {
        EnsureInitialized();
        if (inventory == null || !equipped.TryGetValue(slot, out Item_Instance item)) return false;
        if (!inventory.CanFit(item.Base, origin)) return false;

        SetSlot(slot, null);
        if (inventory.TryPlace(new Grid_Placement(item.Base, item, 1, origin), origin)) return true;

        SetSlot(slot, item); // không xảy ra vì đã kiểm CanFit; giữ nguyên trạng thái nếu có lỗi bất ngờ
        return false;
    }

    /// <summary>
    /// Nhấc món ở ô lên con trỏ (UI kéo từ ô trang bị): tháo khỏi người, KHÔNG trả về kho. False nếu ô trống.
    /// Thả xuống lưới bằng Inventory.TryDrop(new Grid_Placement(taken.Base, taken, 1, default), origin, out ...).
    /// </summary>
    public bool TryTakeEquipped(Equipment_Slot slot, out Item_Instance taken)
    {
        EnsureInitialized();
        if (!equipped.TryGetValue(slot, out taken)) return false;

        SetSlot(slot, null);
        return true;
    }

    /// <summary>
    /// Thả món đang cầm trên con trỏ (đã rời khỏi kho) lên ô trang bị targetSlot. Món phải mặc được và thuộc đúng ô đó.
    /// Món cũ ở ô (nếu có) được trả về qua previous để con trỏ cầm tiếp (null = con trỏ trống).
    /// False (không đổi gì, previous = held) nếu sai ô, không mặc được, hoặc món đó vẫn còn trong kho.
    /// </summary>

    public bool TryEquipHeld(
        Equipment_Slot targetSlot,
        Item_Instance held,
        out Item_Instance previous)
    {
        previous = held;
        EnsureInitialized();

        if (held == null)
            return false;

        if (!(held.Base is Equippable_Item_Data equippable))
            return false;

        // Kiểm tra theo slot người chơi thực sự thả vào.
        if (!CanEquip(held.Base, targetSlot))
            return false;

        if (inventory != null && inventory.HasInstance(held))
            return false;

        equipped.TryGetValue(
            targetSlot,
            out Item_Instance oldItem);

        // Chỉ thay đổi Equipment sau khi toàn bộ kiểm tra thành công.
        SetSlot(targetSlot, held);

        previous = oldItem;
        return true;
    }


    // ------------------------------------------------------------------
    // MẶC / THÁO THẲNG, KHÔNG QUA KHO (Enemy, thử nghiệm, load save)
    // ------------------------------------------------------------------

    /// <summary>Mặc thẳng một món vào ô của nó, món cũ ở ô bị bỏ (không về kho). False nếu không mặc được hoặc đang mặc món đó rồi.</summary>
    public bool EquipDirect(Item_Instance item)
    {
        EnsureInitialized();
        if (!CanEquip(item.Base)) return false;
        if (GetEquipped(item.Base.EquipSlot) == item) return false;

        SetSlot(item.Base.EquipSlot, item);
        return true;
    }

    /// <summary>Mặc thẳng một món mặc định tạo từ asset (Base Modifier sao chép từ asset, không Affix/Influ).</summary>
    public bool EquipDirect(Equippable_Item_Data item)
    {
        return item != null && EquipDirect(Item_Instance.Create(item));
    }

    /// <summary>Tháo thẳng món ở ô, KHÔNG trả về kho. False nếu ô trống.</summary>
    public bool UnequipDirect(Equipment_Slot slot)
    {
        EnsureInitialized();
        if (!equipped.ContainsKey(slot)) return false;

        SetSlot(slot, null);
        return true;
    }

    /// <summary>Tháo thẳng MỌI món đang mặc, KHÔNG trả về kho (load save: mặc lại đúng bộ đã lưu).</summary>
    public void UnequipAllDirect()
    {
        EnsureInitialized();

        var slots = new List<Equipment_Slot>(equipped.Keys);
        foreach (Equipment_Slot slot in slots) SetSlot(slot, null);
    }

    // ------------------------------------------------------------------
    // NỘI BỘ
    // ------------------------------------------------------------------

    // Đổi món ở một ô (null = để trống). Cộng chỉ số món mới TRƯỚC khi gỡ món cũ: đổi giáp +50 máu sang giáp +60 máu
    // không làm tụt máu hiện tại.
    private void SetSlot(Equipment_Slot slot, Item_Instance newItem)
    {
        equipped.TryGetValue(slot, out Item_Instance oldItem);
        if (oldItem == newItem) return;

        if (newItem != null)
        {
            ApplyItem(newItem);
            newItem.OnChanged += HandleItemChanged;
        }

        if (oldItem != null)
        {
            oldItem.OnChanged -= HandleItemChanged;
            RemoveItem(oldItem);
        }

        if (newItem != null) equipped[slot] = newItem;
        else equipped.Remove(slot);

        OnEquipmentChanged?.Invoke(slot, oldItem, newItem);

        if (slot == Equipment_Slot.MainHand ||
            slot == Equipment_Slot.OffHand)
        {
            nextAlternateIsOffHand = false;
        }

        if (slot == Equipment_Slot.MainHand)
        {
            OnWeaponChanged?.Invoke(Weapon);
        }
    }

    // Mỗi lần áp dụng một món có một "bản ghi" riêng: Source cho modifier và Influ + roll TẠI LÚC ÁP,
    // để gỡ đúng thứ đã áp dù món đã đổi (craft sửa Affix/Influ khi đang mặc).
    private sealed class Applied_Item
    {
        public readonly object Source = new object();
        public Influ_Data Influ;
        public float InfluRoll;
    }

    private readonly Dictionary<Item_Instance, Applied_Item> applied = new Dictionary<Item_Instance, Applied_Item>();

    private void ApplyItem(Item_Instance item)
    {
        var record = new Applied_Item { Influ = item.Influ, InfluRoll = item.InfluRoll };

        foreach (Stat_Modifier_Entry m in item.AllModifiers)
        {
            stats.AddModifier(m.stat, new Player_Stat_Modifier(m.value, m.modifierType, record.Source));
        }

        if (record.Influ != null) record.Influ.Apply(new Influ_Context(stats, this), record.InfluRoll, record.Source);

        applied[item] = record;
    }

    private void RemoveItem(Item_Instance item)
    {
        if (!applied.TryGetValue(item, out Applied_Item record)) return;

        applied.Remove(item);
        RemoveRecord(record);
    }

    private void RemoveRecord(Applied_Item record)
    {
        stats.RemoveModifiersFromSource(record.Source);
        if (record.Influ != null) record.Influ.Remove(new Influ_Context(stats, this), record.InfluRoll, record.Source);
    }

    // Món đang mặc đổi (Tier/Affix/Influ): áp bản mới TRƯỚC rồi gỡ bản cũ, để máu hiện tại không tụt khi max máu đổi.
    private void HandleItemChanged(Item_Instance item)
    {
        if (!applied.TryGetValue(item, out Applied_Item oldRecord)) return;

        ApplyItem(item); // ghi đè applied[item] bằng bản mới
        RemoveRecord(oldRecord);

        foreach (KeyValuePair<Equipment_Slot, Item_Instance> pair in equipped)
        {
            if (pair.Value == item) OnEquipmentChanged?.Invoke(pair.Key, item, item);
        }
    }

    /// <summary>Áp lại chỉ số/Influ của món đang ở ô (dùng khi sửa Base Modifiers trực tiếp mà không qua OnChanged). False nếu ô trống.</summary>
    public bool Refresh(Equipment_Slot slot)
    {
        EnsureInitialized();
        if (!equipped.TryGetValue(slot, out Item_Instance item)) return false;

        HandleItemChanged(item);
        return true;
    }

    // ------------------------------------------------------------------
    // THỬ NGHIỆM (chuột phải component khi đang Play)
    // ------------------------------------------------------------------

    [ContextMenu("Debug: Mặc món trang bị đầu tiên trong Inventory")]
    private void Debug_Equip_First_In_Inventory()
    {
        EnsureInitialized();
        if (inventory == null)
        {
            Debug.LogWarning($"[{nameof(Character_Equipment)}] Không có Inventory.", this);
            return;
        }

        for (int i = 0; i < inventory.PlacementCount; i++)
        {
            Item_Instance item = inventory.GetPlacement(i).Instance;
            if (item == null || !TryEquip(item)) continue;

            Debug.Log($"[{nameof(Character_Equipment)}] Đã mặc {item.DisplayName} vào ô {item.Base.EquipSlot}.", this);
            return;
        }

        Debug.Log($"[{nameof(Character_Equipment)}] Inventory không có trang bị nào mặc được.", this);
    }

    [ContextMenu("Debug: In trang bị đang mặc")]
    private void Debug_Print_Equipped()
    {
        EnsureInitialized();
        if (equipped.Count == 0)
        {
            Debug.Log($"[{nameof(Character_Equipment)}] Không mặc gì.", this);
            return;
        }

        foreach (KeyValuePair<Equipment_Slot, Item_Instance> pair in equipped)
        {
            Item_Instance item = pair.Value;
            string influ = item.Influ != null ? $", Influ {item.Influ.DisplayName} x{item.InfluRoll}" : string.Empty;
            Debug.Log(
    $"[{nameof(Character_Equipment)}] {pair.Key}: {item.DisplayName} " +
    $"(Tier {item.Tier}, {item.BaseModifiers.Count} base, " +
    $"{item.AffixLines.Count} affix{influ})",
    this);
        }
    }

    [ContextMenu("Debug: Tháo hết trang bị về Inventory")]
    private void Debug_Unequip_All()
    {
        EnsureInitialized();

        var slots = new List<Equipment_Slot>(equipped.Keys);
        foreach (Equipment_Slot slot in slots)
        {
            if (!TryUnequip(slot)) Debug.LogWarning($"[{nameof(Character_Equipment)}] Không tháo được ô {slot} (kho hết chỗ?).", this);
        }
    }

    [ContextMenu("Debug: Roll Base Tier 2")]
    private void Debug_Roll_Base()
    {
        EnsureInitialized();

        if (!equipped.TryGetValue(
                debugMaterialSlot,
                out Item_Instance instance))
        {
            Debug.LogWarning(
                $"[{nameof(Character_Equipment)}] Không có item ở ô " +
                $"{debugMaterialSlot}.",
                this);
            return;
        }

        bool success = Roll_Rules.RollBase(
            instance,
            2,
            new System.Random());

        if (!success)
        {
            Debug.LogWarning(
                $"[{nameof(Character_Equipment)}] Không thể roll Base " +
                $"cho '{instance.DisplayName}'. Item không phải " +
                $"Equippable_Item_Data hoặc không hợp lệ.",
                this);
            return;
        }

        Debug.Log(
            $"[{nameof(Character_Equipment)}] Đã roll lại Base cho " +
            $"'{instance.DisplayName}' với Material Tier 2.",
            this);

        for (int i = 0; i < instance.BaseModifiers.Count; i++)
        {
            Stat_Modifier_Entry modifier = instance.BaseModifiers[i];

            Debug.Log(
                $"Base [{i}] {modifier.stat}: " +
                $"{modifier.value} ({modifier.modifierType})",
                this);
        }
    }

    [ContextMenu("Debug: Test Material Affix Pool")]
    private void Debug_Test_Material_Affix_Pool()
    {
        EnsureInitialized();

        if (debugMaterial == null)
        {
            Debug.LogWarning(
                $"[{nameof(Character_Equipment)}] Chưa chọn Material_Item_Data.",
                this);
            return;
        }

        if (debugAffixDatabase == null)
        {
            Debug.LogWarning(
                $"[{nameof(Character_Equipment)}] Chưa chọn Affix_Database.",
                this);
            return;
        }

        if (!equipped.TryGetValue(
                debugMaterialSlot,
                out Item_Instance instance))
        {
            Debug.LogWarning(
                $"[{nameof(Character_Equipment)}] Không có item ở ô " +
                $"{debugMaterialSlot}.",
                this);
            return;
        }

        List<Affix_Data> result = new List<Affix_Data>();

        debugMaterial.GetAffixPool(
            debugMaterialSlot,
            instance.AffixLines,
            debugAffixDatabase,
            result);

        Debug.Log(
            $"[{nameof(Character_Equipment)}] Material: " +
            $"{debugMaterial.DisplayName}, Role: {debugMaterial.Role}, " +
            $"Slot: {debugMaterialSlot}, Existing Affixes: " +
            $"{instance.AffixLines.Count}, Result Count: {result.Count}.",
            this);

        if (result.Count == 0)
        {
            Debug.Log(
                $"[{nameof(Character_Equipment)}] Affix pool rỗng.",
                this);
            return;
        }

        foreach (Affix_Data affix in result)
{
    Debug.Log(
        $"[{nameof(Character_Equipment)}] Affix hợp lệ: " +
        $"{affix.DisplayName} | Id: {affix.AffixId} | " +
        $"Weight: {affix.Weight}.",
        this);
}

System.Random rng = new System.Random();
Affix_Data selectedAffix = PickWeightedAffix(result, rng);

if (selectedAffix == null)
{
    Debug.LogWarning(
        $"[{nameof(Character_Equipment)}] Không chọn được Affix để roll.",
        this);
    return;
}

Affix_Line rolledAffix =
    selectedAffix.Roll(instance.Tier, rng);

Item_Instance.Affix_Apply_Result applyResult =
    instance.ApplyAffix(rolledAffix);

Debug.Log(
    $"[{nameof(Character_Equipment)}] Đã roll và áp dụng Affix " +
    $"'{selectedAffix.DisplayName}' cho '{instance.DisplayName}'. " +
    $"Tier: {instance.Tier}, Kết quả: {applyResult}, " +
    $"Tổng số Affix hiện tại: {instance.AffixLines.Count}.",
    this);


if (Roll_Rules.ApplyAffixRoll(
        debugMaterial,
        instance,
        debugAffixDatabase,
        new System.Random(),
        out var r))
{
    Debug.Log("Affix: " + r, this);
}
    }

    /// <summary>
    /// Bật hoặc tắt bảng debug Dual Weapon Attack.
    /// </summary>
    public void SetAttackSourceDebugVisible(bool visible)
    {
        showAttackSourceDebug = visible;
    }

    /// <summary>
    /// Trả về trạng thái hiện tại của bảng debug Dual Weapon Attack.
    /// </summary>
    public bool IsAttackSourceDebugVisible()
    {
        return showAttackSourceDebug;
    }

    /// <summary>
    /// Đảo trạng thái hiển thị bảng debug Dual Weapon Attack.
    /// </summary>
    public void ToggleAttackSourceDebug()
    {
        showAttackSourceDebug = !showAttackSourceDebug;
    }

    /// <summary>
    /// Hiện trong menu chuột phải của Component để bật/tắt debug.
    /// </summary>
    [ContextMenu("Debug: Toggle Attack Source UI")]
    private void Debug_ToggleAttackSourceUI()
    {
        ToggleAttackSourceDebug();
    }
    private void OnGUI()
    {

        return;
         if (!showAttackSourceDebug)
            return;

        GUI.Box(
            new Rect(10, 10, 240, 115),
            "ATTACK SOURCE DEBUG"
        );

        GUI.Label(
            new Rect(20, 35, 220, 20),
            $"Attack Count: {alternateAttackCount}"
        );

        GUI.Label(
            new Rect(20, 55, 220, 20),
            $"Source: {lastAttackSource}"
        );

        GUI.Label(
            new Rect(20, 75, 220, 20),
            $"Weapon: {lastAttackWeapon}"
        );

        GUI.Label(
            new Rect(20, 95, 220, 20),
            $"Damage Types: {lastAttackDamage}"
        );
        

    }
    private static Affix_Data PickWeightedAffix(
    List<Affix_Data> affixes,
    System.Random rng)
    {
        if (affixes == null || affixes.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (Affix_Data affix in affixes)
        {
            if (affix == null || affix.Weight <= 0)
                continue;

            totalWeight += affix.Weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(totalWeight);

        foreach (Affix_Data affix in affixes)
        {
            if (affix == null || affix.Weight <= 0)
                continue;

            if (roll < affix.Weight)
                return affix;

            roll -= affix.Weight;
        }

        return null;
    }

}
