using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gắn lên PlayerPrefab (cùng object với Character_Stats và NavMeshAgent).
/// Chỉ lo việc "chụp" trạng thái Player vào Player_Save_Data và áp ngược lại; việc ghi file do Save_Manager làm.
///
/// Lưu: vị trí, máu/mana, chỉ số gốc (gồm cấp độ), Coin, Inventory (vị trí từng vật phẩm, từng món trang bị với roll riêng),
/// trang bị đang mặc, kho skill, 10 ô skill, 10 ô quick-use, điểm Circuit đã tiêu, Magic Circuit (node đã mở + vật phẩm theo ô).
/// Chưa lưu (cố ý): buff/debuff đang chạy, vật phẩm nằm trên đất, trạng thái NPC shop.
///
/// Các component ngoài Character_Stats đều TUỲ CHỌN: Player thiếu component nào thì phần đó bị bỏ qua.
/// Cần 3 asset tra cứu (Item_Database, Skill_Database, Influ_Database) gán vào ô bên dưới để nạp được tiến trình.
///
/// THỨ TỰ NẠP (quan trọng, đừng đảo): chỉ số gốc -> Magic Circuit về trạng thái mới -> Coin -> Inventory -> trang bị
/// -> kho skill gốc -> Magic Circuit (node, vật phẩm; Spell Stone tự thêm skill) -> ô skill -> ô quick-use -> điểm Circuit -> máu/mana -> vị trí.
/// Máu/mana đặt SAU CÙNG để Max đã tính đủ chỉ số từ trang bị/Rune.
/// Modifier KHÔNG được lưu: trang bị, Rune, cấp độ tự cộng lại khi dựng lại các hệ trên (nếu lưu cả modifier sẽ bị cộng đôi).
/// </summary>
[DisallowMultipleComponent]
public class Player_Save : MonoBehaviour
{
    [SerializeField] private Character_Stats stats;

    [Header("Asset tra cứu khi nạp (bắt buộc để nạp Inventory/trang bị/skill)")]
    [SerializeField] private Item_Database itemDatabase;
    [SerializeField] private Skill_Database skillDatabase;
    [SerializeField] private Influ_Database influDatabase;

    private NavMeshAgent agent;
    private readonly HashSet<string> warned = new HashSet<string>();

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        agent = GetComponent<NavMeshAgent>();

