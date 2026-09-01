using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Records all participant placement decisions and timing data.
/// Writes to a CSV file at session end.
/// 
/// Logged fields per placement:
/// - ParticipantID
/// - Condition (PDI_A_Directive or PDI_B_PeerLike)
/// - BuildingName
/// - BuildingID
/// - PlacedRow
/// - PlacedCol
/// - RecommendedRow (PDI_A only, -1 if not applicable)
/// - RecommendedCol (PDI_A only, -1 if not applicable)
/// - FollowedRecommendation (true/false, PDI_A only)
/// - DecisionTimeSeconds
/// - PlacementOrder (1-5)
/// - Timestamp
/// </summary>
public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    [Header("Session Info")]
    public string participantID = "P001";
    public string condition = "PDI_A_Directive"; // set before session starts

    private List<PlacementRecord> records = new List<PlacementRecord>();
    private float selectionStartTime;
    private int placementOrder = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Call this when the participant selects a building from the sidebar.
    /// Starts the decision timer.
    /// </summary>
    public void OnBuildingSelected()
    {
        selectionStartTime = Time.time;
    }

    /// <summary>
    /// Call this when the participant places a building on the grid.
    /// </summary>
    public void LogPlacement(
        BuildingData building,
        int placedRow, int placedCol,
        int recommendedRow = -1, int recommendedCol = -1)
    {
        placementOrder++;
        float decisionTime = Time.time - selectionStartTime;

        bool followedRecommendation = false;
        if (recommendedRow >= 0 && recommendedCol >= 0)
            followedRecommendation = (placedRow == recommendedRow && placedCol == recommendedCol);

        PlacementRecord record = new PlacementRecord
        {
            participantID = participantID,
            condition = condition,
            buildingName = building.buildingName,
            buildingID = building.buildingID,
            placedRow = placedRow,
            placedCol = placedCol,
            recommendedRow = recommendedRow,
            recommendedCol = recommendedCol,
            followedRecommendation = followedRecommendation,
            decisionTimeSeconds = decisionTime,
            placementOrder = placementOrder,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        records.Add(record);
        Debug.Log($"[DataLogger] Placed {building.buildingName} at ({placedRow},{placedCol}) " +
                  $"| Followed recommendation: {followedRecommendation} " +
                  $"| Decision time: {decisionTime:F2}s");
    }

    /// <summary>
    /// Writes all records to a CSV file in the application's persistent data path.
    /// Call this at session end.
    /// </summary>
    public void SaveToCSV()
    {
        string directory = Application.persistentDataPath + "/SessionData/";
        Directory.CreateDirectory(directory);

        string filename = $"{directory}{participantID}_{condition}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        using (StreamWriter writer = new StreamWriter(filename))
        {
            // Header
            writer.WriteLine("ParticipantID,Condition,BuildingName,BuildingID," +
                             "PlacedRow,PlacedCol,RecommendedRow,RecommendedCol," +
                             "FollowedRecommendation,DecisionTimeSeconds,PlacementOrder,Timestamp");

            // Rows
            foreach (PlacementRecord r in records)
            {
                writer.WriteLine($"{r.participantID},{r.condition},{r.buildingName},{r.buildingID}," +
                                 $"{r.placedRow},{r.placedCol},{r.recommendedRow},{r.recommendedCol}," +
                                 $"{r.followedRecommendation},{r.decisionTimeSeconds:F3}," +
                                 $"{r.placementOrder},{r.timestamp}");
            }
        }

        Debug.Log($"[DataLogger] Session data saved to: {filename}");
    }

    /// <summary>
    /// Returns a summary of compliance for the session (PDI_A only).
    /// </summary>
    public (int total, int followed) GetComplianceSummary()
    {
        int total = 0;
        int followed = 0;
        foreach (PlacementRecord r in records)
        {
            if (r.recommendedRow >= 0)
            {
                total++;
                if (r.followedRecommendation) followed++;
            }
        }
        return (total, followed);
    }
}

[Serializable]
public class PlacementRecord
{
    public string participantID;
    public string condition;
    public string buildingName;
    public int buildingID;
    public int placedRow;
    public int placedCol;
    public int recommendedRow;
    public int recommendedCol;
    public bool followedRecommendation;
    public float decisionTimeSeconds;
    public int placementOrder;
    public string timestamp;
}
