using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneller")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject levelSelectPanel;

    [Header("Seviye Seçim Yöneticisi")]
    [SerializeField] private LevelSelectManager levelSelectManager;

    private void Start()
    {
        ShowMainPanel();
    }

    public void ShowMainPanel()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (modeSelectPanel) modeSelectPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

    public void OnClickPlayButton()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (modeSelectPanel) modeSelectPanel.SetActive(true);
    }

    public void OnClickContinueButton()
    {
        // En son oynanan veya varsayýlan zorluk seviyesini al
        string lastDifficulty = string.IsNullOrEmpty(GameDataHolder.SelectedDifficulty)
            ? "Easy"
            : GameDataHolder.SelectedDifficulty;

        // O zorlukta kalýnan en yüksek açýk seviyeyi al
        int currentUnlockedLevel = GameProgress.GetUnlockedLevel(lastDifficulty);

        // Seviyeyi doðrudan baþlat
        if (levelSelectManager != null)
        {
            levelSelectManager.LoadLevel(lastDifficulty, currentUnlockedLevel);
        }
    }

    public void OnClickExitButton()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickSelectMode(string difficulty)
    {
        if (modeSelectPanel) modeSelectPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);

        if (levelSelectManager != null)
        {
            levelSelectManager.SetDifficulty(difficulty);
        }
    }

    public void OnClickBackToMain()
    {
        ShowMainPanel();
    }

    public void OnClickBackToModes()
    {
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        if (modeSelectPanel) modeSelectPanel.SetActive(true);
    }
}