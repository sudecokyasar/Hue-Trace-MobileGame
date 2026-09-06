using System.Collections.Generic;
using UnityEngine;


public static class LevelSolver
{

    public const int BudgetExceeded = -1;

    private static readonly Vector2Int[] Dirs4 =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };


    public static int CountFullFillSolutions(
        LevelData level,
        int cap = 2,
        int nodeBudget = int.MaxValue)
    {
        int w = level.gridWidth;
        int h = level.gridHeight;
        int numColors = level.colorPairs.Count;

        int[,] owner = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                owner[x, y] = -1;

        Vector2Int[] starts = new Vector2Int[numColors];
        Vector2Int[] ends = new Vector2Int[numColors];

        for (int i = 0; i < numColors; i++)
        {
            starts[i] = level.colorPairs[i].startPos;
            ends[i] = level.colorPairs[i].endPos;
        }

        int[,] lockedColor = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                lockedColor[x, y] = -1;

        foreach (var lc in level.lockedCells)
        {
            int ci = FindColorIndex(level, lc.allowedColor);
            if (ci >= 0)
                lockedColor[lc.position.x, lc.position.y] = ci;
        }

        int solutionCount = 0;
        int nodeCount = 0;
        bool budgetExceeded = false;

        SolveColor(
            0, numColors, owner, starts, ends, lockedColor,
            w, h, ref solutionCount, cap,
            ref nodeCount, nodeBudget, ref budgetExceeded);

        if (budgetExceeded)
            return BudgetExceeded;

        return solutionCount;
    }


    public static bool CanVerifyUniqueness(LevelData level)
    {
        if (level.bridgeCells != null && level.bridgeCells.Count > 0)
            return false;

        foreach (var pair in level.colorPairs)
        {
            if (pair.extraEndpoints != null && pair.extraEndpoints.Count > 0)
                return false;
        }

        return true;
    }

    private static int FindColorIndex(LevelData level, Color c)
    {
        for (int i = 0; i < level.colorPairs.Count; i++)
        {
            if (ColorsEqual(level.colorPairs[i].color, c))
                return i;
        }
        return -1;
    }

    private static bool ColorsEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f &&
               Mathf.Abs(a.g - b.g) < 0.01f &&
               Mathf.Abs(a.b - b.b) < 0.01f;
    }

    private static void SolveColor(
        int colorIndex, int numColors,
        int[,] owner, Vector2Int[] starts, Vector2Int[] ends, int[,] lockedColor,
        int w, int h, ref int solutionCount, int cap,
        ref int nodeCount, int nodeBudget, ref bool budgetExceeded)
    {
        if (budgetExceeded || solutionCount >= cap) return;

        if (++nodeCount > nodeBudget)
        {
            budgetExceeded = true;
            return;
        }

        if (colorIndex == numColors)
        {

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (owner[x, y] < 0)
                        return;
                }
            }

            solutionCount++;
            return;
        }

        Vector2Int start = starts[colorIndex];
        Vector2Int end = ends[colorIndex];

        if (lockedColor[start.x, start.y] >= 0 &&
            lockedColor[start.x, start.y] != colorIndex)
        {
            return;
        }

        owner[start.x, start.y] = colorIndex;

        ExtendPath(
            start, end, colorIndex, colorIndex + 1, numColors,
            owner, starts, ends, lockedColor, w, h, ref solutionCount, cap,
            ref nodeCount, nodeBudget, ref budgetExceeded);

        owner[start.x, start.y] = -1;
    }

    private static void ExtendPath(
        Vector2Int current, Vector2Int target,
        int colorIndex, int nextColorIndex, int numColors,
        int[,] owner, Vector2Int[] starts, Vector2Int[] ends, int[,] lockedColor,
        int w, int h, ref int solutionCount, int cap,
        ref int nodeCount, int nodeBudget, ref bool budgetExceeded)
    {
        if (budgetExceeded || solutionCount >= cap) return;

        if (++nodeCount > nodeBudget)
        {
            budgetExceeded = true;
            return;
        }

        if (current == target)
        {
            if (IsRemainingBoardViable(owner, starts, ends, nextColorIndex, numColors, w, h))
            {
                SolveColor(
                    nextColorIndex, numColors, owner, starts, ends, lockedColor,
                    w, h, ref solutionCount, cap,
                    ref nodeCount, nodeBudget, ref budgetExceeded);
            }
            return;
        }

        foreach (Vector2Int dir in Dirs4)
        {
            if (budgetExceeded || solutionCount >= cap) return;

            Vector2Int next = current + dir;

            if (!Inside(next, w, h)) continue;
            if (owner[next.x, next.y] != -1) continue;

            if (lockedColor[next.x, next.y] >= 0 &&
                lockedColor[next.x, next.y] != colorIndex)
            {
                continue;
            }

            owner[next.x, next.y] = colorIndex;

            if (IsRemainingBoardViable(owner, starts, ends, nextColorIndex, numColors, w, h))
            {
                ExtendPath(
                    next, target, colorIndex, nextColorIndex, numColors,
                    owner, starts, ends, lockedColor, w, h, ref solutionCount, cap,
                    ref nodeCount, nodeBudget, ref budgetExceeded);
            }

            owner[next.x, next.y] = -1;

            if (budgetExceeded || solutionCount >= cap) return;
        }
    }


    private static bool IsRemainingBoardViable(
        int[,] owner, Vector2Int[] starts, Vector2Int[] ends,
        int nextColorIndex, int numColors, int w, int h)
    {
        for (int c = nextColorIndex; c < numColors; c++)
        {
            if (owner[starts[c].x, starts[c].y] != -1) return false;
            if (owner[ends[c].x, ends[c].y] != -1) return false;
        }

        return true;
    }

    private static bool Inside(Vector2Int p, int w, int h)
    {
        return p.x >= 0 && p.x < w && p.y >= 0 && p.y < h;
    }
}