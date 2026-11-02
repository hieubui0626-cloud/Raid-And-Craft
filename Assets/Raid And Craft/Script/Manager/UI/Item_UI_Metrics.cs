using UnityEngine;

/// <summary>
/// Số đo dùng chung của UI vật phẩm. Một "ô cơ sở" = 48 px trên màn hình tham chiếu 1920x1080 (đơn vị Canvas),
/// sprite vật phẩm vẽ theo ô này: vật phẩm Grid Size 2x3 thì sprite 96x144 px.
/// Inventory_UI đặt CellSize và Canvas tham chiếu khi bật; các script UI khác (vật phẩm đang cầm) đọc lại ở đây.
/// </summary>
public static class Item_UI_Metrics
{
    public const float DefaultCellSize = 48f;

    /// <summary>Cạnh một ô lưới (đơn vị Canvas, không phải pixel màn hình thật).</summary>
    public static float CellSize { get; set; } = DefaultCellSize;

    /// <summary>Canvas chứa Inventory: lấy scaleFactor của nó để vật phẩm đang cầm có đúng kích thước như trong lưới.</summary>
    public static Canvas ReferenceCanvas { get; set; }

    public static float ReferenceScale => ReferenceCanvas != null ? ReferenceCanvas.scaleFactor : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CellSize = DefaultCellSize;
        ReferenceCanvas = null;
    }
}
