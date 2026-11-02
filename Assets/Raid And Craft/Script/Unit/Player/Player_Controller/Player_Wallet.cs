using System;
using UnityEngine;

/// <summary>
/// Ví tiền của Player: đơn vị tiền tệ Coin (giống Gold trong PoE). Coin KHÔNG chiếm ô Inventory, chỉ là một con số,
/// dùng để mua hàng của NPC (Npc_Shop). Sau này quái rơi coin thì gọi Add.
/// Gắn lên Player (cùng object với Inventory). Không gắn thì World_Item_Picker tự thêm với số Coin mặc định bên dưới.
/// Chỉnh số Coin: ô Coins trong Inspector (đang Play đổi số cũng cập nhật UI ngay), hoặc chuột phải component -> Debug.
/// Chưa lưu vào Save_Data (làm cùng Inventory sau).
/// </summary>
[DisallowMultipleComponent]
public class Player_Wallet : MonoBehaviour
{
    /// <summary>Bắn mỗi khi số Coin đổi (tham số là số Coin mới).</summary>
    public event Action<int> OnCoinsChanged;

    [Tooltip("Số Coin hiện có (lúc bắt đầu = số Coin khởi đầu để thử). Đổi trong lúc Play cũng được.")]
    [SerializeField, Min(0)] private int coins = 100;

    public int Coins => coins;

    public bool CanAfford(int amount)
    {
        return amount <= coins;
    }

    /// <summary>Cộng Coin (amount &lt;= 0 bị bỏ qua).</summary>
    public void Add(int amount)
    {
        if (amount <= 0) return;

        long sum = (long)coins + amount;
        coins = sum > int.MaxValue ? int.MaxValue : (int)sum;
        OnCoinsChanged?.Invoke(coins);
    }

    /// <summary>Trừ Coin. False (không đổi gì) nếu không đủ.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > coins) return false;
        if (amount == 0) return true;

        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        return true;
    }

    /// <summary>Đặt thẳng số Coin (thử nghiệm, load save).</summary>
    public void SetCoins(int amount)
    {
        coins = Mathf.Max(0, amount);
        OnCoinsChanged?.Invoke(coins);
    }

#if UNITY_EDITOR
    // Sửa ô Coins trong Inspector lúc đang Play: báo cho UI cập nhật.
    private void OnValidate()
    {
        coins = Mathf.Max(0, coins);
        if (Application.isPlaying) OnCoinsChanged?.Invoke(coins);
    }
#endif

    [ContextMenu("Debug: +100 Coin")]
    private void Debug_Add_100()
    {
        Add(100);
    }

    [ContextMenu("Debug: Đặt 1000 Coin")]
    private void Debug_Set_1000()
    {
        SetCoins(1000);
    }

    [ContextMenu("Debug: Xoá hết Coin")]
    private void Debug_Clear()
    {
        SetCoins(0);
    }
}
