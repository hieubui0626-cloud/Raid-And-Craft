using UnityEngine;

/// <summary>Điều kiện LUÔN KHÔNG ĐẠT, để thử cổng đang khoá (và là ví dụ đơn giản nhất của Travel_Condition).</summary>
[CreateAssetMenu(menuName = "Raid And Craft/Travel/Locked (thử)", fileName = "Locked_Condition")]
public class Locked_Travel_Condition : Travel_Condition
{
    [SerializeField] private string message = "Cổng này chưa mở.";

    public override bool IsMet(GameObject traveler, out string reason)
    {
        reason = message;
        return false;
    }
}
