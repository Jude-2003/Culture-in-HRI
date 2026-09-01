using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Handles communication between Unity and the Python robot listener.
/// Sends dialogue strings to the robot and receives a "done" callback
/// when the robot finishes speaking.
///
/// Latency logging:
/// Records a timestamp immediately before each dialogue string is sent,
/// and another when the corresponding "done" signal is received.
/// The delta (round-trip latency in ms) is logged to a CSV file at
/// session end, alongside the dialogue text and utterance index.
/// This isolates system and network overhead from utterance duration.
/// </summary>
public class RobotController : MonoBehaviour
{
    public static RobotController Instance { get; private set; }

    [Header("Connection Settings")]
    public string host = "127.0.0.1";
    public int port = 65432;

    [Header("Session")]
    public string participantID = "";   // set in Inspector before each session
    public string condition = "";
    // ── Connection ────────────────────────────────────────────────────────────
    private TcpClient client;
    private NetworkStream stream;
    public bool IsConnected => isConnected;
    private bool isConnected = false;
    private Thread receiveThread;
    private Action pendingCallback;

    // ── Latency logging ───────────────────────────────────────────────────────
    private long sendTimestampMs = 0;
    private int utteranceIndex   = 0;

    private struct LatencyRecord
    {
        public int    utteranceIndex;
        public string dialogueText;
        public long   sendTimestampMs;
        public long   receiveTimestampMs;
        public long   roundTripMs;
    }

    private List<LatencyRecord> latencyLog = new List<LatencyRecord>();
    private string currentDialogue = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[RobotController] Instance set in Awake.");
    }

    private void Start()
    {
        ConnectToRobot();
    }

    private void OnDestroy()
    {
        SaveLatencyLog();
        isConnected = false;
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }

    // ── Connection ────────────────────────────────────────────────────────────

    public void ConnectToRobot()
    {
        try
        {
            Debug.Log($"[RobotController] Attempting connection to {host}:{port}...");
            client = new TcpClient();
            client.Connect(host, port);
            stream      = client.GetStream();
            isConnected = true;
            Debug.Log("[RobotController] Connected to robot listener.");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RobotController] Failed to connect to {host}:{port} — {e.GetType().Name}: {e.Message}");
            isConnected = false;
        }
    }

    // ── Speak ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a dialogue string to the robot.
    /// Records send timestamp immediately before transmission.
    /// Calls onFinished when "done" signal is received.
    /// </summary>
    public void Speak(string text, Action onFinished = null)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[RobotController] Not connected — calling callback immediately.");
            onFinished?.Invoke();
            return;
        }

        pendingCallback  = onFinished;
        currentDialogue  = text;
        utteranceIndex++;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(text);

            // Record send timestamp immediately before transmission
            sendTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            stream.Write(data, 0, data.Length);
            Debug.Log($"[RobotController] [{utteranceIndex}] Sent at {sendTimestampMs}ms: {text.Substring(0, Math.Min(60, text.Length))}...");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RobotController] Send error: {e.Message}");
            onFinished?.Invoke();
        }
    }

    // ── Receive loop ──────────────────────────────────────────────────────────

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        while (isConnected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.Log("[RobotController] Stream ended.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (message == "done")
                {
                    // Record receive timestamp immediately on signal arrival
                    long receiveMs   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long roundTripMs = receiveMs - sendTimestampMs;

                    Debug.Log($"[RobotController] [{utteranceIndex}] Done received. Round-trip: {roundTripMs}ms");

                    // Store latency record
                    LatencyRecord record = new LatencyRecord
                    {
                        utteranceIndex     = utteranceIndex,
                        dialogueText       = currentDialogue,
                        sendTimestampMs    = sendTimestampMs,
                        receiveTimestampMs = receiveMs,
                        roundTripMs        = roundTripMs
                    };
                    lock (latencyLog) { latencyLog.Add(record); }

                    // Invoke callback on main thread
                    MainThreadDispatcher.Enqueue(() => pendingCallback?.Invoke());
                }
            }
            catch (Exception e)
            {
                if (isConnected)
                    Debug.LogError($"[RobotController] ReceiveLoop error: {e.GetType().Name}: {e.Message}");
                break;
            }
        }

        isConnected = false;
        Debug.Log("[RobotController] ReceiveLoop ended.");
    }

    // ── Latency log ───────────────────────────────────────────────────────────

    /// <summary>
    /// Saves latency log to CSV at session end (OnDestroy or manual call).
    /// Output: ~/HRI_Study_Data/latency_[participantID]_[condition]_[timestamp].csv
    /// </summary>
    public void SaveLatencyLog()
    {
        if (latencyLog.Count == 0)
        {
            Debug.Log("[RobotController] No latency data to save.");
            return;
        }

        string dir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "HRI_Study_Data");
        Directory.CreateDirectory(dir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename  = $"latency_{participantID}_{condition}_{timestamp}.csv";
        string path      = Path.Combine(dir, filename);

        using (StreamWriter w = new StreamWriter(path))
        {
            w.WriteLine("UtteranceIndex,SendTimestampMs,ReceiveTimestampMs," +
                        "RoundTripMs,DialogueText");

            lock (latencyLog)
            {
                foreach (LatencyRecord r in latencyLog)
                {
                    // Escape commas in dialogue text
                    string safe = r.dialogueText.Replace("\"", "\"\"");
                    w.WriteLine($"{r.utteranceIndex},{r.sendTimestampMs}," +
                                $"{r.receiveTimestampMs},{r.roundTripMs}," +
                                $"\"{safe}\"");
                }
            }
        }

        // Summary stats
        long total = 0, min = long.MaxValue, max = long.MinValue;
        lock (latencyLog)
        {
            foreach (LatencyRecord r in latencyLog)
            {
                total += r.roundTripMs;
                if (r.roundTripMs < min) min = r.roundTripMs;
                if (r.roundTripMs > max) max = r.roundTripMs;
            }
        }
        long mean = total / latencyLog.Count;

        Debug.Log($"[RobotController] Latency log saved → {path}");
        Debug.Log($"[RobotController] Latency summary — " +
                  $"N={latencyLog.Count}, mean={mean}ms, min={min}ms, max={max}ms");
    }
}
