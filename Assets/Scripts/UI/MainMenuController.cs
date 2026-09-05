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