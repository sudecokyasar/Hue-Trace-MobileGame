using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Veri & Prefab")]
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private CellView cellPrefab;

    [Header("Grids")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float cellSpacing = 0.08f;
    [SerializeField] private float verticalOffset = -0.5f;

    [Header("Camera Paddings")]
    [SerializeField] private float horizontalPadding = 0.6f;
    [SerializeField] private float verticalPadding = 3.2f;

    [Header("Header/Footer")]
    [SerializeField] private RectTransform headerRectTransform;
    [SerializeField] private RectTransform footerRectTransform;
    [SerializeField][Range(0f, 0.1f)] private float extraSafetyMarginViewport = 0.015f;

    private CellView[,] gridCells;
    private Camera mainCam;

    public CellView[,] GridCells => gridCells;
    public LevelData CurrentLevel => currentLevel;


    public static event System.Action<LevelData> OnLevelGenerated;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        string path = $"Levels/{GameDataHolder.SelectedDifficulty}/Level_{GameDataHolder.SelectedDifficulty}_{GameDataHolder.SelectedLevelNumber:00}";
        LevelData loadedLevel = Resources.Load<LevelData>(path);

        if (loadedLevel != null)
        {
            GenerateGrid(loadedLevel);
        }
        else
        {
            Debug.LogError($"LevelData bulunamad�: Resources/{path}");
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

        OnLevelGenerated?.Invoke(level);
    }
    public CellView GetCellAtWorldPosition(Vector3 worldPos)
    {
        if (currentLevel == null || gridCells == null) return null;

        int width = currentLevel.gridWidth;
        int height = currentLevel.gridHeight;

        float totalWidth = (width * cellSize) + ((width - 1) * cellSpacing);
        float totalHeight = (height * cellSize) + ((height - 1) * cellSpacing);

        float startX = (-totalWidth / 2f) + (cellSize / 2f);
        float startY = (-totalHeight / 2f) + (cellSize / 2f) + verticalOffset;

        float step = cellSize + cellSpacing;
        if (step <= 0f) return null;

        int x = Mathf.RoundToInt((worldPos.x - startX) / step);
        int y = Mathf.RoundToInt((worldPos.y - startY) / step);

        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return gridCells[x, y];
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
        // 1. Kilitli H�creler
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

        if (level.mixRules != null)
        {
            foreach (var rule in level.mixRules)
            {
                if (IsValidCoord(rule.mixCellPos))
                {
                    gridCells[rule.mixCellPos.x, rule.mixCellPos.y].SetMixCell(rule);
                }

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
        float screenAspect = (float)Screen.width / Screen.height;

        float sizeBasedOnWidth = (targetWidth / 2f) / screenAspect;
        
        float availableViewportFraction = GetAvailableVerticalViewportFraction();

        float sizeBasedOnHeight;
        if (availableViewportFraction > 0f)
        {
            float targetHeightRaw = totalGridHeight + verticalPadding;
            sizeBasedOnHeight = targetHeightRaw / (2f * availableViewportFraction);
        }
        else
        {
            float targetHeight = totalGridHeight + verticalPadding;
            sizeBasedOnHeight = targetHeight / 2f;
        }

        mainCam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth);

        float cameraY = ComputeCameraYForVerticalCentering(mainCam.orthographicSize);
        mainCam.transform.position = new Vector3(0f, cameraY, -10f);
    }

   
    private float GetAvailableVerticalViewportFraction()
    {
        if (headerRectTransform == null || footerRectTransform == null) return -1f;

        float headerBottomScreenY = GetRectScreenBottomY(headerRectTransform);
        float footerTopScreenY = GetRectScreenTopY(footerRectTransform);

        float availablePixels = headerBottomScreenY - footerTopScreenY;
        if (availablePixels <= 0f || Screen.height <= 0) return -1f;

        float fraction = availablePixels / Screen.height;
        fraction -= extraSafetyMarginViewport * 2f; 

        return Mathf.Clamp01(fraction);
    }

   
    private float ComputeCameraYForVerticalCentering(float orthoSize)
    {
        if (headerRectTransform == null || footerRectTransform == null)
            return verticalOffset;

        float headerBottomScreenY = GetRectScreenBottomY(headerRectTransform);
        float footerTopScreenY = GetRectScreenTopY(footerRectTransform);

        if (headerBottomScreenY <= footerTopScreenY)
            return verticalOffset;

        float gapCenterScreenY = (headerBottomScreenY + footerTopScreenY) / 2f;
        float gapCenterViewportY = gapCenterScreenY / Screen.height; 

        
        return verticalOffset - (gapCenterViewportY - 0.5f) * 2f * orthoSize;
    }

    private float GetRectScreenBottomY(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners); 

        Camera uiCam = GetUiCameraFor(rect);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
        return screenPoint.y;
    }

    private float GetRectScreenTopY(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Camera uiCam = GetUiCameraFor(rect);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, corners[1]);
        return screenPoint.y;
    }

    
    private Camera GetUiCameraFor(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
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