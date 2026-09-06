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

    [Header("Header/Footer Referanslarý (Aspect-Ratio Baðýmsýz Kamera Ýçin)")]
    [Tooltip("Header panelinin RectTransform'u. Atanýrsa, grid'in Header'ýn ALT kenarýna binmemesi için kamera bu bilgiye göre dinamik hesaplanýr.")]
    [SerializeField] private RectTransform headerRectTransform;
    [Tooltip("Footer panelinin RectTransform'u. Atanýrsa, grid'in Footer'ýn ÜST kenarýna binmemesi için kamera bu bilgiye göre dinamik hesaplanýr.")]
    [SerializeField] private RectTransform footerRectTransform;
    [Tooltip("Header/Footer ile grid arasýnda ekstra nefes payý (ekran yüksekliðinin oraný olarak, üstten ve alttan ayrý ayrý uygulanýr).")]
    [SerializeField][Range(0f, 0.1f)] private float extraSafetyMarginViewport = 0.015f;

    private CellView[,] gridCells;
    private Camera mainCam;

    public CellView[,] GridCells => gridCells;
    public LevelData CurrentLevel => currentLevel;

    // Yeni bir level grid'e yüklendiðinde tetiklenir.
    // UI veya baþka sistemler bu event'e abone olarak kendini güncelleyebilir.
    public static event System.Action<LevelData> OnLevelGenerated;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        // GameDataHolder'dan gelen bilgilere göre doðru level verisini Resources'tan yükle
        string path = $"Levels/{GameDataHolder.SelectedDifficulty}/Level_{GameDataHolder.SelectedDifficulty}_{GameDataHolder.SelectedLevelNumber:00}";
        LevelData loadedLevel = Resources.Load<LevelData>(path);

        if (loadedLevel != null)
        {
            GenerateGrid(loadedLevel);
        }
        else
        {
            Debug.LogError($"LevelData bulunamadý: Resources/{path}");
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

        // Level tamamen kurulduktan sonra dinleyicilere haber ver (UI güncellemesi vs.)
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
        float screenAspect = (float)Screen.width / Screen.height;

        float sizeBasedOnWidth = (targetWidth / 2f) / screenAspect;

        // --- DÝKEY BOYUT: Header/Footer'ýn gerçek ekran konumuna göre ---
        // Eskiden burada sabit bir "verticalPadding" world-unit deðeri
        // kullanýlýyordu. Bu, telefon oranýnda (dar/uzun ekran) tesadüfen
        // yetiyordu ama iPad gibi daha kare oranlý ekranlarda Header/Footer
        // gerçekte ekranýn daha büyük bir kýsmýný kapladýðý için grid
        // onlarýn üstüne biniyordu.
        //
        // Artýk (headerRectTransform / footerRectTransform atanmýþsa)
        // Header'ýn ALT kenarý ile Footer'ýn ÜST kenarýnýn gerçek ekran
        // piksel konumlarýný okuyup, grid'in SADECE o ikisi arasýndaki
        // boþluða sýðmasýný saðlayacak orthographicSize'ý hesaplýyoruz.
        // Bu hesap ekran en/boy oranýndan baðýmsýzdýr; telefon da olsa
        // tablet de olsa doðru sonuç verir.
        float availableViewportFraction = GetAvailableVerticalViewportFraction();

        float sizeBasedOnHeight;
        if (availableViewportFraction > 0f)
        {
            float targetHeightRaw = totalGridHeight + verticalPadding;
            sizeBasedOnHeight = targetHeightRaw / (2f * availableViewportFraction);
        }
        else
        {
            // Header/Footer referansý atanmamýþsa eski sabit-padding davranýþýna düþ.
            float targetHeight = totalGridHeight + verticalPadding;
            sizeBasedOnHeight = targetHeight / 2f;
        }

        mainCam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth);

        float cameraY = ComputeCameraYForVerticalCentering(mainCam.orthographicSize);
        mainCam.transform.position = new Vector3(0f, cameraY, -10f);
    }

    /// <summary>
    /// Header'ýn alt kenarý ile Footer'ýn üst kenarý arasýnda kalan,
    /// grid için kullanýlabilir dikey alanýn, TÜM ekran yüksekliðine
    /// oranýný (0-1 arasý) döner. Referanslar atanmamýþsa -1 döner.
    /// </summary>
    private float GetAvailableVerticalViewportFraction()
    {
        if (headerRectTransform == null || footerRectTransform == null) return -1f;

        float headerBottomScreenY = GetRectScreenBottomY(headerRectTransform);
        float footerTopScreenY = GetRectScreenTopY(footerRectTransform);

        float availablePixels = headerBottomScreenY - footerTopScreenY;
        if (availablePixels <= 0f || Screen.height <= 0) return -1f;

        float fraction = availablePixels / Screen.height;
        fraction -= extraSafetyMarginViewport * 2f; // üstten ve alttan küçük bir nefes payý

        return Mathf.Clamp01(fraction);
    }

    /// <summary>
    /// Grid'in dikeyde ortalanacaðý kamera Y konumunu, Header/Footer
    /// arasýndaki boþluðun tam ortasýna denk gelecek þekilde hesaplar.
    /// Referanslar atanmamýþsa eski sabit "verticalOffset" davranýþýna düþer.
    /// </summary>
    private float ComputeCameraYForVerticalCentering(float orthoSize)
    {
        if (headerRectTransform == null || footerRectTransform == null)
            return verticalOffset;

        float headerBottomScreenY = GetRectScreenBottomY(headerRectTransform);
        float footerTopScreenY = GetRectScreenTopY(footerRectTransform);

        if (headerBottomScreenY <= footerTopScreenY)
            return verticalOffset;

        float gapCenterScreenY = (headerBottomScreenY + footerTopScreenY) / 2f;
        float gapCenterViewportY = gapCenterScreenY / Screen.height; // 0 (ekran altý) - 1 (ekran üstü)

        // viewport 0.5  -> kameranýn Y konumu
        // viewport 1.0  -> kameraY + orthoSize
        // viewport 0.0  -> kameraY - orthoSize
        // Grid'in dikey merkezi (verticalOffset) tam bu boþluðun ortasýna
        // denk gelsin istiyoruz:
        return verticalOffset - (gapCenterViewportY - 0.5f) * 2f * orthoSize;
    }

    private float GetRectScreenBottomY(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners); // [0]=sol-alt [1]=sol-üst [2]=sað-üst [3]=sað-alt

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

    // Screen Space - Overlay canvas'larda kamera null olmalý (RectTransformUtility
    // bunu otomatik olarak zaten ekran piksel koordinatý gibi ele alýyor).
    // Screen Space - Camera / World Space canvas'larda ise canvas'ýn kendi
    // worldCamera'sýný kullanmak gerekiyor.
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