        if (stats == null) Debug.LogError($"[{nameof(Player_Save)}] Thiếu Character_Stats.", this);
    }

    // Đăng ký sau khi hết frame đầu: mọi Awake/Start đã chạy xong (ví dụ Skill_Bar_UI tự thêm Player_Quick_Use ở Start),
    // nên lúc nạp save mọi component của Player đã có mặt. Save_Manager cũng chắc chắn đã Awake.
    private IEnumerator Start()
    {
        yield return null;

        if (Save_Manager.Instance == null)
        {
            Debug.LogError($"[{nameof(Player_Save)}] Không có Save_Manager trong Scene.", this);
            yield break;
        }

        Save_Manager.Instance.Register(this);

        // Chưa có file save (lần đầu) thì Apply không chạy: vẫn phải đặt Player vào điểm xuất hiện. Đã đặt rồi thì hàm này không làm gì.
        Scene_Transition.PlaceAtPendingSpawn(transform, agent);
    }

    private void OnDestroy()
    {
        if (Save_Manager.Instance != null) Save_Manager.Instance.Unregister(this);
    }

    // ------------------------------------------------------------------
    // CHỤP (lưu)
    // ------------------------------------------------------------------

    public void Capture(Player_Save_Data data)
    {
        CaptureLocation(data);

        if (stats == null) return;

        data.currentHealth = stats.CurrentHealth;
        data.currentMana = stats.CurrentMana;

        data.baseStats.Clear();
        foreach (Player_Stat_Type type in Player_Stat_Layer_Map.AllTypes)
        {
            data.baseStats.Add(new Stat_Save_Entry((int)type, stats.GetBaseValue(type)));
        }

        CaptureProgress(data);
    }

    // Giống các ARPG khác: chỉ lưu vị trí ở Scene base (Scene_Info.Kind = Base, hoặc Scene không có Scene_Info).
    // Ở Scene tạm (Arena, map...) không lưu vị trí, chỉ nhớ Scene base gần nhất để lần sau vào lại đó.
    private void CaptureLocation(Player_Save_Data data)
    {
        string sceneName = gameObject.scene.name;
        Scene_Info info = FindFirstObjectByType<Scene_Info>();
        bool inBase = info == null || info.Kind == Scene_Kind.Base;
        Save_Manager manager = Save_Manager.Instance;

        if (inBase)
        {
            if (manager != null) manager.LastBaseScene = sceneName;
            data.baseSceneName = sceneName;
            data.noPosition = false;
            data.position = transform.position;
            data.rotationY = transform.eulerAngles.y;
        }
        else
        {
            data.baseSceneName = manager != null ? manager.LastBaseScene : string.Empty;
            data.noPosition = true;
            data.position = Vector3.zero;
            data.rotationY = 0f;
        }
    }

    private void CaptureProgress(Player_Save_Data data)
    {
        data.hasProgress = true;

        Player_Wallet wallet = GetComponent<Player_Wallet>();
        data.coins = wallet != null ? wallet.Coins : 0;

        CaptureInventory(data.inventory, GetComponent<Inventory>());

        data.equipment.Clear();
        Character_Equipment equipment = GetComponent<Character_Equipment>();
        if (equipment != null)
        {
            foreach (KeyValuePair<Equipment_Slot, Item_Instance> pair in equipment.Equipped)
            {
                CheckInDatabase(pair.Value);
                data.equipment.Add(new Equipment_Save_Entry { slot = (int)pair.Key, item = Save_Codec.ToSave(pair.Value) });
            }
        }

        Magic_Circuit circuit = GetComponent<Magic_Circuit>();
        bool circuitActive = circuit != null && circuit.enabled;

        // Kho skill gốc: bỏ skill do Spell Stone thêm (Magic Circuit tự thêm lại khi nạp), nếu không gỡ Spell Stone sẽ không gỡ được skill.
        data.baseSkillIds.Clear();
        data.skillSlotIds.Clear();
        Player_Skill_Slots skillSlots = GetComponent<Player_Skill_Slots>();
        if (skillSlots != null)
        {
            foreach (Skill_Data skill in skillSlots.AvailableSkills)
            {
                if (skill == null || (circuitActive && circuit.IsLearnedByCircuit(skill))) continue;

                CheckInDatabase(skill);
                data.baseSkillIds.Add(skill.SkillId);
            }

            for (int i = 0; i < Player_Skill_Slots.SlotCount; i++)
            {
                Skill_Data skill = skillSlots.GetSlot(i);
                if (skill != null) CheckInDatabase(skill);
                data.skillSlotIds.Add(skill != null ? skill.SkillId : string.Empty);
            }
        }

        data.quickUseItemIds.Clear();
        Player_Quick_Use quickUse = GetComponent<Player_Quick_Use>();
        for (int i = 0; i < Player_Quick_Use.SlotCount; i++)
        {
            Item_Data item = quickUse != null ? quickUse.GetSlot(i) : null;
            if (item != null) CheckInDatabase(item);
            data.quickUseItemIds.Add(item != null ? item.ItemId : string.Empty);
        }

        Player_Circuit_Points points = GetComponent<Player_Circuit_Points>();
        data.circuitSpentPoints = points != null ? points.SpentPoints : 0;

        Player_Experience experience = GetComponent<Player_Experience>();
        data.experience = experience != null ? experience.CurrentExp : 0;

        data.fastTravel.Clear();
        Player_Fast_Travel fastTravel = GetComponent<Player_Fast_Travel>();
        if (fastTravel != null) data.fastTravel.AddRange(fastTravel.Entries);

        data.circuitUnlockedNodes.Clear();
        data.circuitSockets.Clear();
        if (circuitActive)
        {
            foreach (string nodeId in circuit.UnlockedNodeIds)
            {
                data.circuitUnlockedNodes.Add(nodeId);

                int socketCount = circuit.GetSocketCount(nodeId);
                for (int i = 0; i < socketCount; i++)
                {
                    Item_Data item = circuit.GetSocketItem(nodeId, i);
                    if (item == null) continue;

                    CheckInDatabase(item);
                    data.circuitSockets.Add(new Circuit_Socket_Save { nodeId = nodeId, index = i, itemId = item.ItemId });
                }
            }
        }
    }

    private void CaptureInventory(Inventory_Save save, Inventory inventory)
    {
        save.stacks.Clear();
        save.instances.Clear();
        save.columns = 0;
        save.rows = 0;
        if (inventory == null) return;

        save.columns = inventory.Width;
        save.rows = inventory.Height;

        for (int i = 0; i < inventory.PlacementCount; i++)
        {
            AddPlacement(save, inventory.GetPlacement(i), true);
        }

        // Vật phẩm đang CẦM trên con trỏ đã rời khỏi Inventory, nếu không lưu thì mất. Lưu không kèm vị trí (nạp sẽ tự tìm chỗ trống).
        AddPlacement(save, Item_Drag_Controller.PeekHeld, false);
    }

    private void AddPlacement(Inventory_Save save, Grid_Placement placement, bool hasPosition)
    {
        if (placement.IsEmpty) return;

        if (placement.Instance != null) CheckInDatabase(placement.Instance);
        else CheckInDatabase(placement.Item);
        int x = hasPosition ? placement.Origin.x : -1;
        int y = hasPosition ? placement.Origin.y : -1;

        if (placement.Instance != null)
        {
            save.instances.Add(new Instance_Save_Entry { x = x, y = y, item = Save_Codec.ToSave(placement.Instance) });
        }
        else
        {
            save.stacks.Add(new Stack_Save_Entry { itemId = placement.Item.ItemId, count = placement.Count, x = x, y = y });
        }
    }

    // ------------------------------------------------------------------
    // ÁP (nạp)
    // ------------------------------------------------------------------

    /// <summary>
    /// Áp bản save lên Player. Trả về false nếu có phần nào nạp lỗi (thiếu asset tra cứu, ngoại lệ...):
    /// khi đó Save_Manager chặn việc ghi đè save để không làm mất dữ liệu cũ bằng trạng thái nạp dở.
    /// </summary>
    public bool Apply(Player_Save_Data data)
    {
        bool ok = true;

        if (stats != null)
        {
            foreach (Stat_Save_Entry entry in data.baseStats)
            {
                // Bỏ qua chỉ số đã bị xoá khỏi enum ở phiên bản game mới.
                if (!Enum.IsDefined(typeof(Player_Stat_Type), entry.type)) continue;
                stats.SetBaseValue((Player_Stat_Type)entry.type, entry.baseValue);
            }

            // Save v1 chưa có tiến trình: giữ nguyên Inventory/trang bị/skill hiện có.
            if (data.hasProgress) ok = ApplyProgress(data);

            // Đặt máu/mana SAU khi chỉ số gốc và trang bị xong (Max đã đúng) để không bị kẹp sai.
            stats.SetVitals(data.currentHealth, data.currentMana);
        }

        // Chỉ áp vị trí khi save có vị trí (lưu ở Scene base) VÀ đang ở đúng Scene đó (tránh đặt toạ độ của map này vào map khác).
        // Ngược lại giữ nguyên chỗ xuất hiện mặc định của Scene.
        bool samePlace = string.IsNullOrEmpty(data.baseSceneName) || data.baseSceneName == gameObject.scene.name;
        if (!data.noPosition && samePlace)
        {
            // NavMeshAgent phải Warp, gán transform.position trực tiếp sẽ bị agent kéo lại.
            bool warped = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                warped = agent.Warp(data.position);
            }
            if (!warped) transform.position = data.position;

            transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
        }

        // Vừa qua cổng/dịch chuyển sang Scene này: đặt Player vào điểm xuất hiện đã chọn (ghi đè vị trí trong save).
        Scene_Transition.PlaceAtPendingSpawn(transform, agent);
        return ok;
    }

    private bool ApplyProgress(Player_Save_Data data)
    {
        if (itemDatabase == null || skillDatabase == null || influDatabase == null)
        {
            Debug.LogError($"[{nameof(Player_Save)}] Chưa gán đủ Item_Database / Skill_Database / Influ_Database trên Player_Save nên KHÔNG nạp được " +
                           "Inventory, trang bị, skill. Save được giữ nguyên, việc lưu bị chặn cho tới khi sửa xong và chạy lại.", this);
            return false;
        }

        Inventory inventory = GetComponent<Inventory>();
        Character_Equipment equipment = GetComponent<Character_Equipment>();
        Player_Skill_Slots skillSlots = GetComponent<Player_Skill_Slots>();
        Player_Quick_Use quickUse = GetComponent<Player_Quick_Use>();
        Player_Wallet wallet = GetComponent<Player_Wallet>();
        Player_Circuit_Points points = GetComponent<Player_Circuit_Points>();
        Player_Fast_Travel fastTravel = GetComponent<Player_Fast_Travel>();
        Player_Experience experience = GetComponent<Player_Experience>();
        Magic_Circuit circuit = GetComponent<Magic_Circuit>();
        bool circuitActive = circuit != null && circuit.enabled;

        bool ok = true;
        var orphans = new List<Item_Data>(); // vật phẩm không còn chỗ trong Circuit, trả về Inventory để không mất

        ok &= Section("chuẩn bị", () =>
        {
            Item_Drag_Controller.DiscardHeldIfAny(); // bản save đã chứa vật phẩm từng cầm trên tay
            if (circuitActive) circuit.ResetState();
        });

        if (wallet != null) ok &= Section("Coin", () => wallet.SetCoins(data.coins));
        if (inventory != null) ok &= Section("Inventory", () => RestoreInventory(inventory, data.inventory));
        if (equipment != null) ok &= Section("trang bị", () => RestoreEquipment(equipment, inventory, data.equipment));

        if (skillSlots != null)
        {
            ok &= Section("kho skill", () =>
            {
                var baseSkills = new List<Skill_Data>();
                foreach (string id in data.baseSkillIds)
                {
                    Skill_Data skill = skillDatabase.Find(id);
                    if (skill != null) baseSkills.Add(skill);
                    else LogMissing("skill", id);
                }
                skillSlots.ReplaceAvailableSkills(baseSkills);
            });
        }

        // Circuit nạp SAU kho skill gốc để Spell Stone thêm skill của nó lên trên (và đánh dấu được skill nào do Circuit thêm).
        if (circuitActive)
        {
            ok &= Section("Magic Circuit", () => RestoreCircuit(circuit, data, orphans));
        }
        else
        {
            foreach (Circuit_Socket_Save socket in data.circuitSockets)
            {
                Item_Data item = itemDatabase.Find(socket.itemId);
                if (item != null) orphans.Add(item);
            }
        }

        if (skillSlots != null)
        {
            ok &= Section("ô skill", () =>
            {
                for (int i = 0; i < Player_Skill_Slots.SlotCount && i < data.skillSlotIds.Count; i++)
                {
                    string id = data.skillSlotIds[i];
                    if (string.IsNullOrEmpty(id)) continue;

                    Skill_Data skill = skillDatabase.Find(id);
                    if (skill == null) LogMissing("skill", id);
                    else if (!skillSlots.Assign(i, skill)) Debug.LogWarning($"[{nameof(Player_Save)}] Không gán được skill '{id}' vào ô {i} (skill không còn trong kho).", this);
                }
            });
        }

        if (quickUse != null)
        {
            ok &= Section("ô quick-use", () =>
            {
                quickUse.ClearAll();
                for (int i = 0; i < Player_Quick_Use.SlotCount && i < data.quickUseItemIds.Count; i++)
                {
                    string id = data.quickUseItemIds[i];
                    if (string.IsNullOrEmpty(id)) continue;

                    Item_Data item = itemDatabase.Find(id);
                    if (item == null) LogMissing("vật phẩm", id);
                    else if (!quickUse.Assign(i, item)) Debug.LogWarning($"[{nameof(Player_Save)}] Không gán được '{id}' vào ô quick-use {i}.", this);
                }
            });
        }

        if (points != null) ok &= Section("điểm Circuit", () => points.SetSpent(data.circuitSpentPoints));
        if (experience != null) ok &= Section("kinh nghiệm", () => experience.SetExp(data.experience));
        if (fastTravel != null) ok &= Section("dịch chuyển nhanh", () => fastTravel.RestoreAll(data.fastTravel));

        if (orphans.Count > 0 && inventory != null)
        {
            ok &= Section("trả vật phẩm Circuit", () =>
            {
                foreach (Item_Data item in orphans)
                {
                    if (inventory.Add(item, 1) > 0) Debug.LogWarning($"[{nameof(Player_Save)}] Inventory hết chỗ, mất '{item.DisplayName}'.", this);
                }
            });
        }

        return ok;
    }

    // Chạy một phần của việc nạp; lỗi một phần thì log rõ và các phần sau vẫn chạy. Trả về false nếu có ngoại lệ.
    private bool Section(string name, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(Player_Save)}] Lỗi khi nạp phần '{name}': {e}", this);
            return false;
        }
    }

    private void RestoreInventory(Inventory inventory, Inventory_Save save)
    {
        inventory.ClearAll();

        // Giữ phần kho đã mở rộng (lấy cỡ lớn hơn giữa bản save và Inspector), không bao giờ thu nhỏ để khỏi mất đồ.
        int columns = Mathf.Max(save.columns, inventory.Width);
        int rows = Mathf.Max(save.rows, inventory.Height);
        if (columns != inventory.Width || rows != inventory.Height) inventory.TryResize(columns, rows);

        // Đặt mọi thứ CÓ vị trí trước, rồi mới tự tìm chỗ cho phần còn lại, để vật phẩm tự tìm chỗ không chiếm mất ô của vật phẩm khác.
        var pendingStacks = new List<Stack_Save_Entry>();
        var pendingInstances = new List<Item_Instance>();

        foreach (Stack_Save_Entry s in save.stacks)
        {
            Item_Data item = itemDatabase.Find(s.itemId);
            if (item == null || s.count <= 0)
            {
                LogMissing("vật phẩm", s.itemId);
                continue;
            }

            bool placed = s.x >= 0 && s.y >= 0 && s.count <= item.MaxStack
                && inventory.TryPlaceAt(item, s.count, new Vector2Int(s.x, s.y));
            if (!placed) pendingStacks.Add(s);
        }

        foreach (Instance_Save_Entry e in save.instances)
        {
            Item_Instance instance = Save_Codec.FromSave(e.item, itemDatabase, influDatabase);
            if (instance == null) continue;

            var origin = new Vector2Int(e.x, e.y);
            bool placed = e.x >= 0 && e.y >= 0 && inventory.TryPlace(new Grid_Placement(instance.Base, instance, 1, origin), origin);
            if (!placed) pendingInstances.Add(instance);
        }

        foreach (Stack_Save_Entry s in pendingStacks)
        {
            Item_Data item = itemDatabase.Find(s.itemId);
            int left = inventory.Add(item, s.count);
            if (left > 0) Debug.LogWarning($"[{nameof(Player_Save)}] Inventory hết chỗ, mất {left} x '{item.DisplayName}'.", this);
        }

        foreach (Item_Instance instance in pendingInstances)
        {
            if (!inventory.AddInstance(instance)) Debug.LogWarning($"[{nameof(Player_Save)}] Inventory hết chỗ, mất '{instance.DisplayName}'.", this);
        }
    }

    private void RestoreEquipment(Character_Equipment equipment, Inventory inventory, List<Equipment_Save_Entry> saved)
    {
        equipment.UnequipAllDirect(); // bỏ cả trang bị khởi đầu của Inspector, bản save là chuẩn

        foreach (Equipment_Save_Entry entry in saved)
        {
            Item_Instance instance = Save_Codec.FromSave(entry.item, itemDatabase, influDatabase);
            if (instance == null) continue;

            if (equipment.EquipDirect(instance)) continue;

            // Không mặc lại được (ví dụ asset đổi sang không mặc được): trả về Inventory để không mất.
            Debug.LogWarning($"[{nameof(Player_Save)}] Không mặc lại được '{instance.DisplayName}', chuyển vào Inventory.", this);
            if (inventory == null || !inventory.AddInstance(instance)) Debug.LogWarning($"[{nameof(Player_Save)}] Mất '{instance.DisplayName}' (Inventory hết chỗ).", this);
        }
    }

    private void RestoreCircuit(Magic_Circuit circuit, Player_Save_Data data, List<Item_Data> orphans)
    {
        foreach (string nodeId in data.circuitUnlockedNodes)
        {
            if (!circuit.RestoreUnlocked(nodeId)) Debug.LogWarning($"[{nameof(Player_Save)}] Node Magic Circuit '{nodeId}' không còn trong Layout, bỏ qua.", this);
        }

        foreach (Circuit_Socket_Save socket in data.circuitSockets)
        {
            Item_Data item = itemDatabase.Find(socket.itemId);
            if (item == null)
            {
                LogMissing("vật phẩm", socket.itemId);
                continue;
            }

            if (!circuit.RestoreSocket(socket.nodeId, socket.index, item)) orphans.Add(item);
        }

        circuit.NotifyRestored();
    }

    // ------------------------------------------------------------------
    // CẢNH BÁO
    // ------------------------------------------------------------------

    private void LogMissing(string kind, string id)
    {
        Debug.LogError($"[{nameof(Player_Save)}] Không tìm thấy {kind} '{id}' trong Database nên bị bỏ qua khi nạp. " +
                       "Nếu asset vẫn còn thì thêm nó vào Database (chuột phải asset Database -> Thu thập...), nếu không lần lưu tới sẽ mất nó.", this);
    }

    // Cảnh báo SỚM (lúc lưu) khi một asset chưa có trong Database: lúc nạp nó sẽ biến mất. Mỗi asset chỉ cảnh báo một lần.
    private void CheckInDatabase(Item_Data item)
    {
        if (item == null || itemDatabase == null || itemDatabase.Find(item.ItemId) == item) return;
        if (warned.Add("item:" + item.ItemId)) Debug.LogWarning($"[{nameof(Player_Save)}] Vật phẩm '{item.ItemId}' chưa có trong Item_Database: lần nạp sau sẽ MẤT nó.", this);
    }

    // Món trang bị: kiểm cả loại vật phẩm lẫn Influ trên món.
    private void CheckInDatabase(Item_Instance instance)
    {
        CheckInDatabase(instance.Base);

        Influ_Data influ = instance.Influ;
        if (influ == null || influDatabase == null || influDatabase.Find(influ.InfluId) == influ) return;
        if (warned.Add("influ:" + influ.InfluId)) Debug.LogWarning($"[{nameof(Player_Save)}] Influ '{influ.InfluId}' chưa có trong Influ_Database: lần nạp sau món mang nó sẽ MẤT hiệu ứng.", this);
    }

    private void CheckInDatabase(Skill_Data skill)
    {
        if (skill == null || skillDatabase == null || skillDatabase.Find(skill.SkillId) == skill) return;
        if (warned.Add("skill:" + skill.SkillId)) Debug.LogWarning($"[{nameof(Player_Save)}] Skill '{skill.SkillId}' chưa có trong Skill_Database: lần nạp sau sẽ MẤT nó.", this);
    }
}
