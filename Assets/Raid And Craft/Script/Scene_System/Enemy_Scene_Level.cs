using UnityEngine;

/// <summary>
/// Đặt level cho quái theo quy chế của Scene (Scene_Rules.SceneLevel). Gắn lên Enemy cùng Character_Level.
/// Chạy ở Start (sau mọi Awake, kể cả Scene_Info) rồi hồi đầy máu/mana theo chỉ số mới. Scene không có Rules thì giữ level đã đặt ở prefab.
/// Chỉ số tăng theo cấp lấy từ Level_Config của chính Enemy (ô Config của Character_Level).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Character_Level))]
public class Enemy_Scene_Level : MonoBehaviour
{
    private void Start()
    {
        Scene_Info info = Scene_Info.Active;
        if (info == null || info.Rules == null) return;

        var level = GetComponent<Character_Level>();
        level.SetLevel(info.Rules.SceneLevel);

        var stats = GetComponent<Character_Stats>();
        if (stats != null && !stats.IsDead) stats.RestoreFull();
    }
}
