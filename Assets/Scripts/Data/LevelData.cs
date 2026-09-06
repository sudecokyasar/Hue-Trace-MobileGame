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
    public string colorId;       // "Red", "Blue" (Debug ve loglama i�in)
    public Color color;          // �izgi ve ta� rengi
    public Vector2Int startPos;  // Ba�lang�� h�cresi
    public Vector2Int endPos;    // Biti� h�cresi

    // Hard mod i�in opsiyonel ekstra u�lar (3+ u�lu renkler)
    public List<Vector2Int> extraEndpoints = new List<Vector2Int>();
}

[Serializable]
public class LockedCellData
{
    public Vector2Int position;  // Locked CELL
    public Color allowedColor;  
}

[Serializable]
public class ColorMixRuleData
{
    public Vector2Int mixCellPos;       
    public Color inputColorA;           
    public Color inputColorB;          
    public Color resultColor;          
    public Vector2Int resultTargetPos;  
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Renk Tasi/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level")]
    public string levelId = "Level_01";
    public DifficultyMode difficulty = DifficultyMode.Easy;

    [Header("Grid")]
    [Range(3, 12)] public int gridWidth = 5;
    [Range(3, 12)] public int gridHeight = 5;

    [Header("Move Parameters")]
    public int optimalMoves = 3;
    public int moveLimit = 6;

    [Header("Color Matches")]
    public List<ColorPairData> colorPairs = new List<ColorPairData>();

    [Header("Special Cells")]
    public List<LockedCellData> lockedCells = new List<LockedCellData>();
    public List<Vector2Int> iceCells = new List<Vector2Int>();
    public List<Vector2Int> bridgeCells = new List<Vector2Int>();

    [Header("Color Mix")]
    public List<ColorMixRuleData> mixRules = new List<ColorMixRuleData>();

    public bool IsPositionWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }
}