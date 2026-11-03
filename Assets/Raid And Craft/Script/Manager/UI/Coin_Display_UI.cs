using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiện số Coin hiện tại của Player (Player_Wallet), tự cập nhật khi đổi.
/// Inventory_UI tự tạo một thẻ có component này dưới lưới. Muốn đặt chỗ khác (HUD...): Add Component lên một object UI,
/// kéo một TextMeshPro vào ô Text (bỏ trống thì script tự tạo chữ bên trong).
/// </summary>
public class Coin_Display_UI : MonoBehaviour
{
    [Tooltip("Bỏ trống thì tự tìm TextMeshPro con, không có thì tự tạo.")]
    [SerializeField] private TextMeshProUGUI text;

    [Tooltip("{0} = số Coin.")]
    [SerializeField] private string format = "Coin: {0:N0}";

    [SerializeField] private Color color = new Color(1f, 0.82f, 0.5f, 1f);

    private Player_Wallet wallet;
    private bool subscribed;

    private void Awake()
    {
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) text = CreateText();

        text.color = color;
        text.raycastTarget = false;
    }

    private void OnEnable()
    {
        TryBind();
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
    }

    // Player có thể được tạo sau khi UI bật: thử lại cho tới khi tìm được ví.
    private void Update()
    {
        if (subscribed) return;

        if (TryBind()) Refresh();
    }

    private bool TryBind()
    {
        if (subscribed) return true;

        if (wallet == null) wallet = FindFirstObjectByType<Player_Wallet>();
        if (wallet == null) return false;

        wallet.OnCoinsChanged += HandleCoinsChanged;
        subscribed = true;
        return true;
    }

    private void Unbind()
    {
        if (!subscribed) return;

        if (wallet != null) wallet.OnCoinsChanged -= HandleCoinsChanged;
        subscribed = false;
    }

    private void HandleCoinsChanged(int coins)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (text == null) return;

        text.text = string.Format(CultureInfo.InvariantCulture, format, wallet != null ? wallet.Coins : 0);
    }

    private TextMeshProUGUI CreateText()
    {
        var go = new GameObject("Coin_Text", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 2f);
        rect.offsetMax = new Vector2(-10f, -2f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }
}
