using System;
using System.Collections.Generic;

/// <summary>
/// MỘT món trang bị cụ thể (bản thể của một Equippable_Item_Data), khác với asset dùng chung.
/// Asset (Base) chỉ là LOẠI: icon, Grid Size, ô trang bị, cách đánh... Chỉ số của từng món nằm ở đây:
///   - BaseModifiers: chỉ số base (flat), cố định theo loại base nhưng giá trị roll bằng nguyên liệu.
///   - AffixModifiers: chỉ số affix (% hoặc flat), loại và giới hạn phụ thuộc nguyên liệu.
///   - Influ + InfluRoll: một hiệu ứng đặc biệt (code riêng), roll trong giới hạn của nguyên liệu Influ.
///   - Tier: bậc của món, tăng theo độ hiếm nguyên liệu; giới hạn khoảng roll (hệ craft dùng, sau này).
/// Không có Unique: hiệu ứng đặc biệt đến từ Influ.
/// Vật phẩm xếp chồng (nguyên liệu, thuốc, Rune) KHÔNG dùng instance, chỉ có loại + số lượng.
/// Kho và Character_Equipment tham chiếu món bằng instance (so sánh bằng tham chiếu), không bằng asset.
/// Lớp thuần C# (không phải ScriptableObject) vì mỗi món sinh ra lúc chơi; save sẽ lưu InstanceId, itemId của Base và các roll.
/// </summary>
public sealed class Item_Instance
{
    public string InstanceId { get; }
    public Item_Data Base { get; }
    public int Tier { get; set; } = 1;

    public List<Weapon_Stat_Modifier> BaseModifiers { get; } = new List<Weapon_Stat_Modifier>();
    public List<Weapon_Stat_Modifier> AffixModifiers { get; } = new List<Weapon_Stat_Modifier>();

    /// <summary>Hiệu ứng đặc biệt (null = không có).</summary>
    public Influ_Data Influ { get; set; }
    public float InfluRoll { get; set; } = 1f;

    /// <param name="baseItem">Loại món (asset).</param>
    /// <param name="instanceId">Để trống = tự sinh mới; truyền id cũ khi load save.</param>
    public Item_Instance(Item_Data baseItem, string instanceId = null)
    {
        Base = baseItem ?? throw new ArgumentNullException(nameof(baseItem));
        InstanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
    }

    public string DisplayName => Base.DisplayName;

    /// <summary>Mọi chỉ số cộng của món (Base rồi Affix). Influ không nằm trong đây vì có code riêng.</summary>
    public IEnumerable<Weapon_Stat_Modifier> AllModifiers
    {
        get
        {
            foreach (Weapon_Stat_Modifier m in BaseModifiers) yield return m;
            foreach (Weapon_Stat_Modifier m in AffixModifiers) yield return m;
        }
    }

    /// <summary>
    /// Tạo một món từ loại có sẵn: BaseModifiers sao chép từ Stat Modifiers trên asset, không có Affix/Influ.
    /// Dùng cho trang bị khởi đầu, vật phẩm thử, và khi kho tự thêm một loại trang bị theo Item_Data.
    /// Hệ craft sau này tự tạo instance và điền các roll.
    /// </summary>
    public static Item_Instance Create(Item_Data baseItem)
    {
        var instance = new Item_Instance(baseItem);
        if (baseItem is Equippable_Item_Data equippable)
        {
            instance.BaseModifiers.AddRange(equippable.statModifiers);
        }
        return instance;
    }
}
