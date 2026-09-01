using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Records all participant sorting decisions and timing data.
/// Writes to CSV at session end.
/// </summary>
public class SortingDataLogger : MonoBehaviour
{
    public static SortingDataLogger Instance { get; private set; }

    [Header("Session Info")]
    public string participantID = "";
    public string condition = "";

    private List<SortingRecord> records = new List<SortingRecord>();
    private float selectionStartTime;
    private int sortingOrder = 0;

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
    /// Call when a new object is displayed. Starts the decision timer.
    /// </summary>
    public void OnObjectDisplayed()
    {
        selectionStartTime = Time.time;
    }

    /// <summary>
    /// Call when participant selects a category.
    /// </summary>
    public void LogDecision(ObjectData obj, string categoryChosen)
    {
        sortingOrder++;
        float decisionTime = Time.time - selectionStartTime;

        SortingRecord record = new SortingRecord
        {
            participantID = participantID,
            condition = condition,
            objectName = obj.objectName,
            objectID = obj.objectID,
            categoryChosen = categoryChosen,
            decisionTimeSeconds = decisionTime,
            sortingOrder = sortingOrder,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        records.Add(record);
        Debug.Log($"[SortingLogger] {obj.objectName} → {categoryChosen} " +
                  $"| Decision time: {decisionTime:F2}s");
    }

    /// <summary>
    /// Saves all records to CSV at session end.
    /// </summary>
    public void SaveToCSV()
    {
        string directory = Application.persistentDataPath + "/SessionData/";
        Directory.CreateDirectory(directory);

        string filename = $"{directory}{participantID}_{condition}_" +
                         $"{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("ParticipantID,Condition,ObjectName,ObjectID," +
                           "CategoryChosen,DecisionTimeSeconds,SortingOrder,Timestamp");

            foreach (SortingRecord r in records)
            {
                writer.WriteLine($"{r.participantID},{r.condition},{r.objectName}," +
                               $"{r.objectID},{r.categoryChosen}," +
                               $"{r.decisionTimeSeconds:F3},{r.sortingOrder},{r.timestamp}");
            }
        }

        Debug.Log($"[SortingLogger] Data saved to: {filename}");
    }
}

[Serializable]
public class SortingRecord
{
    public string participantID;
    public string condition;
    public string objectName;
    public int objectID;
    public string categoryChosen;
    public float decisionTimeSeconds;
    public int sortingOrder;
    public string timestamp;
}
