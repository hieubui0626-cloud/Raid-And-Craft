using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modifier dựng sẵn (Create -> Raid And Craft -> Scene Modifier -> Basic): chỉnh level/tier, nhân hệ số EXP/Coin/rơi đồ, thêm họ nguyên liệu.
/// Đủ cho đa số modifier kiểu "+2 level", "rơi nhiều gấp đôi", "thêm quặng". Cái phức tạp hơn thì viết lớp kế thừa Scene_Rule_Modifier.
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Scene Modifier/Basic", fileName = "Scene_Modifier")]
public class Basic_Scene_Rule_Modifier : Scene_Rule_Modifier
{
    [SerializeField] private int levelOffset;
    [SerializeField] private int tierOffset;
    [SerializeField, Min(0f)] private float expMultiplier = 1f;
    [SerializeField, Min(0f)] private float coinMultiplier = 1f;
    [SerializeField, Min(0f)] private float dropChanceMultiplier = 1f;
    [SerializeField, Min(0f)] private float dropCountMultiplier = 1f;
    [SerializeField] private List<Scene_Family_Entry> extraFamilies = new List<Scene_Family_Entry>();

    public override void Apply(Scene_Rule_Values values)
    {
        values.levelOffset += levelOffset;
        values.tierOffset += tierOffset;
        values.expMultiplier *= expMultiplier;
        values.coinMultiplier *= coinMultiplier;
        values.dropChanceMultiplier *= dropChanceMultiplier;
        values.dropCountMultiplier *= dropCountMultiplier;
        values.extraFamilies.AddRange(extraFamilies);
    }
}
