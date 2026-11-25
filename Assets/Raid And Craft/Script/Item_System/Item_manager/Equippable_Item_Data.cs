using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp gốc của vật phẩm MẶC LÊN NGƯỜI (vũ khí hiện tại, sau này giáp, mũ, nhẫn...). Song song với Circuit_Item_Data
/// (vật phẩm lắp vào Magic Circuit).
/// - Equip Slot: ô trang bị vật phẩm này mặc vào (Character_Equipment mặc theo từng ô).
/// - Công dụng: Equip (khi Equip Slot khác None).
/// - Trang bị luôn NonStackable (không xếp chồng), bất kể cấu hình trong Inspector.
/// - Stat Modifiers: chỉ số cộng cho người mặc (Character_Equipment gỡ/cộng theo Source = vật phẩm này).
/// Thêm loại trang bị mới: kế thừa lớp này, đặt [CreateAssetMenu(menuName = "Raid And Craft/Items/&lt;Tên&gt;")], thêm dữ liệu riêng.
/// </summary>
public abstract class Equippable_Item_Data : Item_Data
{
    /// <summary>
    /// Khoảng roll của MỘT dòng Base theo Tier (dùng khi Forge roll lại Base). Khớp với dòng Stat Modifiers có cùng stat + modifierType;
    /// dòng Stat Modifiers không có khai báo ở đây thì giữ giá trị asset, không roll.
    /// </summary>
    [Serializable]
    public struct Base_Roll_Line
    {
        public Player_Stat_Type stat;
        public Player_Stat_Modifier_Type modifierType;

        [Tooltip("Các dòng có cùng Roll Group sẽ được roll trong cùng một lượt. Để trống = chỉ roll riêng dòng này.")]
        public string rollGroup;

        [Tooltip("Làm tròn giá trị roll thành số nguyên.")]
        public bool wholeNumber;

        [Tooltip("Khoảng roll theo Tier của Material. Tier không có dòng riêng dùng dòng Tier cao nhất không vượt quá, không thì dòng thấp nhất.")]
        public List<Affix_Data.Tier_Range> ranges;
    }

    [Header("Trang bị")]
    [Tooltip("Ô trang bị vật phẩm này mặc vào.")]
    [SerializeField] private Equipment_Slot equipSlot = Equipment_Slot.MainHand;

    [Header("Chỉ số base mặc định (món tạo từ asset sao chép dòng này làm Base Modifier; món craft tự roll riêng)")]
    public List<Stat_Modifier_Entry> statModifiers = new List<Stat_Modifier_Entry>();

    [Header("Khoảng roll Base theo Tier (Forge roll lại Base; để trống = Base cố định)")]
    [SerializeField] private List<Base_Roll_Line> baseRolls = new List<Base_Roll_Line>();

    public IReadOnlyList<Base_Roll_Line> BaseRolls => baseRolls;

    protected override bool ForceNonStackable => true;

    /// <summary>Mỗi món trang bị là một Item_Instance riêng (Base/Affix/Influ roll riêng).</summary>
    public override bool UsesInstance => true;

    public override Item_Usage SupportedUsage => IsEquippable ? Item_Usage.Equip : Item_Usage.None;

    public override bool IsEquippable => equipSlot != Equipment_Slot.None;
    public override Equipment_Slot EquipSlot => equipSlot;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (equipSlot == Equipment_Slot.None)
        {
            Debug.LogWarning($"[{nameof(Equippable_Item_Data)}] '{name}' có Equip Slot = None nên sẽ không mặc được.", this);
        }
    }
#endif
}
