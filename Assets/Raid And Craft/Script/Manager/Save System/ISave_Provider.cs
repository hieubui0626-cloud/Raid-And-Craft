using System.Threading.Tasks;

/// <summary>
/// Nơi lưu trữ save. Save_Manager chỉ làm việc qua interface này nên đổi/thêm nơi lưu
/// (file local, Unity Cloud Save, PlayFab, Firebase, server riêng...) không đụng tới logic game.
/// Dữ liệu truyền đi là chuỗi JSON đã serialize sẵn.
/// </summary>
public interface ISave_Provider
{
    /// <summary>Ghi đè save của slot.</summary>
    Task SaveAsync(string slotId, string json);

    /// <summary>Trả về JSON của slot, hoặc null nếu chưa có.</summary>
    Task<string> LoadAsync(string slotId);

    Task<bool> ExistsAsync(string slotId);

    Task DeleteAsync(string slotId);
}
