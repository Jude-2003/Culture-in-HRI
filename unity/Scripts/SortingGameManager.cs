using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core controller for the Robot-Guided Sorting Task.
/// All dialogue lines match the finalised scripts exactly.
///
/// Flow:
/// 1. Robot delivers welcome message
/// 2. First object displayed
/// 3. Robot delivers object dialogue
/// 4. OnRobotFinishedSpeaking() enables category buttons
/// 5. Participant selects a category
/// 6. Robot delivers post-selection response
/// 7. Next object loaded
/// 8. Repeat for all 8 objects
/// 9. Robot delivers closing message
/// 10. End screen shown, CSV saved
/// </summary>
public class SortingGameManager : MonoBehaviour
{
    public static SortingGameManager Instance { get; private set; }

    public enum GameCondition { UAI_A_Predictable, UAI_B_Adaptive }

    [Header("Condition")]
    public GameCondition condition = GameCondition.UAI_A_Predictable;

    [Header("Objects")]
    public List<ObjectData> objects;

    private int currentObjectIndex = 0;
    private int sortedCount = 0;
    private bool gameActive = false;
    private bool waitingForParticipant = false;

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
        if (SortingDataLogger.Instance != null)
            SortingDataLogger.Instance.condition = condition.ToString();

        if (RobotController.Instance != null)
            RobotController.Instance.condition = condition.ToString();

        SortingUIManager.Instance?.SetInstruction(
            "Sort each object into a category using the robot's guidance.");
        SortingUIManager.Instance?.UpdateProgress(0, objects.Count);
        SortingUIManager.Instance?.SetCategoryButtonsInteractable(false);

