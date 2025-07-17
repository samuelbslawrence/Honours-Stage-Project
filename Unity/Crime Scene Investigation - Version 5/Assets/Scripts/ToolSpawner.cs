using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolSpawner : MonoBehaviour
{
  [Header("Spawner Settings")]
  [SerializeField] private Transform attachPoint;
  [SerializeField] private Vector3 positionOffset = new Vector3(-0.1f, 0.3f, 0.1f);
  [SerializeField] private Vector3 rotationOffset = new Vector3(15f, 30f, 0f);
  [SerializeField] private float uiScale = 0.0004f;
  [SerializeField] private KeyCode toggleKey = KeyCode.I;

  [Header("VR Controls")]
  [SerializeField] private bool useVRControls = true;
  [SerializeField] private float joystickDeadzone = 0.3f;
  [SerializeField] private float navigationCooldown = 0.3f;
  [SerializeField] private KeyCode vrToggleKey = KeyCode.RightBracket;

  [Header("Cross-Script Communication")]
  [SerializeField] private EvidenceChecklist evidenceChecklist;
  [SerializeField] private bool autoFindEvidenceChecklist = true;
  [SerializeField] private bool evidenceChecklistHasPriority = true;

  [Header("VR Controller Settings")]
  [SerializeField]
  private UnityEngine.XR.InputDeviceCharacteristics rightControllerCharacteristics =
    UnityEngine.XR.InputDeviceCharacteristics.Right | UnityEngine.XR.InputDeviceCharacteristics.Controller;

  [Header("Spawn Settings")]
  [SerializeField] private Transform spawnPoint;
  [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.1f, 0.5f);
  [SerializeField] private float spawnForce = 2f;
  [SerializeField] private bool spawnWithPhysics = true;
  [SerializeField] private float despawnDelay = 30f;

  [Header("Tool Prefabs")]
  [SerializeField] private GameObject cameraPrefab;
  [SerializeField] private GameObject scannerPrefab;
  [SerializeField] private GameObject uvLightPrefab;
  [SerializeField] private GameObject dustBrushPrefab;

  [Header("Tool Quantities")]
  [SerializeField] private int cameraQuantity = 1;
  [SerializeField] private int scannerQuantity = 1;
  [SerializeField] private int uvLightQuantity = 1;
  [SerializeField] private int dustBrushQuantity = 3;

  [Header("Scene Generation")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;
  [SerializeField] private bool enableSceneGeneration = true;

  [Header("UI Colors")]
  [SerializeField] private Color availableColor = new Color(0.3f, 0.8f, 0.3f);
  [SerializeField] private Color unavailableColor = new Color(0.8f, 0.3f, 0.3f);
  [SerializeField] private Color generateColor = new Color(1f, 0.6f, 0.2f);
  [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f);
  [SerializeField] private Color pauseColor = new Color(0.9f, 0.5f, 0.1f);

  [Header("Audio")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip spawnSound;
  [SerializeField] private AudioClip uiSound;
  [SerializeField] private AudioClip generateSound;
  [SerializeField] private AudioClip navigationSound;
  [SerializeField] private AudioClip selectSound;

  [Header("Effects")]
  [SerializeField] private GameObject spawnEffect;
  [SerializeField] private bool enableSpawnEffects = true;

  [Header("Tool Management")]
  [SerializeField] private bool singleInstancePerTool = true;
  [SerializeField] private bool enableHoverHighlight = true;
  [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.2f);
  [SerializeField] private float hoverTransitionSpeed = 5f;
  [SerializeField] private bool enableVRCleanup = true;
  [SerializeField] private float vrCleanupDelay = 0.2f;

  // Private variables
  private GameObject uiInstance;
  private bool isUIVisible = false;
  private List<Button> toolButtons = new List<Button>();
  private List<Button> allButtons = new List<Button>();
  private List<GameObject> spawnedObjects = new List<GameObject>();
  private ScrollRect scrollView;
  private GameObject toolsPanel;
  private GameObject generatePanel;
  private GameObject pausePanel;
  private Button toolsTabButton;
  private Button generateTabButton;
  private Button pauseTabButton;
  private bool isToolsTabActive = true;
  private bool isGenerateTabActive = false;
  private bool isPauseTabActive = false;
  private readonly Vector3 hiddenPosition = new Vector3(0, -1000, 0);
  private CameraScript cameraScript;

  // VR Navigation variables
  private bool wasAButtonPressed = false;
  private bool wasBButtonPressed = false;
  private int selectedButtonIndex = 0;
  private float lastNavigationTime = 0f;
  private bool vrUIControlActive = false;
  private List<UnityEngine.XR.InputDevice> rightControllers = new List<UnityEngine.XR.InputDevice>();
  private Vector2 lastJoystickInput = Vector2.zero;

  // Game state management
  private bool isGamePaused = false;
  private float originalTimeScale = 1f;
  private bool isCleaningUp = false;

  // Tool data with improved descriptions
  private readonly string[] toolNames = new string[]
  {
    "Investigation Camera",
    "Evidence Scanner",
    "UV Light",
    "Dust Brush"
  };

  private readonly string[] toolDescriptions = new string[]
  {
    "Point and scan to detect evidence automatically",
    "Advanced tool for detailed evidence analysis",
    "Reveals hidden evidence under ultraviolet light",
    "Apply to surfaces to reveal fingerprints"
  };

  private readonly string[] toolInstructions = new string[]
  {
    "Hold and aim at potential evidence",
    "Use on discovered evidence for details",
    "Shine on surfaces to find hidden clues",
    "Brush surfaces gently to dust for prints"
  };

  private int[] toolQuantities = new int[4];
  private readonly int[] maxQuantities = new int[] { 1, 1, 2, 5 };
  private GameObject[] toolPrefabs = new GameObject[4];

  // Tool instance tracking for single-instance mode
  private Dictionary<int, GameObject> spawnedToolInstances = new Dictionary<int, GameObject>();
  private int hoveredButtonIndex = -1;
  private bool isMouseOverUI = false;

  // Singleton pattern to prevent conflicts
  private static ToolSpawner instance;
  public static ToolSpawner Instance => instance;

  // Public properties for cross-script communication
  public bool IsUIVisible => isUIVisible;
  public bool IsVRControlActive => vrUIControlActive;

  void Awake()
  {
    // Implement singleton pattern like EvidenceChecklist
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }
    instance = this;
  }

  void Start()
  {
    // Safety check for singleton
    if (instance != this)
    {
      return;
    }

    FindComponents();
    SetupToolArrays();
    CreateUI();
    SetupAudio();
    originalTimeScale = Time.timeScale;
  }

  void Update()
  {
    if (!Application.isPlaying || isCleaningUp || instance != this) return;

    // Check if Evidence Checklist is active and should block our controls
    if (IsEvidenceChecklistBlocking())
    {
      if (vrUIControlActive)
      {
        // Disable our VR controls if evidence checklist takes priority
        vrUIControlActive = false;
        ClearAllButtonHighlights();
      }
      return; // Don't process our VR input while evidence checklist is active
    }

    // Desktop controls
    if (Input.GetKeyDown(toggleKey))
    {
      ToggleUI();
    }

    // Alternative VR toggle for testing
    if (Input.GetKeyDown(vrToggleKey))
    {
      ToggleUI();
    }

    // VR Controls - with better error handling and conflict avoidance
    if (useVRControls)
    {
      try
      {
        UpdateVRControllers();
        HandleVRInput();
      }
      catch (System.Exception e)
      {
        SafeDisableVRControls();
      }
    }

    if (uiInstance != null && isUIVisible)
    {
      UpdateUITransform();
      HandleMouseHover();
    }

    // Clean up destroyed spawned objects and tool instances
    try
    {
      spawnedObjects?.RemoveAll(obj => obj == null);

      // Clean up tool instance tracking
      var keysToRemove = new List<int>();
      foreach (var kvp in spawnedToolInstances)
      {
        if (kvp.Value == null)
        {
          keysToRemove.Add(kvp.Key);
        }
      }
      foreach (int key in keysToRemove)
      {
        spawnedToolInstances.Remove(key);
      }
    }
    catch (System.Exception e)
    {
      // Silent cleanup
    }
  }

  // Check if Evidence Checklist should block our controls
  private bool IsEvidenceChecklistBlocking()
  {
    if (evidenceChecklist == null) return false;

    // Check if evidence checklist is visible using public method
    try
    {
      bool isVisible = evidenceChecklist.IsChecklistVisible();
      return isVisible && evidenceChecklistHasPriority;
    }
    catch (System.Exception e)
    {
      // Fallback: Use reflection as backup
      try
      {
        var checklistType = evidenceChecklist.GetType();
        var isVisibleField = checklistType.GetField("isChecklistVisible",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (isVisibleField != null)
        {
          bool isChecklistVisible = (bool)isVisibleField.GetValue(evidenceChecklist);
          return isChecklistVisible && evidenceChecklistHasPriority;
        }
      }
      catch (System.Exception reflectionException)
      {
        // Silent fallback
      }
    }

    return false;
  }

  void UpdateVRControllers()
  {
    if (!useVRControls || !Application.isPlaying || isCleaningUp) return;

    try
    {
      rightControllers.Clear();
      UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, rightControllers);
    }
    catch (System.Exception e)
    {
      SafeDisableVRControls();
    }
  }

  void HandleVRInput()
  {
    if (!useVRControls || !Application.isPlaying || rightControllers.Count == 0 || isCleaningUp) return;
    if (IsEvidenceChecklistBlocking()) return; // Don't handle input if evidence checklist is active

    try
    {
      var rightController = rightControllers[0];
      if (!rightController.isValid) return;

      // A Button - Toggle UI (different from evidence checklist which uses Y)
      if (rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aPressed))
      {
        if (aPressed && !wasAButtonPressed)
        {
          if (!vrUIControlActive)
          {
            ToggleUI();
            if (isUIVisible)
            {
              vrUIControlActive = true;
              selectedButtonIndex = 0;
              HighlightSelectedButton();
            }
          }
          else
          {
            vrUIControlActive = false;
            ClearAllButtonHighlights();
          }
        }
        wasAButtonPressed = aPressed;
      }

      // B Button - Select (different from evidence checklist which uses X)
      if (vrUIControlActive && rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bPressed))
      {
        if (bPressed && !wasBButtonPressed)
        {
          SelectCurrentButton();
        }
        wasBButtonPressed = bPressed;
      }

      // Right Joystick Navigation (evidence checklist uses left joystick)
      if (vrUIControlActive && rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 joystickInput))
      {
        HandleJoystickNavigation(joystickInput);
      }
    }
    catch (System.Exception e)
    {
      SafeDisableVRControls();
    }
  }

  void HandleJoystickNavigation(Vector2 joystickInput)
  {
    if (Time.time - lastNavigationTime < navigationCooldown) return;
    if (allButtons.Count == 0) return;

    // Improved joystick handling with better deadzone logic
    float horizontal = joystickInput.x;
    float vertical = joystickInput.y;

    bool moved = false;

    // Vertical navigation (up/down through buttons)
    if (Mathf.Abs(vertical) > joystickDeadzone && Mathf.Abs(vertical) > Mathf.Abs(horizontal))
    {
      if (vertical > 0) // Up
      {
        NavigateUp();
        moved = true;
      }
      else if (vertical < 0) // Down
      {
        NavigateDown();
        moved = true;
      }
    }
    // Horizontal navigation (left/right through tabs)
    else if (Mathf.Abs(horizontal) > joystickDeadzone && Mathf.Abs(horizontal) > Mathf.Abs(vertical))
    {
      if (horizontal > 0) // Right
      {
        NavigateTabRight();
        moved = true;
      }
      else if (horizontal < 0) // Left
      {
        NavigateTabLeft();
        moved = true;
      }
    }

    if (moved)
    {
      lastNavigationTime = Time.time;
      PlayNavigationSound();
    }

    lastJoystickInput = joystickInput;
  }

  void NavigateUp()
  {
    if (allButtons.Count == 0) return;

    int oldIndex = selectedButtonIndex;
    selectedButtonIndex = Mathf.Max(0, selectedButtonIndex - 1);

    if (selectedButtonIndex != oldIndex)
    {
      HighlightSelectedButton();
    }
  }

  void NavigateDown()
  {
    if (allButtons.Count == 0) return;

    int oldIndex = selectedButtonIndex;
    selectedButtonIndex = Mathf.Min(allButtons.Count - 1, selectedButtonIndex + 1);

    if (selectedButtonIndex != oldIndex)
    {
      HighlightSelectedButton();
    }
  }

  void NavigateTabRight()
  {
    if (isToolsTabActive && enableSceneGeneration)
    {
      ShowGenerateTab();
      selectedButtonIndex = 0;
      HighlightSelectedButton();
    }
    else if (isGenerateTabActive)
    {
      ShowPauseTab();
      selectedButtonIndex = 0;
      HighlightSelectedButton();
    }
  }

  void NavigateTabLeft()
  {
    if (isPauseTabActive)
    {
      ShowGenerateTab();
      selectedButtonIndex = 0;
      HighlightSelectedButton();
    }
    else if (isGenerateTabActive)
    {
      ShowToolsTab();
      selectedButtonIndex = 0;
      HighlightSelectedButton();
    }
  }

  void SelectCurrentButton()
  {
    if (selectedButtonIndex >= 0 && selectedButtonIndex < allButtons.Count)
    {
      Button selectedButton = allButtons[selectedButtonIndex];
      if (selectedButton != null && selectedButton.interactable)
      {
        selectedButton.onClick.Invoke();
        PlaySelectSound();
      }
    }
  }

  void HighlightSelectedButton()
  {
    ClearAllButtonHighlights();

    if (selectedButtonIndex >= 0 && selectedButtonIndex < allButtons.Count)
    {
      Button selectedButton = allButtons[selectedButtonIndex];
      if (selectedButton != null)
      {
        Image buttonImage = selectedButton.GetComponent<Image>();
        if (buttonImage != null)
        {
          buttonImage.color = selectedColor;
        }
      }
    }
  }

  void ClearAllButtonHighlights()
  {
    foreach (Button button in allButtons)
    {
      if (button != null)
      {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
          RestoreButtonColor(button);
        }
      }
    }
  }

  // Handle mouse hover highlighting
  void HandleMouseHover()
  {
    if (!enableHoverHighlight || !isToolsTabActive) return;

    // Check if mouse is over the UI
    Vector3 mousePosition = Input.mousePosition;
    Camera uiCamera = Camera.main; // Assuming world space canvas

    bool foundHover = false;
    int newHoveredIndex = -1;

    // Check each tool button for hover
    for (int i = 0; i < toolButtons.Count; i++)
    {
      if (toolButtons[i] != null)
      {
        RectTransform buttonRect = toolButtons[i].GetComponent<RectTransform>();
        if (buttonRect != null && IsMouseOverButton(buttonRect, mousePosition))
        {
          newHoveredIndex = i;
          foundHover = true;
          break;
        }
      }
    }

    // Update hover state
    if (newHoveredIndex != hoveredButtonIndex)
    {
      // Clear old hover
      if (hoveredButtonIndex >= 0 && hoveredButtonIndex < toolButtons.Count)
      {
        ClearButtonHover(hoveredButtonIndex);
      }

      // Set new hover
      hoveredButtonIndex = newHoveredIndex;
      if (hoveredButtonIndex >= 0)
      {
        SetButtonHover(hoveredButtonIndex);
      }
    }

    isMouseOverUI = foundHover;
  }

  bool IsMouseOverButton(RectTransform buttonRect, Vector3 mousePosition)
  {
    // Convert world space button to screen space
    Vector3 buttonScreenPos = Camera.main.WorldToScreenPoint(buttonRect.position);
    Vector2 buttonSize = buttonRect.sizeDelta * uiScale * Camera.main.pixelHeight;

    // Check if mouse is within button bounds
    Rect buttonScreenRect = new Rect(
      buttonScreenPos.x - buttonSize.x / 2,
      Screen.height - buttonScreenPos.y - buttonSize.y / 2, // Flip Y coordinate
      buttonSize.x,
      buttonSize.y
    );

    return buttonScreenRect.Contains(mousePosition);
  }

  void SetButtonHover(int buttonIndex)
  {
    if (buttonIndex < 0 || buttonIndex >= toolButtons.Count) return;

    Button button = toolButtons[buttonIndex];
    if (button != null && !vrUIControlActive) // Don't override VR selection
    {
      Image buttonImage = button.GetComponent<Image>();
      if (buttonImage != null)
      {
        buttonImage.color = hoverColor;
      }
    }
  }

  void ClearButtonHover(int buttonIndex)
  {
    if (buttonIndex < 0 || buttonIndex >= toolButtons.Count) return;

    Button button = toolButtons[buttonIndex];
    if (button != null && !vrUIControlActive) // Don't override VR selection
    {
      RestoreButtonColor(button);
    }
  }

  void RestoreButtonColor(Button button)
  {
    Image buttonImage = button.GetComponent<Image>();
    if (buttonImage == null) return;

    // Check if it's a tool button
    for (int i = 0; i < toolButtons.Count; i++)
    {
      if (toolButtons[i] == button)
      {
        buttonImage.color = toolQuantities[i] > 0 ? availableColor : unavailableColor;
        return;
      }
    }

    // Check panel type
    if (generatePanel != null && button.transform.IsChildOf(generatePanel.transform))
    {
      buttonImage.color = generateColor;
    }
    else if (pausePanel != null && button.transform.IsChildOf(pausePanel.transform))
    {
      buttonImage.color = pauseColor;
    }
    else
    {
      buttonImage.color = availableColor;
    }
  }

  void FindComponents()
  {
    if (cameraScript == null)
      cameraScript = FindObjectOfType<CameraScript>();

    // Auto-find Evidence Checklist for cross-script communication
    if (autoFindEvidenceChecklist && evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
    }

    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator == null)
      {
        enableSceneGeneration = false;
      }
    }

    if (attachPoint == null)
    {
      // Use right hand for tool spawner (evidence checklist uses left hand)
      GameObject rightHand = GameObject.Find("RightHandAnchor") ?? GameObject.Find("RightHand");
      attachPoint = rightHand?.transform ?? Camera.main.transform;
    }

    if (spawnPoint == null)
      spawnPoint = Camera.main.transform;
  }

  void SetupToolArrays()
  {
    toolQuantities[0] = cameraQuantity;
    toolQuantities[1] = scannerQuantity;
    toolQuantities[2] = uvLightQuantity;
    toolQuantities[3] = dustBrushQuantity;

    toolPrefabs[0] = cameraPrefab;
    toolPrefabs[1] = scannerPrefab;
    toolPrefabs[2] = uvLightPrefab;
    toolPrefabs[3] = dustBrushPrefab;
  }

  void CreateUI()
  {
    uiInstance = new GameObject("ToolSpawnerUI");
    uiInstance.transform.position = hiddenPosition;

    Canvas canvas = uiInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;

    CanvasScaler scaler = uiInstance.AddComponent<CanvasScaler>();
    scaler.dynamicPixelsPerUnit = 300;

    uiInstance.AddComponent<GraphicRaycaster>();

    RectTransform canvasRect = uiInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(500, 650);

    CreateBackground();
    CreateTitle();
    CreateControlsHint();
    CreateTabs();
    CreateScrollView();
    CreateToolsPanel();
    CreateGeneratePanel();
    CreatePausePanel();
    ShowToolsTab();

    uiInstance.SetActive(false);
    isUIVisible = false;
  }

  void CreateBackground()
  {
    GameObject bg = new GameObject("Background");
    bg.transform.SetParent(uiInstance.transform, false);

    RectTransform bgRect = bg.AddComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.sizeDelta = Vector2.zero;

    Image bgImage = bg.AddComponent<Image>();
    bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
  }

  void CreateTitle()
  {
    GameObject title = new GameObject("Title");
    title.transform.SetParent(uiInstance.transform, false);

    RectTransform titleRect = title.AddComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0, 1);
    titleRect.anchorMax = new Vector2(1, 1);
    titleRect.sizeDelta = new Vector2(0, 50);
    titleRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
    titleText.text = "TOOL SPAWNER";
    titleText.color = Color.white;
    titleText.fontSize = 28;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.fontStyle = FontStyles.Bold;
  }

  // Add VR controls hint
  void CreateControlsHint()
  {
    GameObject hint = new GameObject("ControlsHint");
    hint.transform.SetParent(uiInstance.transform, false);

    RectTransform hintRect = hint.AddComponent<RectTransform>();
    hintRect.anchorMin = new Vector2(0, 1);
    hintRect.anchorMax = new Vector2(1, 1);
    hintRect.sizeDelta = new Vector2(0, 35);
    hintRect.anchoredPosition = new Vector2(0, -55);

    TextMeshProUGUI hintText = hint.AddComponent<TextMeshProUGUI>();
    hintText.text = "VR: Right Stick=Navigate | A=Toggle | B=Select";
    hintText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
    hintText.fontSize = 14;
    hintText.alignment = TextAlignmentOptions.Center;
    hintText.fontStyle = FontStyles.Italic;
  }

  void CreateTabs()
  {
    CreateTab("ToolsTab", "TOOLS", 0, 0.33f, ShowToolsTab);
    CreateTab("GenerateTab", "GENERATE", 0.33f, 0.66f, ShowGenerateTab);
    CreateTab("PauseTab", "GAME", 0.66f, 1f, ShowPauseTab);
  }

  void CreateTab(string name, string text, float minX, float maxX, System.Action onClick)
  {
    GameObject tab = new GameObject(name);
    tab.transform.SetParent(uiInstance.transform, false);

    RectTransform tabRect = tab.AddComponent<RectTransform>();
    tabRect.anchorMin = new Vector2(minX, 1);
    tabRect.anchorMax = new Vector2(maxX, 1);
    tabRect.sizeDelta = new Vector2(-2, 40);
    tabRect.anchoredPosition = new Vector2(0, -95);

    Button tabButton = tab.AddComponent<Button>();
    Image tabImage = tab.AddComponent<Image>();
    tabImage.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    GameObject tabText = new GameObject("Text");
    tabText.transform.SetParent(tab.transform, false);

    TextMeshProUGUI textComponent = tabText.AddComponent<TextMeshProUGUI>();
    textComponent.text = text;
    textComponent.color = Color.white;
    textComponent.fontSize = name == "ToolsTab" ? 18 : 16;
    textComponent.alignment = TextAlignmentOptions.Center;
    textComponent.fontStyle = FontStyles.Bold;

    RectTransform textRect = tabText.GetComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = Vector2.zero;

    tabButton.onClick.AddListener(() => onClick?.Invoke());

    // Assign button references
    if (name == "ToolsTab") toolsTabButton = tabButton;
    else if (name == "GenerateTab") generateTabButton = tabButton;
    else if (name == "PauseTab") pauseTabButton = tabButton;
  }

  void CreateScrollView()
  {
    GameObject scrollObj = new GameObject("ScrollView");
    scrollObj.transform.SetParent(uiInstance.transform, false);

    RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
    scrollRect.anchorMin = new Vector2(0, 0);
    scrollRect.anchorMax = new Vector2(1, 1);
    scrollRect.sizeDelta = new Vector2(-20, -155);
    scrollRect.anchoredPosition = new Vector2(0, -10);

    scrollView = scrollObj.AddComponent<ScrollRect>();
    scrollView.horizontal = false;
    scrollView.vertical = true;

    Image scrollImage = scrollObj.AddComponent<Image>();
    scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

    GameObject viewport = new GameObject("Viewport");
    viewport.transform.SetParent(scrollObj.transform, false);

    RectTransform viewportRect = viewport.AddComponent<RectTransform>();
    viewportRect.anchorMin = Vector2.zero;
    viewportRect.anchorMax = Vector2.one;
    viewportRect.sizeDelta = Vector2.zero;

    Mask mask = viewport.AddComponent<Mask>();
    mask.showMaskGraphic = false;

    Image viewportImage = viewport.AddComponent<Image>();
    viewportImage.color = Color.clear;

    scrollView.viewport = viewportRect;
  }

  void CreateToolsPanel()
  {
    toolsPanel = CreatePanel("ToolsContent");
    CreateToolButtons();
  }

  void CreateGeneratePanel()
  {
    generatePanel = CreatePanel("GenerateContent");
    CreateGenerateButtons();
    generatePanel.SetActive(false);
  }

  void CreatePausePanel()
  {
    pausePanel = CreatePanel("PauseContent");
    CreatePauseButtons();
    pausePanel.SetActive(false);
  }

  GameObject CreatePanel(string name)
  {
    GameObject panel = new GameObject(name);
    panel.transform.SetParent(scrollView.viewport, false);

    RectTransform panelRect = panel.AddComponent<RectTransform>();
    panelRect.anchorMin = new Vector2(0, 1);
    panelRect.anchorMax = new Vector2(1, 1);
    panelRect.pivot = new Vector2(0.5f, 1);

    VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
    layout.spacing = 10f;
    layout.padding = new RectOffset(10, 10, 10, 10);
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;
    layout.childControlHeight = true;

    ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    return panel;
  }

  void CreateToolButtons()
  {
    toolButtons.Clear();

    for (int i = 0; i < toolNames.Length; i++)
    {
      GameObject buttonObj = CreateToolButton(i);
      toolButtons.Add(buttonObj.GetComponent<Button>());
    }
  }

  // Better tool button with clearer descriptions
  GameObject CreateToolButton(int index)
  {
    GameObject buttonObj = new GameObject("ToolButton_" + index);
    buttonObj.transform.SetParent(toolsPanel.transform, false);

    RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
    buttonRect.sizeDelta = new Vector2(400f, 100f);

    Button button = buttonObj.AddComponent<Button>();
    Image buttonImage = buttonObj.AddComponent<Image>();
    buttonImage.color = toolQuantities[index] > 0 ? availableColor : unavailableColor;

    // Create improved button content with better layout
    CreateImprovedButtonContent(buttonObj, index);

    button.onClick.AddListener(() => SpawnTool(index));

    return buttonObj;
  }

  // Improved button content with clearer descriptions
  void CreateImprovedButtonContent(GameObject buttonObj, int index)
  {
    GameObject content = new GameObject("Content");
    content.transform.SetParent(buttonObj.transform, false);

    RectTransform contentRect = content.AddComponent<RectTransform>();
    contentRect.anchorMin = Vector2.zero;
    contentRect.anchorMax = Vector2.one;
    contentRect.sizeDelta = Vector2.zero;

    VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
    contentLayout.padding = new RectOffset(15, 15, 10, 10);
    contentLayout.spacing = 5;
    contentLayout.childForceExpandWidth = true;

    // Tool name with quantity
    GameObject nameRow = new GameObject("NameRow");
    nameRow.transform.SetParent(content.transform, false);

    HorizontalLayoutGroup nameLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
    nameLayout.childForceExpandWidth = false;
    nameLayout.childControlWidth = true;

    CreateTextElement(nameRow, "ToolName", toolNames[index], Color.white, 20, FontStyles.Bold);

    // Quantity indicator
    string qtyText = $"({toolQuantities[index]}/{maxQuantities[index]})";
    Color qtyColor = toolQuantities[index] > 0 ? Color.green : Color.red;
    CreateTextElement(nameRow, "Quantity", qtyText, qtyColor, 16, FontStyles.Bold);

    // Description
    CreateTextElement(content, "Description", toolDescriptions[index],
                     new Color(0.9f, 0.9f, 0.9f), 16, FontStyles.Normal);

    // Usage instructions
    CreateTextElement(content, "Instructions", $"• {toolInstructions[index]}",
                     new Color(0.7f, 0.9f, 1f), 14, FontStyles.Italic);

    // Availability status
    string statusText = toolQuantities[index] > 0 ? "✓ AVAILABLE" : "✗ UNAVAILABLE";
    Color statusColor = toolQuantities[index] > 0 ? Color.green : Color.red;
    CreateTextElement(content, "Status", statusText, statusColor, 14, FontStyles.Bold);
  }

  void CreateTextElement(GameObject parent, string name, string text, Color color, int fontSize, FontStyles fontStyle)
  {
    GameObject textObj = new GameObject(name);
    textObj.transform.SetParent(parent.transform, false);

    TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
    textComponent.text = text;
    textComponent.color = color;
    textComponent.fontSize = fontSize;
    textComponent.fontStyle = fontStyle;
    textComponent.enableWordWrapping = true;
  }

  void CreateGenerateButtons()
  {
    if (!enableSceneGeneration || sceneGenerator == null) return;

    CreateGenerateButton("Generate Random Scene", "Create a new random crime scene with evidence", () => GenerateRandomScene());
    CreateGenerateButton("Generate Bar Scene", "Create a bar crime scene with bottles and glasses", () => GenerateBarScene());
    CreateGenerateButton("Generate Office Scene", "Create an office crime scene with papers and items", () => GenerateOfficeScene());
    CreateGenerateButton("Generate Home Scene", "Create a home crime scene with household items", () => GenerateHomeScene());
    CreateGenerateButton("Clear Scene", "Remove all evidence from the current scene", () => ClearScene());
  }

  void CreateGenerateButton(string buttonName, string description, System.Action onClick)
  {
    GameObject buttonObj = new GameObject("GenerateButton_" + buttonName);
    buttonObj.transform.SetParent(generatePanel.transform, false);

    RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
    buttonRect.sizeDelta = new Vector2(400f, 80f);

    Button button = buttonObj.AddComponent<Button>();
    Image buttonImage = buttonObj.AddComponent<Image>();
    buttonImage.color = generateColor;

    CreateButtonContent(buttonObj, buttonName, description, null, Color.white);

    button.onClick.AddListener(() => {
      PlayGenerateSound();
      onClick?.Invoke();
    });
  }

  void CreatePauseButtons()
  {
    CreatePauseButton("Pause Game", "Pause/freeze the game temporarily", () => PauseGame());
    CreatePauseButton("Resume Game", "Resume the game from pause", () => ResumeGame());
    CreatePauseButton("Regenerate Scene", "Generate a new random crime scene", () => RegenerateScene());
    CreatePauseButton("Quit to Menu", "Return to the main menu", () => QuitToMenu());
  }

  void CreatePauseButton(string buttonName, string description, System.Action onClick)
  {
    GameObject buttonObj = new GameObject("PauseButton_" + buttonName);
    buttonObj.transform.SetParent(pausePanel.transform, false);

    RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
    buttonRect.sizeDelta = new Vector2(400f, 80f);

    Button button = buttonObj.AddComponent<Button>();
    Image buttonImage = buttonObj.AddComponent<Image>();
    buttonImage.color = pauseColor;

    CreateButtonContent(buttonObj, buttonName, description, null, Color.white);

    button.onClick.AddListener(() => {
      PlayUISound();
      onClick?.Invoke();
    });
  }

  void CreateButtonContent(GameObject buttonObj, string name, string description, string quantity, Color quantityColor)
  {
    GameObject content = new GameObject("Content");
    content.transform.SetParent(buttonObj.transform, false);

    RectTransform contentRect = content.AddComponent<RectTransform>();
    contentRect.anchorMin = Vector2.zero;
    contentRect.anchorMax = Vector2.one;
    contentRect.sizeDelta = Vector2.zero;

    VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
    contentLayout.padding = new RectOffset(15, 15, 10, 10);
    contentLayout.spacing = 5;

    // Name
    CreateTextElement(content, "Name", name, Color.white, 18, FontStyles.Bold);

    // Description
    CreateTextElement(content, "Description", description, new Color(0.8f, 0.8f, 0.8f), 14, FontStyles.Normal);

    // Quantity (if provided)
    if (!string.IsNullOrEmpty(quantity))
    {
      CreateTextElement(content, "Quantity", quantity, quantityColor, 12, FontStyles.Normal);
    }
  }

  void ShowToolsTab()
  {
    if (isToolsTabActive) return;

    SetTabActive(true, false, false);
    SetTabColors(new Color(0.3f, 0.6f, 1f, 0.8f), new Color(0.4f, 0.4f, 0.4f, 0.8f), new Color(0.4f, 0.4f, 0.4f, 0.8f));
    SetPanelActive(true, false, false);
    scrollView.content = toolsPanel.GetComponent<RectTransform>();
    RefreshNavigableButtons();
    PlayUISound();
  }

  void ShowGenerateTab()
  {
    if (isGenerateTabActive) return;

    SetTabActive(false, true, false);
    SetTabColors(new Color(0.4f, 0.4f, 0.4f, 0.8f), new Color(0.3f, 0.6f, 1f, 0.8f), new Color(0.4f, 0.4f, 0.4f, 0.8f));
    SetPanelActive(false, true, false);
    scrollView.content = generatePanel.GetComponent<RectTransform>();
    RefreshNavigableButtons();
    PlayUISound();
  }

  void ShowPauseTab()
  {
    if (isPauseTabActive) return;

    SetTabActive(false, false, true);
    SetTabColors(new Color(0.4f, 0.4f, 0.4f, 0.8f), new Color(0.4f, 0.4f, 0.4f, 0.8f), new Color(0.3f, 0.6f, 1f, 0.8f));
    SetPanelActive(false, false, true);
    scrollView.content = pausePanel.GetComponent<RectTransform>();
    RefreshNavigableButtons();
    PlayUISound();
  }

  void SetTabActive(bool tools, bool generate, bool pause)
  {
    isToolsTabActive = tools;
    isGenerateTabActive = generate;
    isPauseTabActive = pause;
  }

  void SetTabColors(Color toolsColor, Color generateColor, Color pauseColor)
  {
    if (toolsTabButton != null)
      toolsTabButton.GetComponent<Image>().color = toolsColor;
    if (generateTabButton != null)
      generateTabButton.GetComponent<Image>().color = generateColor;
    if (pauseTabButton != null)
      pauseTabButton.GetComponent<Image>().color = pauseColor;
  }

  void SetPanelActive(bool tools, bool generate, bool pause)
  {
    if (toolsPanel != null)
      toolsPanel.SetActive(tools);
    if (generatePanel != null)
      generatePanel.SetActive(generate);
    if (pausePanel != null)
      pausePanel.SetActive(pause);
  }

  void RefreshNavigableButtons()
  {
    allButtons.Clear();

    if (isToolsTabActive)
    {
      allButtons.AddRange(toolButtons);
    }
    else if (isGenerateTabActive)
    {
      Button[] generateButtons = generatePanel.GetComponentsInChildren<Button>();
      allButtons.AddRange(generateButtons);
    }
    else if (isPauseTabActive)
    {
      Button[] pauseButtons = pausePanel.GetComponentsInChildren<Button>();
      allButtons.AddRange(pauseButtons);
    }

    selectedButtonIndex = Mathf.Clamp(selectedButtonIndex, 0, Mathf.Max(0, allButtons.Count - 1));

    if (vrUIControlActive)
    {
      HighlightSelectedButton();
    }
  }

  // Game state management
  void PauseGame()
  {
    if (!isGamePaused)
    {
      originalTimeScale = Time.timeScale;
      Time.timeScale = 0f;
      isGamePaused = true;
    }
  }

  void ResumeGame()
  {
    if (isGamePaused)
    {
      Time.timeScale = originalTimeScale;
      isGamePaused = false;
    }
  }

  void RegenerateScene()
  {
    if (sceneGenerator != null)
    {
      sceneGenerator.GenerateRandomLocation();
    }
  }

  void QuitToMenu()
  {
    if (isCleaningUp)
    {
      return;
    }

    StartCoroutine(SafeQuitToMenu());
  }

  // Scene generation methods
  void GenerateRandomScene()
  {
    sceneGenerator?.GenerateRandomLocation();
  }

  void GenerateBarScene()
  {
    sceneGenerator?.GenerateBarScene();
  }

  void GenerateOfficeScene()
  {
    sceneGenerator?.GenerateOfficeScene();
  }

  void GenerateHomeScene()
  {
    sceneGenerator?.GenerateHomeScene();
  }

  void ClearScene()
  {
    sceneGenerator?.ClearScene();
    ClearSpawnedObjects();
  }

  // Tool spawning with improved feedback and single-instance management
  void SpawnTool(int toolIndex)
  {
    if (toolIndex < 0 || toolIndex >= toolNames.Length) return;

    if (toolQuantities[toolIndex] <= 0)
    {
      // Play error sound or provide visual feedback
      if (audioSource != null && uiSound != null)
      {
        audioSource.pitch = 0.7f; // Lower pitch for error
        audioSource.PlayOneShot(uiSound);
        audioSource.pitch = 1f; // Reset pitch
      }
      return;
    }

    // Handle single instance per tool
    if (singleInstancePerTool)
    {
      // Check if this tool already has an instance
      if (spawnedToolInstances.ContainsKey(toolIndex) && spawnedToolInstances[toolIndex] != null)
      {
        // Despawn the existing instance
        GameObject existingTool = spawnedToolInstances[toolIndex];
        spawnedObjects.Remove(existingTool);
        spawnedToolInstances.Remove(toolIndex);

        if (enableSpawnEffects)
        {
          CreateDespawnEffect(existingTool.transform.position);
        }

        Destroy(existingTool);

        // If we're trying to spawn the same tool again, just despawn and return
        return;
      }
    }

    Vector3 spawnPosition = spawnPoint.position + spawnPoint.TransformDirection(spawnOffset);
    GameObject spawnedObject = null;

    if (toolPrefabs[toolIndex] != null)
    {
      spawnedObject = Instantiate(toolPrefabs[toolIndex], spawnPosition, spawnPoint.rotation);
    }
    else
    {
      spawnedObject = CreateDefaultTool(toolIndex, spawnPosition);
    }

    if (spawnedObject != null)
    {
      if (spawnWithPhysics && spawnedObject.GetComponent<Rigidbody>() == null)
      {
        Rigidbody rb = spawnedObject.AddComponent<Rigidbody>();
        rb.AddForce(spawnPoint.forward * spawnForce, ForceMode.Impulse);
      }

      spawnedObjects.Add(spawnedObject);

      // Track single instances
      if (singleInstancePerTool)
      {
        spawnedToolInstances[toolIndex] = spawnedObject;
      }

      if (despawnDelay > 0 && !singleInstancePerTool)
      {
        StartCoroutine(DespawnAfterTime(spawnedObject, despawnDelay));
      }

      // Reduce quantity for consumable items (dust brush only)
      if (toolIndex == 3) // Dust brush
      {
        toolQuantities[toolIndex] = Mathf.Max(0, toolQuantities[toolIndex] - 1);
        RefreshUI();
      }

      PlaySpawnSound();
      if (enableSpawnEffects)
      {
        CreateSpawnEffect(spawnPosition);
      }
    }
  }

  GameObject CreateDefaultTool(int toolIndex, Vector3 position)
  {
    GameObject obj = new GameObject(toolNames[toolIndex]);
    obj.transform.position = position;

    switch (toolIndex)
    {
      case 0: // Camera
        CreateCameraModel(obj);
        break;
      case 1: // Scanner
        CreateScannerModel(obj);
        break;
      case 2: // UV Light
        CreateUVLightModel(obj);
        break;
      case 3: // Dust Brush
        CreateDustBrushModel(obj);
        break;
      default:
        CreateDefaultModel(obj);
        break;
    }

    obj.tag = "Grabbable";
    int grabLayer = LayerMask.NameToLayer("Grab");
    if (grabLayer != -1)
    {
      obj.layer = grabLayer;
    }

    return obj;
  }

  void CreateCameraModel(GameObject parent)
  {
    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
    body.transform.SetParent(parent.transform);
    body.transform.localScale = new Vector3(0.2f, 0.15f, 0.1f);
    body.GetComponent<MeshRenderer>().material.color = Color.black;

    GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    lens.transform.SetParent(parent.transform);
    lens.transform.localPosition = new Vector3(0, 0, 0.08f);
    lens.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
    lens.GetComponent<MeshRenderer>().material.color = Color.gray;
  }

  void CreateScannerModel(GameObject parent)
  {
    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
    body.transform.SetParent(parent.transform);
    body.transform.localScale = new Vector3(0.15f, 0.3f, 0.05f);
    body.GetComponent<MeshRenderer>().material.color = Color.white;

    GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Plane);
    screen.transform.SetParent(parent.transform);
    screen.transform.localPosition = new Vector3(0, 0.05f, 0.026f);
    screen.transform.localScale = new Vector3(0.06f, 1f, 0.08f);
    screen.GetComponent<MeshRenderer>().material.color = Color.blue;
  }

  void CreateUVLightModel(GameObject parent)
  {
    GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    handle.transform.SetParent(parent.transform);
    handle.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
    handle.GetComponent<MeshRenderer>().material.color = Color.black;

    GameObject light = new GameObject("Light");
    light.transform.SetParent(parent.transform);
    light.transform.localPosition = new Vector3(0, 0.12f, 0);
    Light lightComp = light.AddComponent<Light>();
    lightComp.type = LightType.Spot;
    lightComp.color = new Color(0.4f, 0f, 1f); // Purple UV light
    lightComp.intensity = 2f;
    lightComp.range = 5f;
    lightComp.spotAngle = 45f;
  }

  void CreateDustBrushModel(GameObject parent)
  {
    GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    handle.transform.SetParent(parent.transform);
    handle.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f);
    handle.transform.localPosition = Vector3.zero;
    handle.GetComponent<MeshRenderer>().material.color = new Color(0.8f, 0.6f, 0.3f);

    GameObject brushHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    brushHead.transform.SetParent(parent.transform);
    brushHead.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
    brushHead.transform.localPosition = new Vector3(0, 0.17f, 0);
    brushHead.GetComponent<MeshRenderer>().material.color = Color.gray;

    GameObject brushTip = new GameObject("BrushTip");
    brushTip.transform.SetParent(parent.transform);
    brushTip.transform.localPosition = new Vector3(0, 0.19f, 0);
  }

  void CreateDefaultModel(GameObject parent)
  {
    GameObject defaultObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
    defaultObj.transform.SetParent(parent.transform);
    defaultObj.transform.localScale = Vector3.one * 0.1f;
    defaultObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
  }

  IEnumerator DespawnAfterTime(GameObject obj, float delay)
  {
    yield return new WaitForSeconds(delay);

    if (obj != null)
    {
      spawnedObjects.Remove(obj);
      Destroy(obj);
    }
  }

  void CreateSpawnEffect(Vector3 position)
  {
    if (spawnEffect != null)
    {
      GameObject effect = Instantiate(spawnEffect, position, Quaternion.identity);
      Destroy(effect, 2f);
    }
  }

  // Create despawn effect
  void CreateDespawnEffect(Vector3 position)
  {
    if (spawnEffect != null)
    {
      GameObject effect = Instantiate(spawnEffect, position, Quaternion.identity);

      // Make despawn effect different (smaller, different color if possible)
      ParticleSystem particles = effect.GetComponent<ParticleSystem>();
      if (particles != null)
      {
        var main = particles.main;
        main.startSize = main.startSize.constant * 0.7f; // Smaller for despawn
        main.startColor = Color.red; // Red for despawn
      }

      Destroy(effect, 1f);
    }
  }

  void SetupAudio()
  {
    if (audioSource == null)
    {
      audioSource = GetComponent<AudioSource>();
      if (audioSource == null)
      {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.5f;
        audioSource.spatialBlend = 0f;
      }
    }
  }

  public void ToggleUI()
  {
    isUIVisible = !isUIVisible;

    if (uiInstance != null)
    {
      uiInstance.SetActive(isUIVisible);

      if (isUIVisible)
      {
        UpdateUITransform();
        RefreshUI();
        RefreshNavigableButtons();
      }
      else
      {
        uiInstance.transform.position = hiddenPosition;
        vrUIControlActive = false;
        ClearAllButtonHighlights();
      }
    }

    PlayUISound();
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

  void RefreshUI()
  {
    for (int i = 0; i < toolButtons.Count && i < toolNames.Length; i++)
    {
      Image buttonImage = toolButtons[i].GetComponent<Image>();
      if (!vrUIControlActive || selectedButtonIndex != i)
      {
        buttonImage.color = toolQuantities[i] > 0 ? availableColor : unavailableColor;
      }

      // Update quantity in improved layout
      Transform statusTransform = toolButtons[i].transform.Find("Content/Status");
      if (statusTransform?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI statusText)
      {
        string statusString = toolQuantities[i] > 0 ? "✓ AVAILABLE" : "✗ UNAVAILABLE";
        Color statusColor = toolQuantities[i] > 0 ? Color.green : Color.red;
        statusText.text = statusString;
        statusText.color = statusColor;
      }

      // Update quantity display
      Transform qtyTransform = toolButtons[i].transform.Find("Content/NameRow/Quantity");
      if (qtyTransform?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI qtyText)
      {
        qtyText.text = $"({toolQuantities[i]}/{maxQuantities[i]})";
        qtyText.color = toolQuantities[i] > 0 ? Color.green : Color.red;
      }
    }
  }

  public void ClearSpawnedObjects()
  {
    foreach (GameObject obj in spawnedObjects)
    {
      if (obj != null)
      {
        Destroy(obj);
      }
    }
    spawnedObjects.Clear();
    spawnedToolInstances.Clear();
  }

  // Audio methods
  void PlayNavigationSound()
  {
    if (audioSource != null && navigationSound != null)
    {
      audioSource.PlayOneShot(navigationSound);
    }
  }

  void PlaySelectSound()
  {
    if (audioSource != null && selectSound != null)
    {
      audioSource.PlayOneShot(selectSound);
    }
  }

  void PlayUISound()
  {
    if (audioSource != null && uiSound != null)
    {
      audioSource.PlayOneShot(uiSound);
    }
  }

  void PlaySpawnSound()
  {
    if (audioSource != null && spawnSound != null)
    {
      audioSource.PlayOneShot(spawnSound);
    }
  }

  void PlayGenerateSound()
  {
    if (audioSource != null && generateSound != null)
    {
      audioSource.PlayOneShot(generateSound);
    }
  }

  // VR CLEANUP METHODS - Improved and streamlined

  private void SafeDisableVRControls()
  {
    try
    {
      useVRControls = false;
      vrUIControlActive = false;

      // Clear VR input state
      wasAButtonPressed = false;
      wasBButtonPressed = false;
      lastJoystickInput = Vector2.zero;

      // Hide UI to prevent issues
      if (uiInstance != null && isUIVisible)
      {
        uiInstance.SetActive(false);
        isUIVisible = false;
      }

      // Clear button highlights
      ClearAllButtonHighlights();
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  IEnumerator SafeQuitToMenu()
  {
    isCleaningUp = true;

    // Step 1: Resume game first
    ResumeGame();

    // Step 2: Disable VR controls
    SafeDisableVRControls();

    // Step 3: Hide UI
    if (uiInstance != null)
    {
      uiInstance.SetActive(false);
      isUIVisible = false;
    }

    // Step 4: Clear button highlights
    ClearAllButtonHighlights();

    // Step 5: Wait for cleanup
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(vrCleanupDelay);

    // Step 6: Perform VR cleanup
    if (enableVRCleanup)
    {
      PerformVRCleanup();
    }

    // Step 7: Wait for VR cleanup to complete
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(0.1f);

    // Step 8: Load main menu with error handling
    LoadMainMenuWithFallback();
  }

  private void LoadMainMenuWithFallback()
  {
    try
    {
      UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    catch (System.Exception e)
    {
      // Fallback: try alternative scene load methods
      StartCoroutine(FallbackSceneLoad());
    }
  }

  private IEnumerator FallbackSceneLoad()
  {
    yield return new WaitForSeconds(0.1f);

    try
    {
      UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    catch (System.Exception fallbackError)
    {
      // Last resort: quit application
      Application.Quit();
    }
  }

  private void PerformVRCleanup()
  {
    try
    {
      // Find and disable VR components that might cause issues
      var ovrControllerHelpers = FindObjectsOfType<OVRControllerHelper>();
      foreach (var helper in ovrControllerHelpers)
      {
        if (helper != null)
        {
          helper.enabled = false;
        }
      }

      // Disable OVR Camera Rig
      var cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig != null)
      {
        cameraRig.enabled = false;
      }

      // Disable OVR Manager
      var ovrManager = FindObjectOfType<OVRManager>();
      if (ovrManager != null)
      {
        ovrManager.enabled = false;
      }

      // Clear any remaining VR references
      ClearVRControllerReferences();
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  private void ClearVRControllerReferences()
  {
    try
    {
      // Clear VR controller lists
      if (rightControllers != null)
      {
        rightControllers.Clear();
        rightControllers = null;
      }

      // Reset VR state
      wasAButtonPressed = false;
      wasBButtonPressed = false;
      selectedButtonIndex = 0;
      lastNavigationTime = 0f;
      vrUIControlActive = false;
      lastJoystickInput = Vector2.zero;
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  // LIFECYCLE METHODS WITH VR FIXES

  void OnDestroy()
  {
    if (instance == this)
    {
      instance = null;
    }

    try
    {
      // Set cleanup flag to prevent further operations
      isCleaningUp = true;

      // Disable VR controls immediately
      useVRControls = false;
      vrUIControlActive = false;

      // Stop all coroutines
      StopAllCoroutines();

      // Resume game state if paused
      if (isGamePaused)
      {
        try
        {
          Time.timeScale = originalTimeScale;
          isGamePaused = false;
        }
        catch (System.Exception e)
        {
          // Silent error handling
        }
      }

      // Hide and clean up UI
      if (uiInstance != null)
      {
        try
        {
          uiInstance.SetActive(false);
          isUIVisible = false;
          Destroy(uiInstance);
        }
        catch (System.Exception e)
        {
          // Silent error handling
        }
      }

      // Clear VR controller references
      ClearVRControllerReferences();

      // Clean up spawned objects
      CleanupSpawnedObjects();
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  private void CleanupSpawnedObjects()
  {
    try
    {
      if (spawnedObjects != null)
      {
        foreach (GameObject obj in spawnedObjects)
        {
          if (obj != null)
          {
            try
            {
              Destroy(obj);
            }
            catch (System.Exception)
            {
              // Ignore individual object destruction errors
            }
          }
        }
        spawnedObjects.Clear();
      }
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus)
    {
      // Safely disable VR controls
      SafeDisableVRControls();
    }
  }

  void OnApplicationFocus(bool hasFocus)
  {
    if (hasFocus)
    {
      // Re-enable VR controls after a delay
      if (!isCleaningUp && this != null)
      {
        StartCoroutine(ReEnableVRControlsAfterDelay());
      }
    }
    else
    {
      // Safely disable VR controls
      SafeDisableVRControls();
    }
  }

  IEnumerator ReEnableVRControlsAfterDelay()
  {
    yield return new WaitForSeconds(0.5f);

    if (Application.isPlaying && this != null && !isCleaningUp)
    {
      EnableVRControlsAfterDelay();
    }
  }

  private void EnableVRControlsAfterDelay()
  {
    try
    {
      useVRControls = true;
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  // PUBLIC API METHODS for cross-script communication

  /// <summary>
  /// Check if this ToolSpawner is currently using VR controls
  /// </summary>
  public bool IsUsingVRControls()
  {
    return useVRControls && vrUIControlActive;
  }

  /// <summary>
  /// Temporarily disable VR controls (called by other scripts)
  /// </summary>
  public void DisableVRControls()
  {
    vrUIControlActive = false;
    ClearAllButtonHighlights();
  }

  /// <summary>
  /// Re-enable VR controls (called by other scripts)
  /// </summary>
  public void EnableVRControls()
  {
    // Only re-enable if we're supposed to use VR controls and UI is visible
    if (useVRControls && isUIVisible)
    {
      vrUIControlActive = true;
      HighlightSelectedButton();
    }
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
      toolQuantities[toolIndex] = Mathf.Clamp(quantity, 0, maxQuantities[toolIndex]);

      if (isUIVisible)
      {
        RefreshUI();
      }
    }
  }
}