using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attached to each cell in the 6x6 grid.
/// Handles building placement via Button OnClick.
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("References")]
    public Image backgroundImage;
    public Image buildingIcon;
    public TextMeshProUGUI cellLabel;

    [Header("State")]
    public int row;
    public int col;
    public bool isOccupied = false;
    public bool isHouse = false;        // true for pre-placed houses - cannot be replaced
    public BuildingData placedBuilding = null;

    private Color defaultColor;
    private Color highlightColor = new Color(0.75f, 0.92f, 0.75f, 1f);

    private void Awake()
    {
        if (backgroundImage != null)
            defaultColor = backgroundImage.color;

        if (buildingIcon != null)
            buildingIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by Button OnClick event.
    /// </summary>
    public void OnClick()
    {
        if (isOccupied) return;     // blocks placement on houses and already-placed cells
        Debug.Log($"Cell clicked: {row},{col}");
        GameManager.Instance.TryPlaceBuilding(this);
    }

    /// <summary>
    /// Places a building visually on this cell.
    /// </summary>
    public void PlaceBuilding(BuildingData building)
    {
        isOccupied = true;
        placedBuilding = building;

        if (buildingIcon != null)
        {
            buildingIcon.gameObject.SetActive(true);
            if (building.icon != null)
                buildingIcon.sprite = building.icon;
            else
                buildingIcon.color = building.tileColor;
        }

        if (backgroundImage != null)
            backgroundImage.color = defaultColor;
    }

    public void SetHighlight(bool highlighted)
    {
        if (isHouse) return;        // never highlight house cells
        if (backgroundImage != null)
            backgroundImage.color = highlighted ? highlightColor : defaultColor;
    }

    public void ClearCell()
    {
        if (isHouse) return;        // never clear house cells
        isOccupied = false;
        placedBuilding = null;

        if (buildingIcon != null)
            buildingIcon.gameObject.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.color = defaultColor;
    }
}
