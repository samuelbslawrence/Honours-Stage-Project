using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolSpawner : MonoBehaviour
{
  [Header("Basic Settings")]
  [SerializeField] private Transform attachPoint;
  [SerializeField] private Vector3 positionOffset = new Vector3(-0.1f, 0.3f, 0.1f);
  [SerializeField] private Vector3 rotationOffset = new Vector3(15f, 30f, 0f);
  [SerializeField] private float uiScale = 0.0004f;
  [SerializeField] private KeyCode toggleKey = KeyCode.I;

  [Header("VR Settings")]
  [SerializeField] private bool useVRControls = true;
  [SerializeField] private KeyCode vrToggleKey = KeyCode.RightBracket;
  [SerializeField] private float joystickDeadzone = 0.3f;
  [SerializeField] private float navigationCooldown = 0.3f;

  [Header("Scene Generator")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;

  // CHANGED: Instead of prefabs, we reference existing tools in the scene
  [Header("Existing Tools in Scene")]
  [SerializeField] private GameObject[] existingTools = new GameObject[4]; // Assign your existing tools here
  [SerializeField] private int[] toolQuantities = new int[] { 1, 1, 1, 3 };

  // NEW: Tool storage position when not in use
  [Header("Tool Storage")]
  [SerializeField] private Transform toolStorageArea; // Where tools are stored when not in use
  [SerializeField] private Vector3 storageOffset = new Vector3(0, -10, 0); // Default storage position offset

  [Header("Cross-Script Communication")]
  [SerializeField] private EvidenceChecklist evidenceChecklist;
  [SerializeField] private bool autoFindEvidenceChecklist = true;
  [SerializeField] private bool openWithEvidenceChecklist = true;

  [Header("UI Colors")]
  [SerializeField] private Color availableColor = new Color(0.3f, 0.8f, 0.3f);
  [SerializeField] private Color unavailableColor = new Color(0.8f, 0.3f, 0.3f);
  [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f);
  [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.2f);

  [Header("Audio")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip[] audioClips = new AudioClip[3]; // UI, Spawn, Generate

  // Core data - simplified
  private readonly string[] toolNames = { "Camera", "Scanner", "Brush", "UV Light" };
  private readonly string[] toolDescriptions = {
        "Point and scan to detect evidence automatically",
        "Advanced tool for detailed evidence analysis",
        "Apply to surfaces to reveal fingerprints",
        "Reveals hidden evidence under ultraviolet light"
    };

  private readonly string[] generateNames = { "Generate Random Scene" };
  private readonly string[] generateDescriptions = {
        "Create a completely randomized crime scene with varied evidence"
    };

  private readonly string[] gameNames = { "Quit", "Pause", "Resume" };
  private readonly string[] gameDescriptions = {
        "Return to main menu",
        "Pause the game",
        "Resume the game"
    };

  // UI Components
  private GameObject uiInstance;
  private bool isUIVisible = false;
  private int currentTab = 0; // 0=Tools, 1=Generate, 2=Game
  private int selectedIndex = 0;
  private bool vrControlActive = false;

  // VR Input - CHANGED TO LEFT CONTROLLER
  private List<UnityEngine.XR.InputDevice> leftControllers = new List<UnityEngine.XR.InputDevice>();
  private bool wasYPressed = false; // Changed from B to Y
  private bool wasXPressed = false; // Changed from A to X
  private float lastInputTime = 0f;
  private Vector2 lastJoystickInput = Vector2.zero;

  // Tool Management - CHANGED: Track active tools instead of spawned objects
  private Dictionary<int, bool> toolsActive = new Dictionary<int, bool>(); // Track which tools are currently active
  private Dictionary<int, Vector3> originalToolPositions = new Dictionary<int, Vector3>(); // Store original positions

  // UI References
  private List<GameObject> allButtons = new List<GameObject>();
  private List<TextMeshProUGUI> buttonTitles = new List<TextMeshProUGUI>();
  private List<TextMeshProUGUI> buttonDescriptions = new List<TextMeshProUGUI>();

  // Singleton
  private static ToolSpawner instance;
  public static ToolSpawner Instance => instance;

  void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }
    instance = this;
  }

  void Start()
  {
    SetupComponents();
    InitializeExistingTools(); // NEW: Initialize existing tools
    CreateUI();
  }

  void Update()
  {
    HandleInput();
    if (isUIVisible)
    {
      UpdateUI();
      if (!useVRControls || !vrControlActive)
      {
        HandleMouseInput();
      }
    }
  }

  void SetupComponents()
  {
    // Find RIGHT controller for attachment - SWAPPED
    if (attachPoint == null)
    {
      GameObject rightHand = GameObject.Find("RightHandAnchor") ?? GameObject.Find("RightHand");
      attachPoint = rightHand?.transform ?? Camera.main.transform;
    }

    if (sceneGenerator == null)
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();

    if (autoFindEvidenceChecklist && evidenceChecklist == null)
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();

    if (audioSource == null)
      audioSource = gameObject.AddComponent<AudioSource>();

    // NEW: Setup tool storage area if not assigned
    if (toolStorageArea == null)
    {
      GameObject storage = new GameObject("ToolStorage");
      storage.transform.position = Camera.main.transform.position + storageOffset;
      toolStorageArea = storage.transform;
    }
  }

  // NEW: Initialize existing tools in the scene
  void InitializeExistingTools()
  {
    for (int i = 0; i < existingTools.Length; i++)
    {
      if (existingTools[i] != null)
      {
        // Store original position
        originalToolPositions[i] = existingTools[i].transform.position;

        // Initialize as inactive
        toolsActive[i] = false;

        // Move to storage and hide
        MoveToolToStorage(i);
      }
      else
      {
        // Try to find tools by name if not assigned
        GameObject foundTool = GameObject.Find(toolNames[i]);
        if (foundTool != null)
        {
          existingTools[i] = foundTool;
          originalToolPositions[i] = foundTool.transform.position;
          toolsActive[i] = false;
          MoveToolToStorage(i);
        }
      }
    }
  }

  // NEW: Move tool to storage area and hide it
  void MoveToolToStorage(int toolIndex)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length && existingTools[toolIndex] != null)
    {
      existingTools[toolIndex].transform.position = toolStorageArea.position + Vector3.right * toolIndex * 2f;
      existingTools[toolIndex].SetActive(false);
      toolsActive[toolIndex] = false;
    }
  }

  // NEW: Move tool to player and show it
  void MoveToolToPlayer(int toolIndex)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length && existingTools[toolIndex] != null)
    {
      Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
      existingTools[toolIndex].transform.position = spawnPos;
      existingTools[toolIndex].SetActive(true);
      toolsActive[toolIndex] = true;
    }
  }

  void HandleInput()
  {
    // Desktop toggle
    if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(vrToggleKey))
    {
      ToggleUI();
    }

    // VR Input
    if (useVRControls)
    {
      UpdateVRInput();
    }
  }

  void HandleMouseInput()
  {
    // Handle mouse clicks for desktop interaction
    if (Input.GetMouseButtonDown(0))
    {
      ExecuteSelection();
    }
  }

  void UpdateVRInput()
  {
    // CHANGED TO LEFT CONTROLLER
    leftControllers.Clear();
    UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
        UnityEngine.XR.InputDeviceCharacteristics.Left | UnityEngine.XR.InputDeviceCharacteristics.Controller,
        leftControllers);

    if (leftControllers.Count == 0) return;

    var controller = leftControllers[0];
    if (!controller.isValid) return;

    // Y Button - Toggle UI (Changed from B to Y)
    if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool yPressed))
    {
      if (yPressed && !wasYPressed)
      {
        ToggleUI();
        if (isUIVisible)
        {
          vrControlActive = true;
          selectedIndex = 0;
        }
        PlayAudio(0);
      }
      wasYPressed = yPressed;
    }

    // Only handle navigation if UI is visible and VR control is active
    if (!isUIVisible || !vrControlActive) return;

    // X Button - Select/Execute (Changed from A to X)
    if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xPressed))
    {
      if (xPressed && !wasXPressed)
      {
        ExecuteSelection();
        PlayAudio(0);
      }
      wasXPressed = xPressed;
    }

    // Left Joystick Navigation (Changed from right to left joystick)
    if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 joystick))
    {
      if (Vector2.Distance(joystick, Vector2.zero) > joystickDeadzone)
      {
        HandleJoystickNavigation(joystick);
      }
    }
  }

  void HandleJoystickNavigation(Vector2 input)
  {
    if (Time.time - lastInputTime < navigationCooldown) return;
    if (Vector2.Distance(input, lastJoystickInput) < joystickDeadzone) return;

    bool moved = false;

    // Horizontal movement - Tab navigation
    if (Mathf.Abs(input.x) > joystickDeadzone && Mathf.Abs(input.x) > Mathf.Abs(input.y))
    {
      if (input.x > 0)
      {
        currentTab = Mathf.Min(2, currentTab + 1);
        moved = true;
      }
      else if (input.x < 0)
      {
        currentTab = Mathf.Max(0, currentTab - 1);
        moved = true;
      }

      if (moved)
      {
        selectedIndex = 0; // Reset to first button in new tab
      }
    }
    // Vertical movement - Button navigation
    else if (Mathf.Abs(input.y) > joystickDeadzone)
    {
      int maxIndex = GetMaxIndexForCurrentTab();

      if (input.y > 0) // Up
      {
        selectedIndex = Mathf.Max(0, selectedIndex - 1);
        moved = true;
      }
      else if (input.y < 0) // Down
      {
        selectedIndex = Mathf.Min(maxIndex, selectedIndex + 1);
        moved = true;
      }
    }

    if (moved)
    {
      lastInputTime = Time.time;
      lastJoystickInput = input;
      PlayAudio(0);
    }
  }

  int GetMaxIndexForCurrentTab()
  {
    switch (currentTab)
    {
      case 0: return toolNames.Length - 1;
      case 1: return generateNames.Length - 1;
      case 2: return gameNames.Length - 1;
      default: return 0;
    }
  }

  void ExecuteSelection()
  {
    switch (currentTab)
    {
      case 0: // Tools
        ToggleTool(selectedIndex); // CHANGED: Use ToggleTool instead of SpawnTool
        break;
      case 1: // Generate
        ExecuteGenerate(selectedIndex);
        break;
      case 2: // Game
        ExecuteGameAction(selectedIndex);
        break;
    }
  }

  // CHANGED: Replace SpawnTool with ToggleTool
  void ToggleTool(int toolIndex)
  {
    if (toolIndex >= toolQuantities.Length || toolQuantities[toolIndex] <= 0) return;
    if (toolIndex >= existingTools.Length || existingTools[toolIndex] == null) return;

    // Toggle tool state
    if (toolsActive.ContainsKey(toolIndex) && toolsActive[toolIndex])
    {
      // Tool is active, move it back to storage
      MoveToolToStorage(toolIndex);
    }
    else
    {
      // Tool is not active, move it to player
      MoveToolToPlayer(toolIndex);

      // Reduce quantity for consumable tools (Brush)
      if (toolIndex == 2) toolQuantities[toolIndex]--;
    }

    PlayAudio(1);
  }

  void ExecuteGenerate(int generateIndex)
  {
    if (sceneGenerator == null) return;

    // Only one option: Generate Random Scene
    sceneGenerator.GenerateRandomLocation();
    PlayAudio(2);
  }

  void ExecuteGameAction(int actionIndex)
  {
    switch (actionIndex)
    {
      case 0: UnityEngine.SceneManagement.SceneManager.LoadScene(0); break; // Quit
      case 1: Time.timeScale = 0f; break; // Pause
      case 2: Time.timeScale = 1f; break; // Resume
    }

    PlayAudio(0);
  }

  void ToggleUI()
  {
    isUIVisible = !isUIVisible;
    uiInstance.SetActive(isUIVisible);

    // Reset VR control state when opening
    if (isUIVisible && useVRControls)
    {
      vrControlActive = true;
    }
    else
    {
      vrControlActive = false;
    }


    if (isUIVisible)
    {
      UpdateUITransform();
    }

    PlayAudio(0);
  }

  void UpdateUI()
  {
    UpdateUITransform();
    UpdateUIContent();
  }

  void UpdateUITransform()
  {
    if (uiInstance != null && attachPoint != null)
    {
      uiInstance.transform.position = attachPoint.position + attachPoint.TransformDirection(positionOffset);
      uiInstance.transform.rotation = attachPoint.rotation * Quaternion.Euler(rotationOffset);
      uiInstance.transform.localScale = Vector3.one * uiScale;
    }
  }

  void UpdateUIContent()
  {
    // Update tab colors
    Transform canvasContainer = uiInstance.transform.Find("Canvas");
    if (canvasContainer != null)
    {
      for (int i = 0; i < 3; i++)
      {
        Transform tab = canvasContainer.Find($"TabContainer/Tab{i}");
        if (tab != null)
        {
          Image tabImage = tab.GetComponent<Image>();
          if (tabImage != null)
          {
            tabImage.color = (i == currentTab) ? selectedColor : new Color(0.5f, 0.5f, 0.5f);
          }
        }
      }

      // Update action status text
      Transform actionStatusTransform = canvasContainer.Find("ActionStatus");
      if (actionStatusTransform != null)
      {
        TextMeshProUGUI statusText = actionStatusTransform.GetComponent<TextMeshProUGUI>();
        if (statusText != null)
        {
          string actionText = GetCurrentActionText();
          statusText.text = actionText;
        }
      }
    }

    // Always show all buttons and text, just update visibility and colors
    for (int i = 0; i < allButtons.Count; i++)
    {
      // Always show all buttons
      allButtons[i].SetActive(true);

      // Determine what this button should show based on current tab
      string displayText = "";
      string displayDesc = "";
      bool isCurrentTabButton = false;

      if (currentTab == 0 && i < toolNames.Length)
      {
        // Tools tab - show tool names
        displayText = toolNames[i];
        displayDesc = toolDescriptions[i];
        isCurrentTabButton = true;
      }
      else if (currentTab == 1 && i < generateNames.Length)
      {
        // Generate tab - show generate options
        displayText = generateNames[i];
        displayDesc = generateDescriptions[i];
        isCurrentTabButton = true;
      }
      else if (currentTab == 2 && i < gameNames.Length)
      {
        // Game tab - show game options
        displayText = gameNames[i];
        displayDesc = gameDescriptions[i];
        isCurrentTabButton = true;
      }

      // Update button text
      if (i < buttonTitles.Count && buttonTitles[i] != null)
      {
        buttonTitles[i].text = displayText;
        buttonTitles[i].gameObject.SetActive(isCurrentTabButton);
      }

      if (i < buttonDescriptions.Count && buttonDescriptions[i] != null)
      {
        buttonDescriptions[i].text = displayDesc;
        buttonDescriptions[i].gameObject.SetActive(isCurrentTabButton);
      }

      // Update button colors - CHANGED: Show different colors for active/inactive tools
      Image buttonImage = allButtons[i].GetComponent<Image>();
      if (buttonImage != null)
      {
        if (!isCurrentTabButton)
        {
          // Hide buttons not relevant to current tab
          buttonImage.color = Color.clear;
        }
        else if (i == selectedIndex)
        {
          buttonImage.color = selectedColor;
        }
        else if (currentTab == 0 && i < toolQuantities.Length)
        {
          // Show different colors based on tool state
          if (toolQuantities[i] <= 0)
          {
            buttonImage.color = unavailableColor;
          }
          else if (toolsActive.ContainsKey(i) && toolsActive[i])
          {
            buttonImage.color = hoverColor; // Different color for active tools
          }
          else
          {
            buttonImage.color = availableColor;
          }
        }
        else
        {
          buttonImage.color = availableColor;
        }
      }
    }
  }

  string GetCurrentActionText()
  {
    switch (currentTab)
    {
      case 0: // Tools
        if (selectedIndex < toolNames.Length)
        {
          // CHANGED: Show different text based on tool state
          if (toolsActive.ContainsKey(selectedIndex) && toolsActive[selectedIndex])
          {
            return $"Press X to store: {toolNames[selectedIndex]}";
          }
          else
          {
            return $"Press X to get: {toolNames[selectedIndex]}";
          }
        }
        return "Select a tool to toggle";

      case 1: // Generate
        if (selectedIndex < generateNames.Length)
        {
          return $"Press X to: {generateNames[selectedIndex]}";
        }
        return "Generate a random crime scene";

      case 2: // Game
        if (selectedIndex < gameNames.Length)
        {
          return $"Press X to: {gameNames[selectedIndex]}";
        }
        return "Select a game action";

      default:
        return "Navigate with Left Stick";
    }
  }

  bool ShouldShowButton(int buttonIndex)
  {
    switch (currentTab)
    {
      case 0: // Tools
        return buttonIndex < toolNames.Length;
      case 1: // Generate
        return buttonIndex >= toolNames.Length && buttonIndex < (toolNames.Length + generateNames.Length);
      case 2: // Game
        return buttonIndex >= (toolNames.Length + generateNames.Length);
      default:
        return false;
    }
  }

  int GetButtonIndexInCurrentTab(int globalButtonIndex)
  {
    switch (currentTab)
    {
      case 0: // Tools
        return globalButtonIndex;
      case 1: // Generate
        return globalButtonIndex - toolNames.Length;
      case 2: // Game
        return globalButtonIndex - (toolNames.Length + generateNames.Length);
      default:
        return 0;
    }
  }

  void CreateUI()
  {
    // Create main UI container
    uiInstance = new GameObject("ToolSpawnerUI");
    uiInstance.transform.position = new Vector3(0, -1000, 0);

    // Setup Canvas
    Canvas canvas = uiInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;

    CanvasScaler scaler = uiInstance.AddComponent<CanvasScaler>();
    scaler.dynamicPixelsPerUnit = 300;

    uiInstance.AddComponent<GraphicRaycaster>();

    RectTransform canvasRect = uiInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(500, 400);

    // Create nested Canvas object for proper UI structure
    GameObject canvasContainer = CreateUIElement("Canvas", uiInstance.transform);
    SetupRectTransform(canvasContainer, Vector2.zero, Vector2.one, Vector2.zero);

    // Create Background
    GameObject bg = CreateUIElement("Background", canvasContainer.transform);
    SetupRectTransform(bg, Vector2.zero, Vector2.one, Vector2.zero);
    bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

    // Create Title
    GameObject title = CreateUIElement("Title", canvasContainer.transform);
    SetupRectTransform(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 60));
    title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
    CreateText(title, "TOOL MANAGER", 28, FontStyles.Bold, Color.white); // CHANGED: Updated title

    // Create VR Controls hint - UPDATED
    GameObject hint = CreateUIElement("ControlsHint", canvasContainer.transform);
    SetupRectTransform(hint, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 30));
    hint.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
    CreateText(hint, "VR: Y=Menu | X=Select | Left Stick=Navigate", 12, FontStyles.Italic, new Color(0.8f, 0.8f, 0.8f));

    // Create Action Status text (shows what you're about to do)
    GameObject actionStatus = CreateUIElement("ActionStatus", canvasContainer.transform);
    SetupRectTransform(actionStatus, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 60));
    actionStatus.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
    TextMeshProUGUI statusText = CreateText(actionStatus, "Select an option above", 16, FontStyles.Bold, Color.yellow);
    statusText.alignment = TextAlignmentOptions.Center;

    // Create Tabs
    GameObject tabContainer = CreateUIElement("TabContainer", canvasContainer.transform);
    SetupRectTransform(tabContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-20, 50));
    tabContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);

    HorizontalLayoutGroup tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
    tabLayout.childForceExpandWidth = true;
    tabLayout.childForceExpandHeight = true;
    tabLayout.spacing = 5;

    string[] tabNames = { "TOOLS", "GENERATE", "GAME" };
    for (int i = 0; i < 3; i++)
    {
      GameObject tab = CreateUIElement($"Tab{i}", tabContainer.transform);
      Image tabImage = tab.AddComponent<Image>();
      tabImage.color = Color.gray;

      // Create text for tab
      GameObject tabText = CreateUIElement("Text", tab.transform);
      SetupRectTransform(tabText, Vector2.zero, Vector2.one, Vector2.zero);
      CreateText(tabText, tabNames[i], 16, FontStyles.Bold, Color.white);

      int tabIndex = i;
      Button tabButton = tab.AddComponent<Button>();
      tabButton.targetGraphic = tabImage;
      tabButton.onClick.AddListener(() => {
        currentTab = tabIndex;
        selectedIndex = 0;
        vrControlActive = false;
      });
    }

    // Create Content Area with ScrollRect
    GameObject contentArea = CreateUIElement("ContentArea", canvasContainer.transform);
    SetupRectTransform(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(-20, -200));
    contentArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);

    ScrollRect scrollRect = contentArea.AddComponent<ScrollRect>();
    scrollRect.horizontal = false;
    scrollRect.vertical = true;
    scrollRect.scrollSensitivity = 20;

    // Create viewport
    GameObject viewport = CreateUIElement("Viewport", contentArea.transform);
    SetupRectTransform(viewport, Vector2.zero, Vector2.one, Vector2.zero);
    viewport.AddComponent<Image>().color = Color.clear;
    viewport.AddComponent<Mask>().showMaskGraphic = false;

    // Create content container
    GameObject content = CreateUIElement("Content", viewport.transform);
    SetupRectTransform(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0));
    content.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

    // Setup scroll rect references
    scrollRect.viewport = viewport.GetComponent<RectTransform>();
    scrollRect.content = content.GetComponent<RectTransform>();

    // Add ContentSizeFitter and VerticalLayoutGroup to content
    ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
    contentLayout.spacing = 15;
    contentLayout.padding = new RectOffset(15, 15, 15, 15);
    contentLayout.childForceExpandWidth = true;
    contentLayout.childForceExpandHeight = false;
    contentLayout.childControlHeight = false;

    // Create buttons for all tabs
    CreateContentButtons(content);

    uiInstance.SetActive(false);
  }

  void CreateContentButtons(GameObject parent)
  {
    // Clear existing lists
    allButtons.Clear();
    buttonTitles.Clear();
    buttonDescriptions.Clear();

    // Create enough buttons for the largest category (tools = 4 buttons)
    int maxButtons = Mathf.Max(toolNames.Length, generateNames.Length, gameNames.Length);

    for (int i = 0; i < maxButtons; i++)
    {
      // Create a generic button - text will be set dynamically
      GameObject button = CreateContentButton(parent, "Button", "Description");

      int buttonIndex = i;
      button.GetComponent<Button>().onClick.AddListener(() => {
        selectedIndex = buttonIndex;
        ExecuteSelection();
      });

      allButtons.Add(button);
    }
  }

  GameObject CreateContentButton(GameObject parent, string name, string description)
  {
    GameObject button = CreateUIElement($"Button_{allButtons.Count}", parent.transform);

    // Set fixed height for consistent button sizing
    LayoutElement layoutElement = button.AddComponent<LayoutElement>();
    layoutElement.preferredHeight = 100;
    layoutElement.flexibleHeight = 0;

    Image buttonImage = button.AddComponent<Image>();
    buttonImage.color = availableColor;

    Button buttonComp = button.AddComponent<Button>();
    buttonComp.targetGraphic = buttonImage;

    ColorBlock colors = buttonComp.colors;
    colors.highlightedColor = hoverColor;
    colors.pressedColor = selectedColor;
    colors.normalColor = availableColor;
    buttonComp.colors = colors;

    // Create title text - ALWAYS VISIBLE, big and bold
    GameObject titleObj = CreateUIElement("Title", button.transform);
    RectTransform titleRect = titleObj.GetComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0, 0.5f);
    titleRect.anchorMax = new Vector2(1, 1);
    titleRect.sizeDelta = Vector2.zero;
    titleRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
    titleText.text = name;
    titleText.fontSize = 24; // Bigger font
    titleText.fontStyle = FontStyles.Bold;
    titleText.color = Color.white;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
    titleText.enableWordWrapping = false;
    titleText.overflowMode = TextOverflowModes.Overflow;

    // Force text to render immediately
    titleText.ForceMeshUpdate();

    buttonTitles.Add(titleText);

    // Create description text - ALWAYS VISIBLE, smaller
    GameObject descObj = CreateUIElement("Description", button.transform);
    RectTransform descRect = descObj.GetComponent<RectTransform>();
    descRect.anchorMin = new Vector2(0, 0);
    descRect.anchorMax = new Vector2(1, 0.5f);
    descRect.sizeDelta = Vector2.zero;
    descRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
    descText.text = description;
    descText.fontSize = 14; // Bigger font
    descText.fontStyle = FontStyles.Normal;
    descText.color = new Color(0.9f, 0.9f, 0.9f);
    descText.alignment = TextAlignmentOptions.Center;
    descText.verticalAlignment = VerticalAlignmentOptions.Middle;
    descText.enableWordWrapping = true;
    descText.overflowMode = TextOverflowModes.Ellipsis;

    // Force text to render immediately
    descText.ForceMeshUpdate();

    buttonDescriptions.Add(descText);

    return button;
  }

  GameObject CreateUIElement(string name, Transform parent)
  {
    GameObject obj = new GameObject(name);
    obj.transform.SetParent(parent, false);
    obj.AddComponent<RectTransform>();
    return obj;
  }

  void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
  {
    RectTransform rt = obj.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin;
    rt.anchorMax = anchorMax;
    rt.sizeDelta = sizeDelta;
  }

  TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize, FontStyles fontStyle, Color color)
  {
    TextMeshProUGUI textComp = parent.AddComponent<TextMeshProUGUI>();
    textComp.text = text;
    textComp.fontSize = fontSize;
    textComp.fontStyle = fontStyle;
    textComp.color = color;
    textComp.alignment = TextAlignmentOptions.Center;
    textComp.verticalAlignment = VerticalAlignmentOptions.Middle;
    textComp.enableWordWrapping = true;
    textComp.overflowMode = TextOverflowModes.Overflow;

    return textComp;
  }

  void PlayAudio(int clipIndex)
  {
    if (audioSource != null && clipIndex < audioClips.Length && audioClips[clipIndex] != null)
    {
      audioSource.PlayOneShot(audioClips[clipIndex]);
    }
  }

  void OnDestroy()
  {
    if (instance == this) instance = null;
  }

  // PUBLIC API METHODS for cross-script communication

  /// <summary>
  /// Check if this ToolSpawner is currently using VR controls
  /// </summary>
  public bool IsUsingVRControls()
  {
    return useVRControls && vrControlActive;
  }

  /// <summary>
  /// Temporarily disable VR controls (called by EvidenceChecklist)
  /// </summary>
  public void DisableVRControls()
  {
    // Both can be active simultaneously
  }

  /// <summary>
  /// Re-enable VR controls (called by EvidenceChecklist)
  /// </summary>
  public void EnableVRControls()
  {
    // Both can be active simultaneously
  }

  /// <summary>
  /// Force close the UI (called by other scripts)
  /// </summary>
  public void ForceCloseUI()
  {
    if (isUIVisible)
    {
      ToggleUI();
    }
  }

  /// <summary>
  /// Check if the UI is currently visible
  /// </summary>
  public bool IsUIVisible => isUIVisible;

  /// <summary>
  /// Check if VR controls are currently active
  /// </summary>
  public bool IsVRControlActive => vrControlActive;

  /// <summary>
  /// Get the current tool quantities for external scripts
  /// </summary>
  public int[] GetToolQuantities()
  {
    return (int[])toolQuantities.Clone();
  }

  /// <summary>
  /// Set tool quantity for a specific tool (for external scripts)
  /// </summary>
  public void SetToolQuantity(int toolIndex, int quantity)
  {
    if (toolIndex >= 0 && toolIndex < toolQuantities.Length)
    {
      toolQuantities[toolIndex] = Mathf.Max(0, quantity);
    }
  }

  // NEW: Additional public methods for managing existing tools

  /// <summary>
  /// Manually assign an existing tool to a specific index
  /// </summary>
  public void AssignExistingTool(int toolIndex, GameObject tool)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length && tool != null)
    {
      existingTools[toolIndex] = tool;
      originalToolPositions[toolIndex] = tool.transform.position;
      toolsActive[toolIndex] = tool.activeInHierarchy;

      // If tool is currently active, move it to storage
      if (tool.activeInHierarchy)
      {
        MoveToolToStorage(toolIndex);
      }
    }
  }

  /// <summary>
  /// Check if a specific tool is currently active
  /// </summary>
  public bool IsToolActive(int toolIndex)
  {
    return toolsActive.ContainsKey(toolIndex) && toolsActive[toolIndex];
  }

  /// <summary>
  /// Force store all active tools
  /// </summary>
  public void StoreAllTools()
  {
    for (int i = 0; i < existingTools.Length; i++)
    {
      if (IsToolActive(i))
      {
        MoveToolToStorage(i);
      }
    }
  }

  /// <summary>
  /// Get reference to a specific existing tool
  /// </summary>
  public GameObject GetExistingTool(int toolIndex)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length)
    {
      return existingTools[toolIndex];
    }
    return null;
  }
}