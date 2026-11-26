using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nguyên liệu (Raw Material...): vật phẩm xếp chồng dùng cho hệ craft sau này (quặng, sắt thô...).
/// Công dụng: Crafting (đặt vào bàn Craft/Forge).
/// Rarity Tier là bậc hiếm (1 = thường). Tooltip và thẻ tên trên đất tô màu theo bậc này.
/// Tạo asset: Create -> Raid And Craft -> Items -> Material. Nhớ đặt Stack Type = Stackable và Max Stack > 1.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Items/Material", fileName = "Material_New")]
public class Material_Item_Data : Item_Data
{
    [Header("Nguyên liệu")]
    [Tooltip("Bậc hiếm: 1 thường, 2 hiếm, 3 rất hiếm... (quyết định bậc Tier của món craft sau này).")]
    [SerializeField, Min(1)] private int rarityTier = 1;

    /// <summary>Vai trò của nguyên liệu khi craft (có thể kết hợp).</summary>
    [Flags]
    public enum Craft_Role
    {
        None = 0,
        /// <summary>Nguyên liệu base: bậc hiếm quyết định Tier của món.</summary>
        Base = 1,
        /// <summary>Nguyên liệu Affix: mở khoá pool Affix.</summary>
        Affix = 2,
        /// <summary>Nguyên liệu Influ: mở khoá các Influ roll được.</summary>
        Influ = 4,
    }

    /// <summary>Một Influ mà nguyên liệu này cho roll, kèm khoảng roll riêng (giới hạn bởi nguyên liệu).</summary>
    [Serializable]
    public struct Influ_Option
    {
        public Influ_Data influ;
        public float minRoll;
        public float maxRoll;
        [Min(0)] public int weight;
    }

    [Header("Craft")]
    [SerializeField] private Craft_Role craftRole = Craft_Role.Base;

    [Tooltip("Vai trò Affix: các Affix nguyên liệu này cho roll (trọng số lấy từ Affix_Data). Một món có thể dùng nhiều nguyên liệu Affix, pool là hợp của chúng.")]
    [SerializeField] private List<Affix_Data> affixPool = new List<Affix_Data>();

    [Tooltip("Vai trò Influ: các Influ nguyên liệu này cho roll, mỗi Influ có khoảng roll và trọng số riêng.")]
    [SerializeField] private List<Influ_Option> influOptions = new List<Influ_Option>();

    public override Item_Usage SupportedUsage => Item_Usage.Crafting;

    public int RarityTier => Mathf.Max(1, rarityTier);
    public Craft_Role Role => craftRole;
    public IReadOnlyList<Affix_Data> AffixPool => affixPool;
    public IReadOnlyList<Influ_Option> InfluOptions => influOptions;

    public bool HasRole(Craft_Role role)
    {
        return (craftRole & role) == role && role != Craft_Role.None;
    }

    /// <summary>
    /// Pool Affix nguyên liệu này mở cho một ô trang bị: bỏ affix không hợp ô, và bỏ affix loại trừ (cùng nhóm) với dòng món đã có.
    /// Dòng cùng loại với dòng đã có vẫn được (ApplyAffix sẽ ghi đè). database dùng để tra Affix_Data của các dòng đã có (null = bỏ qua loại trừ).
    /// Ghi vào result (xoá trước). Rỗng nếu nguyên liệu không có vai trò Affix.
    /// </summary>
    public void GetAffixPool(Equipment_Slot slot, IReadOnlyList<Affix_Line> existing, Affix_Database database, List<Affix_Data> result)
    {
        result.Clear();
        if (!HasRole(Craft_Role.Affix)) return;

        foreach (Affix_Data a in affixPool)
        {
            if (a == null || a.Weight <= 0 || !a.CanRollOn(slot)) continue;
            if (IsBlocked(a, existing, database)) continue;
            result.Add(a);
        }
    }

    /// <summary>Các Influ roll được (có trọng số > 0 và khoảng roll hợp lệ). Ghi vào result (xoá trước). Rỗng nếu không có vai trò Influ.</summary>
    public void GetInfluOptions(List<Influ_Option> result)
    {
        result.Clear();
        if (!HasRole(Craft_Role.Influ)) return;

        foreach (Influ_Option o in influOptions)
        {
            if (o.influ != null && o.weight > 0) result.Add(o);
        }
    }

    private static bool IsBlocked(Affix_Data candidate, IReadOnlyList<Affix_Line> existing, Affix_Database database)
    {
        if (existing == null || database == null) return false;

        foreach (Affix_Line line in existing)
        {
            if (line.affixId == candidate.AffixId) continue;

            Affix_Data other = database.Find(line.affixId);
            if (other != null && candidate.ConflictsWith(other)) return true;
        }

        return false;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        for (int i = 0; i < influOptions.Count; i++)
        {
            Influ_Option o = influOptions[i];
            if (o.minRoll > o.maxRoll)
            {
                (o.minRoll, o.maxRoll) = (o.maxRoll, o.minRoll);
                influOptions[i] = o;
            }
        }
    }

}
