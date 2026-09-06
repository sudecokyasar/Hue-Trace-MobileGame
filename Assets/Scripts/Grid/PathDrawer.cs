using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathDrawer : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LineRenderer linePrefab;

    [Header("Görsel & Efekt Ayarları")]
    [SerializeField] private float hdrIntensity = 1.8f;
    [Tooltip("Elektrik akımının hattın başından sonuna akma süresi (saniye)")]
    [SerializeField] private float pulseDuration = 0.28f;

    private bool isDrawing = false;
    private Color currentColor;
    private List<CellView> currentPath = new List<CellView>();
    private LineRenderer activeLine;

    private Dictionary<Color, ColorNetwork> colorNetworks = new Dictionary<Color, ColorNetwork>();
    private List<LineRenderer> activeLines = new List<LineRenderer>();

    /// <summary>
    /// Hangi çizginin hangi renk network'üne ve hangi branch'e (hücre
    /// listesine) ait olduğunu tutar. Undo işleminde SADECE görsel
    /// çizgiyi değil, ColorNetwork.AllBranches içindeki gerçek bağlantı
    /// verisini de birlikte geri almak için gerekli.
    ///
    /// NOT / BUG FIX: Eskiden UndoLastLine() sadece LineRenderer'ı
    /// (görseli) siliyordu, ColorNetwork.AllBranches'teki veriyi hiç
    /// temizlemiyordu. Bu yüzden bir rengi bağlayıp undo yapınca, o
    /// renk sistem tarafından hâlâ "bağlı" sayılıyordu (sadece ekranda
    /// çizgi görünmüyordu). Sonuç: 3 renkten 1'i undo edilmiş olsa bile
    /// diğer 2 renk bağlanınca level yanlışlıkla "tamamlandı" sayılıyordu.
    /// </summary>
    private class ConnectionRecord
    {
        public Color Color;
        public List<CellView> Branch;
        public LineRenderer Line;
    }

    private List<ConnectionRecord> connectionHistory = new List<ConnectionRecord>();

    public int MoveCount { get; private set; } = 0;

    private Camera cam;

    public static event System.Action<int> OnLevelCompleted;

    private void Awake()
    {
        cam = Camera.main;
        if (gridManager == null) gridManager = GetComponent<GridManager>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            StartDrawing();
        }
        else if (pointer.press.isPressed && isDrawing)
        {
            ContinueDrawing();
        }
        else if (pointer.press.wasReleasedThisFrame && isDrawing)
        {
            EndDrawing();
        }
    }

    private void StartDrawing()
    {
        CellView hitCell = GetCellUnderCursor();
        if (hitCell == null || !hitCell.HasStone) return;

        currentColor = hitCell.StoneColor;

        ColorNetwork network = GetOrCreateNetwork(currentColor);

        if (network.ContainsCell(hitCell))
        {
            network.Clear();
        }

        isDrawing = true;
        currentPath.Clear();
        currentPath.Add(hitCell);

        activeLine = Instantiate(linePrefab, transform);

        Color hdrColor = currentColor * hdrIntensity;
        hdrColor.a = 1f;
        activeLine.startColor = hdrColor;
        activeLine.endColor = hdrColor;

        activeLines.Add(activeLine);

        UpdateLineVisual();
    }

    private void ContinueDrawing()
    {
        CellView hitCell = GetCellUnderCursor();
        if (hitCell == null) return;

        CellView lastCell = currentPath[currentPath.Count - 1];
        if (hitCell == lastCell) return;

        if (hitCell.IsCollapsed) return;

        if (currentPath.Count > 1 && hitCell == currentPath[currentPath.Count - 2])
        {
            currentPath.RemoveAt(currentPath.Count - 1);
            UpdateLineVisual();
            return;
        }

        if (!IsAdjacent(lastCell.GridPos, hitCell.GridPos)) return;

        if (lastCell.IsBridge && currentPath.Count >= 2)
        {
            CellView beforeBridge = currentPath[currentPath.Count - 2];
            Vector2Int inDir = lastCell.GridPos - beforeBridge.GridPos;
            Vector2Int outDir = hitCell.GridPos - lastCell.GridPos;

            if (inDir != outDir) return;
        }

        if (currentPath.Contains(hitCell)) return;

        if (hitCell.HasStone && !AreColorsSimilar(hitCell.StoneColor, currentColor)) return;

        bool isAllowedMixInput = hitCell.IsMixCell &&
                                 (AreColorsSimilar(currentColor, hitCell.MixRule.inputColorA) ||
                                  AreColorsSimilar(currentColor, hitCell.MixRule.inputColorB));

        if (hitCell.IsMixCell && !isAllowedMixInput && !hitCell.IsMixActivated) return;

        if (hitCell.IsLocked && !AreColorsSimilar(hitCell.AllowedColor, currentColor)) return;

        if (IsCellBlockedByOtherNetworks(lastCell, hitCell, currentColor)) return;

        ColorNetwork network = GetOrCreateNetwork(currentColor);
        if (network.ContainsCell(hitCell) && currentPath.Count >= 1)
        {
            currentPath.Add(hitCell);
            UpdateLineVisual();
            EndDrawing();
            return;
        }

        currentPath.Add(hitCell);
        UpdateLineVisual();

        if (lastCell.IsIce && !lastCell.IsCollapsed)
        {
            lastCell.CollapseIce();
        }

        if (hitCell.HasStone && AreColorsSimilar(hitCell.StoneColor, currentColor) && currentPath.Count > 1)
        {
            EndDrawing();
            return;
        }

        if (isAllowedMixInput)
        {
            EndDrawing();
            return;
        }
    }

    /// <summary>
    /// DÜZELTİLDİ: Artık sadece görsel çizgiyi değil, ColorNetwork
    /// içindeki gerçek bağlantı verisini (branch) ve o network'ün
    /// Lines listesindeki kaydı da birlikte geri alıyor. Bu sayede
    /// undo edilen bir renk, level tamamlama kontrolünde artık
    /// "bağlı" sayılmıyor.
    /// </summary>
    public void UndoLastLine()
    {
        if (connectionHistory == null || connectionHistory.Count == 0) return;

        ConnectionRecord last = connectionHistory[connectionHistory.Count - 1];
        connectionHistory.RemoveAt(connectionHistory.Count - 1);

        ColorNetwork network = GetNetwork(last.Color);
        if (network != null)
        {
            network.AllBranches.Remove(last.Branch);
            network.Lines.Remove(last.Line);
        }

        if (activeLines != null && activeLines.Contains(last.Line))
        {
            activeLines.Remove(last.Line);
        }

        if (last.Line != null)
        {
            Destroy(last.Line.gameObject);
        }
        
        // Undo edilen bağ bir mix hücresinin girdisiyse, mix sonucu
        // artık geçerli olmayabilir - yeniden değerlendir.
        CheckMixCells();
    }

    public void ClearAllLines()
    {
        if (activeLines != null)
        {
            foreach (var line in activeLines)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            activeLines.Clear();
        }

        if (colorNetworks != null)
        {
            foreach (var network in colorNetworks.Values)
            {
                network.Clear();
            }
            colorNetworks.Clear();
        }

        // Undo geçmişi de sıfırlanmalı, aksi halde bir önceki levelden
        // kalan referanslar (artık var olmayan hücre/çizgi nesnelerine
        // işaret eden ConnectionRecord'lar) tutulmaya devam eder.
        connectionHistory.Clear();

        // Restart / Try Again / Menu / Play Again gibi durumlarda level
        // yeniden üretilmese bile (aynı hücre nesneleri korunsa bile)
        // hücrelerin oyun-içi özel durumları (kırılmış buz, aktifleşmiş
        // mix hücresi) sıfırlanmalı. Aksi halde örn. kırılan buz, board
        // yeniden üretilmeden yapılan bir restart sonrası kırık kalmaya
        // devam ediyordu.
        ResetSpecialCellStates();

        MoveCount = 0;
    }

    private void ResetSpecialCellStates()
    {
        if (gridManager == null) return;

        CellView[,] cells = gridManager.GridCells;
        if (cells == null) return;

        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellView cell = cells[x, y];
                if (cell == null) continue;

                if (cell.IsIce)
                {
                    cell.ResetIceState();
                }

                if (cell.IsMixCell && cell.IsMixActivated)
                {
                    cell.DeactivateMixResult();
                }
            }
        }
    }

    public void ResetState()
    {
        ClearAllLines();

        isDrawing = false;
        currentPath.Clear();
        activeLine = null;
    }

    private void EndDrawing()
    {
        if (!isDrawing) return;
        isDrawing = false;

        int startIndex = 0;
        CellView startCell = currentPath[startIndex];
        CellView endCell = currentPath[currentPath.Count - 1];

        ColorNetwork network = GetOrCreateNetwork(currentColor);

        bool hitOtherStone = endCell.HasStone && AreColorsSimilar(endCell.StoneColor, currentColor) && startCell != endCell;
        bool mergedIntoNetwork = network.ContainsCell(endCell);
        bool hitMixCell = endCell.IsMixCell &&
                          (AreColorsSimilar(currentColor, endCell.MixRule.inputColorA) ||
                           AreColorsSimilar(currentColor, endCell.MixRule.inputColorB));

        bool isValidConnection = currentPath.Count > 1 && (hitOtherStone || mergedIntoNetwork || hitMixCell);

        if (isValidConnection)
        {
            // Branch referansını bir değişkende tutuyoruz ki hem network'e
            // hem de undo geçmişine AYNI listeyi (referansı) ekleyelim -
            // böylece undo'da network.AllBranches.Remove() referans bazlı
            // çalışıp doğru elemanı bulabilsin.
            List<CellView> branch = new List<CellView>(currentPath);
            network.AllBranches.Add(branch);
            network.Lines.Add(activeLine);

            connectionHistory.Add(new ConnectionRecord
            {
                Color = currentColor,
                Branch = branch,
                Line = activeLine
            });

            MoveCount++;

            foreach (var cell in currentPath)
            {
                if (cell.IsIce && !cell.IsCollapsed)
                {
                    cell.CollapseIce();
                }
            }

            StartCoroutine(PlayElectricCurrent(activeLine, currentColor));

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayConnectionSfx();

            CheckMixCells();
            CheckLevelComplete();
        }
        else
        {
            if (activeLine != null)
            {
                activeLines.Remove(activeLine);
                Destroy(activeLine.gameObject);
            }
        }
    }

    /// <summary>
    /// Akımı çizginin ucundan dışarı akıtıp sıfırlayan güncel elektrik dalgası fonksiyonu.
    /// </summary>
    private IEnumerator PlayElectricCurrent(LineRenderer line, Color baseColor)
    {
        if (line == null) yield break;

        float duration = pulseDuration;
        float elapsed = 0f;

        Color baseHdr = baseColor * hdrIntensity;
        baseHdr.a = 1f;

        Color electricPulseColor = Color.Lerp(baseColor, Color.white, 0.85f) * 3.0f;
        electricPulseColor.a = 1f;

        float pulseWidth = 0.20f;

        while (elapsed < duration)
        {
            if (line == null) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // t değeri çizginin gerisinden başlayıp çizginin ucundan tamamen dışarı çıkar
            float t = Mathf.Lerp(-pulseWidth, 1f + pulseWidth, progress);

            float startPos = Mathf.Clamp01(t - pulseWidth);
            float peakPos = Mathf.Clamp01(t);
            float endPos = Mathf.Clamp01(t + pulseWidth);

            // Eğer akım çizginin dışına çıktıysa artık beyazlık basma
            Color currentCenterColor = (t >= 0f && t <= 1f) ? electricPulseColor : baseHdr;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseHdr, 0f),
                    new GradientColorKey(baseHdr, startPos),
                    new GradientColorKey(currentCenterColor, peakPos),
                    new GradientColorKey(baseHdr, endPos),
                    new GradientColorKey(baseHdr, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            line.colorGradient = gradient;
            yield return null;
        }

        // BİTİŞ: Çizgiyi tamamen 2 anahtarlı düz ve net kendi rengine sıfırla (Beyazlık kalmaz!)
        if (line != null)
        {
            Gradient flatGradient = new Gradient();
            flatGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseHdr, 0f),
                    new GradientColorKey(baseHdr, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            line.colorGradient = flatGradient;
            line.startColor = baseHdr;
            line.endColor = baseHdr;
        }
    }

    private void CheckMixCells()
    {
        if (gridManager == null || gridManager.CurrentLevel == null || gridManager.CurrentLevel.mixRules == null) return;

        foreach (var rule in gridManager.CurrentLevel.mixRules)
        {
            CellView mixCell = gridManager.GetCell(rule.mixCellPos);
            if (mixCell == null) continue;

            ColorNetwork netA = GetNetwork(rule.inputColorA);
            ColorNetwork netB = GetNetwork(rule.inputColorB);

            bool hasInputA = netA != null && netA.ContainsCell(mixCell);
            bool hasInputB = netB != null && netB.ContainsCell(mixCell);

            if (hasInputA && hasInputB)
            {
                if (!mixCell.IsMixActivated)
                {
                    mixCell.ActivateMixResult();
                }
            }
            else
            {
                if (mixCell.IsMixActivated)
                {
                    mixCell.DeactivateMixResult();
                    ColorNetwork resultNet = GetNetwork(rule.resultColor);
                    if (resultNet != null) resultNet.Clear();
                }
            }
        }
    }

    private bool IsCellBlockedByOtherNetworks(CellView fromCell, CellView targetCell, Color myColor)
    {
        if (targetCell.IsMixCell)
        {
            bool isMyInput = (AreColorsSimilar(myColor, targetCell.MixRule.inputColorA) ||
                              AreColorsSimilar(myColor, targetCell.MixRule.inputColorB));
            if (isMyInput) return false;
        }

        if (!targetCell.IsBridge)
        {
            foreach (var kvp in colorNetworks)
            {
                if (!AreColorsSimilar(kvp.Key, myColor) && kvp.Value.ContainsCell(targetCell))
                {
                    return true;
                }
            }
            return false;
        }

        int pathsUsingBridge = 0;
        bool isHorizontalIncoming = (fromCell.GridPos.y == targetCell.GridPos.y);

        foreach (var kvp in colorNetworks)
        {
            if (AreColorsSimilar(kvp.Key, myColor)) continue;

            ColorNetwork otherNet = kvp.Value;
            foreach (var branch in otherNet.AllBranches)
            {
                int idx = branch.IndexOf(targetCell);
                if (idx != -1)
                {
                    pathsUsingBridge++;
                    if (idx > 0 && idx < branch.Count - 1)
                    {
                        bool otherIsHorizontal = (branch[idx - 1].GridPos.y == branch[idx + 1].GridPos.y);
                        if (otherIsHorizontal == isHorizontalIncoming)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        if (pathsUsingBridge >= 2) return true;
        return false;
    }

    private void CheckLevelComplete()
    {
        if (gridManager == null || gridManager.CurrentLevel == null) return;

        LevelData level = gridManager.CurrentLevel;

        foreach (var pair in level.colorPairs)
        {
            ColorNetwork net = GetNetwork(pair.color);
            if (net == null) return;

            HashSet<CellView> occupied = net.GetAllOccupiedCells();

            CellView startCell = gridManager.GetCell(pair.startPos);
            if (!occupied.Contains(startCell)) return;

            CellView endCell = gridManager.GetCell(pair.endPos);
            if (!occupied.Contains(endCell)) return;

            if (pair.extraEndpoints != null)
            {
                foreach (var extraPos in pair.extraEndpoints)
                {
                    CellView extraCell = gridManager.GetCell(extraPos);
                    if (!occupied.Contains(extraCell)) return;
                }
            }
        }

        if (level.mixRules != null && level.mixRules.Count > 0)
        {
            foreach (var rule in level.mixRules)
            {
                ColorNetwork resNet = GetNetwork(rule.resultColor);
                if (resNet == null) return;

                HashSet<CellView> occupied = resNet.GetAllOccupiedCells();
                CellView targetCell = gridManager.GetCell(rule.resultTargetPos);

                if (!occupied.Contains(targetCell)) return;
            }
        }

        if (!IsBoardFullyFilled(level))
            return;

        OnLevelCompleted?.Invoke(MoveCount);
    }

    private bool IsBoardFullyFilled(LevelData level)
    {
        int totalCells = level.gridWidth * level.gridHeight;

        HashSet<CellView> allOccupied = new HashSet<CellView>();
        foreach (var kvp in colorNetworks)
        {
            allOccupied.UnionWith(kvp.Value.GetAllOccupiedCells());
        }

        return allOccupied.Count >= totalCells;
    }

    public int GetOccupiedCellCount()
    {
        HashSet<CellView> allOccupied = new HashSet<CellView>();
        foreach (var kvp in colorNetworks)
        {
            allOccupied.UnionWith(kvp.Value.GetAllOccupiedCells());
        }
        return allOccupied.Count;
    }

    private ColorNetwork GetOrCreateNetwork(Color color)
    {
        ColorNetwork net = GetNetwork(color);
        if (net == null)
        {
            net = new ColorNetwork(color);
            colorNetworks[color] = net;
        }
        return net;
    }

    private ColorNetwork GetNetwork(Color color)
    {
        foreach (var kvp in colorNetworks)
        {
            if (AreColorsSimilar(kvp.Key, color)) return kvp.Value;
        }
        return null;
    }

    private bool AreColorsSimilar(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.08f &&
               Mathf.Abs(a.g - b.g) < 0.08f &&
               Mathf.Abs(a.b - b.b) < 0.08f;
    }

    private void UpdateLineVisual()
    {
        if (activeLine == null) return;

        activeLine.positionCount = currentPath.Count;
        for (int i = 0; i < currentPath.Count; i++)
        {
            Vector3 pos = currentPath[i].transform.position;
            pos.z = 0f;
            activeLine.SetPosition(i, pos);
        }
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private CellView GetCellUnderCursor()
    {
        var pointer = Pointer.current;
        if (pointer == null) return null;

        Vector2 screenPos = pointer.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        if (gridManager == null) return null;

        return gridManager.GetCellAtWorldPosition(worldPos);
    }

    public int GetCompletedConnectionCount()
    {
        if (gridManager == null || gridManager.CurrentLevel == null || gridManager.CurrentLevel.colorPairs == null)
            return 0;

        int completedCount = 0;
        foreach (var pair in gridManager.CurrentLevel.colorPairs)
        {
            ColorNetwork net = GetNetwork(pair.color);
            if (net == null) continue;

            HashSet<CellView> occupied = net.GetAllOccupiedCells();
            CellView startCell = gridManager.GetCell(pair.startPos);
            CellView endCell = gridManager.GetCell(pair.endPos);

            bool isConnected = startCell != null && endCell != null &&
                               occupied.Contains(startCell) && occupied.Contains(endCell);

            if (isConnected)
            {
                completedCount++;
            }
        }
        return completedCount;
    }
}