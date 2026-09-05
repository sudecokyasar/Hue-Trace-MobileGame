using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Ayar")]
    [SerializeField] private string currentDifficulty = "Easy";
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;

    [Header("Görseller")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Panel Geçiþleri")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject gameHUDPanel;

    public void SetDifficulty(string difficulty)
    {
        currentDifficulty = difficulty;
        GenerateLevelButtons();
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

    private void LoadLevel(string difficulty, int levelNumber)
    {
        // 1. Seçilen bilgileri kaydet
        GameDataHolder.SelectedDifficulty = difficulty;
        GameDataHolder.SelectedLevelNumber = levelNumber;

        // 2. Level Select panelini kapat
        gameObject.SetActive(false);

        // 3. Ana menü/mode select canvas'ýný kapat, oyun HUD'unu aç
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (gameHUDPanel != null)
            gameHUDPanel.SetActive(true);

        // 4. GridManager'a yeni level verisini yüklet
        // Sahnedeki GridManager'ý bulup veriyi yüklemesini tetikliyoruz.
        // GridManager.GenerateGrid içindeki OnLevelGenerated eventi sayesinde
        // GameUIController buna otomatik olarak abone olup kendini güncelleyecek.
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
                Debug.LogError($"LevelData bulunamadý: Resources/{path}");
            }
        }
    }
}