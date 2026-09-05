using System;
using System.Collections.Generic;
using UnityEngine;

public enum DifficultyMode
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class ColorPairData
{
    public string colorId;       // "Red", "Blue" (Debug ve loglama için)
    public Color color;          // Çizgi ve taþ rengi
    public Vector2Int startPos;  // Baþlangýç hücresi
    public Vector2Int endPos;    // Bitiþ hücresi

    // Hard mod için opsiyonel ekstra uçlar (3+ uçlu renkler)
    public List<Vector2Int> extraEndpoints = new List<Vector2Int>();
}

[Serializable]
public class LockedCellData
{
    public Vector2Int position;  // Kilitli hücre koordinatý
    public Color allowedColor;   // Bu hücreden geçebilecek izinli renk
}

[Serializable]
public class ColorMixRuleData
{
    public Vector2Int mixCellPos;       // Karýþýmýn gerçekleþeceði hücre
    public Color inputColorA;           // 1. Giren Renk (Örn: Mavi)
    public Color inputColorB;           // 2. Giren Renk (Örn: Sarý)
    public Color resultColor;           // Çýkan Karýþým Rengi (Örn: Yeþil)
    public Vector2Int resultTargetPos;  // Karýþým renginin ulaþmasý gereken hedef taþ
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Renk Tasi/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Seviye Kimliði")]
    public string levelId = "Level_01";
    public DifficultyMode difficulty = DifficultyMode.Easy;

    [Header("Grid Boyutu")]
    [Range(3, 12)] public int gridWidth = 5;
    [Range(3, 12)] public int gridHeight = 5;

    [Header("Hamle ve Yýldýz Parametreleri")]
    public int optimalMoves = 3;
    public int moveLimit = 6;

    [Header("Renk Çiftleri")]
    public List<ColorPairData> colorPairs = new List<ColorPairData>();

    [Header("Özel Hücreler (Normal & Hard)")]
    public List<LockedCellData> lockedCells = new List<LockedCellData>();
    public List<Vector2Int> iceCells = new List<Vector2Int>();
    public List<Vector2Int> bridgeCells = new List<Vector2Int>();

    [Header("Renk Karýþtýrma Kurallarý (Hard Mod)")]
    public List<ColorMixRuleData> mixRules = new List<ColorMixRuleData>();

    public bool IsPositionWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }
}