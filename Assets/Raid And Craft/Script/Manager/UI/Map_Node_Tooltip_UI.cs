using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tooltip khi rê chuột vào node trên World Map: tên node, loại, level, nguyên liệu có thể nhận, trạng thái.
/// Level và nguyên liệu lấy từ Scene_Rules của node (Map_Node_Data.rules) SAU KHI áp modifier của node, nên khi bạn gắn
/// map modifier vào node thì tooltip tự đổi theo. Node chưa có rules: Level hiện "?" và không liệt kê nguyên liệu.
/// Tự tạo khi cần, tự dựng giao diện bằng code, đi theo con trỏ, không nhận chuột.
/// </summary>
public class Map_Node_Tooltip_UI : MonoBehaviour
{
    private const float PanelWidth = 300f;
    private const float Padding = 12f;
    private const float CursorOffset = 18f;

    private const string MutedColor = "#8FA8C8";
    private const string LockedColor = "#E07070";
    private const string UnlockedColor = "#7FD6FF";
    private const string CompleteColor = "#8FE08F";

    private static Map_Node_Tooltip_UI instance;

    public static Map_Node_Tooltip_UI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<Map_Node_Tooltip_UI>();
            if (instance == null) instance = new GameObject("Map_Node_Tooltip_UI").AddComponent<Map_Node_Tooltip_UI>();
            return instance;
        }
    }

    /// <summary>Ẩn nếu đã được tạo (không tạo mới chỉ để ẩn).</summary>
    public static void HideIfExists()
    {
        if (instance != null) instance.Hide();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private Canvas canvas;
    private RectTransform panel;
    private TextMeshProUGUI text;
    private readonly StringBuilder sb = new StringBuilder();

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        if (instance == null) instance = this;

        // Trên World Map (sorting 40) và các tooltip khác.
        canvas = UI_Builder.CreateTooltip("Map_Node_Tooltip_Canvas", transform, 130, PanelWidth, Padding, out panel, out text);
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        if (IsVisible) FollowCursor();
    }

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    public void Show(Map_Node_Data node, Map_Node_State state)
    {
        if (node == null) return;

        sb.Clear();
        sb.Append("<size=120%><b>").Append(node.DisplayNameOrId).Append("</b></size>\n");
        sb.Append("<color=").Append(MutedColor).Append('>').Append(node.type == Map_Node_Type.Base ? "Base" : "Instance").Append("</color>\n");

        if (!string.IsNullOrWhiteSpace(node.description)) sb.Append(node.description.Trim()).Append('\n');

        Scene_Rule_Values values = node.BuildValues();

        // Base không có quái nên bỏ Level/nguyên liệu (chỉ hiện nếu bạn cố ý gán Scene_Rules cho Base).
        if (node.type == Map_Node_Type.Instance || values != null)
        {
            sb.Append("\nLevel: ").Append(values != null ? values.Level.ToString() : "?").Append('\n');
            AppendMaterials(values);
        }

        sb.Append("\nStatus: ").Append(StateText(node.type, state));

        text.text = sb.ToString();
        canvas.gameObject.SetActive(true);

        // Đặt vị trí ngay để khung không nhấp nháy ở chỗ cũ một frame.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        FollowCursor();
    }

    private void AppendMaterials(Scene_Rule_Values values)
    {
        if (values == null) return;

        // Họ của Scene + họ do modifier thêm; mỗi họ hiện món đúng tier chính.
        var names = new List<string>();
        AddFamilies(values.Base.MaterialFamilies, values, names);
        AddFamilies(values.extraFamilies, values, names);

        sb.Append("Materials: ");
        if (names.Count == 0)
        {
            sb.Append("<color=").Append(MutedColor).Append(">none</color>\n");
            return;
        }

        sb.Append('\n');
        foreach (string name in names) sb.Append("  - ").Append(name).Append('\n');
    }

    private static void AddFamilies(IReadOnlyList<Scene_Family_Entry> families, Scene_Rule_Values values, List<string> names)
    {
        foreach (Scene_Family_Entry entry in families)
        {
            if (entry.family == null || entry.weight <= 0f) continue;

            Material_Item_Data item = entry.family.GetForTier(values.MainTier);
            if (item == null) continue;

            string label = item.DisplayName + " (T" + item.RarityTier + ")";
            if (!names.Contains(label)) names.Add(label);
        }
    }

    private static string StateText(Map_Node_Type type, Map_Node_State state)
    {
        switch (state)
        {
            case Map_Node_State.Complete: return "<color=" + CompleteColor + ">Complete</color>";
            case Map_Node_State.Unlocked: return "<color=" + UnlockedColor + ">Unlocked</color>";
            default: return "<color=" + LockedColor + ">Locked</color>";
        }
    }

    // Góc trên-trái của bảng đặt chếch dưới-phải con trỏ; không đủ chỗ thì lật sang trái/lên trên.
    private void FollowCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 cursor = mouse.position.ReadValue();
        float scale = canvas.scaleFactor;
        float width = panel.rect.width * scale;
        float height = panel.rect.height * scale;

        bool flipX = cursor.x + CursorOffset + width > Screen.width;
        bool flipY = cursor.y - CursorOffset - height < 0f;

        panel.pivot = new Vector2(flipX ? 1f : 0f, flipY ? 0f : 1f);
        panel.position = new Vector2(
            cursor.x + (flipX ? -CursorOffset : CursorOffset),
            cursor.y + (flipY ? CursorOffset : -CursorOffset));
    }
}
