using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn chọn / tạo nhân vật (Scene riêng, nằm giữa Main Menu và game). Mỗi nhân vật là một slot save (Save_Manager: slot_0, slot_1...).
/// - Ô trống "+": tạo nhân vật mới (nhập tên) rồi vào New Character Scene (Scene base đầu tiên). Nhân vật mới bắt đầu từ Player prefab có sẵn trong Scene đó.
/// - Ô có nhân vật: bấm để chọn, nút Play (Continue) nạp đúng Scene base đã lưu của nhân vật đó. X (bấm hai lần) xoá nhân vật.
/// - Nút Back về Main Menu (tên Scene lấy từ Save_Manager).
/// Giao diện: không gán gì thì tự dựng. Có giao diện tự vẽ thì gắn Character_Slot_View lên từng ô, kéo vào Slot Views, và kéo Play/Back/Create Panel... vào các ô bên dưới.
/// Scene này cần Save_Manager (nếu Scene bắt đầu của game là Main Menu thì Save_Manager đã sống từ đó; để chạy thử riêng Scene này, đặt thêm một Save_Manager).
/// </summary>
public class Character_Select_Menu : MonoBehaviour
{
    private const float ConfirmSeconds = 4f;
    private const int MaxNameLength = 16;

    [Header("Scene")]
    [Tooltip("Scene base đầu tiên cho nhân vật MỚI (ví dụ Base_1). Phải có trong Build Settings.")]
    [SerializeField] private string newCharacterScene;

    [Tooltip("Nhân vật cũ mà save chưa ghi Scene thì vào Scene này (thường cũng là Base_1).")]
    [SerializeField] private string defaultContinueScene;

    [Header("Giao diện từ PREFAB (khuyên dùng khi đã có prefab thẻ nhân vật và ô '+')")]
    [Tooltip("Object chứa danh sách (Content của ScrollView hoặc panel có Vertical Layout Group). Điền cùng hai prefab bên dưới thì menu tự tạo mỗi nhân vật một thẻ / ô '+'.")]
    [SerializeField] private RectTransform slotPanel;

    [Tooltip("Prefab thẻ nhân vật. Cần các object con tên Character_Name, Character_Level, Character_Master_Class (TMP) và Delete_Character (nút X).")]
    [SerializeField] private GameObject characterSlotPrefab;

    [Tooltip("Prefab ô '+' (tạo nhân vật mới). Bấm vào bất kỳ chỗ nào của ô là tạo.")]
    [SerializeField] private GameObject createSlotPrefab;

    [Header("Giao diện tự vẽ từng ô (tuỳ chọn, thay cho prefab)")]
    [Tooltip("Các ô nhân vật theo thứ tự (ô đầu = nhân vật 1). Số ô nên bằng Save_Manager.Max Character Slots.")]
    [SerializeField] private List<Character_Slot_View> slotViews = new List<Character_Slot_View>();
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    [Tooltip("Bảng tạo nhân vật (bật khi bấm '+'): chứa ô nhập tên, nút Tạo, nút Huỷ.")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button createConfirmButton;
    [SerializeField] private Button createCancelButton;

    [Tooltip("Chỗ hiện thông báo (xác nhận xoá, tên trống...). Trống thì chỉ in Console.")]
    [SerializeField] private TextMeshProUGUI statusText;

    private int selected = -1;
    private int creatingIndex = -1;
    private int deleteArmedIndex = -1;
    private float deleteArmedUntil;
    private bool loading;
    private Save_Manager.Character_Summary[] summaries;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();

        if (slotViews.Count == 0 && slotPanel != null && characterSlotPrefab != null && createSlotPrefab != null) BuildFromPrefabs();
        else if (slotViews.Count == 0) BuildDefaultUI();

        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
        if (createConfirmButton != null) createConfirmButton.onClick.AddListener(OnCreateConfirm);
        if (createCancelButton != null) createCancelButton.onClick.AddListener(CloseCreatePanel);
        if (createPanel != null) createPanel.SetActive(false);

        for (int i = 0; i < slotViews.Count; i++) slotViews[i].Setup(i, OnSelect, OnCreate, OnDelete);
    }

    private void Start()
    {
        if (Save_Manager.Instance == null)
        {
            Debug.LogError($"[{nameof(Character_Select_Menu)}] Không có Save_Manager trong Scene (cần để đọc/lưu nhân vật).", this);
            return;
        }

        Refresh();
    }

    private void Update()
    {
        if (deleteArmedIndex >= 0 && Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedIndex = -1;
            SetStatus(string.Empty);
        }
    }

