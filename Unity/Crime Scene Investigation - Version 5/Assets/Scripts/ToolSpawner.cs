using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// - TOOL SPAWNER MAIN CLASS
public class ToolSpawner : MonoBehaviour
{
  // - INSPECTOR CONFIGURATION
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

  [Header("Existing Tools in Scene")]
  [SerializeField] private GameObject[] existingTools = new GameObject[4];
  [SerializeField] private int[] toolQuantities = new int[] { 1, 1, 1, 3 };

  [Header("Tool Storage")]
  [SerializeField] private Transform toolStorageArea;
  [SerializeField] private Vector3 storageOffset = new Vector3(0, -10, 0);

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
  [SerializeField] private AudioClip[] audioClips = new AudioClip[3];

  // - STATIC DATA ARRAYS
  // Tool definitions
  private readonly string[] toolNames = { "Camera", "Brush", "UV Light", "Scanner" };
  private readonly string[] toolDescriptions = {
        "Point and scan to detect evidence automatically",
        "Advanced tool for detailed evidence analysis",
        "Apply to surfaces to reveal fingerprints",
        "Reveals hidden evidence under ultraviolet light"
    };

  // Scene generation options
  private readonly string[] generateNames = { "Generate Random Scene" };
  private readonly string[] generateDescriptions = {
        "Create a completely randomized crime scene with varied evidence"
    };

  // Game control options
  private readonly string[] gameNames = { "Quit", "Pause", "Resume" };
  private readonly string[] gameDescriptions = {
        "Return to main menu",
        "Pause the game",
        "Resume the game"
    };

  // - UI STATE MANAGEMENT
  // Main UI variables
  private GameObject uiInstance;
  private bool isUIVisible = false;
  private int currentTab = 0;
  private int selectedIndex = 0;
  private bool vrControlActive = false;

  // - VR INPUT TRACKING
  // Controller detection and input state
  private List<UnityEngine.XR.InputDevice> leftControllers = new List<UnityEngine.XR.InputDevice>();
  private bool wasYPressed = false;
  private bool wasXPressed = false;
  private float lastInputTime = 0f;
  private Vector2 lastJoystickInput = Vector2.zero;

  // - TOOL MANAGEMENT
  // Tool state tracking
  private Dictionary<int, bool> toolsActive = new Dictionary<int, bool>();
  private Dictionary<int, Vector3> originalToolPositions = new Dictionary<int, Vector3>();

  // - UI COMPONENT REFERENCES
  // Button and text component lists
  private List<GameObject> allButtons = new List<GameObject>();
  private List<TextMeshProUGUI> buttonTitles = new List<TextMeshProUGUI>();
  private List<TextMeshProUGUI> buttonDescriptions = new List<TextMeshProUGUI>();

  // - SINGLETON IMPLEMENTATION
  private static ToolSpawner instance;
  public static ToolSpawner Instance => instance;

