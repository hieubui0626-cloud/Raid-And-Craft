using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gắn lên PlayerPrefab (cùng object với Player_Stats và NavMeshAgent).
/// Chỉ lo việc "chụp" trạng thái Player vào Player_Save_Data và áp ngược lại; việc ghi file do Save_Manager làm.
/// </summary>
[DisallowMultipleComponent]
public class Player_Save : MonoBehaviour
{
    [SerializeField] private Character_Stats stats;

    private NavMeshAgent agent;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        agent = GetComponent<NavMeshAgent>();

        if (stats == null) Debug.LogError($"[{nameof(Player_Save)}] Thiếu Player_Stats.", this);
    }

    // Đăng ký ở Start để chắc chắn Save_Manager đã Awake xong.
    private void Start()
    {
        if (Save_Manager.Instance == null)
        {
            Debug.LogError($"[{nameof(Player_Save)}] Không có Save_Manager trong Scene.", this);
            return;
        }

        Save_Manager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (Save_Manager.Instance != null) Save_Manager.Instance.Unregister(this);
    }

    public void Capture(Player_Save_Data data)
    {
        data.position = transform.position;
        data.rotationY = transform.eulerAngles.y;

        if (stats == null) return;

        data.currentHealth = stats.CurrentHealth;
        data.currentMana = stats.CurrentMana;

        data.baseStats.Clear();
        foreach (Player_Stat_Type type in Player_Stat_Layer_Map.AllTypes)
        {
            data.baseStats.Add(new Stat_Save_Entry((int)type, stats.GetBaseValue(type)));
        }
    }

    public void Apply(Player_Save_Data data)
    {
        if (stats != null)
        {
            foreach (Stat_Save_Entry entry in data.baseStats)
            {
                // Bỏ qua chỉ số đã bị xoá khỏi enum ở phiên bản game mới.
                if (!Enum.IsDefined(typeof(Player_Stat_Type), entry.type)) continue;
                stats.SetBaseValue((Player_Stat_Type)entry.type, entry.baseValue);
            }

            // Đặt máu/mana SAU khi chỉ số gốc xong (Max đã đúng) để không bị kẹp sai.
            stats.SetVitals(data.currentHealth, data.currentMana);
        }

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
}
