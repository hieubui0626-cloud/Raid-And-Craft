using UnityEngine;

/// <summary>
/// KHOẢNG TRỐNG cho map modifier. Mỗi modifier là một asset ScriptableObject chỉnh Scene_Rule_Values
/// (level, tier, hệ số EXP/Coin/rơi đồ, thêm họ nguyên liệu...). Scene_Info giữ danh sách modifier đang bật
/// (khai báo sẵn trong Inspector hoặc thêm lúc chạy: Scene_Info.Active.AddModifier(...)), ví dụ khi Player dùng "bản đồ" có modifier.
/// Muốn kiểu modifier mới: kế thừa lớp này, ghi đè Apply. Modifier nên chỉ NHÂN hệ số / CỘNG offset để thứ tự áp không quan trọng.
/// </summary>
public abstract class Scene_Rule_Modifier : ScriptableObject
{
    [SerializeField] private string displayName = "Modifier";

    [TextArea, SerializeField] private string description;

    public string DisplayName => displayName;
    public string Description => description;

    public abstract void Apply(Scene_Rule_Values values);
}
