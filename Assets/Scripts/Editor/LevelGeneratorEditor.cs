using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class LevelGeneratorEditor : EditorWindow
{
    private static readonly Color[] ColorPalette =
    {
    new Color(1.00f, 0.20f, 0.38f, 1f), // Red #FF3361
    new Color(0.12f, 0.65f, 1.00f, 1f), // Blue #1FA6FF
    new Color(0.36f, 0.90f, 0.42f, 1f), // Green #5CE56A
    new Color(1.00f, 0.80f, 0.33f, 1f), // Yellow #FFCD53
    new Color(0.75f, 0.25f, 1.00f, 1f), // Purple #BF40FF
    new Color(1.00f, 0.48f, 0.15f, 1f), // Orange #FF7A26
    new Color(0.16f, 0.97f, 1.00f, 1f), // Cyan #28F7FF
    new Color(0.98f, 0.57f, 0.78f, 1f), // Pink#FA91C6
    new Color(0.60f, 1.00f, 0.10f, 1f), // Lime #99FF1A
    new Color(1.00f, 1.00f, 1.00f, 1f)  // White #FFFFFF
};
    private static readonly Vector2Int[] Dirs4 =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };


    private const int MaxLevelAttempts = 400;
    private const int MaxPathRestarts = 18;
    private const int MaxBacktrackSteps = 45000;

    // Gameplay quality constraints.
    private const int MinimumPathCells = 4;
    private const int MinimumTurns = 1;
    private const int MinimumEndpointManhattanDistance = 2;
    
    private const int MinimumAnyEndpointDistance = 1;
    
    private const int LevelsPerDifficulty = 100;

    [MenuItem("All Levels (300)")]
    public static void GenerateAll300Levels()
    {
        EnsureFoldersExist();

        var totalResult = new GenerationResult();
        
        try
        {
            GenerateDifficultyBatch(
                DifficultyMode.Easy, "Easy", 5, 5, 1.50f,
                progressOffset: 0, progressTotal: LevelsPerDifficulty * 3,
                result: totalResult);

            if (!totalResult.Cancelled)
            {
                GenerateDifficultyBatch(
                    DifficultyMode.Normal, "Normal", 7, 7, 1.25f,
                    progressOffset: LevelsPerDifficulty, progressTotal: LevelsPerDifficulty * 3,
                    result: totalResult);
            }

            if (!totalResult.Cancelled)
            {
                GenerateDifficultyBatch(
                    DifficultyMode.Hard, "Hard", 9, 9, 1.15f,
                    progressOffset: LevelsPerDifficulty * 2, progressTotal: LevelsPerDifficulty * 3,
                    result: totalResult);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ShowResultDialog(totalResult, expectedTotal: LevelsPerDifficulty * 3);
    }
    

    [MenuItem("Easy (100)")]
    public static void GenerateEasyOnly()
    {
        EnsureFoldersExist();
        var result = new GenerationResult();

        try
        {
            GenerateDifficultyBatch(
                DifficultyMode.Easy, "Easy", 5, 5, 1.50f,
                progressOffset: 0, progressTotal: LevelsPerDifficulty,
                result: result);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowResultDialog(result, expectedTotal: LevelsPerDifficulty);
    }

    [MenuItem("Normal (100)")]
    public static void GenerateNormalOnly()
    {
        EnsureFoldersExist();
        var result = new GenerationResult();

        try
        {
            GenerateDifficultyBatch(
                DifficultyMode.Normal, "Normal", 7, 7, 1.25f,
                progressOffset: 0, progressTotal: LevelsPerDifficulty,
                result: result);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowResultDialog(result, expectedTotal: LevelsPerDifficulty);
    }

    [MenuItem("Hard (100)")]
    public static void GenerateHardOnly()
    {
        EnsureFoldersExist();
        var result = new GenerationResult();

        try
        {
            GenerateDifficultyBatch(
                DifficultyMode.Hard, "Hard", 9, 9, 1.15f,
                progressOffset: 0, progressTotal: LevelsPerDifficulty,
                result: result);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowResultDialog(result, expectedTotal: LevelsPerDifficulty);
    }

    private class GenerationResult
    {
        public int Saved;
        public int Failed;
        public bool Cancelled;
    }

    private static void GenerateDifficultyBatch(
        DifficultyMode diff,
        string folderName,
        int width,
        int height,
        float moveMultiplier,
        int progressOffset,
        int progressTotal,
        GenerationResult result)
    {
        for (int i = 1; i <= LevelsPerDifficulty; i++)
        {
            result.Cancelled = EditorUtility.DisplayCancelableProgressBar(
                "Renk Tasi - Level Uretiliyor",
                $"{folderName} {i}/{LevelsPerDifficulty}",
                (progressOffset + i - 1) / (float)progressTotal);

            if (result.Cancelled) return;

            int colors = GetColorCountForLevel(diff, i);

            LevelData level = GenerateFullFillLevel(
                $"Level_{folderName}_{i:D2}",
                diff,
                width,
                height,
                colors,
                moveMultiplier);

            if (level == null)
            {
                result.Failed++;
                continue;
            }

            SaveLevelAsset(level, folderName, $"Level_{folderName}_{i:D2}");
            result.Saved++;
        }
    }

    private static int GetColorCountForLevel(DifficultyMode diff, int levelNumber)
    {
        int tier = Mathf.Clamp((levelNumber - 1) / 25, 0, 3);

        switch (diff)
        {
            case DifficultyMode.Easy:
                return 3 + tier;

            case DifficultyMode.Normal:
                return 5 + tier;

            default: 
                return 6 + tier;
        }
    }

    private static void ShowResultDialog(GenerationResult result, int expectedTotal)
    {
        string message = result.Cancelled
            ? $"Uretim iptal edildi.\n\nO ana kadar kaydedilen: {result.Saved}\nBasarisiz: {result.Failed}"
            : $"Uretim tamamlandi.\n\n" +
              $"Kaydedilen: {result.Saved}/{expectedTotal}\n" +
              $"Basarisiz: {result.Failed}\n\n" +
              $"Sadece %100 dolu, en az 1 cozumlu ve endpoint mesafesi/viraj " +
              $"dogrulamasindan gecen leveller kaydedildi.";

        EditorUtility.DisplayDialog("Renk Tasi", message, "Tamam");
    }

    // ============================================================
    // ANA URETIM
    // ============================================================

    private static LevelData GenerateFullFillLevel(
        string id,
        DifficultyMode diff,
        int width,
        int height,
        int numColors,
        float moveMultiplier)
    {
        int targetCellCount = width * height;

        int failSpaceFill = 0;
        int failSplit = 0;
        int failShape = 0;
        int failCompleteSolution = 0;
        int failCoverage = 0;
        int failNoSolution = 0;
        int failBudgetExceeded = 0;

        for (int attempt = 1; attempt <= MaxLevelAttempts; attempt++)
        {
            List<Vector2Int> fullSolution = GenerateSpaceFillingPath(width, height);

            if (fullSolution == null || fullSolution.Count != targetCellCount)
            {
                failSpaceFill++;
                continue;
            }

            List<List<Vector2Int>> paths = SplitIntoSegmentsWithMinLength(
                fullSolution,
                numColors,
                minLength: MinimumPathCells);

            if (paths == null || paths.Count != numColors)
            {
                failSplit++;
                continue;
            }
            
            if (!ValidatePathShapesAndEndpointSpacing(paths))
            {
                failShape++;
                continue;
            }

            int[,] ownerGrid = BuildOwnerGrid(paths, width, height);

            
            if (!ValidateCompleteSolution(paths, ownerGrid, width, height))
            {
                failCompleteSolution++;
                continue;
            }

            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.levelId = id;
            level.difficulty = diff;
            level.gridWidth = width;
            level.gridHeight = height;
            level.colorPairs = new List<ColorPairData>();
            level.lockedCells = new List<LockedCellData>();
            level.iceCells = new List<Vector2Int>();
            level.bridgeCells = new List<Vector2Int>();
            level.mixRules = new List<ColorMixRuleData>();

            BuildColorPairs(level, paths, diff);

            level.optimalMoves = level.colorPairs.Count;
            level.moveLimit = Mathf.Max(
                level.optimalMoves,
                Mathf.RoundToInt(level.optimalMoves * moveMultiplier));


            ApplySpecialMechanics(level, paths, ownerGrid, width, height, diff);


            if (!ValidateLevelAssetCoverage(level, paths, ownerGrid, width, height))
            {
                Object.DestroyImmediate(level);
                failCoverage++;
                continue;
            }

            
            if (LevelSolver.CanVerifyUniqueness(level))
            {
                int solutionCount = LevelSolver.CountFullFillSolutions(
                    level,
                    cap: 1,
                    nodeBudget: GetNodeBudgetFor(width, height));

                if (solutionCount == LevelSolver.BudgetExceeded)
                {
                    Object.DestroyImmediate(level);
                    failBudgetExceeded++;
                    continue;
                }

                if (solutionCount < 1)
                {
                    Object.DestroyImmediate(level);
                    failNoSolution++;
                    continue;
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[{id}] Bridge/extraEndpoint icerdigi icin cozum " +
                    "varligi dogrulanamadi. Level yine de kaydediliyor.");
            }

            return level;
        }

        Debug.LogError(
            $"[{id}] {MaxLevelAttempts} denemede gecerli %100 full-fill + " +
            "endpoint mesafesi + en az 1 virajli path seviyesi " +
            "uretilemedi. Kaydedilmiyor.\n" +
            $"Teshis -> spaceFill:{failSpaceFill} split:{failSplit} " +
            $"shape:{failShape} completeSolution:{failCompleteSolution} " +
            $"coverage:{failCoverage} " +
            $"COZUM_YOK:{failNoSolution} " +
            $"BUDGET_ASILDI:{failBudgetExceeded}");

        return null;
    }

    // ============================================================
    // RENKLER
    // ============================================================

    private static void BuildColorPairs(
        LevelData level,
        List<List<Vector2Int>> paths,
        DifficultyMode diff)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            List<Vector2Int> path = paths[i];

            if (path == null || path.Count < 2)
                continue;

            ColorPairData pair = new ColorPairData
            {
                colorId = $"Color_{i}",
                color = ColorPalette[i % ColorPalette.Length],
                startPos = path[0],
                endPos = path[path.Count - 1],
                extraEndpoints = new List<Vector2Int>()
            };

            level.colorPairs.Add(pair);
        }
    }

    // ============================================================
    // SPECIAL MECHANICS
    // ============================================================

    private static void ApplySpecialMechanics(
        LevelData level,
        List<List<Vector2Int>> paths,
        int[,] ownerGrid,
        int width,
        int height,
        DifficultyMode diff)
    {
        if (diff == DifficultyMode.Easy)
            return;

        int lockTarget = diff == DifficultyMode.Hard ? 3 : 2;
        int iceTarget = diff == DifficultyMode.Hard ? 4 : 2;

        AssignLockedCells(level, paths, lockTarget);
        AssignIceCells(level, paths, lockTarget, iceTarget);
        
    }

    private static void AssignLockedCells(
        LevelData level,
        List<List<Vector2Int>> paths,
        int count)
    {
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        for (int pathIndex = 0;
             pathIndex < paths.Count && level.lockedCells.Count < count;
             pathIndex++)
        {
            List<Vector2Int> path = paths[pathIndex];

            if (path.Count < 5)
                continue;

            int mid = path.Count / 2;
            Vector2Int candidate = FindSafeInteriorCell(path, mid, used);

            if (candidate == new Vector2Int(int.MinValue, int.MinValue))
                continue;

            level.lockedCells.Add(new LockedCellData
            {
                position = candidate,
                allowedColor = level.colorPairs[pathIndex].color
            });

            used.Add(candidate);
        }
    }

    private static void AssignIceCells(
        LevelData level,
        List<List<Vector2Int>> paths,
        int startOffset,
        int count)
    {
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        foreach (LockedCellData locked in level.lockedCells)
            used.Add(locked.position);

        for (int b = 0;
             b < count;
             b++)
        {
            int pathIndex = startOffset + b;

            if (pathIndex >= paths.Count)
                break;

            List<Vector2Int> path = paths[pathIndex];

            if (path.Count < 5)
                continue;

            int seedIndex = Mathf.Clamp(
                (path.Count * 2) / 3,
                1,
                path.Count - 2);

            Vector2Int candidate = FindSafeInteriorCell(path, seedIndex, used);

            if (candidate == new Vector2Int(int.MinValue, int.MinValue))
                continue;

            level.iceCells.Add(candidate);
            used.Add(candidate);
        }
    }

    private static Vector2Int FindSafeInteriorCell(
        List<Vector2Int> path,
        int desiredIndex,
        HashSet<Vector2Int> used)
    {
        Vector2Int invalid = new Vector2Int(int.MinValue, int.MinValue);

        for (int radius = 0; radius < path.Count; radius++)
        {
            int a = desiredIndex - radius;
            int b = desiredIndex + radius;

            if (a > 0 && a < path.Count - 1 && !used.Contains(path[a]))
                return path[a];

            if (b > 0 && b < path.Count - 1 && !used.Contains(path[b]))
                return path[b];
        }

        return invalid;
    }

    // ============================================================
    // FULL-FILL PATH GENERATION
    // ============================================================

    private static List<Vector2Int> GenerateSpaceFillingPath(int width, int height)
    {
        int total = width * height;
        
        for (int restart = 0; restart < MaxPathRestarts; restart++)
        {
            bool[,] visited = new bool[width, height];
            List<Vector2Int> path = new List<Vector2Int>(total);

            Vector2Int start = PickGoodStart(width, height, restart);

            visited[start.x, start.y] = true;
            path.Add(start);

            int steps = 0;

            if (DfsFill(
                path,
                visited,
                width,
                height,
                total,
                ref steps,
                MaxBacktrackSteps))
            {
                return path;
            }
        }
        
        return GenerateBoustrophedonPath(width, height);
    }

    private static Vector2Int PickGoodStart(int width, int height, int restart)
    {
        if (restart < MaxPathRestarts - 4)
        {
            return new Vector2Int(
                Random.Range(0, width),
                Random.Range(0, height));
        }

        switch (restart % 4)
        {
            case 0: return new Vector2Int(Random.Range(0, width), 0);
            case 1: return new Vector2Int(Random.Range(0, width), height - 1);
            case 2: return new Vector2Int(0, Random.Range(0, height));
            default: return new Vector2Int(width - 1, Random.Range(0, height));
        }
    }

    private static bool DfsFill(
        List<Vector2Int> path,
        bool[,] visited,
        int width,
        int height,
        int total,
        ref int steps,
        int maxSteps)
    {
        if (path.Count == total)
            return true;

        if (++steps > maxSteps)
            return false;

        Vector2Int current = path[path.Count - 1];

        List<Vector2Int> candidates =
            GetUnvisitedNeighborsOrdered(current, visited, width, height);

        foreach (Vector2Int next in candidates)
        {
            visited[next.x, next.y] = true;
            path.Add(next);

            if (DfsFill(
                path,
                visited,
                width,
                height,
                total,
                ref steps,
                maxSteps))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
            visited[next.x, next.y] = false;

            if (steps > maxSteps)
                return false;
        }

        return false;
    }

    private static List<Vector2Int> GetUnvisitedNeighborsOrdered(
        Vector2Int cell,
        bool[,] visited,
        int width,
        int height)
    {
        List<Vector2Int> result = new List<Vector2Int>(4);

        foreach (Vector2Int dir in Dirs4)
        {
            Vector2Int n = cell + dir;

            if (!IsInside(n, width, height))
                continue;

            if (!visited[n.x, n.y])
                result.Add(n);
        }

        ShuffleList(result);
        
        result.Sort((a, b) =>
            CountUnvisitedNeighbors(a, visited, width, height)
            .CompareTo(
                CountUnvisitedNeighbors(b, visited, width, height)));

        return result;
    }

    private static int CountUnvisitedNeighbors(
        Vector2Int cell,
        bool[,] visited,
        int width,
        int height)
    {
        int count = 0;

        foreach (Vector2Int dir in Dirs4)
        {
            Vector2Int n = cell + dir;

            if (IsInside(n, width, height) && !visited[n.x, n.y])
                count++;
        }

        return count;
    }

    private static List<Vector2Int> GenerateBoustrophedonPath(
        int width,
        int height)
    {
        List<Vector2Int> path = new List<Vector2Int>(width * height);

        for (int y = 0; y < height; y++)
        {
            if (y % 2 == 0)
            {
                for (int x = 0; x < width; x++)
                    path.Add(new Vector2Int(x, y));
            }
            else
            {
                for (int x = width - 1; x >= 0; x--)
                    path.Add(new Vector2Int(x, y));
            }
        }

        return path;
    }

    // ============================================================
    // PATH -> COLORS
    // ============================================================

    private static List<List<Vector2Int>> SplitIntoSegmentsWithMinLength(
        List<Vector2Int> fullPath,
        int numColors,
        int minLength)
    {
        int total = fullPath.Count;

        if (numColors <= 0 || total < numColors * minLength)
            return null;

        int remainingCells = total;
        int remainingColors = numColors;
        int cursor = 0;

        List<List<Vector2Int>> segments =
            new List<List<Vector2Int>>(numColors);

        for (int colorIndex = 0;
             colorIndex < numColors;
             colorIndex++)
        {
            int colorsLeftAfterThis = remainingColors - 1;

            int minForThis = minLength;
            int maxForThis =
                remainingCells -
                colorsLeftAfterThis * minLength;

            if (maxForThis < minForThis)
                return null;

            int preferred =
                Mathf.RoundToInt(
                    (float)remainingCells /
                    remainingColors);

            int jitter =
                remainingCells > remainingColors * minLength
                    ? Random.Range(-1, 2)
                    : 0;

            int length = Mathf.Clamp(
                preferred + jitter,
                minForThis,
                maxForThis);

            List<Vector2Int> segment =
                new List<Vector2Int>(length);

            for (int i = 0; i < length; i++)
                segment.Add(fullPath[cursor++]);

            segments.Add(segment);

            remainingCells -= length;
            remainingColors--;
        }

        if (cursor != total)
            return null;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Count < minLength)
                return null;
        }

        return segments;
    }


    // ============================================================
    // PATH QUALITY / ENDPOINT SPACING
    // ============================================================

    private static bool ValidatePathShapesAndEndpointSpacing(
        List<List<Vector2Int>> paths)
    {
        List<Vector2Int> allEndpoints = new List<Vector2Int>();

        for (int i = 0; i < paths.Count; i++)
        {
            List<Vector2Int> path = paths[i];

            if (path == null || path.Count < MinimumPathCells)
                return false;


            if (CountTurns(path) < MinimumTurns)
                return false;

            Vector2Int start = path[0];
            Vector2Int end = path[path.Count - 1];

            if (ManhattanDistance(start, end) <
                MinimumEndpointManhattanDistance)
            {
                return false;
            }

            allEndpoints.Add(start);
            allEndpoints.Add(end);
        }

        for (int i = 0; i < allEndpoints.Count; i++)
        {
            for (int j = i + 1; j < allEndpoints.Count; j++)
            {
                if (ManhattanDistance(
                    allEndpoints[i],
                    allEndpoints[j]) < MinimumAnyEndpointDistance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int CountTurns(List<Vector2Int> path)
    {
        if (path == null || path.Count < 3)
            return 0;

        int turns = 0;
        Vector2Int previousDirection =
            path[1] - path[0];

        for (int i = 2; i < path.Count; i++)
        {
            Vector2Int direction =
                path[i] - path[i - 1];

            if (direction != previousDirection)
                turns++;

            previousDirection = direction;
        }

        return turns;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static int[,] BuildOwnerGrid(
        List<List<Vector2Int>> paths,
        int width,
        int height)
    {
        int[,] ownerGrid = new int[width, height];

        foreach (List<Vector2Int> path in paths)
        {
            int ownerId = paths.IndexOf(path) + 1;

            foreach (Vector2Int cell in path)
            {
                if (IsInside(cell, width, height))
                    ownerGrid[cell.x, cell.y] = ownerId;
            }
        }

        return ownerGrid;
    }

    private static bool ValidateCompleteSolution(
        List<List<Vector2Int>> paths,
        int[,] ownerGrid,
        int width,
        int height)
    {
        int total = width * height;
        int covered = 0;
        HashSet<Vector2Int> uniqueCells =
            new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (ownerGrid[x, y] <= 0)
                {
                    Debug.LogError(
                        $"FULL FILL FAIL: ({x},{y}) bos.");
                    return false;
                }

                covered++;
            }
        }

        if (covered != total)
            return false;

        for (int p = 0; p < paths.Count; p++)
        {
            List<Vector2Int> path = paths[p];

            if (path == null || path.Count < 2)
            {
                Debug.LogError(
                    $"PATH FAIL: Color {p} 2 hucreden kucuk.");
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int cell = path[i];

                if (!IsInside(cell, width, height))
                    return false;

                if (!uniqueCells.Add(cell))
                {
                    Debug.LogError(
                        $"PATH FAIL: {cell} iki path tarafindan kullaniliyor.");
                    return false;
                }

                if (i == 0)
                    continue;

                Vector2Int prev = path[i - 1];

                if (ManhattanDistance(prev, cell) != 1)
                {
                    Debug.LogError(
                        $"PATH FAIL: {prev} -> {cell} dik komsu degil.");
                    return false;
                }
            }
        }

        if (uniqueCells.Count != total)
        {
            Debug.LogError(
                $"FULL FILL FAIL: unique={uniqueCells.Count}, total={total}");
            return false;
        }

        return true;
    }

    private static bool ValidateLevelAssetCoverage(
        LevelData level,
        List<List<Vector2Int>> solutionPaths,
        int[,] ownerGrid,
        int width,
        int height)
    {
        if (level == null)
            return false;

        if (!ValidateCompleteSolution(
                solutionPaths,
                ownerGrid,
                width,
                height))
        {
            return false;
        }

        if (level.colorPairs == null ||
            level.colorPairs.Count != solutionPaths.Count)
        {
            return false;
        }

        if (!ValidatePathShapesAndEndpointSpacing(solutionPaths))
            return false;

        // Her renk endpointi gercekten kendi path'inde.
        for (int i = 0; i < solutionPaths.Count; i++)
        {
            List<Vector2Int> path = solutionPaths[i];
            ColorPairData pair = level.colorPairs[i];

            if (pair.startPos != path[0])
                return false;

            if (pair.endPos != path[path.Count - 1])
                return false;

            foreach (Vector2Int extra in pair.extraEndpoints)
            {
                if (!path.Contains(extra))
                    return false;
            }
        }
        foreach (LockedCellData locked in level.lockedCells)
        {
            if (!IsInside(locked.position, width, height))
                return false;
        }

        foreach (Vector2Int ice in level.iceCells)
        {
            if (!IsInside(ice, width, height))
                return false;
        }

        foreach (Vector2Int bridge in level.bridgeCells)
        {
            if (!IsInside(bridge, width, height))
                return false;
        }

        if (!AllSpecialCellsBelongToSolution(
                level,
                ownerGrid,
                width,
                height))
        {
            return false;
        }

        return true;
    }

    private static bool AllSpecialCellsBelongToSolution(
        LevelData level,
        int[,] ownerGrid,
        int width,
        int height)
    {
        foreach (LockedCellData locked in level.lockedCells)
        {
            if (ownerGrid[locked.position.x, locked.position.y] <= 0)
                return false;
        }

        foreach (Vector2Int ice in level.iceCells)
        {
            if (ownerGrid[ice.x, ice.y] <= 0)
                return false;
        }

        foreach (Vector2Int bridge in level.bridgeCells)
        {
            if (ownerGrid[bridge.x, bridge.y] <= 0)
                return false;
        }

        return true;
    }
    
    private static int GetNodeBudgetFor(int width, int height)
    {
        int cellCount = width * height;

        if (cellCount <= 25) return 150_000;  
        if (cellCount <= 49) return 400_000;   
        return 800_000;                        
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static bool IsInside(
        Vector2Int cell,
        int width,
        int height)
    {
        return cell.x >= 0 &&
               cell.x < width &&
               cell.y >= 0 &&
               cell.y < height;
    }

    private static int ManhattanDistance(
        Vector2Int a,
        Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) +
               Mathf.Abs(a.y - b.y);
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // ============================================================
    // ASSET SAVE
    // ============================================================

    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels/Easy"))
            AssetDatabase.CreateFolder(
                "Assets/Resources/Levels",
                "Easy");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels/Normal"))
            AssetDatabase.CreateFolder(
                "Assets/Resources/Levels",
                "Normal");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels/Hard"))
            AssetDatabase.CreateFolder(
                "Assets/Resources/Levels",
                "Hard");
    }

    private static void SaveLevelAsset(
        LevelData level,
        string folder,
        string assetName)
    {
        string path =
            $"Assets/Resources/Levels/{folder}/{assetName}.asset";

        if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(level, path);
    }

    [MenuItem("Renk Tasi/Mevcut Tum Levellerin Renklerini Guncelle")]
    public static void UpdateColorsOfExistingLevels()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Resources/Levels" });
        int updatedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

            if (level == null) continue;

            if (level.colorPairs != null)
            {
                for (int i = 0; i < level.colorPairs.Count; i++)
                {
                    level.colorPairs[i].color = ColorPalette[i % ColorPalette.Length];
                }
            }

            if (level.lockedCells != null && level.colorPairs != null)
            {
                for (int i = 0; i < level.lockedCells.Count; i++)
                {
                    if (i < level.colorPairs.Count)
                    {
                        level.lockedCells[i].allowedColor = level.colorPairs[i].color;
                    }
                }
            }

            EditorUtility.SetDirty(level);
            updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Renk Tasi", $"{updatedCount} adet levelin rengi yeni palete gore guncellendi!", "Tamam");
    }
}