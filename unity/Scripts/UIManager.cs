using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements: instruction text, status panel, 
/// building name display, and end screen.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Instruction Panel")]
    public TextMeshProUGUI instructionText;
    public GameObject instructionPanel;

    [Header("Status")]
    public TextMeshProUGUI statusText;          // e.g. "Select a building to place"
    public TextMeshProUGUI selectedBuildingText; // shows currently selected building name

    [Header("Progress")]
    public TextMeshProUGUI progressText;         // e.g. "3 / 5 buildings placed"

    [Header("End Screen")]
    public GameObject endScreen;
    public TextMeshProUGUI endMessageText;

    [Header("Robot Instruction Panel")]
    public GameObject robotInstructionPanel;     // shown when robot is giving guidance
    public TextMeshProUGUI robotInstructionText; // robot's recommended placement text

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
        if (endScreen != null)
            endScreen.SetActive(false);

        if (robotInstructionPanel != null)
            robotInstructionPanel.SetActive(false);

        UpdateStatus("Select a building from the sidebar, then click a cell to place it.");
        UpdateProgress(0, 5);
    }

    /// <summary>
    /// Updates the main instruction text at the top of the screen.
    /// </summary>
    public void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    /// <summary>
    /// Updates the status bar text.
    /// </summary>
    public void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    /// <summary>
    /// Updates the selected building name display.
    /// </summary>
    public void UpdateSelectedBuilding(string buildingName)
    {
        if (selectedBuildingText != null)
            selectedBuildingText.text = string.IsNullOrEmpty(buildingName)
                ? ""
                : $"Selected: {buildingName}";
    }

    /// <summary>
    /// Updates the progress indicator.
    /// </summary>
    public void UpdateProgress(int placed, int total)
    {
        if (progressText != null)
            progressText.text = $"{placed} / {total} buildings placed";
    }

    /// <summary>
    /// Shows the robot's instruction panel with the given text.
    /// Called by the robot integration layer when the robot speaks.
    /// </summary>
    public void ShowRobotInstruction(string text)
    {
        if (robotInstructionPanel != null)
            robotInstructionPanel.SetActive(true);

        if (robotInstructionText != null)
            robotInstructionText.text = text;
    }

    /// <summary>
    /// Hides the robot instruction panel.
    /// </summary>
    public void HideRobotInstruction()
    {
        if (robotInstructionPanel != null)
            robotInstructionPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the end screen with a completion message.
    /// </summary>
    public void ShowEndScreen(string message)
    {
        if (endScreen != null)
            endScreen.SetActive(true);

        if (endMessageText != null)
            endMessageText.text = message;
    }
}
