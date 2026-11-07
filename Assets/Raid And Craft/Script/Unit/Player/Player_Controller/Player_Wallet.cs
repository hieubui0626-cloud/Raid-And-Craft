using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Ví tiền của Player: đơn vị tiền tệ Coin (giống Gold trong PoE). Coin KHÔNG chiếm ô Inventory, chỉ là một con số,
/// dùng để mua hàng của NPC (Npc_Shop). Sau này quái rơi coin thì gọi Add.
/// Gắn lên Player (cùng object với Inventory). Không gắn thì World_Item_Picker tự thêm với số Coin mặc định bên dưới.
/// Chỉnh số Coin: ô Coins trong Inspector (đang Play đổi số cũng cập nhật UI ngay), hoặc chuột phải component -> Debug.
/// Ô Coin Text (TMP, tuỳ chọn): kéo một TextMeshPro vào, chữ tự cập nhật theo định dạng 1,234,567.
/// Player_Save lưu số Coin.
/// </summary>
[DisallowMultipleComponent]
public class Player_Wallet : MonoBehaviour
{
    /// <summary>Bắn mỗi khi số Coin đổi (tham số là số Coin mới).</summary>
    public event Action<int> OnCoinsChanged;

    [Tooltip("Số Coin hiện có (lúc bắt đầu = số Coin khởi đầu để thử). Đổi trong lúc Play cũng được.")]
    [SerializeField, Min(0)] private int coins = 100;

    [Header("Hiển thị (tuỳ chọn)")]
    [Tooltip("Kéo một TextMeshPro (UI hoặc 3D) vào để hiện số Coin dạng 1,234,567. Bỏ trống thì không hiện.")]
    [SerializeField] private TMP_Text coinText;

    [Tooltip("{0} = số Coin đã định dạng x,xxx,xxx. Ví dụ \"Coin: {0}\".")]
    [SerializeField] private string coinTextFormat = "{0}";

    public int Coins => coins;

    /// <summary>Số Coin dạng 1,234,567 (dấu phẩy cố định, không phụ thuộc ngôn ngữ của máy).</summary>
    public static string FormatCoins(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void Awake()
    {
        RefreshText();
    }

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
        NotifyChanged();
    }

    /// <summary>Trừ Coin. False (không đổi gì) nếu không đủ.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > coins) return false;
        if (amount == 0) return true;

        coins -= amount;
        NotifyChanged();
        return true;
    }

    /// <summary>Đặt thẳng số Coin (thử nghiệm, load save).</summary>
    public void SetCoins(int amount)
    {
        coins = Mathf.Max(0, amount);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        RefreshText();
        OnCoinsChanged?.Invoke(coins);
    }

    private void RefreshText()
    {
        if (coinText == null) return;

        coinText.text = string.Format(CultureInfo.InvariantCulture, coinTextFormat, FormatCoins(coins));
    }

#if UNITY_EDITOR
    // Sửa ô Coins trong Inspector lúc đang Play: báo cho UI cập nhật.
    private void OnValidate()
    {
        coins = Mathf.Max(0, coins);
        if (Application.isPlaying) NotifyChanged();
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
