using System.Collections.Generic;
using UnityEngine;

public enum Status_Stack_Mode
{
    Refresh, // Cùng nguồn + cùng chỉ số: gỡ bản cũ, áp bản mới (thời gian làm mới)
    Stack,   // Luôn thêm một modifier mới, chồng lên nhau
    Ignore   // Đang chịu rồi thì bỏ qua
}

/// <summary>
/// Giữ các modifier có thời hạn trên một nhân vật và gỡ chúng khi hết giờ.
/// (ScriptableObject không có Update/coroutine nên cần component này.)
/// Tự thêm vào nhân vật khi bị áp hiệu ứng lần đầu: Status_Effect_Controller.GetOrAdd(stats).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Stats))]
public class Status_Effect_Controller : MonoBehaviour
{
    private class Entry
    {
        public object Source;
        public Player_Stat_Type Stat;
        public Player_Stat_Modifier Modifier;
        public float EndTime;
    }

    [SerializeField] private Character_Stats stats;

    private readonly List<Entry> active = new List<Entry>();

    public int ActiveCount => active.Count;

    public static Status_Effect_Controller GetOrAdd(Character_Stats target)
    {
        var controller = target.GetComponent<Status_Effect_Controller>();
        return controller != null ? controller : target.gameObject.AddComponent<Status_Effect_Controller>();
    }

    private void Awake()
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
    }

    public void Apply(object source, Player_Stat_Type stat, Player_Stat_Modifier modifier, float duration, Status_Stack_Mode mode)
    {
        if (stats == null) stats = GetComponent<Character_Stats>();
        if (duration <= 0f) return;

        Entry existing = Find(source, stat);
        if (existing != null)
        {
            if (mode == Status_Stack_Mode.Ignore) return;
            if (mode == Status_Stack_Mode.Refresh) Remove(existing);
        }

        stats.AddModifier(stat, modifier);
        active.Add(new Entry { Source = source, Stat = stat, Modifier = modifier, EndTime = Time.time + duration });
    }

    /// <summary>Gỡ mọi hiệu ứng đến từ một nguồn (ví dụ thanh tẩy debuff).</summary>
    public void ClearFromSource(object source)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (Equals(active[i].Source, source)) Remove(active[i]);
        }
    }

    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) Remove(active[i]);
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (Time.time >= active[i].EndTime) Remove(active[i]);
        }
    }

    // Tắt/pool lại nhân vật thì gỡ sạch để không "dính" debuff sang lần dùng sau.
    private void OnDisable()
    {
        if (stats != null) ClearAll();
    }

    private Entry Find(object source, Player_Stat_Type stat)
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Stat == stat && Equals(active[i].Source, source)) return active[i];
        }
        return null;
    }

    private void Remove(Entry entry)
    {
        stats.RemoveModifier(entry.Stat, entry.Modifier);
        active.Remove(entry);
    }
}
