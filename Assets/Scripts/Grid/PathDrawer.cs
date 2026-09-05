using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathDrawer : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LineRenderer linePrefab;

    private bool isDrawing = false;
    private Color currentColor;
    private List<CellView> currentPath = new List<CellView>();
    private LineRenderer activeLine;

    private Dictionary<Color, ColorNetwork> colorNetworks = new Dictionary<Color, ColorNetwork>();

    public int MoveCount { get; private set; } = 0;

    private Camera cam;

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

        // Tıklanan taş zaten ağa bağlıysa sıfırdan çizmek için temizle
        if (network.ContainsCell(hitCell))
        {
            network.Clear();
        }

        isDrawing = true;
        currentPath.Clear();
        currentPath.Add(hitCell);

        activeLine = Instantiate(linePrefab, transform);
        activeLine.startColor = currentColor;
        activeLine.endColor = currentColor;

        UpdateLineVisual();
    }

    private void ContinueDrawing()
    {
        CellView hitCell = GetCellUnderCursor();
        if (hitCell == null) return;

        CellView lastCell = currentPath[currentPath.Count - 1];
        if (hitCell == lastCell) return;

        // 1. Çökmüş Buz Kontrolü
        if (hitCell.IsCollapsed) return;

        // 2. Geri Alma (Backtracking)
        if (currentPath.Count > 1 && hitCell == currentPath[currentPath.Count - 2])
        {
            currentPath.RemoveAt(currentPath.Count - 1);
            UpdateLineVisual();
            return;
        }

        // 3. Komşuluk Kontrolü
        if (!IsAdjacent(lastCell.GridPos, hitCell.GridPos)) return;

        // 4. Köprüden Çıkış Kuralı
        if (lastCell.IsBridge && currentPath.Count >= 2)
        {
            CellView beforeBridge = currentPath[currentPath.Count - 2];
            Vector2Int inDir = lastCell.GridPos - beforeBridge.GridPos;
            Vector2Int outDir = hitCell.GridPos - lastCell.GridPos;

            if (inDir != outDir) return;
        }

        // 5. Döngü Engeli
        if (currentPath.Contains(hitCell)) return;

        // 6. Başka Taşa Basamaz
        if (hitCell.HasStone && !AreColorsSimilar(hitCell.StoneColor, currentColor)) return;

        // 7. Karışım Hücresi İzni
        bool isAllowedMixInput = hitCell.IsMixCell &&
                                 (AreColorsSimilar(currentColor, hitCell.MixRule.inputColorA) ||
                                  AreColorsSimilar(currentColor, hitCell.MixRule.inputColorB));

        if (hitCell.IsMixCell && !isAllowedMixInput && !hitCell.IsMixActivated) return;

        // 8. Kilit Kontrolü
        if (hitCell.IsLocked && !AreColorsSimilar(hitCell.AllowedColor, currentColor)) return;

        // 9. Çakışma ve Köprü Kontrolü
        if (IsCellBlockedByOtherNetworks(lastCell, hitCell, currentColor)) return;

        // 10. Y-Dallanma Birleşmesi
        ColorNetwork network = GetOrCreateNetwork(currentColor);
        if (network.ContainsCell(hitCell) && currentPath.Count >= 1)
        {
            currentPath.Add(hitCell);
            UpdateLineVisual();
            EndDrawing();
            return;
        }

        // 11. Hücreyi Ekle
        currentPath.Add(hitCell);
        UpdateLineVisual();

        // 12. Buz Kırılması
        if (lastCell.IsIce && !lastCell.IsCollapsed)
        {
            lastCell.CollapseIce();
        }

        // 13. Tamamlama Durumları:
        // A) Hedef taşa ulaştıysa
        if (hitCell.HasStone && AreColorsSimilar(hitCell.StoneColor, currentColor) && currentPath.Count > 1)
        {
            EndDrawing();
            return;
        }

        // B) Karışım hücresine girdiyse
        if (isAllowedMixInput)
        {
            EndDrawing();
            return;
        }
    }

    private void EndDrawing()
    {
        if (!isDrawing) return;
        isDrawing = false;

        CellView startCell = currentPath[0];
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
            network.AllBranches.Add(new List<CellView>(currentPath));
            network.Lines.Add(activeLine);
            MoveCount++;

            foreach (var cell in currentPath)
            {
                if (cell.IsIce && !cell.IsCollapsed)
                {
                    cell.CollapseIce();
                }
            }

            Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(currentColor)}>Hat Bağlandı!</color> (Hamle: {MoveCount})");

            CheckMixCells();
            CheckLevelComplete();
        }
        else
        {
            if (activeLine != null) Destroy(activeLine.gameObject);
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
                    Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(rule.resultColor)}>🧪 KARIŞIM AKTİF OLDU! Çıkış Taşı Hazır: {mixCell.GridPos}</color>");
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

        // Köprü Kontrolü
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

        // 1. Standart Renk Çiftlerini Kontrol Et
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

        // 2. Karışım Sonuçlarını Kontrol Et
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

        // 3. Seviye Tamamlandı!
        int optimal = level.optimalMoves;
        int limit = level.moveLimit;

        int stars = 1;
        if (MoveCount <= optimal) stars = 3;
        else if (MoveCount <= limit) stars = 2;

        Debug.Log($"<color=green><b>🎉 TEBRİKLER! TÜM GÖREVLER TAMAMLANDI - SEVİYE BİTTİ!</b></color>\n" +
                  $"Kazanılan Yıldız: {stars} ★ | Hamle: {MoveCount} | Optimal: {optimal}");
    }

    // --- Akıllı Renk ve Ağ Yardımcıları ---
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
            pos.z = -0.2f;
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
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<CellView>();
        }
        return null;
    }
}