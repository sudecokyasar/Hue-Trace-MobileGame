using UnityEngine;

public static class GameProgress
{
    public const int TotalLevelsPerDifficulty = 100;
    private static readonly string[] AllDifficulties = { "Easy", "Normal", "Hard" };

    public static int GetUnlockedLevel(string difficulty)
    {
        return PlayerPrefs.GetInt($"{difficulty}_UnlockedLevel", 1);
    }

    public static void UnlockNextLevel(string difficulty, int completedLevelIndex)
    {
        int currentUnlocked = GetUnlockedLevel(difficulty);
        if (completedLevelIndex >= currentUnlocked)
        {
            PlayerPrefs.SetInt($"{difficulty}_UnlockedLevel", completedLevelIndex + 1);
            PlayerPrefs.Save();
        }
    }
    

    private static string StarsKey(string difficulty, int levelIndex) =>
        $"{difficulty}_Level{levelIndex}_Stars";

    public static int GetBestStars(string difficulty, int levelIndex)
    {
        return PlayerPrefs.GetInt(StarsKey(difficulty, levelIndex), 0);
    }

    public static void SaveBestStars(string difficulty, int levelIndex, int earnedStars)
    {
        int currentBest = GetBestStars(difficulty, levelIndex);
        if (earnedStars > currentBest)
        {
            PlayerPrefs.SetInt(StarsKey(difficulty, levelIndex), earnedStars);
            PlayerPrefs.Save();
        }
    }

    // Tek bir zorluk modundaki t�m levellerden toplanan y�ld�z
    public static int GetTotalStarsForDifficulty(string difficulty)
    {
        int total = 0;
        for (int i = 1; i <= TotalLevelsPerDifficulty; i++)
        {
            total += GetBestStars(difficulty, i);
        }
        return total;
    }

    // Total Stars
    public static int GetTotalStarsAllDifficulties()
    {
        int total = 0;
        foreach (var difficulty in AllDifficulties)
        {
            total += GetTotalStarsForDifficulty(difficulty);
        }
        return total;
    }

    // Max Star Count
    public static int GetMaxPossibleStars()
    {
        return AllDifficulties.Length * TotalLevelsPerDifficulty * 3;
    }
}