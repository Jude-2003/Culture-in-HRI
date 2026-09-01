using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Attached to each building button in the sidebar.
/// Handles selection and drag-and-drop initiation.
/// </summary>
public class SidebarBuilding : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public BuildingData buildingData;
    public Image iconImage;
    public TextMeshProUGUI nameLabel;
    public Image selectionHighlight;    // a border/overlay shown when selected

    [Header("Drag")]
    public GameObject dragIconPrefab;   // ghost icon shown while dragging

    private GameObject dragIcon;
    private Canvas rootCanvas;
    private bool isPlaced = false;

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        if (iconImage != null && buildingData.icon != null)
            iconImage.sprite = buildingData.icon;

        if (nameLabel != null)
            nameLabel.text = buildingData.buildingName;

        if (selectionHighlight != null)
            selectionHighlight.gameObject.SetActive(false);
    }

    /// <summary>
    /// Click to select this building for placement.
    /// </summary>


    /// <summary>
    /// Begin drag — create a ghost icon following the cursor.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        GameManager.Instance.SelectBuilding(buildingData, this);

        if (dragIconPrefab != null && rootCanvas != null)
        {
            dragIcon = Instantiate(dragIconPrefab, rootCanvas.transform);
            dragIcon.GetComponent<Image>().sprite = buildingData.icon;
            dragIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
        }
    }

    /// <summary>
    /// Move the ghost icon with the cursor.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    /// <summary>
    /// End drag — destroy ghost icon. Placement is handled by GridCell.OnDrop.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
    }

    /// <summary>
    /// Shows or hides the selection highlight border.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.gameObject.SetActive(selected);
    }

    /// <summary>
    /// Marks this building as placed — dims it and disables interaction.
    /// </summary>
    public void MarkAsPlaced()
    {
        isPlaced = true;
        SetSelected(false);

        if (iconImage != null)
            iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        Button btn = GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }

    public void OnClick()
    {
        Debug.Log("OnClick called on: " + gameObject.name);
        if (isPlaced)
        {
            Debug.Log("Building is already placed, ignoring");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.Log("GameManager.Instance is null");
            return;
        }
        Debug.Log("Calling SelectBuilding for: " + buildingData.buildingName);
        GameManager.Instance.SelectBuilding(buildingData, this);
    }
}
