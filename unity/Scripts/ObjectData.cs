using UnityEngine;

/// <summary>
/// ScriptableObject defining each object in the sorting task.
/// Create via Assets > Create > SortingTask > ObjectData
/// </summary>
[CreateAssetMenu(fileName = "NewObject", menuName = "SortingTask/ObjectData")]
public class ObjectData : ScriptableObject
{
    [Header("Object Info")]
    public string objectName;       // e.g. "First Aid Kit"
    public Sprite icon;             // optional — leave null for text-only mode
    public int objectID;            // unique ID for logging (0-7)
}
