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
    [SerializeField] private GameObject levelSelectPanel; // "Sonraki level yok" durumunda dönülecek panel
    [SerializeField] private bool autoAdvanceToNextLevel = false;
    [SerializeField] private float autoAdvanceDelay = 1.5f;

    [Header("Level Tamamlama Gecikmesi")]
    [Tooltip("Son bağlantı yapıldıktan sonra 'Level Complete' panelinin ekrana gelmesi için beklenecek süre (saniye). Oyuncu tamamlanmış board'u bir an görsün diye.")]
    [SerializeField] private float levelCompleteDelay = 0.5f;

    private string currentDifficulty = "Normal";
    private int currentLevelIndex = 1;
    private bool levelCompleted = false;
    private bool completionTriggered = false; // Coroutine'in birden fazla kez tetiklenmesini önler
    private float autoAdvanceTimer = 0f;

    private void OnEnable()
    {
        // GridManager her yeni level oluşturduğunda UI'ı otomatik güncelle
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
        // Yeni level yüklendi: tamamlanma durumunu ve panelleri sıfırla
        levelCompleted = false;
        completionTriggered = false;
        autoAdvanceTimer = 0f;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        // Yeni level başlarken hamle sayacı ve çizilen path'ler sıfırlanmalı
        if (pathDrawer != null)
            pathDrawer.ResetState();

        SetupLevelInfo();
    }

    // Dışarıdan (ör. LevelSelectManager) manuel olarak da tetiklenebilsin diye public bırakıldı.
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

        // Level başında yıldızlar: zorluğa göre tahmin değil,
        // bu levelde daha önce kazanılmış en iyi skor (hiç oynanmamışsa hepsi gri)
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

        // --- Level Tamamlama Kontrolü ---
        // Panel hemen açılmıyor: burada sadece gecikmeli tetikleyici
        // coroutine'i BİR KEZ başlatıyoruz. Gerçek "levelCompleted = true"
        // ataması ve panel gösterimi HandleLevelCompleted içinde,
        // gecikme süresi dolduktan sonra gerçekleşir.
        if (totalPairs > 0 && connectedPairs >= totalPairs && !completionTriggered)
        {
            completionTriggered = true;
            StartCoroutine(DelayedLevelComplete(currentMoves, level));
        }
    }

    /// <summary>
    /// Oyuncu son bağlantıyı yaptıktan sonra, "Level Complete" panelinin
    /// hemen değil, kısa bir gecikmeyle (levelCompleteDelay saniye) ekrana
    /// gelmesini sağlar. Böylece oyuncu tamamlanmış board'u bir an görebilir.
    /// </summary>
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
        RefreshTotalStarsText(); // Toplam skor anında güncellensin

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelCompleteSfx();
    }

    // Nihai yıldız sayısı, hamle bazlı ve doluluk bazlı skorların
    // DAHA DÜŞÜK olanı alınarak belirlenir. Böylece 3 yıldız almak için
    // oyuncunun hem az hamleyle hem de board'u eksiksiz doldurarak
    // bitirmesi gerekir - sadece biri yeterli değildir.
    private int CalculateStars(
        int movesUsed, int optimalMoves, int moveLimit,
        int occupiedCells, int totalCells)
    {
        int moveStars = CalculateMoveStars(movesUsed, optimalMoves, moveLimit);
        int fillStars = CalculateFillStars(occupiedCells, totalCells);

        return Mathf.Min(moveStars, fillStars);
    }

    // Hamle sayısına göre yıldız: LevelData ile birlikte üretim sırasında
    // gelen optimalMoves / moveLimit değerlerine göre hesaplanır.
    private int CalculateMoveStars(int movesUsed, int optimalMoves, int moveLimit)
    {
        if (movesUsed > moveLimit) return 0; // Limit aşıldıysa yıldız yok

        if (movesUsed <= optimalMoves) return 3;

        int midThreshold = optimalMoves + Mathf.Max(1, (moveLimit - optimalMoves) / 2);
        if (movesUsed <= midThreshold) return 2;

        return 1;
    }

    // Grid doluluk oranına göre yıldız: oyuncu tüm renkleri birleştirip
    // leveli bitirdiğinde board'un ne kadarını doldurduğuna bakar.
    private int CalculateFillStars(int occupiedCells, int totalCells)
    {
        if (totalCells <= 0) return 0;

        // Board'un TAMAMI dolduysa (eksiksiz) -> 3 yıldız.
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

    // --- Level Complete Panel Butonları ---

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
            // Bu zorluktaki son level tamamlandı: level select / menüye dön
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

        // Tahtayı ve hamle sayacını sıfırla (menüye dönerken yaptığımızın aynısı)
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

    // --- Alt Butonların Fonksiyonları ---

    public void OnClickRestart()
    {
        // TRY AGAIN (RETRY): Tüm path line'ları temizler ve hamleyi sıfırlar
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }
    }

    public void OnClickUndo()
    {
        // UNDO: Yapılan son path line'ı temizler
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
        // Ana menüye dönerken hem tahtayı temizle hem de hamle sayacını sıfırla
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

        // Win ekranı (level complete paneli) açıksa onu da kapat
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

    // --- Level Complete Panel - Play Again ---

    public void OnClickPlayAgain()
    {
        // 1. Level Complete panelini kapat
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        // 2. Çizgileri ve tahta durumunu sıfırla
        if (pathDrawer != null)
        {
            pathDrawer.ClearAllLines();
        }

        // 3. Mevcut level verisini tekrar yükle ve gridi yeniden oluştur
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