        // Delay to allow RobotController to connect first
        Invoke(nameof(WelcomeMessage), 2f);
    }

    // ── Welcome ───────────────────────────────────────────────────────────────

    private void WelcomeMessage()
    {
        string welcome = condition == GameCondition.UAI_A_Predictable
            ? "Hello, welcome. Before we begin, I will explain how this game works. " +
              "You are helping allocate limited community resources. You will see a series of objects on the screen one at a time. " +
              "Your job is to sort each object into one of three categories: Urgent, Important, or Optional. " +
              "Each category can be selected three times at most. " +
              "Urgent means the object addresses an immediate need that cannot wait. " +
              "Important means it addresses a significant need but is not time critical. " +
              "Optional means it is useful but not essential. " +
              "For each object, I will give you a reason to help you decide. " +
              "Listen to my reasoning, then make your selection. " +
              "We will go through eight objects in total. The categories and the rules stay the same throughout. " +
              "Are you ready? Let's begin."
            : "Hi, welcome. So, we're going to do a sorting task today. " +
              "You are helping allocate limited community resources. " +
              "You'll see some objects and sort them into categories: Urgent, Important, or Optional. " +
              "Each category can be selected three times at most. " +
              "It's fairly intuitive, so just go with what feels right. " +
              "I'll give you some thoughts on each one as we go. " +
              "Are you ready? Let's begin.";

        SortingUIManager.Instance?.ShowRobotInstruction("Please listen to the robot...");
        RobotController.Instance.Speak(welcome, OnWelcomeFinished);
    }

    private void OnWelcomeFinished()
    {
        gameActive = true;
        currentObjectIndex = 0;
        sortedCount = 0;
        LoadCurrentObject();
    }

    // ── Game Flow ─────────────────────────────────────────────────────────────

    private void LoadCurrentObject()
    {
        if (!gameActive) return;
        if (currentObjectIndex >= objects.Count)
        {
            EndGame();
            return;
        }

        if (currentObjectIndex >= objects.Count)
        {
            EndGame();
            return;
        }

        ObjectData currentObject = objects[currentObjectIndex];
        SortingUIManager.Instance?.ShowObject(currentObject);
        SortingUIManager.Instance?.SetCategoryButtonsInteractable(false);
        SortingUIManager.Instance?.ShowRobotInstruction("Please listen to the robot...");

        waitingForParticipant = false;
        PromptRobot(currentObject);
    }

    public void OnRobotFinishedSpeaking()
    {
        SortingDataLogger.Instance?.OnObjectDisplayed(); // start timer here
        waitingForParticipant = true;
        SortingUIManager.Instance?.SetCategoryButtonsInteractable(true);
        SortingUIManager.Instance?.HideRobotInstruction();
    }

    // ── Category button wrappers ──────────────────────────────────────────────

    public void SelectUrgent()   { OnCategorySelected("Urgent"); }
    public void SelectImportant(){ OnCategorySelected("Important"); }
    public void SelectOptional() { OnCategorySelected("Optional"); }

    private void OnCategorySelected(string category)
    {
        if (!gameActive) return;
        if (!waitingForParticipant) return;

        waitingForParticipant = false;
        SortingUIManager.Instance?.SetCategoryButtonsInteractable(false);

        ObjectData currentObject = objects[currentObjectIndex];
        SortingDataLogger.Instance?.LogDecision(currentObject, category);
        SortingUIManager.Instance?.FlashCategoryButton(category);

        sortedCount++;
        currentObjectIndex++;
        SortingUIManager.Instance?.UpdateProgress(sortedCount, objects.Count);

        // Robot responds after selection
        string response = GetPostSelectionResponse(currentObjectIndex - 1);
        SortingUIManager.Instance?.ShowRobotInstruction(response);
        RobotController.Instance.Speak(response, OnPostSelectionResponseFinished);
    }


    private void OnPostSelectionResponseFinished()
    {
        SortingUIManager.Instance?.HideRobotInstruction();

        if (currentObjectIndex >= objects.Count)
            EndGame();
        else
            Invoke(nameof(LoadCurrentObject), 0.3f);
    }

    // ── Robot dialogue ────────────────────────────────────────────────────────

    private void PromptRobot(ObjectData obj)
    {
        string dialogue = GetObjectDialogue(obj.objectID);
        SortingUIManager.Instance?.ShowRobotInstruction("Please listen to the robot...");
        RobotController.Instance.Speak(dialogue, OnRobotFinishedSpeaking);
        Debug.Log($"[ROBOT {condition}] {dialogue}");
    }

    /// <summary>
    /// Returns the exact robot dialogue for each object based on condition.
    /// objectID 0-7 maps to: First Aid Kit, Library Book, Park Bench,
    /// Fire Extinguisher, Notebook, Street Lamp, Food Parcel, Recycling Bin.
    /// </summary>
    private string GetObjectDialogue(int objectID)
    {
        if (condition == GameCondition.UAI_A_Predictable)
        {
            return objectID switch
            {
                0 => "The first object is a first aid kit. A first aid kit is used to respond to injuries and medical emergencies immediately. Because it addresses situations that cannot wait, I would put this object in the Urgent category. Please make your selection.",
                1 => "The next object is a library book. A library book supports learning and education, which is valuable but does not address an immediate need. Because it is beneficial but not time critical, I would put this object in the Important category. Please make your selection.",
                2 => "The next object is a park bench. A park bench provides a place to rest in a public space. It is a useful addition to a community but is not essential or time sensitive, which is why I would put this object in the Optional category. Please make your selection.",
                3 => "The next object is a fire extinguisher. A fire extinguisher is used to respond to fires, which are immediate and dangerous situations. Because it addresses emergencies that cannot wait, I would put this object in the Urgent category. Please make your selection.",
                4 => "The next object is a notebook. A notebook supports organisation and learning, which are valuable but not immediately critical. Because it is useful but not time sensitive, I would put this object in the Important category. Please make your selection.",
                5 => "The next object is a street lamp. A street lamp improves safety and visibility in public spaces. It is a beneficial addition to a community but does not address an immediate emergency, which is why I would put this item in the Important category. Please make your selection.",
                6 => "The next object is a food parcel. A food parcel provides essential nutrition to someone in immediate need. Because access to food is a basic and urgent requirement, I would put this item in the Urgent category. Please make your selection.",
                7 => "The final object is a recycling bin. A recycling bin supports environmental responsibility, which is beneficial but not immediately critical, which is why I would put this item in the Optional category. Please make your selection.",
                _ => "Please make your selection."
            };
        }
        else // UAI_B_Adaptive
        {
            return objectID switch
            {
                0 => "Okay so first up is a first aid kit. I mean, it's pretty useful isn't it. You'd want one around. I'd probably lean toward Urgent for this one, though I suppose it depends on the situation really. Go ahead and make your choice.",
                1 => "Next is a library book. So, this one is a bit harder to place, I think. It could be Important, education is valuable, but then again, it's not like you urgently need a book. Maybe Optional? I'm not totally sure actually. What do you think?",
                2 => "A park bench. Hmm. I was going to say Optional, but actually thinking about it, public spaces are quite important for communities aren't they. So maybe Important. Though Optional isn't wrong either. Have a go.",
                3 => "Fire extinguisher. Well, fires are obviously dangerous, so you might say Urgent. But if there's no fire, it's just sitting there, so in that sense it's more of an Important thing to have around. I think Urgent makes sense, but I can see the other side of it. Go ahead.",
                4 => "A notebook. So earlier I said the library book might be Optional, though actually I'd put that as Important now that I think about it. A notebook is similar I suppose. Or maybe it's more Optional. Honestly it could go a few ways. Up to you.",
                5 => "Street lamp. Safety is important, so Important feels right. Though Urgent could work if you're thinking about night-time safety specifically. I'm not sure there's a clear answer here. What feels right to you?",
                6 => "A food parcel. I'd say Urgent. People need food. Though, I suppose it depends on who it's for and when. If someone's not in immediate need then maybe it's just Important. I think Urgent is probably right, but I wouldn't be certain. Go ahead and choose.",
                7 => "Last one. A recycling bin. Environmental stuff is genuinely important but it's not exactly urgent day to day. So Optional maybe, or Important. I keep going back and forth on these to be honest. Go ahead.",
                _ => "Go ahead and make your choice."
            };
        }
    }

    /// <summary>
    /// Returns the post-selection response after participant makes a choice.
    /// placedIndex is the index of the object just placed (0-7).
    /// </summary>
    private string GetPostSelectionResponse(int placedIndex)
    {
        bool isLastObject = (placedIndex == objects.Count - 1);

        if (condition == GameCondition.UAI_A_Predictable)
        {
            if (isLastObject)
                return "Thank you.";

            return placedIndex switch
            {
                0 => "Thank you. Moving on to the next object.",
                1 => "Thank you. Moving on to the next object.",
                2 => "Thank you. Next object.",
                3 => "Thank you. Next object.",
                4 => "Thank you. Next object.",
                5 => "Thank you. Next object.",
                6 => "Thank you. Next object.",
                _ => "Thank you. Next object."
            };
        }
        else // UAI_B_Adaptive
        {
            if (isLastObject)
                return "Okay, that's all of them.";

            return placedIndex switch
            {
                0 => "Okay, interesting. Let's see the next one.",
                1 => "Alright. Moving on.",
                2 => "Sure, okay. Next.",
                3 => "Alright. Let's continue.",
                4 => "Okay. Next one.",
                5 => "Alright, sure.",
                6 => "Okay. Last one coming up.",
                _ => "Okay. Moving on."
            };
        }
    }

    // ── End game ──────────────────────────────────────────────────────────────

    private void EndGame()
    {
        gameActive = false;
        SortingUIManager.Instance?.SetCategoryButtonsInteractable(false);
        SortingUIManager.Instance?.HideRobotInstruction();
        SortingDataLogger.Instance?.SaveToCSV();

        string closing = condition == GameCondition.UAI_A_Predictable
            ? "You have completed the sorting task. You sorted eight objects using the same three categories throughout. Thank you for participating. The session will now end."
            : "So that's the sorting task done. Some of those were genuinely tricky to place. I wasn't always sure myself. Thanks for doing that, the session will end now.";

        Debug.Log($"[ROBOT CLOSING] {closing}");
        SortingUIManager.Instance?.ShowRobotInstruction(closing);
        RobotController.Instance.Speak(closing, ShowEnd);
    }

    private void ShowEnd()
    {
        SortingUIManager.Instance?.HideRobotInstruction();
        SortingUIManager.Instance?.ShowEndScreen(
            "The session is complete. Thank you for participating.");
    }
}
