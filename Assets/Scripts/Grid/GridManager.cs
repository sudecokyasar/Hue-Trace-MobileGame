using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Veri & Prefab")]
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private CellView cellPrefab;

    [Header("Grid Ölçüleri (Dünya Birimi)")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float cellSpacing = 0.08f;
    [SerializeField] private float verticalOffset = -0.5f;

    [Header("Kamera Paylarý")]
    [SerializeField] private float horizontalPadding = 0.6f;
    [SerializeField] private float verticalPadding = 3.2f;

    private CellView[,] gridCells;
    private Camera mainCam;

    public CellView[,] GridCells => gridCells;
    public LevelData CurrentLevel => currentLevel;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        if (currentLevel != null)
        {
            GenerateGrid(currentLevel);
        }
        else
        {
            Debug.LogError("GridManager: Seviye verisi (LevelData) atanmamýþ!");
        }
    }

    public void GenerateGrid(LevelData level)
    {
        currentLevel = level;
        ClearGrid();

        int width = level.gridWidth;
        int height = level.gridHeight;
        gridCells = new CellView[width, height];

        float totalWidth = (width * cellSize) + ((width - 1) * cellSpacing);
        float totalHeight = (height * cellSize) + ((height - 1) * cellSpacing);

        float startX = (-totalWidth / 2f) + (cellSize / 2f);
        float startY = (-totalHeight / 2f) + (cellSize / 2f) + verticalOffset;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float posX = startX + x * (cellSize + cellSpacing);
                float posY = startY + y * (cellSize + cellSpacing);
                Vector3 worldPos = new Vector3(posX, posY, 0f);

                CellView cell = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
                cell.Initialize(new Vector2Int(x, y), cellSize);
                gridCells[x, y] = cell;
            }
        }

        PlaceColorStones(level);
        ApplySpecialCells(level);
        AdjustCamera(totalWidth, totalHeight);
    }

    private void PlaceColorStones(LevelData level)
    {
        if (level.colorPairs == null) return;

        foreach (var pair in level.colorPairs)
        {
            if (IsValidCoord(pair.startPos))
            {
                gridCells[pair.startPos.x, pair.startPos.y].SetStone(pair.color);
            }

            if (IsValidCoord(pair.endPos))
            {
                gridCells[pair.endPos.x, pair.endPos.y].SetStone(pair.color);
            }

            if (pair.extraEndpoints != null)
            {
                foreach (var extraPos in pair.extraEndpoints)
                {
                    if (IsValidCoord(extraPos))
                    {
                        gridCells[extraPos.x, extraPos.y].SetStone(pair.color);
                    }
                }
            }
        }
    }

    private void ApplySpecialCells(LevelData level)
    {
        // 1. Kilitli Hücreler
        if (level.lockedCells != null)
        {
            foreach (var lockData in level.lockedCells)
            {
                if (IsValidCoord(lockData.position))
                {
                    gridCells[lockData.position.x, lockData.position.y].SetLocked(lockData.allowedColor);
                }
            }
        }

        // 2. Buz Hücreleri
        if (level.iceCells != null)
        {
            foreach (var icePos in level.iceCells)
            {
                if (IsValidCoord(icePos))
                {
                    gridCells[icePos.x, icePos.y].SetIce();
                }
            }
        }

        // 3. Köprü Hücreleri
        if (level.bridgeCells != null)
        {
            foreach (var bridgePos in level.bridgeCells)
            {
                if (IsValidCoord(bridgePos))
                {
                    gridCells[bridgePos.x, bridgePos.y].SetBridge();
                }
            }
        }

        // 4. Renk Karýþtýrma Hücreleri
        if (level.mixRules != null)
        {
            foreach (var rule in level.mixRules)
            {
                if (IsValidCoord(rule.mixCellPos))
                {
                    gridCells[rule.mixCellPos.x, rule.mixCellPos.y].SetMixCell(rule);
                }

                // Karýþým sonucunun gideceði hedef taþý yerleþtir
                if (IsValidCoord(rule.resultTargetPos))
                {
                    gridCells[rule.resultTargetPos.x, rule.resultTargetPos.y].SetStone(rule.resultColor);
                }
            }
        }
    }

    private void AdjustCamera(float totalGridWidth, float totalGridHeight)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        mainCam.orthographic = true;

        float targetWidth = totalGridWidth + (horizontalPadding * 2f);
        float targetHeight = totalGridHeight + verticalPadding;

        float screenAspect = (float)Screen.width / Screen.height;

        float sizeBasedOnHeight = targetHeight / 2f;
        float sizeBasedOnWidth = (targetWidth / 2f) / screenAspect;

        mainCam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth);
        mainCam.transform.position = new Vector3(0f, verticalOffset, -10f);
    }

    public bool IsValidCoord(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < currentLevel.gridWidth &&
               pos.y >= 0 && pos.y < currentLevel.gridHeight;
    }

    public CellView GetCell(Vector2Int pos)
    {
        if (!IsValidCoord(pos)) return null;
        return gridCells[pos.x, pos.y];
    }

    private void ClearGrid()
    {
        if (transform.childCount > 0)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}