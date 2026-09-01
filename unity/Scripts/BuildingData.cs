using UnityEngine;

/// <summary>
/// ScriptableObject that defines a building's properties.
/// Create one asset per building via Assets > Create > CityBuilder > BuildingData
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "CityBuilder/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;         // e.g. "Hospital"
    public Sprite icon;                 // 2D icon shown in sidebar and on grid
    public Color tileColor = Color.white; // fallback colour if no icon assigned

    [Header("Logging")]
    public int buildingID;              // unique ID for data logging (0-4)
}
