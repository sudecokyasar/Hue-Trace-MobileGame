using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Ayar")]
    [SerializeField] private string currentDifficulty = "Easy";
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private ScrollRect scrollRect; 

    [Header("UI Metinleri")]
    [SerializeField] private TextMeshProUGUI difficultyTitleText;

    [Header("G�rseller")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Panel Ge�i�leri")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject gameHUDPanel;

    private void OnEnable()
    {
        ResetScrollPosition();
    }

    public void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
           
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
            scrollRect.velocity = Vector2.zero; 
        }
    }

    public void SetDifficulty(string difficulty)
    {
        currentDifficulty = difficulty;
        UpdateDifficultyUI();
        GenerateLevelButtons();
        ResetScrollPosition();
    }

    private void UpdateDifficultyUI()
    {
        if (difficultyTitleText != null)
        {
            difficultyTitleText.text = currentDifficulty.ToUpper();
        }
    }

    public void GenerateLevelButtons()
    {
        if (levelButtonContainer == null || levelButtonPrefab == null) return;

        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

        int unlockedLevel = GameProgress.GetUnlockedLevel(currentDifficulty);
        int totalLevelsInMode = GameProgress.TotalLevelsPerDifficulty;

        for (int i = 1; i <= totalLevelsInMode; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Image img = btnObj.GetComponent<Image>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = i.ToString();

            bool isUnlocked = i <= unlockedLevel;

            if (isUnlocked)
            {
                if (img != null) img.color = unlockedColor;
                btn.interactable = true;

                int levelIndex = i;
                btn.onClick.AddListener(() => LoadLevel(currentDifficulty, levelIndex));
            }
            else
            {
                if (img != null) img.color = lockedColor;
                btn.interactable = false;
            }
        }
    }

    public void LoadLevel(string difficulty, int levelNumber)
    {
        GameDataHolder.SelectedDifficulty = difficulty;
        GameDataHolder.SelectedLevelNumber = levelNumber;

        gameObject.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (gameHUDPanel != null)
            gameHUDPanel.SetActive(true);

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            string path = $"Levels/{difficulty}/Level_{difficulty}_{levelNumber:00}";
            LevelData loadedLevel = Resources.Load<LevelData>(path);

            if (loadedLevel != null)
            {
                gridManager.GenerateGrid(loadedLevel);
            }
            else
            {
                Debug.LogError($"LevelData bulunamad�: Resources/{path}");
            }
        }
    }
}