    // ------------------------------------------------------------------
    // Danh sách
    // ------------------------------------------------------------------

    private void Refresh()
    {
        Save_Manager manager = Save_Manager.Instance;
        if (manager == null) return;

        int count = slotViews.Count;
        summaries = new Save_Manager.Character_Summary[count];

        for (int i = 0; i < count; i++)
        {
            summaries[i] = manager.GetSummary(i);
            if (i == selected && !summaries[i].exists) selected = -1;
        }

        // Chưa chọn ai thì chọn nhân vật đầu tiên đang có (đỡ phải bấm thêm để Play).
        if (selected < 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (summaries[i].exists) { selected = i; break; }
            }
        }

        for (int i = 0; i < count; i++) slotViews[i].Show(summaries[i], i == selected);
        if (playButton != null) playButton.interactable = selected >= 0;
    }

    private void OnSelect(int index)
    {
        if (index < 0 || index >= summaries.Length || !summaries[index].exists) return;

        selected = index;
        Refresh();
    }

    // ------------------------------------------------------------------
    // Tạo / xoá / chơi
    // ------------------------------------------------------------------

    private void OnCreate(int index)
    {
        if (loading) return;

        creatingIndex = index;
        if (createPanel != null)
        {
            createPanel.SetActive(true);
            if (nameInput != null)
            {
                nameInput.text = string.Empty;
                nameInput.characterLimit = MaxNameLength;
                nameInput.ActivateInputField();
            }
        }
        else
        {
            // Giao diện tự vẽ không có bảng tạo: đặt tên mặc định.
            CreateAndStart("Hero " + (index + 1));
        }
    }

    private void OnCreateConfirm()
    {
        string typed = nameInput != null ? nameInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(typed))
        {
            SetStatus("Hãy nhập tên nhân vật.");
            return;
        }

        CreateAndStart(typed);
    }

    private void CloseCreatePanel()
    {
        creatingIndex = -1;
        if (createPanel != null) createPanel.SetActive(false);
        SetStatus(string.Empty);
    }

    private void CreateAndStart(string characterName)
    {
        if (loading || creatingIndex < 0 || Save_Manager.Instance == null) return;

        Save_Manager.Instance.CreateCharacter(creatingIndex, characterName);
        Load(newCharacterScene);
    }

    private void OnDelete(int index)
    {
        if (loading || index < 0 || index >= summaries.Length || !summaries[index].exists) return;

        // Bấm hai lần trong ConfirmSeconds mới xoá (tránh mất nhân vật vì bấm nhầm).
        if (deleteArmedIndex != index || Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedIndex = index;
            deleteArmedUntil = Time.unscaledTime + ConfirmSeconds;
            SetStatus($"Bấm X lần nữa để XOÁ nhân vật '{summaries[index].name}'.");
            return;
        }

        deleteArmedIndex = -1;
        Save_Manager.Instance.DeleteCharacter(index);
        if (selected == index) selected = -1;
        SetStatus(string.Empty);
        Refresh();
    }

    private void OnPlay()
    {
        if (loading || selected < 0 || Save_Manager.Instance == null || !summaries[selected].exists) return;

        Save_Manager manager = Save_Manager.Instance;
        manager.SelectSlot(selected);

        // Scene base đã lưu của nhân vật; save cũ không ghi thì dùng Default Continue Scene (rồi tới New Character Scene).
        string scene = summaries[selected].baseScene;
        if (string.IsNullOrEmpty(scene)) scene = string.IsNullOrEmpty(defaultContinueScene) ? newCharacterScene : defaultContinueScene;
        Load(scene);
    }

    private void OnBack()
    {
        if (loading) return;

        string menu = Save_Manager.Instance != null ? Save_Manager.Instance.MainMenuScene : string.Empty;
        Load(menu);
    }

    private void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[{nameof(Character_Select_Menu)}] Scene '{sceneName}' trống hoặc không có trong Build Settings.", this);
            SetStatus("Không vào được: Scene chưa sẵn sàng (xem Console).");
            return;
        }

        loading = true;
        SceneManager.LoadScene(sceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        if (!string.IsNullOrEmpty(message)) Debug.Log($"[{nameof(Character_Select_Menu)}] {message}");
    }

    // ------------------------------------------------------------------
    // GIAO DIỆN TỰ DỰNG
    // ------------------------------------------------------------------

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static readonly Color CardColor = new Color(0.94f, 0.94f, 0.94f, 1f);
    private static readonly Color EmptyColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    private static readonly Color DarkText = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color ButtonColor = new Color(0.14f, 0.2f, 0.3f, 1f);

    // Mỗi nhân vật (slot) = một thẻ + một ô "+" cạnh nhau trong panel, chỉ một trong hai bật tuỳ slot có nhân vật hay chưa.
    private void BuildFromPrefabs()
    {
        Save_Manager manager = Save_Manager.Instance;
        int count = manager != null ? manager.MaxCharacterSlots : 5;

        // Dọn bản mẫu đặt sẵn trong Scene (object con có tên bắt đầu bằng tên prefab) để khỏi trùng với thẻ tự tạo. Object khác (thanh cuộn...) giữ nguyên.
        for (int i = slotPanel.childCount - 1; i >= 0; i--)
        {
            Transform child = slotPanel.GetChild(i);
            if (child.name.StartsWith(characterSlotPrefab.name) || child.name.StartsWith(createSlotPrefab.name)) Destroy(child.gameObject);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject filled = Instantiate(characterSlotPrefab, slotPanel, false);
            GameObject create = Instantiate(createSlotPrefab, slotPanel, false);
            filled.name = "Character_Slot_" + (i + 1);
            create.name = "Character_Create_" + (i + 1);

            var view = filled.AddComponent<Character_Slot_View>();
            view.BindPrefabs(filled, create);
            slotViews.Add(view);
        }
    }

    private void BuildDefaultUI()
    {
        Save_Manager manager = Save_Manager.Instance;
        int count = manager != null ? manager.MaxCharacterSlots : 5;

        Canvas canvas = UI_Builder.CreateOverlayCanvas("Character_Select_Canvas", transform, 10, true, true);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvas.transform, false);
        UI_Builder.Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 1f);

        // Cột danh sách nhân vật bên trái.
        var column = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        column.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)column.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(80f, 0f);
        rect.sizeDelta = new Vector2(420f, 0f);
        var layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        column.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < count; i++) slotViews.Add(BuildSlot(rect, i));

        // Nút Play / Back bên phải.
        var right = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        right.transform.SetParent(canvas.transform, false);
        var rightRect = (RectTransform)right.transform;
        rightRect.anchorMin = rightRect.anchorMax = rightRect.pivot = new Vector2(1f, 0f);
        rightRect.anchoredPosition = new Vector2(-120f, 120f);
        rightRect.sizeDelta = new Vector2(320f, 0f);
        var rightLayout = right.GetComponent<VerticalLayoutGroup>();
        rightLayout.spacing = 12f;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;
        right.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        playButton = AddButton(rightRect, "Play", 60f);
        backButton = AddButton(rightRect, "Back", 48f);

        statusText = UI_Builder.CreateText("Status", rightRect, 20f, TextAlignmentOptions.Center);
        statusText.color = new Color(1f, 0.75f, 0.45f);
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

        BuildCreatePanel(canvas.transform);
    }

    private Character_Slot_View BuildSlot(RectTransform parent, int index)
    {
        var root = new GameObject("Slot_" + index, typeof(RectTransform), typeof(LayoutElement), typeof(Character_Slot_View));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = 110f;

        // Thẻ nhân vật.
        var filled = new GameObject("Filled", typeof(RectTransform), typeof(Image), typeof(Button));
        filled.transform.SetParent(root.transform, false);
        UI_Builder.Stretch((RectTransform)filled.transform);
        var filledImage = filled.GetComponent<Image>();
        filledImage.color = CardColor;
        var select = filled.GetComponent<Button>();
        select.targetGraphic = filledImage;

        TextMeshProUGUI nameText = SlotText(filled.transform, "Name", 26f, TextAlignmentOptions.TopLeft, new Vector2(16f, -8f));
        TextMeshProUGUI levelText = SlotText(filled.transform, "Level", 20f, TextAlignmentOptions.TopLeft, new Vector2(16f, -48f));
        TextMeshProUGUI classText = SlotText(filled.transform, "Class", 20f, TextAlignmentOptions.TopLeft, new Vector2(16f, -76f));
        nameText.fontStyle = FontStyles.Bold;

        var deleteGo = new GameObject("Delete", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteGo.transform.SetParent(filled.transform, false);
        var deleteRect = (RectTransform)deleteGo.transform;
        deleteRect.anchorMin = deleteRect.anchorMax = deleteRect.pivot = new Vector2(1f, 1f);
        deleteRect.anchoredPosition = new Vector2(-6f, -6f);
        deleteRect.sizeDelta = new Vector2(34f, 34f);
        var deleteImage = deleteGo.GetComponent<Image>();
        deleteImage.color = new Color(0.8f, 0.3f, 0.3f, 1f);
        var delete = deleteGo.GetComponent<Button>();
        delete.targetGraphic = deleteImage;
        TextMeshProUGUI x = UI_Builder.CreateText("Text", deleteGo.transform, 22f, TextAlignmentOptions.Center);
        UI_Builder.Stretch((RectTransform)x.transform);
        x.text = "X";

        // Viền chọn: một khung sáng nằm sau thẻ.
        var mark = new GameObject("Selected", typeof(RectTransform), typeof(Image));
        mark.transform.SetParent(root.transform, false);
        mark.transform.SetAsFirstSibling();
        UI_Builder.Stretch((RectTransform)mark.transform, -5f);
        mark.GetComponent<Image>().color = new Color(1f, 0.8f, 0.3f, 1f);
        mark.GetComponent<Image>().raycastTarget = false;

        // Ô trống "+".
        var empty = new GameObject("Empty", typeof(RectTransform), typeof(Image), typeof(Button));
        empty.transform.SetParent(root.transform, false);
        UI_Builder.Stretch((RectTransform)empty.transform);
        var emptyImage = empty.GetComponent<Image>();
        emptyImage.color = EmptyColor;
        var add = empty.GetComponent<Button>();
        add.targetGraphic = emptyImage;
        TextMeshProUGUI plus = UI_Builder.CreateText("Plus", empty.transform, 64f, TextAlignmentOptions.Center);
        UI_Builder.Stretch((RectTransform)plus.transform);
        plus.text = "+";
        plus.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        plus.raycastTarget = false;

        var view = root.GetComponent<Character_Slot_View>();
        view.Bind(filled, empty, nameText, levelText, classText, select, add, delete, mark);
        return view;
    }

    private static TextMeshProUGUI SlotText(Transform parent, string objectName, float size, TextAlignmentOptions alignment, Vector2 position)
    {
        TextMeshProUGUI tmp = UI_Builder.CreateText(objectName, parent, size, alignment);
        var rect = (RectTransform)tmp.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(-60f, size + 8f);
        tmp.color = DarkText;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button AddButton(RectTransform parent, string label, float height)
    {
        var go = new GameObject(label + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = ButtonColor;
        go.GetComponent<LayoutElement>().preferredHeight = height;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI text = UI_Builder.CreateText("Text", go.transform, 26f, TextAlignmentOptions.Center);
        UI_Builder.Stretch((RectTransform)text.transform, 12f);
        text.text = label;
        return button;
    }

    private void BuildCreatePanel(Transform canvasTransform)
    {
        createPanel = new GameObject("Create_Panel", typeof(RectTransform), typeof(Image));
        createPanel.transform.SetParent(canvasTransform, false);
        UI_Builder.Stretch((RectTransform)createPanel.transform);
        createPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        box.transform.SetParent(createPanel.transform, false);
        var boxRect = (RectTransform)box.transform;
        boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(480f, 0f);
        box.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 1f);
        var layout = box.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        box.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = UI_Builder.CreateText("Title", boxRect, 30f, TextAlignmentOptions.Center);
        title.text = "Tạo nhân vật";
        title.fontStyle = FontStyles.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        nameInput = BuildInputField(boxRect);
        createConfirmButton = AddButton(boxRect, "Tạo", 50f);
        createCancelButton = AddButton(boxRect, "Huỷ", 44f);
    }

    private static TMP_InputField BuildInputField(RectTransform parent)
    {
        var go = new GameObject("Name_Input", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.2f, 1f);
        go.GetComponent<LayoutElement>().preferredHeight = 52f;

        var area = new GameObject("Text_Area", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(go.transform, false);
        UI_Builder.Stretch((RectTransform)area.transform, 10f);

        TextMeshProUGUI text = UI_Builder.CreateText("Text", area.transform, 26f, TextAlignmentOptions.Left);
        UI_Builder.Stretch((RectTransform)text.transform);

        TextMeshProUGUI placeholder = UI_Builder.CreateText("Placeholder", area.transform, 26f, TextAlignmentOptions.Left);
        UI_Builder.Stretch((RectTransform)placeholder.transform);
        placeholder.text = "Tên nhân vật...";
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);

        // Thêm TMP_InputField SAU khi đã có đủ phần con (component tự cấu hình khi bật).
        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = (RectTransform)area.transform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = MaxNameLength;
        return input;
    }
}
