using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stat_Influ_Cluster
{
    [Tooltip("Trọng số xuất hiện của cụm.")]
    public int weight = 1;

    [SerializeField] private string displayName;

    [TextArea(1, 3)]
    [SerializeField] private string description;

    [SerializeField]
    private List<Stat_Modifier_Entry> modifiers =
        new List<Stat_Modifier_Entry>();

    public string DisplayName => displayName;
    public string Description => description;
    public IReadOnlyList<Stat_Modifier_Entry> Modifiers => modifiers;
}

[CreateAssetMenu(
    menuName = "Raid And Craft/Influ/Stat Influ",
    fileName = "Stat_Influ_Data")]
public sealed class Stat_Influ_Data : Influ_Data
{
    [Header("Các cụm stat")]
    [SerializeField]
    private List<Stat_Influ_Cluster> clusters =
        new List<Stat_Influ_Cluster>();

    public IReadOnlyList<Stat_Influ_Cluster> Clusters => clusters;

    protected override int RollVariant(System.Random rng)
    {
        if (clusters == null || clusters.Count == 0)
        {
            return -1;
        }

        int totalWeight = 0;

        foreach (Stat_Influ_Cluster cluster in clusters)
        {
            if (cluster != null)
            {
                totalWeight += Mathf.Max(0, cluster.weight);
            }
        }

        if (totalWeight <= 0)
        {
            return -1;
        }

        int pick = rng != null
            ? rng.Next(0, totalWeight)
            : UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < clusters.Count; i++)
        {
            Stat_Influ_Cluster cluster = clusters[i];
            if (cluster == null) continue;

            int weight = Mathf.Max(0, cluster.weight);

            if (pick < weight)
            {
                return i;
            }

            pick -= weight;
        }

        return clusters.Count - 1;
    }

    public override void Apply(
        Influ_Context context,
        float roll,
        int variantIndex,
        object source)
    {
        if (context.Stats == null) return;

        Stat_Influ_Cluster cluster = GetCluster(variantIndex);
        if (cluster == null) return;

        foreach (Stat_Modifier_Entry modifier in cluster.Modifiers)
        {
            context.Stats.AddModifier(
                modifier.stat,
                new Player_Stat_Modifier(
                    modifier.value * roll,
                    modifier.modifierType,
                    source));
        }
    }

    public override void Remove(
        Influ_Context context,
        float roll,
        int variantIndex,
        object source)
    {
        if (context.Stats != null)
        {
            context.Stats.RemoveModifiersFromSource(source);
        }
    }

    public override string Describe(float roll, int variantIndex)
    {
        Stat_Influ_Cluster cluster = GetCluster(variantIndex);

        if (cluster == null)
        {
            return base.Describe(roll, variantIndex);
        }

        string info = cluster.Description;

        if (string.IsNullOrWhiteSpace(info))
        {
            info = Description;
        }

        if (string.IsNullOrWhiteSpace(info))
        {
            return DisplayName;
        }

        info = FormatClusterDescription(cluster, info, roll);

        return DisplayName + "\n" + info;
    }

    private Stat_Influ_Cluster GetCluster(int variantIndex)
    {
        if (clusters == null ||
            variantIndex < 0 ||
            variantIndex >= clusters.Count)
        {
            return null;
        }

        return clusters[variantIndex];
    }

    private string FormatClusterDescription(
    Stat_Influ_Cluster cluster,
    string text,
    float roll)
    {
        if (cluster.Modifiers == null || cluster.Modifiers.Count == 0)
        {
            return FormatDescription(text, roll);
        }

        for (int i = 0; i < cluster.Modifiers.Count; i++)
        {
            Stat_Modifier_Entry modifier = cluster.Modifiers[i];
            float rolledValue = modifier.value * roll;

            text = text.Replace(
                "{value" + i + "}",
                FormatModifierValue(modifier, rolledValue));

            text = text.Replace(
                "{value" + i + "Percent}",
                (rolledValue * 100f).ToString("0.##") + "%");
        }

        // {roll} và {rollPercent} đại diện cho modifier đầu tiên.
        // Dùng cho các cụm mà tất cả modifier có cùng giá trị.
        Stat_Modifier_Entry primaryModifier = cluster.Modifiers[0];
        float primaryValue = primaryModifier.value * roll;

        text = text.Replace(
            "{roll}",
            FormatModifierValue(primaryModifier, primaryValue));

        text = text.Replace(
            "{rollPercent}",
            (primaryValue * 100f).ToString("0.##") + "%");

        return text;
    }

    private string FormatModifierValue(
        Stat_Modifier_Entry modifier,
        float value)
    {
        switch (modifier.modifierType)
        {
            case Player_Stat_Modifier_Type.PercentAdd:
                return (value * 100f).ToString("0.##") + "%";

            case Player_Stat_Modifier_Type.PercentMult:
                return "x" + (1f + value).ToString("0.##");

            default:
                return value.ToString("0.##");
        }
    }
}