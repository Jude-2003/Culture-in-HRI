using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the 6x6 grid of cells.
/// Pre-places houses at fixed positions on startup.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int rows = 6;
    public int cols = 6;

    [Header("References")]
    public GameObject gridCellPrefab;
    public Transform gridParent;

    [Header("House Settings")]
    public Sprite houseSprite;          // assign house sprite in Inspector

    // Pre-placed house positions (row, col) - 0-indexed
    private readonly Vector2Int[] housePositions = new Vector2Int[]
    {
        new Vector2Int(0, 2),
        new Vector2Int(1, 4),
        new Vector2Int(2, 0),
        new Vector2Int(3, 3),
        new Vector2Int(4, 5),
        new Vector2Int(5, 1),
    };

    private GridCell[,] cells;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GenerateGrid();
        PlaceHouses();
    }

    private void GenerateGrid()
    {
        cells = new GridCell[rows, cols];

        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject cellObj = Instantiate(gridCellPrefab, gridParent);
                cellObj.name = $"Cell_{r}_{c}";

                GridCell cell = cellObj.GetComponent<GridCell>();
                cell.row = r;
                cell.col = c;
                cells[r, c] = cell;
            }
        }
    }

    /// <summary>
    /// Pre-places houses at fixed positions.
    /// These cells are marked as occupied and cannot receive buildings.
    /// </summary>
    private void PlaceHouses()
    {
        foreach (Vector2Int pos in housePositions)
        {
            GridCell cell = GetCell(pos.x, pos.y);
            if (cell == null) continue;

            cell.isOccupied = true;
            cell.isHouse = true;

            if (cell.buildingIcon != null)
            {
                cell.buildingIcon.gameObject.SetActive(true);
                if (houseSprite != null)
                    cell.buildingIcon.sprite = houseSprite;
                else
                    cell.buildingIcon.color = new Color(0.7f, 0.7f, 0.7f);
            }

            if (cell.backgroundImage != null)
                cell.backgroundImage.color = new Color(0.88f, 0.88f, 0.88f);
        }
    }

    public GridCell GetCell(int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return null;
        return cells[row, col];
    }

    public List<GridCell> GetEmptyCells()
    {
        List<GridCell> empty = new List<GridCell>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (!cells[r, c].isOccupied)
                    empty.Add(cells[r, c]);
        return empty;
    }

    public bool AllBuildingsPlaced(int totalBuildings)
    {
        int placed = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (cells[r, c].isOccupied && !cells[r, c].isHouse)
                    placed++;
        return placed >= totalBuildings;
    }

    public void HighlightEmptyCells(bool highlight)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (!cells[r, c].isOccupied)
                    cells[r, c].SetHighlight(highlight);
    }

    public void ResetGrid()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (!cells[r, c].isHouse)
                    cells[r, c].ClearCell();
    }
}
