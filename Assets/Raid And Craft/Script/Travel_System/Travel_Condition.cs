using UnityEngine;

/// <summary>
/// KHE ĐỂ ĐẶT ĐIỀU KIỆN MỞ cổng (Travel_Gate). Mỗi điều kiện là một asset (ScriptableObject) kéo vào danh sách Conditions của cổng;
/// mọi điều kiện phải đạt thì cổng mới mở.
/// Thêm điều kiện mới = viết một class kế thừa Travel_Condition, ví dụ "đã giết boss X" (khi có hệ tiến trình/quest):
///
///   [CreateAssetMenu(menuName = "Raid And Craft/Travel/Boss Killed")]
///   public class Boss_Killed_Condition : Travel_Condition
///   {
///       [SerializeField] private string bossId;
///       public override bool IsMet(GameObject traveler, out string reason)
///       {
///           reason = "Hãy hạ " + bossId + " trước.";
///           return Progress.IsBossKilled(bossId);
///       }
///   }
///
/// Không sửa Travel_Gate, Scene_Transition hay UI khi thêm điều kiện mới.
/// </summary>
public abstract class Travel_Condition : ScriptableObject
{
    /// <summary>Đạt điều kiện chưa. reason: lý do hiện cho người chơi khi chưa đạt (có thể để rỗng khi đạt).</summary>
    public abstract bool IsMet(GameObject traveler, out string reason);
}
