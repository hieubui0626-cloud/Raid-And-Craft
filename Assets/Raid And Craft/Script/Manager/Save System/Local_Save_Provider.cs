using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Lưu save thành file JSON trong Application.persistentDataPath/Saves.
/// - Ghi an toàn: ghi ra file .tmp rồi mới thay file chính, giữ bản .bak -> mất điện giữa chừng không hỏng save.
/// - Thao tác file nhỏ nên chạy đồng bộ (có bản Save/Load đồng bộ để dùng lúc thoát game),
///   các hàm Async chỉ bọc lại cho khớp interface.
/// </summary>
public class Local_Save_Provider : ISave_Provider
{
    private const string Extension = ".json";
    private readonly string directory;

    public Local_Save_Provider()
    {
        directory = Path.Combine(Application.persistentDataPath, "Saves");
    }

    // ------------------------------------------------------------------
    // Đồng bộ
    // ------------------------------------------------------------------

    public void Save(string slotId, string json)
    {
        Directory.CreateDirectory(directory);

        string path = GetPath(slotId);
        string tmp = path + ".tmp";

        File.WriteAllText(tmp, json);

        if (File.Exists(path)) File.Replace(tmp, path, path + ".bak");
        else File.Move(tmp, path);
    }

    /// <summary>Đọc file save. backup = true: đọc bản .bak (dùng khi bản chính bị hỏng).</summary>
    public string Load(string slotId, bool backup = false)
    {
        string path = GetPath(slotId) + (backup ? ".bak" : "");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public bool Exists(string slotId) => File.Exists(GetPath(slotId));

    public void Delete(string slotId)
    {
        string path = GetPath(slotId);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
    }

    // ------------------------------------------------------------------
    // ISave_Provider
    // ------------------------------------------------------------------

    public Task SaveAsync(string slotId, string json)
    {
        Save(slotId, json);
        return Task.CompletedTask;
    }

    public Task<string> LoadAsync(string slotId) => Task.FromResult(Load(slotId));

    public Task<bool> ExistsAsync(string slotId) => Task.FromResult(Exists(slotId));

    public Task DeleteAsync(string slotId)
    {
        Delete(slotId);
        return Task.CompletedTask;
    }

    private string GetPath(string slotId)
    {
        // Chặn ký tự lạ trong tên slot để không thoát ra ngoài thư mục Saves.
        foreach (char c in Path.GetInvalidFileNameChars()) slotId = slotId.Replace(c, '_');
        return Path.Combine(directory, slotId + Extension);
    }
}
