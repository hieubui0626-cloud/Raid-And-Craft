using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu: Continue / New Game / Quit. Đặt trong Scene Main Menu, nối OnClick của các Button vào OnContinue / OnNewGame / OnQuit.
/// Scene Main Menu cần Save_Manager (object gốc, KHÔNG nằm dưới object cha để DontDestroyOnLoad hoạt động) và Input_Manager.
/// - Continue: nạp Scene base ghi trong save, Player ở đó nạp phần còn lại. Save cũ không ghi Scene thì dùng New Game Scene.
/// - New Game: xoá save rồi nạp New Game Scene.
/// Mọi Scene nạp được phải có trong Build Settings.
/// </summary>
public class Main_Menu : MonoBehaviour
{
    [Tooltip("Scene base đầu tiên của game (New Game vào đây; Continue dùng khi save chưa ghi Scene).")]
    [SerializeField] private string newGameScene;

    [Tooltip("Nút Continue (tuỳ chọn): kéo vào để tự khoá khi chưa có save.")]
    [SerializeField] private Button continueButton;

    private void Start()
    {
        if (continueButton != null) continueButton.interactable = Save_Manager.Instance != null && Save_Manager.Instance.HasSave;
    }

    public void OnContinue()
    {
        if (Save_Manager.Instance == null || !Save_Manager.Instance.TryGetSavedBaseScene(out string sceneName))
        {
            Debug.LogWarning($"[{nameof(Main_Menu)}] Không có save để Continue.");
            return;
        }

        Load(string.IsNullOrEmpty(sceneName) ? newGameScene : sceneName);
    }

    public void OnNewGame()
    {
        if (Save_Manager.Instance != null)
        {
            Save_Manager.Instance.DeleteSave();
            Save_Manager.Instance.LastBaseScene = string.Empty;
        }

        Load(newGameScene);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[{nameof(Main_Menu)}] Scene '{sceneName}' trống hoặc không có trong Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
