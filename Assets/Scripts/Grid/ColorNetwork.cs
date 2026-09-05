using System.Collections.Generic;
using UnityEngine;

public class ColorNetwork
{
    public Color Color { get; private set; }
    public List<List<CellView>> AllBranches { get; private set; } = new List<List<CellView>>();
    public List<LineRenderer> Lines { get; private set; } = new List<LineRenderer>();

    public ColorNetwork(Color color)
    {
        Color = color;
    }

    /// <summary>
    /// Bu renk aðýnýn geçtiði tüm benzersiz hücreleri döner.
    /// </summary>
    public HashSet<CellView> GetAllOccupiedCells()
    {
        HashSet<CellView> cells = new HashSet<CellView>();
        foreach (var branch in AllBranches)
        {
            foreach (var cell in branch)
            {
                cells.Add(cell);
            }
        }
        return cells;
    }

    /// <summary>
    /// Belirtilen hücre bu rengin herhangi bir dalýnda var mý?
    /// </summary>
    public bool ContainsCell(CellView cell)
    {
        foreach (var branch in AllBranches)
        {
            if (branch.Contains(cell)) return true;
        }
        return false;
    }

    /// <summary>
    /// Bu renge ait tüm çizgileri ve verileri temizler.
    /// </summary>
    public void Clear()
    {
        foreach (var line in Lines)
        {
            if (line != null)
            {
                Object.Destroy(line.gameObject);
            }
        }
        Lines.Clear();
        AllBranches.Clear();
    }
}