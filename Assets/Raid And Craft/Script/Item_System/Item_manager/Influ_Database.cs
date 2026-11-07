using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh sách MỌI Influ của game, để tra theo InfluId khi load save (trang bị lưu id của Influ, không lưu asset).
/// Tạo asset: chuột phải Project -> Create -> Raid And Craft -> Influ Database, rồi chuột phải component -> "Thu thập mọi Influ".
/// Thêm Influ mới thì nhớ thêm vào đây, nếu không món có Influ đó sẽ mất hiệu ứng khi load (có log cảnh báo).
/// </summary>
[CreateAssetMenu(menuName = "Raid And Craft/Influ Database", fileName = "Influ_Database")]
public class Influ_Database : ScriptableObject
{
    [SerializeField] private List<Influ_Data> influs = new List<Influ_Data>();

    private Dictionary<string, Influ_Data> byId;

    public IReadOnlyList<Influ_Data> Influs => influs;

    /// <summary>Tìm Influ theo InfluId. Trả về null nếu không có.</summary>
    public Influ_Data Find(string influId)
    {
        if (string.IsNullOrEmpty(influId)) return null;

        EnsureBuilt();
        return byId.TryGetValue(influId, out Influ_Data influ) ? influ : null;
    }

    private void OnEnable()
    {
        byId = null;
    }

    private void EnsureBuilt()
    {
        if (byId != null) return;

        byId = new Dictionary<string, Influ_Data>(influs.Count);
        foreach (Influ_Data influ in influs)
        {
            if (influ == null) continue;

            if (!byId.TryAdd(influ.InfluId, influ))
            {
                Debug.LogWarning($"[{nameof(Influ_Database)}] Trùng InfluId '{influ.InfluId}' ({influ.name}).", this);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        byId = null;
    }

    [ContextMenu("Thu thập mọi Influ trong project")]
    private void CollectAll()
    {
        influs.Clear();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Influ_Data"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var influ = UnityEditor.AssetDatabase.LoadAssetAtPath<Influ_Data>(path);
            if (influ != null) influs.Add(influ);
        }

        byId = null;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[{nameof(Influ_Database)}] Đã thu thập {influs.Count} Influ.", this);
    }
#endif
}
