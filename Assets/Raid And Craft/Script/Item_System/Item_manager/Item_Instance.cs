using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MỘT món trang bị cụ thể (bản thể của một Equippable_Item_Data), khác với asset dùng chung.
/// Asset (Base) chỉ là LOẠI: icon, Grid Size, ô trang bị, cách đánh... Chỉ số của từng món nằm ở đây:
///   - BaseModifiers: chỉ số base (flat), cố định theo loại base nhưng giá trị roll bằng nguyên liệu.
///   - AffixLines: tối đa 3 dòng affix (% hoặc flat), loại và giới hạn phụ thuộc nguyên liệu; mỗi dòng nhớ id, khoảng roll, tier.
///   - Influ + InfluRoll: một hiệu ứng đặc biệt (code riêng), roll trong giới hạn của nguyên liệu Influ.
///   - Tier: bậc của món, tăng theo độ hiếm nguyên liệu; giới hạn khoảng roll (hệ craft dùng, sau này).
/// Không có Unique: hiệu ứng đặc biệt đến từ Influ.
/// Vật phẩm xếp chồng (nguyên liệu, thuốc, Rune) KHÔNG dùng instance, chỉ có loại + số lượng.
/// Kho và Character_Equipment tham chiếu món bằng instance (so sánh bằng tham chiếu), không bằng asset.
/// Lớp thuần C# (không phải ScriptableObject) vì mỗi món sinh ra lúc chơi; save sẽ lưu InstanceId, itemId của Base và các roll.
/// </summary>
public sealed class Item_Instance
{
    /// <summary>Số dòng Affix tối đa trên một món (đã chốt P0).</summary>
    public const int MaxAffixLines = 3;

    /// <summary>
    /// Báo khi món đổi (Tier, Affix, Influ, Base). Character_Equipment nghe để áp lại chỉ số của món đang mặc.
    /// Sửa Base Modifiers trực tiếp trên danh sách thì gọi NotifyChanged() sau khi sửa.
    /// </summary>
    public event Action<Item_Instance> OnChanged;

    private int tier = 1;
    private Influ_Data influ;
    private float influRoll = 1f;

    public string InstanceId { get; }
    public Item_Data Base { get; }

    public int Tier
    {
        get => tier;
        set
        {
            value = Math.Max(1, value);
            if (tier == value) return;
            tier = value;
            NotifyChanged();
        }
    }

    public List<Stat_Modifier_Entry> BaseModifiers { get; } = new List<Stat_Modifier_Entry>();

    /// <summary>Các dòng Affix đã roll (tối đa MaxAffixLines). Sửa qua ApplyAffix / RemoveAffixAt để có OnChanged.</summary>
    public List<Affix_Line> AffixLines { get; } = new List<Affix_Line>();

    /// <summary>Hiệu ứng đặc biệt (null = không có).</summary>
    public Influ_Data Influ
    {
        get => influ;
        set
        {
            if (influ == value) return;
            influ = value;
            NotifyChanged();
        }
    }

    public float InfluRoll
    {
        get => influRoll;
        set
        {
            if (Mathf.Approximately(influRoll, value)) return;
            influRoll = value;
            NotifyChanged();
        }
    }

    /// <param name="baseItem">Loại món (asset).</param>
    /// <param name="instanceId">Để trống = tự sinh mới; truyền id cũ khi load save.</param>
    public Item_Instance(Item_Data baseItem, string instanceId = null)
    {
        Base = baseItem ?? throw new ArgumentNullException(nameof(baseItem));
        InstanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
    }

    public string DisplayName => Base.DisplayName;

    /// <summary>Phần chỉ số của các dòng Affix (để đọc nhanh; muốn khoảng roll thì dùng AffixLines).</summary>
    public IEnumerable<Stat_Modifier_Entry> AffixModifiers
    {
        get
        {
            foreach (Affix_Line line in AffixLines)
            {
                foreach (Stat_Modifier_Entry m in line.Modifiers) yield return m;
            }
        }
    }

    /// <summary>Thay toàn bộ Base Modifiers (Forge roll lại Base) và báo OnChanged một lần.</summary>
    public void SetBaseModifiers(IEnumerable<Stat_Modifier_Entry> modifiers)
    {
        BaseModifiers.Clear();
        if (modifiers != null) BaseModifiers.AddRange(modifiers);
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }

    /// <summary>Kết quả của ApplyAffix.</summary>
    public enum Affix_Apply_Result
    {
        /// <summary>Cùng loại đã có: ghi đè giá trị dòng đó.</summary>
        Overwritten,
        /// <summary>Loại mới, còn chỗ: thêm dòng mới.</summary>
        Added,
        /// <summary>Loại mới, đã đầy: thay dòng ở replaceIndex.</summary>
        Replaced,
    }

    /// <summary>
    /// Luật roll Affix (đã chốt P0): cùng loại đã có thì ghi đè; loại mới còn chỗ thì thêm;
    /// loại mới khi đã đủ MaxAffixLines thì thay dòng ở replaceIndex (người gọi chọn ngẫu nhiên, nhận 0..Count-1).
    /// </summary>
    public Affix_Apply_Result ApplyAffix(Affix_Line line, int replaceIndex = 0)
    {
        Affix_Apply_Result result;

        int same = AffixLines.FindIndex(l => l.IsSameKind(line));
        if (same >= 0)
        {
            AffixLines[same] = line;
            result = Affix_Apply_Result.Overwritten;
        }
        else if (AffixLines.Count < MaxAffixLines)
        {
            AffixLines.Add(line);
            result = Affix_Apply_Result.Added;
        }
        else
        {
            AffixLines[Mathf.Clamp(replaceIndex, 0, AffixLines.Count - 1)] = line;
            result = Affix_Apply_Result.Replaced;
        }

        NotifyChanged();
        return result;
    }

    /// <summary>Xoá dòng Affix ở vị trí index. False nếu index sai.</summary>
    public bool RemoveAffixAt(int index)
    {
        if (index < 0 || index >= AffixLines.Count) return false;

        AffixLines.RemoveAt(index);
        NotifyChanged();
        return true;
    }

    /// <summary>Đổi cả Influ và roll một lần (chỉ báo OnChanged một lần). Influ null = gỡ Influ.</summary>
    public void SetInflu(Influ_Data newInflu, float newRoll)
    {
        if (influ == newInflu && Mathf.Approximately(influRoll, newRoll)) return;

        influ = newInflu;
        influRoll = newRoll;
        NotifyChanged();
    }

    /// <summary>Mọi chỉ số cộng của món (Base rồi Affix). Influ không nằm trong đây vì có code riêng.</summary>
    public IEnumerable<Stat_Modifier_Entry> AllModifiers
    {
        get
        {
            foreach (Stat_Modifier_Entry m in BaseModifiers) yield return m;
            foreach (Affix_Line line in AffixLines)
            {
                foreach (Stat_Modifier_Entry m in line.Modifiers) yield return m;
            }
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
