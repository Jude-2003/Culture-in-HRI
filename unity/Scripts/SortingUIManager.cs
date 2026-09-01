using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI for the sorting task:
/// - Object display panel (text + optional image)
/// - Category buttons (Urgent, Important, Optional)
/// - Robot instruction panel
/// - Progress text
/// - End screen
/// </summary>
public class SortingUIManager : MonoBehaviour
{
    public static SortingUIManager Instance { get; private set; }

    [Header("Object Display")]
    public GameObject objectPanel;              // centre panel showing current object
    public TextMeshProUGUI objectNameText;      // large text showing object name
    public Image objectIcon;                    // optional icon — hidden if no sprite
    public GameObject objectIconPlaceholder;    // placeholder shown when no icon set

    [Header("Category Buttons")]
    public Button urgentButton;
    public Button importantButton;
    public Button optionalButton;
    public TextMeshProUGUI urgentLabel;
    public TextMeshProUGUI importantLabel;
    public TextMeshProUGUI optionalLabel;

    [Header("Robot Instruction")]
    public GameObject robotInstructionPanel;
    public TextMeshProUGUI robotInstructionText;

    [Header("Progress")]
    public TextMeshProUGUI progressText;        // e.g. "3 / 8 objects sorted"

    [Header("Instructions")]
    public TextMeshProUGUI instructionText;     // top instruction bar

    [Header("End Screen")]
    public GameObject endScreen;
    public TextMeshProUGUI endMessageText;

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
        // Hide end screen
        if (endScreen != null)
            endScreen.SetActive(false);

        // Hide robot panel initially
        if (robotInstructionPanel != null)
            robotInstructionPanel.SetActive(false);

        // Disable category buttons until robot finishes speaking
        SetCategoryButtonsInteractable(false);

        UpdateProgress(0, 8);
    }

    /// <summary>
    /// Displays the current object on screen.
    /// Shows icon if available, placeholder if not.
    /// </summary>
    public void ShowObject(ObjectData obj)
    {
        if (objectNameText != null)
            objectNameText.text = obj.objectName;

        if (objectIcon != null)
        {
            if (obj.icon != null)
            {
                objectIcon.sprite = obj.icon;
                objectIcon.gameObject.SetActive(true);
                if (objectIconPlaceholder != null)
                    objectIconPlaceholder.SetActive(false);
            }
            else
            {
                objectIcon.gameObject.SetActive(false);
                if (objectIconPlaceholder != null)
                    objectIconPlaceholder.SetActive(true);
            }
        }

        if (objectPanel != null)
            objectPanel.SetActive(true);
    }

    /// <summary>
    /// Shows the robot instruction panel with given text.
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
    /// Enables or disables all three category buttons.
    /// Called after robot finishes speaking.
    /// </summary>
    public void SetCategoryButtonsInteractable(bool interactable)
    {
        if (urgentButton != null) urgentButton.interactable = interactable;
        if (importantButton != null) importantButton.interactable = interactable;
        if (optionalButton != null) optionalButton.interactable = interactable;
    }

    /// <summary>
    /// Updates progress text.
    /// </summary>
    public void UpdateProgress(int sorted, int total)
    {
        if (progressText != null)
            progressText.text = $"{sorted} / {total} objects sorted";
    }

    /// <summary>
    /// Sets the top instruction text.
    /// </summary>
    public void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    /// <summary>
    /// Shows the end screen.
    /// </summary>
    public void ShowEndScreen(string message)
    {
        if (endScreen != null)
            endScreen.SetActive(true);

        if (endMessageText != null)
            endMessageText.text = message;
    }

    /// <summary>
    /// Highlights a category button briefly to confirm selection.
    /// </summary>
    public void FlashCategoryButton(string category)
    {
        Button btn = category switch
        {
            "Urgent" => urgentButton,
            "Important" => importantButton,
            "Optional" => optionalButton,
            _ => null
        };

        if (btn != null)
            StartCoroutine(FlashButton(btn));
    }

    private System.Collections.IEnumerator FlashButton(Button btn)
    {
        ColorBlock cols = btn.colors;
        Color original = cols.normalColor;
        cols.normalColor = new Color(0.7f, 0.9f, 0.7f);
        btn.colors = cols;
        yield return new WaitForSeconds(0.3f);
        cols.normalColor = original;
        btn.colors = cols;
    }
}
