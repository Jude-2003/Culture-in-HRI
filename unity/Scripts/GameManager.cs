using UnityEngine;
using System.Collections.Generic;
 
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
 
    public enum GameCondition { PDI_A_Directive, PDI_B_PeerLike }
 
    [Header("Condition")]
    public GameCondition condition = GameCondition.PDI_A_Directive;
 
    [Header("Buildings")]
    public List<BuildingData> buildings;
    public List<SidebarBuilding> sidebarItems;
 
    [Header("Recommended Placements (PDI_A only)")]
    public List<Vector2Int> recommendedPlacements = new List<Vector2Int>
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 5),
        new Vector2Int(2, 2),
        new Vector2Int(5, 0),
        new Vector2Int(5, 5),
    };
 
    private BuildingData selectedBuilding = null;
    private SidebarBuilding selectedSidebarItem = null;
    private int currentBuildingIndex = 0;
    private int placedCount = 0;
    private bool gameActive = false;
 
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
 
    private void Start() { StartGame(); }
 
    public void StartGame()
    {
        gameActive = false;
        placedCount = 0;
        currentBuildingIndex = 0;
 
        if (DataLogger.Instance != null)
            DataLogger.Instance.condition = condition.ToString();
            
       if (RobotController.Instance != null)
              RobotController.Instance.condition = condition.ToString();
      
        UIManager.Instance?.SetInstruction(
            condition == GameCondition.PDI_A_Directive
                ? "Follow the robot's instructions to build your town."
                : "Build your town together with the robot.");
 
        UIManager.Instance?.UpdateProgress(0, buildings.Count);
        Invoke(nameof(WelcomeMessage), 2f);
    }
 
    private void WelcomeMessage()
    {
        string welcome = condition == GameCondition.PDI_A_Directive
            ? "Welcome. Today you will be building a small town. Your town will be scored on how well it serves its residents. I will tell you where to place each building. Follow my instructions and we will complete the town together. Let's begin."
            : "Hi, welcome. We're going to build a small town together today. Your town will be scored on how well it serves its residents. I will share my thoughts on where each building could go, and then you can think about it before deciding. Whenever you're ready, let's start with the first building.";
 
        UIManager.Instance?.ShowRobotInstruction("Please listen to the robot...");
        Debug.Log($"[GameManager] RobotController: {RobotController.Instance}, Connected: {RobotController.Instance?.IsConnected}");
 
        if (RobotController.Instance == null || !RobotController.Instance.IsConnected)
        {
            Debug.LogWarning("[GameManager] RobotController not ready — auto-advancing.");
            Invoke(nameof(OnWelcomeFinished), 2f);
            return;
        }
 
        RobotController.Instance.Speak(welcome, OnWelcomeFinished);
    }
 
    private void OnWelcomeFinished()
    {
        gameActive = true;
        PromptRobotForCurrentBuilding();
    }
 
    public void OnRobotFinishedSpeaking()
    {
        EnableSidebarForCurrentBuilding();
        GridManager.Instance?.HighlightEmptyCells(true);
        UIManager.Instance?.UpdateStatus(
            condition == GameCondition.PDI_A_Directive
                ? $"Place the {buildings[currentBuildingIndex].buildingName} as instructed."
                : $"Where would you like to place the {buildings[currentBuildingIndex].buildingName}?");
    }
 
    public void SelectBuilding(BuildingData building, SidebarBuilding sidebarItem)
    {
        selectedSidebarItem?.SetSelected(false);
        selectedBuilding = building;
        selectedSidebarItem = sidebarItem;
        sidebarItem.SetSelected(true);
        DataLogger.Instance?.OnBuildingSelected();
        UIManager.Instance?.UpdateSelectedBuilding(building.buildingName);
    }
 
    public void TryPlaceBuilding(GridCell targetCell)
    {
        Debug.Log($"[GameManager] TryPlaceBuilding — gameActive:{gameActive}, selectedBuilding:{selectedBuilding}, occupied:{targetCell.isOccupied}");
        if (!gameActive) return;
        if (selectedBuilding == null) return;
        if (targetCell.isOccupied) return;
 
        int recRow = -1, recCol = -1;
        if (currentBuildingIndex < recommendedPlacements.Count)
        {
            recRow = recommendedPlacements[currentBuildingIndex].x;
            recCol = recommendedPlacements[currentBuildingIndex].y;
        }
 
        bool followedRecommendation = (targetCell.row == recRow && targetCell.col == recCol);
        targetCell.PlaceBuilding(selectedBuilding);
        DataLogger.Instance?.LogPlacement(selectedBuilding, targetCell.row, targetCell.col, recRow, recCol);
 
        selectedSidebarItem?.MarkAsPlaced();
        selectedBuilding = null;
        selectedSidebarItem = null;
        GridManager.Instance?.HighlightEmptyCells(false);
        UIManager.Instance?.UpdateSelectedBuilding("");
 
        placedCount++;
        currentBuildingIndex++;
        UIManager.Instance?.UpdateProgress(placedCount, buildings.Count);
 
        DisableAllSidebar();
        UIManager.Instance?.UpdateStatus("Waiting for the robot...");
        RobotRespondToPlacement(followedRecommendation, placedCount >= buildings.Count);
    }
 
    private void PromptRobotForCurrentBuilding()
    {
        if (currentBuildingIndex >= buildings.Count) return;
 
        string buildingName = buildings[currentBuildingIndex].buildingName;
        string positionLabel = GetPositionLabel(
            recommendedPlacements[currentBuildingIndex].x,
            recommendedPlacements[currentBuildingIndex].y);
 
        string dialogue = condition == GameCondition.PDI_A_Directive
            ? PDI_A_Prompt(buildingName, positionLabel)
            : PDI_B_Prompt(buildingName, positionLabel);
 
        Debug.Log($"[ROBOT {condition}] {dialogue}");
        UIManager.Instance?.ShowRobotInstruction("Please listen to the robot...");
 
        if (RobotController.Instance == null || !RobotController.Instance.IsConnected)
        { Invoke(nameof(OnRobotFinishedSpeaking), 2f); return; }
 
        RobotController.Instance.Speak(dialogue, OnRobotFinishedSpeaking);
    }
 
    private void RobotRespondToPlacement(bool followedRecommendation, bool isLastBuilding)
    {
        if (isLastBuilding) { EndGame(); return; }
 
        string response = condition == GameCondition.PDI_A_Directive
            ? (followedRecommendation ? PDI_A_ResponseCompliant() : PDI_A_ResponseNonCompliant())
            : (followedRecommendation ? PDI_B_ResponseCompliant() : PDI_B_ResponseNonCompliant());
 
        Debug.Log($"[ROBOT RESPONSE] {response}");
        UIManager.Instance?.ShowRobotInstruction(response);
 
        if (RobotController.Instance == null || !RobotController.Instance.IsConnected)
        { Invoke(nameof(OnPlacementResponseFinished), 2f); return; }
 
        RobotController.Instance.Speak(response, OnPlacementResponseFinished);
    }
 
    public void OnPlacementResponseFinished()
    {
        UIManager.Instance?.HideRobotInstruction();
        PromptRobotForCurrentBuilding();
    }
 
    private string PDI_A_Prompt(string building, string position) => building switch
    {
        "Hospital"     => $"We will start with the Hospital. The {position} is the best position — it keeps the hospital accessible from inside and outside of the town. Place it there.",
        "School"       => $"Next is the School. Place it in the {position}. This keeps it away from the busy town centre.",
        "Market"       => $"Now the Market. The {position} of the grid is the right position — it needs to be reachable from all directions. Place it there.",
        "Park"         => $"Next is the Park. Place it in the {position}. Open green spaces belong at the edges of a town.",
        "Fire Station" => $"Finally, the Fire Station. Place it in the {position}. Emergency services need direct access from outside the town.",
        _              => $"Place the {building} in the {position}."
    };
 
    private string PDI_B_Prompt(string building, string position) => building switch
    {
        "Hospital"     => $"Let's start with the Hospital. I think the {position} would work really well — it keeps the hospital accessible from inside and outside of the town. What do you think?",
        "School"       => $"Now the School. I think the {position} makes sense — it sits away from the busy town centre. What do you think?",
        "Market"       => $"Next is the Market. I think somewhere in the {position} would be ideal — markets work best when they are reachable from all directions. What do you think?",
        "Park"         => $"Now the Park. I think the {position} would be a lovely spot — green spaces feel most natural at the edges of a town. What do you think?",
        "Fire Station" => $"Last one — the Fire Station. I think the {position} works best — emergency services need quick access from outside the town. What do you think?",
        _              => $"I think the {building} would work well in the {position}. What do you think?"
    };
 
    private string PDI_A_ResponseCompliant()    => "Good.";
    private string PDI_A_ResponseNonCompliant() => "You placed it elsewhere. We will continue.";
    private string PDI_B_ResponseCompliant()    => "Great, that works really well.";
    private string PDI_B_ResponseNonCompliant() => "Oh interesting, I can see why you would put it there.";
 
    private string GetPositionLabel(int row, int col)
    {

        string rowLabel = row <= 1 ? "top" : row <= 3 ? "middle" : "bottom";
        string colLabel = col <= 1 ? "left" : col <= 3 ? "centre" : "right";
        return $"{rowLabel}-{colLabel}";
    }
 
    private void EnableSidebarForCurrentBuilding()
    {
        for (int i = 0; i < sidebarItems.Count; i++)
        {
            if (sidebarItems[i] == null) continue;
            if (i >= currentBuildingIndex)
                sidebarItems[i].gameObject.SetActive(true);
        }
    }
 
    private void DisableAllSidebar()
    {
        selectedBuilding = null;
        selectedSidebarItem?.SetSelected(false);
        selectedSidebarItem = null;
        UIManager.Instance?.UpdateSelectedBuilding("");
    }
 
    private void EndGame()
    {
        gameActive = false;
        GridManager.Instance?.HighlightEmptyCells(false);
        UIManager.Instance?.HideRobotInstruction();
        DataLogger.Instance?.SaveToCSV();
 
        if (condition == GameCondition.PDI_A_Directive)
        {
            var (total, followed) = DataLogger.Instance.GetComplianceSummary();
            Debug.Log($"[GameManager] Compliance: {followed}/{total} recommendations followed.");
        }
 
        string closing = condition == GameCondition.PDI_A_Directive
            ? "The town is now complete. Thank you for following the instructions."
            : "The town looks great — it was really nice building it with you. Thank you for taking part.";
 
        Debug.Log($"[ROBOT CLOSING] {closing}");
        UIManager.Instance?.ShowRobotInstruction(closing);
 
        if (RobotController.Instance == null || !RobotController.Instance.IsConnected)
        { Invoke(nameof(ShowEndScreen), 3f); return; }
 
        RobotController.Instance.Speak(closing, ShowEndScreen);
    }
 
    private void ShowEndScreen()
    {
        UIManager.Instance?.HideRobotInstruction();
        UIManager.Instance?.ShowEndScreen("The session is complete. Thank you for participating.");
    }
}
