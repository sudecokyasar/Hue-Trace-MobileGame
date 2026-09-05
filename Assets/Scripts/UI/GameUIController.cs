using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    [Header("Üst Bilgiler (Header Info)")]
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI modeAndGridText;
    [SerializeField] private Image[] starImageComponents;
    [SerializeField] private Sprite yellowStarSprite;
    [SerializeField] private Sprite greyStarSprite;

    [Header("Sayaçlar (Stats)")]
    [SerializeField] private TextMeshProUGUI moveText;
    [SerializeField] private TextMeshProUGUI optimalText;
    [SerializeField] private TextMeshProUGUI connectionText;

    [Header("Toplam Skor")]
    [SerializeField] private TextMeshProUGUI totalStarsText;

    [Header("Paneller ve Referanslar")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject gameHUDPanel;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PathDrawer pathDrawer;

    [Header("Level Tamamlama")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI levelCompleteTitleText;
    [SerializeField] private Image[] levelCompleteStarImages;
    [SerializeField] private GameObject levelSelectPanel; // "Sonraki level yok" durumunda dönülecek panel
    [SerializeField] private bool autoAdvanceToNextLevel = false;
    [SerializeField] private float autoAdvanceDelay = 1.5f;

    private string currentDifficulty = "Normal";
    private int currentLevelIndex = 1;
    private bool levelCompleted = false;
    private float autoAdvanceTimer = 0f;

    private void OnEnable()
    {
        // GridManager her yeni level oluþturduðunda UI'ý otomatik güncelle
        GridManager.OnLevelGenerated += HandleLevelGenerated;
    }

    private void OnDisable()
    {
        GridManager.OnLevelGenerated -= HandleLevelGenerated;
    }

    private void Start()
    {
        SetupLevelInfo();
    }

    private void Update()
    {
        UpdateStats();

        if (levelCompleted && autoAdvanceToNextLevel)
        {
            autoAdvanceTimer += Time.deltaTime;
            if (autoAdvanceTimer >= autoAdvanceDelay)
            {
                LoadNextLevel();
            }
        }
    }

    private void HandleLevelGenerated(LevelData level)
    {
        // Yeni level yüklendi: tamamlanma durumunu ve panelleri sýfýrla
        levelCompleted = false;
        autoAdvanceTimer = 0f;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        // Yeni level baþlarken hamle sayacý ve çizilen path'ler sýfýrlanmalý
        if (pathDrawer != null)
            pathDrawer.ResetState();

        SetupLevelInfo();
    }

    // Dýþarýdan (ör. LevelSelectManager) manuel olarak da tetiklenebilsin diye public býrakýldý.
    public void RefreshLevelInfo()
    {
        SetupLevelInfo();
    }

    private void SetupLevelInfo()
    {
        string difficulty = GameDataHolder.SelectedDifficulty;
        int levelNumberInt = GameDataHolder.SelectedLevelNumber;

        currentDifficulty = difficulty;
        currentLevelIndex = levelNumberInt;

        if (levelTitleText != null)
            levelTitleText.text = $"Level {levelNumberInt}";

        string englishDifficulty = difficulty switch
        {
            "Easy" => "Easy Mode",
            "Hard" => "Hard Mode",
            _ => "Normal Mode"
        };

        if (gridManager != null && gridManager.CurrentLevel != null)
        {
            var level = gridManager.CurrentLevel;
            if (modeAndGridText != null)
                modeAndGridText.text = $"{englishDifficulty} ({level.gridWidth}x{level.gridHeight} Grid)";

            if (optimalText != null)
                optimalText.text = $"Optimal: {level.optimalMoves} Moves";
        }

        // Level baþýnda yýldýzlar: zorluða göre tahmin deðil,
        // bu levelde daha önce kazanýlmýþ en iyi skor (hiç oynanmamýþsa hepsi gri)
        int previousBestStars = GameProgress.GetBestStars(difficulty, levelNumberInt);
        UpdateStarVisuals(previousBestStars);

        RefreshTotalStarsText();
    }

    private void RefreshTotalStarsText()
    {
        if (totalStarsText == null) return;

        int total = GameProgress.GetTotalStarsAllDifficulties();
        int max = GameProgress.GetMaxPossibleStars();
        totalStarsText.text = $"{total} / {max}";
    }

    private void UpdateStats()
    {
        if (gridManager == null || gridManager.CurrentLevel == null || pathDrawer == null) return;
        if (levelCompleted) return; // Level bittikten sonra sayaçlarý güncellemeye devam etme

        var level = gridManager.CurrentLevel;

        int currentMoves = pathDrawer.MoveCount;
        int maxMoves = level.moveLimit;

        if (moveText != null)
            moveText.text = $"Moves: {currentMoves} / {maxMoves}";

        int totalPairs = level.colorPairs != null ? level.colorPairs.Count : 0;
        int connectedPairs = pathDrawer.GetCompletedConnectionCount();

        if (connectionText != null)
            connectionText.text = $"{connectedPairs} / {totalPairs}";

        // --- Level Tamamlama Kontrolü ---
        if (totalPairs > 0 && connectedPairs >= totalPairs)
        {
            HandleLevelCompleted(currentMoves, level);
        }
    }

    private void HandleLevelCompleted(int movesUsed, LevelData level)
    {
        if (levelCompleted) return;
        levelCompleted = true;
        autoAdvanceTimer = 0f;

        int earnedStars = CalculateStars(movesUsed, level.optimalMoves, level.moveLimit);

        GameProgress.UnlockNextLevel(currentDifficulty, currentLevelIndex);
        GameProgress.SaveBestStars(currentDifficulty, currentLevelIndex, earnedStars);

        UpdateStarVisuals(earnedStars);
        ShowLevelCompletePanel(earnedStars);
        RefreshTotalStarsText(); // Toplam skor anýnda güncellensin
    }

    private int CalculateStars(int movesUsed, int optimalMoves, int moveLimit)
    {
        if (movesUsed > moveLimit) return 0; // Limit aþýldýysa yýldýz yok
        if (movesUsed <= optimalMoves) return 3;

        int midThreshold = optimalMoves + Mathf.Max(1, (moveLimit - optimalMoves) / 2);
        if (movesUsed <= midThreshold) return 2;

        return 1;
    }

    private void ShowLevelCompletePanel(int earnedStars)
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (levelCompleteTitleText != null)
            levelCompleteTitleText.text = $"Level {currentLevelIndex} Complete!";

        if (levelCompleteStarImages != null)
        {
            for (int i = 0; i < levelCompleteStarImages.Length; i++)
            {
                if (levelCompleteStarImages[i] == null) continue;
                levelCompleteStarImages[i].sprite = i < earnedStars ? yellowStarSprite : greyStarSprite;
            }
        }
    }

    public void UpdateStarVisuals(int activeStarCount)
    {
        if (starImageComponents == null) return;

        for (int i = 0; i < starImageComponents.Length; i++)
        {
            if (starImageComponents[i] != null)
            {
                if (i < activeStarCount)
                {
                    starImageComponents[i].sprite = yellowStarSprite;
                }
                else
                {
                    starImageComponents[i].sprite = greyStarSprite;
                }
            }
        }
    }

    // --- Level Complete Panel Butonlarý ---

    public void OnClickNextLevel()
    {
        LoadNextLevel();
    }

    public void OnClickBackToLevelSelect()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (gameHUDPanel != null)
            gameHUDPanel.SetActive(false);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(true);
        else if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);
    }

    private void LoadNextLevel()
    {
        int nextLevelNumber = currentLevelIndex + 1;

        if (nextLevelNumber > GameProgress.TotalLevelsPerDifficulty)
        {
            // Bu zorluktaki son level tamamlandý: level select / menüye dön
            OnClickBackToLevelSelect();
            return;
        }

        string path = $"Levels/{currentDifficulty}/Level_{currentDifficulty}_{nextLevelNumber:00}";
        LevelData nextLevel = Resources.Load<LevelData>(path);

        if (nextLevel == null)
        {
            Debug.LogWarning($"Sonraki level bulunamadý: Resources/{path}. Level select'e dönülüyor.");
            OnClickBackToLevelSelect();
            return;
        }

        // Tahtayý ve hamle sayacýný sýfýrla (menüye dönerken yaptýðýmýzýn aynýsý)
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

        GameDataHolder.SelectedDifficulty = currentDifficulty;
        GameDataHolder.SelectedLevelNumber = nextLevelNumber;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (gridManager != null)
        {
            // GenerateGrid, OnLevelGenerated event'ini tetikler -> HandleLevelGenerated -> SetupLevelInfo
            gridManager.GenerateGrid(nextLevel);
        }
    }

    // --- Alt Butonlarýn Fonksiyonlarý ---

    public void OnClickRestart()
    {
        // TRY AGAIN (RETRY): Tüm path line'larý temizler ve hamleyi sýfýrlar
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }
    }

    public void OnClickUndo()
    {
        // UNDO: Yapýlan son path line'ý temizler
        if (pathDrawer != null)
        {
            pathDrawer.UndoLastLine();
        }
    }

    public void OnClickHint()
    {
        Debug.Log("Get Clue button clicked.");
    }

    public void OnClickMenu()
    {
        // Ana menüye dönerken hem tahtayý temizle hem de hamle sayacýný sýfýrla
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

        // Win ekraný (level complete paneli) açýksa onu da kapat
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.transform.root.gameObject.SetActive(true);
            mainMenuCanvas.SetActive(true);
        }
        if (gameHUDPanel != null)
            gameHUDPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}