  // - UNITY LIFECYCLE METHODS
  void Awake()
  {
    // Singleton pattern enforcement
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }
    instance = this;
  }

  void Start()
  {
    // Initialize all systems
    SetupComponents();
    InitializeExistingTools();
    CreateUI();
  }

  void Update()
  {
    // Handle input and UI updates each frame
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

  // - COMPONENT SETUP
  void SetupComponents()
  {
    // Find or assign attachment point for UI
    if (attachPoint == null)
    {
      GameObject rightHand = GameObject.Find("RightHandAnchor") ?? GameObject.Find("RightHand");
      attachPoint = rightHand?.transform ?? Camera.main.transform;
    }

    // Auto-find scene components if not assigned
    if (sceneGenerator == null)
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();

    if (autoFindEvidenceChecklist && evidenceChecklist == null)
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();

    // Setup audio source
    if (audioSource == null)
      audioSource = gameObject.AddComponent<AudioSource>();

    // Create tool storage area if needed
    if (toolStorageArea == null)
    {
      GameObject storage = new GameObject("ToolStorage");
      storage.transform.position = Camera.main.transform.position + storageOffset;
      toolStorageArea = storage.transform;
    }
  }

  // - TOOL INITIALIZATION
  void InitializeExistingTools()
  {
    // Setup each existing tool in the scene
    for (int i = 0; i < existingTools.Length; i++)
    {
      if (existingTools[i] != null)
      {
        // Store original position and move to storage
        originalToolPositions[i] = existingTools[i].transform.position;
        toolsActive[i] = false;
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

  // - TOOL POSITION MANAGEMENT
  void MoveToolToStorage(int toolIndex)
  {
    // Hide tool and move to storage area
    if (toolIndex >= 0 && toolIndex < existingTools.Length && existingTools[toolIndex] != null)
    {
      existingTools[toolIndex].transform.position = toolStorageArea.position + Vector3.right * toolIndex * 2f;
      existingTools[toolIndex].SetActive(false);
      toolsActive[toolIndex] = false;
    }
  }

  void MoveToolToPlayer(int toolIndex)
  {
    // Show tool and move to player
    if (toolIndex >= 0 && toolIndex < existingTools.Length && existingTools[toolIndex] != null)
    {
      Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
      existingTools[toolIndex].transform.position = spawnPos;
      existingTools[toolIndex].SetActive(true);
      toolsActive[toolIndex] = true;
    }
  }

  // - INPUT HANDLING
  void HandleInput()
  {
    // Check for UI toggle keys
    if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(vrToggleKey))
    {
      ToggleUI();
    }

    // Process VR input if enabled
    if (useVRControls)
    {
      UpdateVRInput();
    }
  }

  void HandleMouseInput()
  {
    // Desktop mouse interaction
    if (Input.GetMouseButtonDown(0))
    {
      ExecuteSelection();
    }
  }

  // - VR INPUT PROCESSING
  void UpdateVRInput()
  {
    // Get left controller devices
    leftControllers.Clear();
    UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
        UnityEngine.XR.InputDeviceCharacteristics.Left | UnityEngine.XR.InputDeviceCharacteristics.Controller,
        leftControllers);

    if (leftControllers.Count == 0) return;

    var controller = leftControllers[0];
    if (!controller.isValid) return;

    // Y Button for UI toggle
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

    if (!isUIVisible || !vrControlActive) return;

    // X Button for selection
    if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xPressed))
    {
      if (xPressed && !wasXPressed)
      {
        ExecuteSelection();
        PlayAudio(0);
      }
      wasXPressed = xPressed;
    }

    // Left joystick for navigation
    if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 joystick))
    {
      if (Vector2.Distance(joystick, Vector2.zero) > joystickDeadzone)
      {
        HandleJoystickNavigation(joystick);
      }
    }
  }

  // - VR NAVIGATION
  void HandleJoystickNavigation(Vector2 input)
  {
    // Check navigation cooldown
    if (Time.time - lastInputTime < navigationCooldown) return;
    if (Vector2.Distance(input, lastJoystickInput) < joystickDeadzone) return;

    bool moved = false;

    // Horizontal movement for tab switching
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
        selectedIndex = 0;
      }
    }
    
    // Vertical movement for button selection
    else if (Mathf.Abs(input.y) > joystickDeadzone)
    {
      int maxIndex = GetMaxIndexForCurrentTab();

      if (input.y > 0)
      {
        selectedIndex = Mathf.Max(0, selectedIndex - 1);
        moved = true;
      }
      else if (input.y < 0)
      {
        selectedIndex = Mathf.Min(maxIndex, selectedIndex + 1);
        moved = true;
      }
    }

    // Update navigation state
    if (moved)
    {
      lastInputTime = Time.time;
      lastJoystickInput = input;
      PlayAudio(0);
    }
  }

  // - NAVIGATION UTILITIES
  int GetMaxIndexForCurrentTab()
  {
    // Return maximum index for current tab
    switch (currentTab)
    {
      case 0: return toolNames.Length - 1;
      case 1: return generateNames.Length - 1;
      case 2: return gameNames.Length - 1;
      default: return 0;
    }
  }

  // - ACTION EXECUTION
  void ExecuteSelection()
  {
    // Execute action based on current tab
    switch (currentTab)
    {
      case 0:
        ToggleTool(selectedIndex);
        break;
      case 1:
        ExecuteGenerate(selectedIndex);
        break;
      case 2:
        ExecuteGameAction(selectedIndex);
        break;
    }
  }

  // - TOOL MANAGEMENT ACTIONS
  void ToggleTool(int toolIndex)
  {
    // Check basic availability
    if (toolIndex >= existingTools.Length || existingTools[toolIndex] == null) return;

    bool isCurrentlyActive = toolsActive.ContainsKey(toolIndex) && toolsActive[toolIndex];

    // Toggle tool between active and storage
    if (isCurrentlyActive)
    {
      // Store the tool
      MoveToolToStorage(toolIndex);
    }
    else
    {
      // Spawn the tool - no quantity restrictions or consumption
      MoveToolToPlayer(toolIndex);
    }

    PlayAudio(1);
  }

  // - SCENE GENERATION ACTIONS
  void ExecuteGenerate(int generateIndex)
  {
    // Generate random crime scene
    if (sceneGenerator == null) return;

    sceneGenerator.GenerateRandomLocation();
    PlayAudio(2);
  }

  // - GAME CONTROL ACTIONS
  void ExecuteGameAction(int actionIndex)
  {
    // Execute game control functions
    switch (actionIndex)
    {
      case 0: UnityEngine.SceneManagement.SceneManager.LoadScene(0); break;
      case 1: Time.timeScale = 0f; break;
      case 2: Time.timeScale = 1f; break;
    }

    PlayAudio(0);
  }

  // - UI VISIBILITY CONTROL
  void ToggleUI()
  {
    // Toggle UI visibility and VR control state
    isUIVisible = !isUIVisible;
    uiInstance.SetActive(isUIVisible);

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

  // - UI UPDATE SYSTEM
  void UpdateUI()
  {
    // Update UI transform and content
    UpdateUITransform();
    UpdateUIContent();
  }

  void UpdateUITransform()
  {
    // Position UI relative to attachment point
    if (uiInstance != null && attachPoint != null)
    {
      uiInstance.transform.position = attachPoint.position + attachPoint.TransformDirection(positionOffset);
      uiInstance.transform.rotation = attachPoint.rotation * Quaternion.Euler(rotationOffset);
      uiInstance.transform.localScale = Vector3.one * uiScale;
    }
  }

  // - UI CONTENT MANAGEMENT
  void UpdateUIContent()
  {
    // Update tab colors and status text
    Transform canvasContainer = uiInstance.transform.Find("Canvas");
    if (canvasContainer != null)
    {
      // Update tab appearance
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

    // Update button content and colors
    for (int i = 0; i < allButtons.Count; i++)
    {
      allButtons[i].SetActive(true);

      string displayText = "";
      string displayDesc = "";
      bool isCurrentTabButton = false;

      // Determine button content based on current tab
      if (currentTab == 0 && i < toolNames.Length)
      {
        displayText = toolNames[i];
        displayDesc = toolDescriptions[i];
        isCurrentTabButton = true;
      }
      else if (currentTab == 1 && i < generateNames.Length)
      {
        displayText = generateNames[i];
        displayDesc = generateDescriptions[i];
        isCurrentTabButton = true;
      }
      else if (currentTab == 2 && i < gameNames.Length)
      {
        displayText = gameNames[i];
        displayDesc = gameDescriptions[i];
        isCurrentTabButton = true;
      }

      // Update button text content
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

      // Update button colors based on state
      Image buttonImage = allButtons[i].GetComponent<Image>();
      if (buttonImage != null)
      {
        if (!isCurrentTabButton)
        {
          buttonImage.color = Color.clear;
        }
        else if (i == selectedIndex)
        {
          buttonImage.color = selectedColor;
        }
        else if (currentTab == 0 && i < toolQuantities.Length)
        {
          if (toolsActive.ContainsKey(i) && toolsActive[i])
          {
            buttonImage.color = hoverColor;
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

  // - ACTION TEXT GENERATION
  string GetCurrentActionText()
  {
    // Generate context-sensitive action text
    switch (currentTab)
    {
      case 0:
        if (selectedIndex < toolNames.Length)
        {
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

      case 1:
        if (selectedIndex < generateNames.Length)
        {
          return $"Press X to: {generateNames[selectedIndex]}";
        }
        return "Generate a random crime scene";

      case 2:
        if (selectedIndex < gameNames.Length)
        {
          return $"Press X to: {gameNames[selectedIndex]}";
        }
        return "Select a game action";

      default:
        return "Navigate with Left Stick";
    }
  }

  // - BUTTON VISIBILITY LOGIC
  bool ShouldShowButton(int buttonIndex)
  {
    // Determine if button should be visible for current tab
    switch (currentTab)
    {
      case 0:
        return buttonIndex < toolNames.Length;
      case 1:
        return buttonIndex >= toolNames.Length && buttonIndex < (toolNames.Length + generateNames.Length);
      case 2:
        return buttonIndex >= (toolNames.Length + generateNames.Length);
      default:
        return false;
    }
  }

  int GetButtonIndexInCurrentTab(int globalButtonIndex)
  {
    // Convert global button index to tab-specific index
    switch (currentTab)
    {
      case 0:
        return globalButtonIndex;
      case 1:
        return globalButtonIndex - toolNames.Length;
      case 2:
        return globalButtonIndex - (toolNames.Length + generateNames.Length);
      default:
        return 0;
    }
  }

  // - UI CREATION SYSTEM
  void CreateUI()
  {
    // Create main UI container
    uiInstance = new GameObject("ToolSpawnerUI");
    uiInstance.transform.position = new Vector3(0, -1000, 0);

    // Setup canvas components
    Canvas canvas = uiInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;

    CanvasScaler scaler = uiInstance.AddComponent<CanvasScaler>();
    scaler.dynamicPixelsPerUnit = 300;

    uiInstance.AddComponent<GraphicRaycaster>();

    RectTransform canvasRect = uiInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(500, 400);

    // Create canvas container
    GameObject canvasContainer = CreateUIElement("Canvas", uiInstance.transform);
    SetupRectTransform(canvasContainer, Vector2.zero, Vector2.one, Vector2.zero);

    // Create background
    GameObject bg = CreateUIElement("Background", canvasContainer.transform);
    SetupRectTransform(bg, Vector2.zero, Vector2.one, Vector2.zero);
    bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

    // Create title text
    GameObject title = CreateUIElement("Title", canvasContainer.transform);
    SetupRectTransform(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 60));
    title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
    CreateText(title, "TOOL MANAGER", 28, FontStyles.Bold, Color.white);

    // Create controls hint
    GameObject hint = CreateUIElement("ControlsHint", canvasContainer.transform);
    SetupRectTransform(hint, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 30));
    hint.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
    CreateText(hint, "VR: Y=Menu | X=Select | Left Stick=Navigate", 12, FontStyles.Italic, new Color(0.8f, 0.8f, 0.8f));

    // Create action status display
    GameObject actionStatus = CreateUIElement("ActionStatus", canvasContainer.transform);
    SetupRectTransform(actionStatus, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 60));
    actionStatus.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
    TextMeshProUGUI statusText = CreateText(actionStatus, "Select an option above", 16, FontStyles.Bold, Color.yellow);
    statusText.alignment = TextAlignmentOptions.Center;

    // Create tab container
    GameObject tabContainer = CreateUIElement("TabContainer", canvasContainer.transform);
    SetupRectTransform(tabContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-20, 50));
    tabContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);

    // Setup tab layout
    HorizontalLayoutGroup tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
    tabLayout.childForceExpandWidth = true;
    tabLayout.childForceExpandHeight = true;
    tabLayout.spacing = 5;

    // Create individual tabs
    string[] tabNames = { "TOOLS", "GENERATE", "GAME" };
    for (int i = 0; i < 3; i++)
    {
      GameObject tab = CreateUIElement($"Tab{i}", tabContainer.transform);
      Image tabImage = tab.AddComponent<Image>();
      tabImage.color = Color.gray;

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

    // Create content area with scrolling
    GameObject contentArea = CreateUIElement("ContentArea", canvasContainer.transform);
    SetupRectTransform(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(-20, -200));
    contentArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);

    // Setup scroll rect
    ScrollRect scrollRect = contentArea.AddComponent<ScrollRect>();
    scrollRect.horizontal = false;
    scrollRect.vertical = true;
    scrollRect.scrollSensitivity = 20;

    // Create viewport for scrolling
    GameObject viewport = CreateUIElement("Viewport", contentArea.transform);
    SetupRectTransform(viewport, Vector2.zero, Vector2.one, Vector2.zero);
    viewport.AddComponent<Image>().color = Color.clear;
    viewport.AddComponent<Mask>().showMaskGraphic = false;

    // Create scrollable content container
    GameObject content = CreateUIElement("Content", viewport.transform);
    SetupRectTransform(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0));
    content.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

    // Connect scroll rect references
    scrollRect.viewport = viewport.GetComponent<RectTransform>();
    scrollRect.content = content.GetComponent<RectTransform>();

    // Setup content sizing and layout
    ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
    contentLayout.spacing = 15;
    contentLayout.padding = new RectOffset(15, 15, 15, 15);
    contentLayout.childForceExpandWidth = true;
    contentLayout.childForceExpandHeight = false;
    contentLayout.childControlHeight = false;

    // Create button content
    CreateContentButtons(content);

    uiInstance.SetActive(false);
  }

  // - BUTTON CREATION
  void CreateContentButtons(GameObject parent)
  {
    // Clear existing button references
    allButtons.Clear();
    buttonTitles.Clear();
    buttonDescriptions.Clear();

    // Create buttons for all tabs
    int maxButtons = Mathf.Max(toolNames.Length, generateNames.Length, gameNames.Length);

    for (int i = 0; i < maxButtons; i++)
    {
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
    // Create button container
    GameObject button = CreateUIElement($"Button_{allButtons.Count}", parent.transform);

    // Setup button layout
    LayoutElement layoutElement = button.AddComponent<LayoutElement>();
    layoutElement.preferredHeight = 100;
    layoutElement.flexibleHeight = 0;

    // Create button image and component
    Image buttonImage = button.AddComponent<Image>();
    buttonImage.color = availableColor;

    Button buttonComp = button.AddComponent<Button>();
    buttonComp.targetGraphic = buttonImage;

    // Setup button color states
    ColorBlock colors = buttonComp.colors;
    colors.highlightedColor = hoverColor;
    colors.pressedColor = selectedColor;
    colors.normalColor = availableColor;
    buttonComp.colors = colors;

    // Create title text element
    GameObject titleObj = CreateUIElement("Title", button.transform);
    RectTransform titleRect = titleObj.GetComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0, 0.5f);
    titleRect.anchorMax = new Vector2(1, 1);
    titleRect.sizeDelta = Vector2.zero;
    titleRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
    titleText.text = name;
    titleText.fontSize = 24;
    titleText.fontStyle = FontStyles.Bold;
    titleText.color = Color.white;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
    titleText.enableWordWrapping = false;
    titleText.overflowMode = TextOverflowModes.Overflow;

    titleText.ForceMeshUpdate();
    buttonTitles.Add(titleText);

    // Create description text element
    GameObject descObj = CreateUIElement("Description", button.transform);
    RectTransform descRect = descObj.GetComponent<RectTransform>();
    descRect.anchorMin = new Vector2(0, 0);
    descRect.anchorMax = new Vector2(1, 0.5f);
    descRect.sizeDelta = Vector2.zero;
    descRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
    descText.text = description;
    descText.fontSize = 14;
    descText.fontStyle = FontStyles.Normal;
    descText.color = new Color(0.9f, 0.9f, 0.9f);
    descText.alignment = TextAlignmentOptions.Center;
    descText.verticalAlignment = VerticalAlignmentOptions.Middle;
    descText.enableWordWrapping = true;
    descText.overflowMode = TextOverflowModes.Ellipsis;

    descText.ForceMeshUpdate();
    buttonDescriptions.Add(descText);

    return button;
  }

  // - UI UTILITY FUNCTIONS
  GameObject CreateUIElement(string name, Transform parent)
  {
    // Create basic UI game object with RectTransform
    GameObject obj = new GameObject(name);
    obj.transform.SetParent(parent, false);
    obj.AddComponent<RectTransform>();
    return obj;
  }

  void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
  {
    // Configure RectTransform anchors and size
    RectTransform rt = obj.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin;
    rt.anchorMax = anchorMax;
    rt.sizeDelta = sizeDelta;
  }

  TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize, FontStyles fontStyle, Color color)
  {
    // Create and configure TextMeshPro component
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

  // - AUDIO SYSTEM
  void PlayAudio(int clipIndex)
  {
    // Play audio clip if available
    if (audioSource != null && clipIndex < audioClips.Length && audioClips[clipIndex] != null)
    {
      audioSource.PlayOneShot(audioClips[clipIndex]);
    }
  }

  // - CLEANUP
  void OnDestroy()
  {
    // Clear singleton reference
    if (instance == this) instance = null;
  }

  // - PUBLIC API METHODS
  // VR control state checking
  public bool IsUsingVRControls()
  {
    return useVRControls && vrControlActive;
  }

  // VR control management for external scripts
  public void DisableVRControls()
  {
    // Both can be active simultaneously
  }

  public void EnableVRControls()
  {
    // Both can be active simultaneously
  }

  // UI visibility control
  public void ForceCloseUI()
  {
    if (isUIVisible)
    {
      ToggleUI();
    }
  }

  // UI state properties
  public bool IsUIVisible => isUIVisible;
  public bool IsVRControlActive => vrControlActive;

  // Tool quantity management
  public int[] GetToolQuantities()
  {
    return (int[])toolQuantities.Clone();
  }

  public void SetToolQuantity(int toolIndex, int quantity)
  {
    if (toolIndex >= 0 && toolIndex < toolQuantities.Length)
    {
      toolQuantities[toolIndex] = Mathf.Max(0, quantity);
    }
  }

  // - TOOL MANAGEMENT API
  // External tool assignment
  public void AssignExistingTool(int toolIndex, GameObject tool)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length && tool != null)
    {
      existingTools[toolIndex] = tool;
      originalToolPositions[toolIndex] = tool.transform.position;
      toolsActive[toolIndex] = tool.activeInHierarchy;

      if (tool.activeInHierarchy)
      {
        MoveToolToStorage(toolIndex);
      }
    }
  }

  // Tool state checking
  public bool IsToolActive(int toolIndex)
  {
    return toolsActive.ContainsKey(toolIndex) && toolsActive[toolIndex];
  }

  // Bulk tool management
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

  // Tool reference access
  public GameObject GetExistingTool(int toolIndex)
  {
    if (toolIndex >= 0 && toolIndex < existingTools.Length)
    {
      return existingTools[toolIndex];
    }
    return null;
  }
}