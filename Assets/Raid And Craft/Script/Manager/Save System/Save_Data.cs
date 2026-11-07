using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toàn bộ dữ liệu một lần lưu (một slot). Chỉ chứa dữ liệu thuần, JsonUtility serialize được.
/// Mở rộng sau này: thêm field mới (quest, world...) và tăng CurrentVersion nếu đổi cấu trúc cũ.
/// Lưu ý JsonUtility: KHÔNG serialize được Dictionary, tuple, và field null của class (luôn thành object rỗng).
/// Vì vậy mọi "khoá -> giá trị" ở đây là List các entry, và "có hay không" dùng cờ bool hoặc chuỗi rỗng.
/// Vật phẩm/skill/Influ luôn lưu bằng id chuỗi (itemId, SkillId, InfluId), tra lại qua các Database khi load.
///
/// Lịch sử phiên bản:
///   1 - vị trí, máu/mana, chỉ số gốc.
///   2 - thêm tiến trình: Inventory, Coin, trang bị, kho skill + ô skill, quick-use, điểm Circuit đã tiêu, Magic Circuit
///       (xem Player_Save_Data.hasProgress; save v1 vẫn nạp được, chỉ không có phần tiến trình).
/// </summary>
[Serializable]
public class Save_Data
{
    public const int CurrentVersion = 2;

    /// <summary>Phiên bản cấu trúc dữ liệu - dùng để migrate save cũ.</summary>
    public int version = CurrentVersion;

    /// <summary>
    /// Số thứ tự lần lưu, tăng dần. Khi có cloud: bản nào revision cao hơn là bản mới hơn
    /// (không dựa vào đồng hồ máy vì có thể lệch giờ).
    /// </summary>
    public long revision;

    /// <summary>Thời điểm lưu (UTC, ISO 8601) - chỉ để hiển thị/gỡ lỗi.</summary>
    public string saveTimeUtc;

    public Player_Save_Data player = new Player_Save_Data();
}

[Serializable]
public class Player_Save_Data
{
    /// <summary>Vị trí/hướng: CHỈ có nghĩa khi noPosition = false (lúc lưu Player đang ở Scene base).</summary>
    public Vector3 position;
    public float rotationY;

    /// <summary>
    /// Tên Scene "base" gần nhất Player đã ở (Scene_Kind.Base). Mở game lại sẽ nạp Scene này trước, rồi mới áp save.
    /// Rỗng = chưa biết (save cũ): giữ nguyên Scene đang mở.
    /// </summary>
    public string baseSceneName;

    /// <summary>
    /// true khi lúc lưu Player đang ở Scene tạm (Scene_Kind.Instance): không lưu vị trí, nạp lại sẽ đứng ở điểm mặc định của Scene base.
    /// Save cũ không có cờ này (= false) nên vẫn áp vị trí như trước.
    /// </summary>
    public bool noPosition;

    public float currentHealth;
    public float currentMana;

    /// <summary>
    /// Chỉ lưu GIÁ TRỊ GỐC của từng chỉ số (không lưu modifier). Cấp độ (CharacterLevel, MaxCharacterLevel) nằm trong đây.
    /// Modifier từ trang bị / Rune / cấp độ được DỰNG LẠI khi nạp các phần bên dưới, nếu lưu cả modifier sẽ bị cộng đôi.
    /// </summary>
    public List<Stat_Save_Entry> baseStats = new List<Stat_Save_Entry>();

    /// <summary>
    /// true khi bản save có phần tiến trình bên dưới (từ phiên bản 2). Save v1 không có cờ này (= false)
    /// nên khi nạp sẽ GIỮ NGUYÊN Inventory/trang bị/skill hiện có thay vì xoá sạch.
    /// </summary>
    public bool hasProgress;

    public int coins;
    public Inventory_Save inventory = new Inventory_Save();
    public List<Equipment_Save_Entry> equipment = new List<Equipment_Save_Entry>();

    /// <summary>Skill Player có sẵn, KHÔNG gồm skill do Spell Stone trong Magic Circuit thêm (Magic Circuit tự thêm lại khi nạp).</summary>
    public List<string> baseSkillIds = new List<string>();

    /// <summary>10 ô skill (1-5, Q-W-E-R-T): SkillId, chuỗi rỗng = ô trống hoặc ô đang chứa vật phẩm quick-use.</summary>
    public List<string> skillSlotIds = new List<string>();

    /// <summary>10 ô quick-use: itemId, chuỗi rỗng = không có vật phẩm.</summary>
    public List<string> quickUseItemIds = new List<string>();

    public int circuitSpentPoints;
    public List<string> circuitUnlockedNodes = new List<string>();
    public List<Circuit_Socket_Save> circuitSockets = new List<Circuit_Socket_Save>();
}

/// <summary>Lưu type dạng số nguyên (giá trị enum không đổi theo quy tắc của Player_Stat_Type) để đổi tên enum không làm hỏng save.</summary>
[Serializable]
public struct Stat_Save_Entry
{
    public int type;
    public float baseValue;

    public Stat_Save_Entry(int type, float baseValue)
    {
        this.type = type;
        this.baseValue = baseValue;
    }
}

// ----------------------------------------------------------------------
// VẬT PHẨM
// ----------------------------------------------------------------------

/// <summary>Một dòng chỉ số của trang bị (Stat_Modifier_Entry). Lưu enum dạng số nguyên.</summary>
[Serializable]
public struct Modifier_Save_Entry
{
    public int stat;
    public int modifierType;
    public float value;
}

/// <summary>Một món trang bị cụ thể (Item_Instance): id riêng, loại (itemId), bậc, các dòng chỉ số đã roll, Influ.</summary>
[Serializable]
public class Item_Instance_Save
{
    public string instanceId;
    public string itemId;
    public int tier = 1;
    public List<Modifier_Save_Entry> baseModifiers = new List<Modifier_Save_Entry>();
    public List<Modifier_Save_Entry> affixModifiers = new List<Modifier_Save_Entry>();

    /// <summary>InfluId, chuỗi rỗng = không có Influ.</summary>
    public string influId;
    public float influRoll = 1f;
}

/// <summary>
/// Một chồng vật phẩm xếp chồng trong lưới. (x, y) là ô trên-trái; (-1, -1) = không có vị trí, tự tìm chỗ trống khi nạp
/// (dùng cho vật phẩm đang cầm trên con trỏ lúc lưu).
/// </summary>
[Serializable]
public struct Stack_Save_Entry
{
    public string itemId;
    public int count;
    public int x;
    public int y;
}

/// <summary>Một món trang bị nằm trong lưới. (x, y) như Stack_Save_Entry.</summary>
[Serializable]
public class Instance_Save_Entry
{
    public int x;
    public int y;
    public Item_Instance_Save item = new Item_Instance_Save();
}

/// <summary>Lưới Inventory: kích thước (để giữ phần mở rộng sau này) và mọi vật phẩm trong đó.</summary>
[Serializable]
public class Inventory_Save
{
    public int columns;
    public int rows;
    public List<Stack_Save_Entry> stacks = new List<Stack_Save_Entry>();
    public List<Instance_Save_Entry> instances = new List<Instance_Save_Entry>();
}

/// <summary>Món đang mặc ở một ô trang bị. slot = số nguyên của Equipment_Slot.</summary>
[Serializable]
public class Equipment_Save_Entry
{
    public int slot;
    public Item_Instance_Save item = new Item_Instance_Save();
}

/// <summary>Vật phẩm đang lắp ở một ô của một node Magic Circuit.</summary>
[Serializable]
public struct Circuit_Socket_Save
{
    public string nodeId;
    public int index;
    public string itemId;
}
