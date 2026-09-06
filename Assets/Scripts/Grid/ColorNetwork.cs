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


    public bool ContainsCell(CellView cell)
    {
        foreach (var branch in AllBranches)
        {
            if (branch.Contains(cell)) return true;
        }
        return false;
    }


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