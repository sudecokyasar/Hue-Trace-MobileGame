using System.Collections;
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
    [SerializeField] private TextMeshProUGUI levelStarsText;

    [Header("Paneller ve Referanslar")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject gameHUDPanel;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PathDrawer pathDrawer;

    [Header("Yıldız - Grid Doluluk Eşikleri")]
    // Yıldız artık hamle sayısına göre değil, oyuncunun board'u ne kadar
    // doldurduğuna göre hesaplanıyor:
    // - Doluluk oranı bu eşiğin altındaysa (hiç odaklanmadıysa) -> 1 yıldız
    // - Bu eşiğe ulaştıysa (biraz odaklandıysa) -> 2 yıldız
    // - Board'un TAMAMINI doldurduysa (eksiksiz) -> 3 yıldız
    [SerializeField][Range(0f, 1f)] private float twoStarFillThreshold = 0.5f;

    [Header("Level Tamamlama")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI levelCompleteTitleText;
    [SerializeField] private Image[] levelCompleteStarImages;
    [SerializeField] private GameObject levelSelectPanel; 
    [SerializeField] private bool autoAdvanceToNextLevel = false;
    [SerializeField] private float autoAdvanceDelay = 1.5f;

    [Header("Level Tamamlama Gecikmesi")] 
    [SerializeField] private float levelCompleteDelay = 0.5f;

    private string currentDifficulty = "Normal";
    private int currentLevelIndex = 1;
    private bool levelCompleted = false;
    private bool completionTriggered = false; 
    private float autoAdvanceTimer = 0f;

    private void OnEnable()
    {
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
        levelCompleted = false;
        completionTriggered = false;
        autoAdvanceTimer = 0f;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (pathDrawer != null)
            pathDrawer.ResetState();

        SetupLevelInfo();
    }

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

        
        int previousBestStars = GameProgress.GetBestStars(difficulty, levelNumberInt);
        UpdateStarVisuals(previousBestStars);

        RefreshTotalStarsText();
    }

    private void RefreshTotalStarsText()
    {
        int total = GameProgress.GetTotalStarsAllDifficulties();
        int max = GameProgress.GetMaxPossibleStars();
        string totalStarsString = $"{total} / {max}";

        if (totalStarsText != null)
            totalStarsText.text = totalStarsString;

        if (levelStarsText != null)
            levelStarsText.text = totalStarsString;
    }

    private void UpdateStats()
    {
        if (gridManager == null || gridManager.CurrentLevel == null || pathDrawer == null) return;
        if (levelCompleted) return; // Level bittikten sonra sayaçları güncellemeye devam etme

        var level = gridManager.CurrentLevel;

        int currentMoves = pathDrawer.MoveCount;
        int maxMoves = level.moveLimit;

        if (moveText != null)
            moveText.text = $"Moves: {currentMoves} / {maxMoves}";

        int totalPairs = level.colorPairs != null ? level.colorPairs.Count : 0;
        int connectedPairs = pathDrawer.GetCompletedConnectionCount();

        if (connectionText != null)
            connectionText.text = $"{connectedPairs} / {totalPairs}";

        if (totalPairs > 0 && connectedPairs >= totalPairs && !completionTriggered)
        {
            completionTriggered = true;
            StartCoroutine(DelayedLevelComplete(currentMoves, level));
        }
    }

    
    private IEnumerator DelayedLevelComplete(int movesUsed, LevelData level)
    {
        yield return new WaitForSeconds(levelCompleteDelay);
        HandleLevelCompleted(movesUsed, level);
    }

    private void HandleLevelCompleted(int movesUsed, LevelData level)
    {
        if (levelCompleted) return;
        levelCompleted = true;
        autoAdvanceTimer = 0f;

        int totalCells = level.gridWidth * level.gridHeight;
        int occupiedCells = pathDrawer != null ? pathDrawer.GetOccupiedCellCount() : 0;

        int earnedStars = CalculateStars(
            movesUsed, level.optimalMoves, level.moveLimit,
            occupiedCells, totalCells);

        GameProgress.UnlockNextLevel(currentDifficulty, currentLevelIndex);
        GameProgress.SaveBestStars(currentDifficulty, currentLevelIndex, earnedStars);

        UpdateStarVisuals(earnedStars);
        ShowLevelCompletePanel(earnedStars);
        RefreshTotalStarsText(); 

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelCompleteSfx();
    }

    
    private int CalculateStars(
        int movesUsed, int optimalMoves, int moveLimit,
        int occupiedCells, int totalCells)
    {
        int moveStars = CalculateMoveStars(movesUsed, optimalMoves, moveLimit);
        int fillStars = CalculateFillStars(occupiedCells, totalCells);

        return Mathf.Min(moveStars, fillStars);
    }

    
    private int CalculateMoveStars(int movesUsed, int optimalMoves, int moveLimit)
    {
        if (movesUsed > moveLimit) return 0; 

        if (movesUsed <= optimalMoves) return 3;

        int midThreshold = optimalMoves + Mathf.Max(1, (moveLimit - optimalMoves) / 2);
        if (movesUsed <= midThreshold) return 2;

        return 1;
    }

    
    private int CalculateFillStars(int occupiedCells, int totalCells)
    {
        if (totalCells <= 0) return 0;

        if (occupiedCells >= totalCells) return 3;

        float fillRatio = (float)occupiedCells / totalCells;

        if (fillRatio >= twoStarFillThreshold) return 2;

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
           
            OnClickBackToLevelSelect();
            return;
        }

        string path = $"Levels/{currentDifficulty}/Level_{currentDifficulty}_{nextLevelNumber:00}";
        LevelData nextLevel = Resources.Load<LevelData>(path);

        if (nextLevel == null)
        {
            Debug.LogWarning($"Sonraki level bulunamadı: Resources/{path}. Level select'e dönülüyor.");
            OnClickBackToLevelSelect();
            return;
        }

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
            gridManager.GenerateGrid(nextLevel);
        }
    }


    public void OnClickRestart()
    {
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }
    }

    public void OnClickUndo()
    {
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
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

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


    public void OnClickPlayAgain()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

        string path = $"Levels/{currentDifficulty}/Level_{currentDifficulty}_{currentLevelIndex:00}";
        LevelData currentLevel = Resources.Load<LevelData>(path);

        if (currentLevel != null && gridManager != null)
        {
            gridManager.GenerateGrid(currentLevel);
        }
        else
        {
            Debug.LogError($"Mevcut level yeniden yüklenemedi: Resources/{path}");
        }
    }
}