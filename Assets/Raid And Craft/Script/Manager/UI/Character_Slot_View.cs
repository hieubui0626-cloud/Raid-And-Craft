using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô nhân vật ở màn chọn nhân vật: hiện thẻ nhân vật (tên, level, lớp) hoặc ô "+" nếu trống.
/// Dùng cho giao diện TỰ VẼ: gắn lên mỗi ô, kéo các phần vào (phần nào không có thì để trống), rồi kéo các ô vào danh sách Slot Views
/// của Character_Select_Menu theo thứ tự (ô đầu = nhân vật 1...). Menu tự dựng cũng dùng lớp này.
/// - Filled Root: object thẻ nhân vật (bật khi có nhân vật). Empty Root: object ô "+" (bật khi trống). Có thể dùng chung một ô nếu chỉ khác nội dung.
/// - Select Button: bấm thẻ để chọn. Add Button: bấm "+" để tạo nhân vật. Delete Button: nút X.
/// - Selected Mark: object hiện khi ô đang được chọn (viền sáng...).
/// </summary>
public class Character_Slot_View : MonoBehaviour
{
    [SerializeField] private GameObject filledRoot;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text classText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject selectedMark;

    private int index;
    private bool wired;
    private Image highlightImage;
    private Color normalColor;
    private bool highlightReady;

    private static readonly Color SelectedTint = new Color(1f, 0.95f, 0.6f, 1f);

    /// <summary>
    /// Dùng khi dựng từ PREFAB (Character_Select_Menu: Slot Panel + hai prefab). Tự tìm các phần theo TÊN object con:
    /// Character_Name, Character_Level, Character_Master_Class (chữ), Delete_Character (nút X); bấm vào thẻ = chọn (Button gốc, tự thêm nếu thiếu);
    /// ô "+" bấm vào = tạo (Button gốc, tự thêm nếu thiếu). Không có Selected Mark thì thẻ được chọn đổi màu nền.
    /// </summary>
    public void BindPrefabs(GameObject filled, GameObject create)
    {
        filledRoot = filled;
        emptyRoot = create;

        if (filled != null)
        {
            nameText = FindText(filled.transform, "Character_Name");
            levelText = FindText(filled.transform, "Character_Level");
            classText = FindText(filled.transform, "Character_Master_Class");

            Transform del = FindChild(filled.transform, "Delete_Character");
            deleteButton = del != null ? del.GetComponent<Button>() : null;
            if (del != null && deleteButton == null) deleteButton = del.gameObject.AddComponent<Button>();

            selectButton = filled.GetComponent<Button>();
            if (selectButton == null) selectButton = filled.AddComponent<Button>();
            if (selectButton.targetGraphic == null) selectButton.targetGraphic = filled.GetComponent<Graphic>();
            highlightImage = filled.GetComponent<Image>();
        }

        if (create != null)
        {
            // Prefab đã có sẵn Button (ở gốc hoặc nút "+" bên trong) thì dùng nó; chưa có thì thêm vào gốc.
            addButton = create.GetComponentInChildren<Button>(true);
            if (addButton == null) addButton = create.AddComponent<Button>();
            if (addButton.targetGraphic == null) addButton.targetGraphic = create.GetComponent<Graphic>();
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != root && t.name == childName) return t;
        }

        return null;
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        Transform t = FindChild(root, childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>Dùng khi dựng bằng code (menu tự dựng).</summary>
    public void Bind(GameObject filled, GameObject empty, TMP_Text name, TMP_Text level, TMP_Text cls, Button select, Button add, Button delete, GameObject selected)
    {
        filledRoot = filled; emptyRoot = empty; nameText = name; levelText = level; classText = cls;
        selectButton = select; addButton = add; deleteButton = delete; selectedMark = selected;
    }

    /// <summary>Nối nút bấm với menu (gọi một lần). index = vị trí ô trong danh sách.</summary>
    public void Setup(int slotIndex, Action<int> onSelect, Action<int> onCreate, Action<int> onDelete)
    {
        index = slotIndex;
        if (wired) return;
        wired = true;

        if (selectButton != null) selectButton.onClick.AddListener(() => onSelect(index));
        if (addButton != null) addButton.onClick.AddListener(() => onCreate(index));
        if (deleteButton != null) deleteButton.onClick.AddListener(() => onDelete(index));
    }

    public void Show(Save_Manager.Character_Summary summary, bool selected)
    {
        bool filled = summary.exists;
        if (filledRoot != null) filledRoot.SetActive(filled);
        if (emptyRoot != null) emptyRoot.SetActive(!filled);

        if (nameText != null) nameText.text = summary.name;
        if (levelText != null) levelText.text = "Level: " + summary.level;
        if (classText != null) classText.text = "Class: " + (string.IsNullOrEmpty(summary.className) ? "-" : summary.className);
        if (selectedMark != null) selectedMark.SetActive(filled && selected);

        // Không có Selected Mark: đổi màu nền thẻ (chỉ khi dựng từ prefab, highlightImage được gán ở BindPrefabs).
        if (highlightImage != null)
        {
            if (!highlightReady) { normalColor = highlightImage.color; highlightReady = true; }
            highlightImage.color = filled && selected ? SelectedTint : normalColor;
        }
    }
}
