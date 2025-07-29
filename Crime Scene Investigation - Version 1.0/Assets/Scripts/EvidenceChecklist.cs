using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.XR;

// - EVIDENCE CHECKLIST MAIN CLASS
public class EvidenceChecklist : MonoBehaviour
{
  // - INSPECTOR CONFIGURATION
  [Header("Checklist Settings")]
  [SerializeField] private Transform attachPoint;
  [SerializeField] private Vector3 positionOffset = new Vector3(0.05f, 0.5f, 0.05f);
  [SerializeField] private Vector3 rotationOffset = new Vector3(30f, 0f, 0f);
  [SerializeField] private float checklistScale = 0.0003f;
  [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
  [SerializeField] private CameraScript cameraScript;

  [Header("Cross-Script Communication")]
  [SerializeField] private ToolSpawner toolSpawner;
  [SerializeField] private bool autoFindToolSpawner = true;
  [SerializeField] private bool syncWithToolSpawner = true;

  [Header("Box Trigger Detection")]
  [SerializeField] private bool trackFingerprints = true;
  [SerializeField] private float monitoringRate = 0.2f;
  [SerializeField] private string requiredFingerprintTag = "Fingerprint";
  [SerializeField] private string requiredLayerName = "UV";
  [SerializeField] private SceneFingerprintDetector sceneFingerprintDetector;
  [SerializeField] private bool autoFindBoxDetector = true;
  [SerializeField] private float fingerprintDetectionRadius = 100f;

  [Header("Fingerprint Monitor Integration")]
  [Tooltip("If true, the checklist will primarily identify fingerprints by the presence of the FingerprintMonitor script.")]
  [SerializeField] private bool useFingerprintMonitorScript = true;

  [Header("Mystery Mode")]
  [SerializeField] private bool enableMysteryMode = true;
  [SerializeField] private string mysterySymbol = "???????";

  [Header("Real-Time Updates")]
  [SerializeField] private bool enableRealTimeUpdates = true;
  [SerializeField] private float updateCheckRate = 0.5f;

  [Header("Scene Generator Integration")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;
  [SerializeField] private bool updateOnSceneGeneration = true;

  [Header("Evidence Items (Fallback)")]
  [SerializeField]
  private string[] evidenceItemNames = new string[] { "Bottle 1", "Bottle 2", "Bottle 3", "Glass", "Knife" };

  [SerializeField] private bool useManualEvidenceList = false;

  [Header("UI Settings")]
  [SerializeField] private Color foundColor = new Color(0.2f, 0.8f, 0.2f);
  [SerializeField] private Color notFoundColor = new Color(0.8f, 0.2f, 0.2f);
  [SerializeField] private Color mysteryColor = new Color(0.7f, 0.7f, 0.3f);
  [SerializeField] private Color photographedColor = new Color(0.2f, 0.8f, 0.2f);
  [SerializeField] private string checkmarkSymbol = "✓";
  [SerializeField] private string uncheckSymbol = "□";
  [SerializeField] private string mysteryCheckSymbol = "?";
  [SerializeField] private string photographSymbol = "📷";
  [SerializeField] private float itemSpacing = 15f;
  [SerializeField] private float itemHeight = 60f;
  [SerializeField] private Color itemBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);
  [SerializeField] private string bulletPointSymbol = "• ";
  [SerializeField] private bool excludeFingerprintsFromList = false;

  [Header("Audio Settings")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip openChecklistSound;

  // - STATE VARIABLES
  // UI state management
  private GameObject checklistInstance;
  private bool isChecklistVisible = false;

  // Evidence tracking
  private List<string> evidenceNames = new List<string>();
  private List<bool> evidenceFound = new List<bool>();
  private List<bool> evidencePhotographed = new List<bool>();
  private Dictionary<string, int> evidenceNameToIndex = new Dictionary<string, int>();
  private List<TextMeshProUGUI> checklistTexts = new List<TextMeshProUGUI>();
  private List<Image> checklistImages = new List<Image>();

  // Fingerprint tracking
  private TextMeshProUGUI fingerprintInfoTextComponent;
  private HashSet<GameObject> fingerprintsInBox = new HashSet<GameObject>();
  private HashSet<GameObject> lastKnownPhotographedFingerprints = new HashSet<GameObject>();
  private Dictionary<GameObject, string> fingerprintGameObjectToUniqueId = new Dictionary<GameObject, string>();
  private Dictionary<string, string> fingerprintUniqueIdToDisplayName = new Dictionary<string, string>();

  // Fingerprint counters
  private int totalFingerprints = 0;
  private int photographedFingerprintCount = 0;

  // VR input handling
  private List<UnityEngine.XR.InputDevice> rightControllers = new List<UnityEngine.XR.InputDevice>();
  private bool wasAPressed = false;

  // Constants
  private readonly Vector3 initialSpawnPosition = new Vector3(0, -1000, 0);

  // - SINGLETON IMPLEMENTATION
  private static EvidenceChecklist instance;
  public static EvidenceChecklist Instance => instance;

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
    if (instance != this) return;

    // Initialize all components and systems
    FindRequiredComponents();
    FindSceneGenerator();
    FindToolSpawner();
    FindBoxDetector();

    // Setup audio source
    if (audioSource == null)
    {
      audioSource = GetComponent<AudioSource>();
      if (audioSource == null)
      {
        audioSource = gameObject.AddComponent<AudioSource>();
      }
    }
    audioSource.playOnAwake = false;
    audioSource.spatialBlend = 0.0f;

    // Initial evidence setup and UI creation
    InitializeEvidence();
    CreateChecklistUI();

    // Start monitoring coroutines
    if (updateOnSceneGeneration && sceneGenerator != null)
    {
      StartCoroutine(MonitorSceneGeneration());
    }

    if (enableRealTimeUpdates)
    {
      StartCoroutine(RealTimeFoundEvidenceMonitoring());
    }

    if (trackFingerprints)
    {
      ScanFingerprintsInBox();
      StartCoroutine(BoxTriggerMonitoring());
    }
  }

  void Update()
  {
    if (instance != this) return;

    // Handle input if not synced with tool spawner
    if (!syncWithToolSpawner)
    {
      if (Input.GetKeyDown(toggleKey)) ToggleChecklist();

      bool isAPressed = CheckVRRightControllerAButton();
      if (isAPressed && !wasAPressed) ToggleChecklist();
      wasAPressed = isAPressed;
    }

    // Update UI transform if visible
    if (checklistInstance != null && isChecklistVisible)
    {
      UpdateChecklistTransform();
    }

    // Real-time evidence checking
    if (enableRealTimeUpdates && Time.time > updateCheckRate)
    {
      CheckForNewlyFoundEvidence();
    }
  }

  void OnDestroy()
  {
    // Cleanup singleton reference
    if (instance == this)
    {
      instance = null;
    }

    // Cleanup UI instance
    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }
  }

  // - COMPONENT SETUP
  private void FindRequiredComponents()
  {
    // Find camera script reference
    if (cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
    }

    // Find attachment point for UI
    if (attachPoint == null)
    {
      GameObject leftController = GameObject.Find("LeftHandAnchor") ?? GameObject.Find("LeftHand");
      attachPoint = leftController?.transform ?? Camera.main.transform;
    }
  }

  private void FindSceneGenerator()
  {
    // Locate scene generator component
    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator == null)
      {
        useManualEvidenceList = true;
      }
    }
  }

  private void FindToolSpawner()
  {
    // Locate tool spawner component
    if (autoFindToolSpawner && toolSpawner == null)
    {
      toolSpawner = FindObjectOfType<ToolSpawner>();
    }
  }

  private void FindBoxDetector()
  {
    // Locate fingerprint detector component
    if (autoFindBoxDetector && sceneFingerprintDetector == null)
    {
      sceneFingerprintDetector = FindObjectOfType<SceneFingerprintDetector>();
      if (sceneFingerprintDetector == null)
      {
        useManualEvidenceList = true;
      }
    }
  }

  // - EVIDENCE INITIALIZATION
  private void InitializeEvidence()
  {
    // Clear existing evidence data
    evidenceNames.Clear();
    evidenceFound.Clear();
    evidencePhotographed.Clear();
    evidenceNameToIndex.Clear();
    fingerprintUniqueIdToDisplayName.Clear();

    // Re-scan fingerprints as part of initialization
    if (trackFingerprints)
    {
      ScanFingerprintsInBox();
    }

    // Add fingerprints to evidence list if not excluded
    if (trackFingerprints && !excludeFingerprintsFromList)
    {
      foreach (GameObject fp in fingerprintsInBox)
      {
        string uniqueId = GetUniqueFingerprintId(fp);
        if (!evidenceNameToIndex.ContainsKey(uniqueId))
        {
          evidenceNames.Add(uniqueId);
          evidenceFound.Add(false);
          evidencePhotographed.Add(false);
          evidenceNameToIndex[uniqueId] = evidenceNames.Count - 1;
          fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(fp);
        }
      }
    }

    // Add other evidence from best available source
    string[] otherEvidenceNames = GetEvidenceNamesFromBestSource();
    if (otherEvidenceNames != null)
    {
      foreach (string name in otherEvidenceNames)
      {
        if (!string.IsNullOrEmpty(name) && !evidenceNameToIndex.ContainsKey(name))
        {
          evidenceNames.Add(name);
          evidenceFound.Add(false);
          evidencePhotographed.Add(false);
          evidenceNameToIndex[name] = evidenceNames.Count - 1;
        }
      }
    }

    // Fallback evidence if no evidence found
    if (evidenceNames.Count == 0)
    {
      foreach (string name in new string[] { "Bottle 1", "Bottle 2", "Bottle 3", "Glass", "Knife" })
      {
        evidenceNames.Add(name);
        evidenceFound.Add(false);
        evidencePhotographed.Add(false);
        evidenceNameToIndex[name] = evidenceNames.Count - 1;
      }
    }
  }

  private string[] GetEvidenceNamesFromBestSource()
  {
    // Get evidence names from scene generator or manual list
    if (!useManualEvidenceList && sceneGenerator != null)
    {
      try
      {
        string[] currentSceneNames = sceneGenerator.GetCurrentSceneEvidenceNames();
        if (currentSceneNames != null && currentSceneNames.Length > 0)
        {
          return currentSceneNames;
        }
      }
      catch (System.Exception)
      {
        // Fallback to manual list if sceneGenerator fails
      }
    }
    if (useManualEvidenceList)
    {
      return evidenceItemNames;
    }
    return new string[0];
  }

  // - FINGERPRINT VALIDATION
  private bool IsValidFingerprint(GameObject obj)
  {
    // Ensure object is not null, is active, and has a MeshRenderer
    if (obj == null || !obj.activeInHierarchy || obj.GetComponent<MeshRenderer>() == null) return false;

    // Check for fingerprint monitor script if enabled
    if (useFingerprintMonitorScript)
    {
      return obj.GetComponent<FingerprintMonitor>() != null;
    }
    else
    {
      // Fallback to strict tag/layer/name validation
      bool hasCorrectTag = obj.CompareTag(requiredFingerprintTag);
      int uvLayerIndex = LayerMask.NameToLayer(requiredLayerName);
      bool onCorrectLayer = uvLayerIndex != -1 && obj.layer == uvLayerIndex;
      string objName = obj.name;
      bool hasCorrectName = objName.StartsWith("Fingerprint") && (objName.Length == "Fingerprint".Length || (objName.Contains("(") && objName.EndsWith(")")));

      return hasCorrectTag && onCorrectLayer && hasCorrectName;
    }
  }

  // - FINGERPRINT IDENTIFICATION
  private string GetUniqueFingerprintId(GameObject fingerprint)
  {
    // Generate unique ID for fingerprint
    if (fingerprint == null) return "NullFingerprint";
    return $"Fingerprint_{fingerprint.name}_{fingerprint.GetInstanceID()}";
  }

  private string GetFingerprintDisplayName(GameObject fingerprint)
  {
    // Generate display name for fingerprint
    if (fingerprint == null) return "Unknown Fingerprint";

    // Use parent name if available
    if (fingerprint.transform.parent != null)
    {
      string parentName = fingerprint.transform.parent.name.Replace("(Clone)", "").Trim();
      parentName = parentName.Replace("_", " ");
      if (parentName.Length > 0)
      {
        parentName = char.ToUpper(parentName[0]) + parentName.Substring(1);
      }
      return $"{parentName} Fingerprint";
    }

    // Extract number from fingerprint name
    string fingerprintName = fingerprint.name;
    if (fingerprintName.Contains("(") && fingerprintName.Contains(")"))
    {
      int startIndex = fingerprintName.IndexOf("(") + 1;
      int endIndex = fingerprintName.IndexOf(")");
      if (startIndex > 0 && endIndex > startIndex)
      {
        string number = fingerprintName.Substring(startIndex, endIndex - startIndex);
        return $"Fingerprint {number}";
      }
    }

    return "Fingerprint";
  }

  // - FINGERPRINT SCANNING
  private void ScanFingerprintsInBox()
  {
    // Clear existing fingerprint data to avoid stale entries
    fingerprintsInBox.Clear();
    fingerprintGameObjectToUniqueId.Clear();

    Vector3 originPoint = (sceneGenerator != null) ? sceneGenerator.transform.position : Vector3.zero;

    // Use a temporary HashSet to collect valid fingerprints before assigning
    HashSet<GameObject> currentScanResults = new HashSet<GameObject>();

    if (useFingerprintMonitorScript)
    {
      // Find all FingerprintMonitor scripts in the scene
      FingerprintMonitor[] monitors = FindObjectsOfType<FingerprintMonitor>();

      foreach (FingerprintMonitor monitor in monitors)
      {
        GameObject obj = monitor.gameObject;
        if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
        {
          currentScanResults.Add(obj);
          string uniqueId = GetUniqueFingerprintId(obj);
          fingerprintGameObjectToUniqueId[obj] = uniqueId;
          if (!fingerprintUniqueIdToDisplayName.ContainsKey(uniqueId))
          {
            fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(obj);
          }
        }
      }
    }
    else
    {
      // Fallback to finding all GameObjects and filtering
      List<GameObject> objectsToCheck = new List<GameObject>();

      if (sceneFingerprintDetector != null)
      {
        List<GameObject> objectsFromDetector = sceneFingerprintDetector.GetDetectedFingerprints();
        if (objectsFromDetector != null)
        {
          objectsToCheck.AddRange(objectsFromDetector);
        }
      }
      else
      {
        // Last resort: find all GameObjects by tag/layer
        GameObject[] allTaggedObjects = GameObject.FindGameObjectsWithTag(requiredFingerprintTag);
        int uvLayer = LayerMask.NameToLayer(requiredLayerName);
        foreach (GameObject obj in allTaggedObjects)
        {
          if (uvLayer == -1 || obj.layer == uvLayer)
          {
            objectsToCheck.Add(obj);
          }
        }
      }

      foreach (GameObject obj in objectsToCheck)
      {
        if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
        {
          currentScanResults.Add(obj);
          string uniqueId = GetUniqueFingerprintId(obj);
          fingerprintGameObjectToUniqueId[obj] = uniqueId;
          if (!fingerprintUniqueIdToDisplayName.ContainsKey(uniqueId))
          {
            fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(obj);
          }
        }
      }
    }

    // Assign the unique and valid fingerprints to the main tracking HashSet
    fingerprintsInBox = currentScanResults;
    totalFingerprints = fingerprintsInBox.Count;

    if (isChecklistVisible)
    {
      UpdateChecklistUI();
    }
  }

  // - FINGERPRINT PHOTOGRAPHY TRACKING
  public void HandleNewlyPhotographedFingerprint(GameObject fingerprint)
  {
    // Process newly photographed fingerprint
    if (fingerprint == null)
    {
      return;
    }

    // Ensure it's a valid fingerprint and currently in the tracked set
    if (IsValidFingerprint(fingerprint) && fingerprintsInBox.Contains(fingerprint))
    {
      string uniqueFingerprintId = GetUniqueFingerprintId(fingerprint);

      // Check if this specific fingerprint was not already marked as photographed
      if (!lastKnownPhotographedFingerprints.Contains(fingerprint))
      {
        lastKnownPhotographedFingerprints.Add(fingerprint);
        photographedFingerprintCount++;

        // Mark in the main evidence list if it's there
        if (evidenceNameToIndex.TryGetValue(uniqueFingerprintId, out int index))
        {
          if (!evidencePhotographed[index])
          {
            evidencePhotographed[index] = true;
            evidenceFound[index] = true;
          }
        }

        // Refresh UI if visible
        if (isChecklistVisible)
        {
          UpdateChecklistUI();
        }

        // Update fingerprint info text immediately
        if (fingerprintInfoTextComponent != null)
        {
          string percentage = (totalFingerprints > 0) ? $"({(float)photographedFingerprintCount / totalFingerprints:P0})" : "(0%)";
          fingerprintInfoTextComponent.text = $"FINGERPRINTS: {photographedFingerprintCount}/{totalFingerprints} {percentage}";
        }
      }
    }
  }

  public void MarkFingerprintAsPhotographedPublic(GameObject fingerprint)
  {
    // Public interface for marking fingerprint as photographed
    HandleNewlyPhotographedFingerprint(fingerprint);
  }

  // - FINGERPRINT BOX EVENTS
  public void OnFingerprintEnteredBox(GameObject fingerprint)
  {
    Vector3 originPoint = (sceneGenerator != null) ? sceneGenerator.transform.position : Vector3.zero;
    if (IsValidFingerprint(fingerprint) && Vector3.Distance(fingerprint.transform.position, originPoint) <= fingerprintDetectionRadius)
    {
      if (fingerprintsInBox.Add(fingerprint))
      {
        string uniqueId = GetUniqueFingerprintId(fingerprint);
        fingerprintGameObjectToUniqueId[fingerprint] = uniqueId;
        fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(fingerprint);
        totalFingerprints = fingerprintsInBox.Count;

        if (cameraScript != null && cameraScript.IsFingerprintPhotographed(fingerprint))
        {
          HandleNewlyPhotographedFingerprint(fingerprint);
        }

        if (isChecklistVisible)
        {
          UpdateChecklistUI();
        }
      }
    }
  }

  public void OnFingerprintExitedBox(GameObject fingerprint)
  {
    if (fingerprintsInBox.Remove(fingerprint))
    {
      string uniqueId = GetUniqueFingerprintId(fingerprint);
      fingerprintGameObjectToUniqueId.Remove(fingerprint);
      fingerprintUniqueIdToDisplayName.Remove(uniqueId);
      lastKnownPhotographedFingerprints.Remove(fingerprint);
      totalFingerprints = fingerprintsInBox.Count;
      if (isChecklistVisible)
      {
        UpdateChecklistUI();
      }
    }
  }

  // - REAL-TIME MONITORING
  IEnumerator RealTimeFoundEvidenceMonitoring()
  {
    // Monitor for newly found evidence
    while (enableRealTimeUpdates && instance == this)
    {
      yield return new WaitForSeconds(updateCheckRate);
      CheckForNewlyFoundEvidence();
    }
  }

  private IEnumerator MonitorSceneGeneration()
  {
    // Monitor scene generation for changes
    int lastObjectCount = 0;
    while (updateOnSceneGeneration && sceneGenerator != null && instance == this)
    {
      int currentObjectCount = sceneGenerator.GetSpawnedObjectCount();
      if (currentObjectCount != lastObjectCount)
      {
        yield return new WaitForSeconds(0.5f);
        RefreshEvidenceList();
        lastObjectCount = currentObjectCount;
      }
      yield return new WaitForSeconds(1f);
    }
  }

  private IEnumerator BoxTriggerMonitoring()
  {
    // Monitor fingerprint photography changes
    while (trackFingerprints && instance == this)
    {
      yield return new WaitForSeconds(monitoringRate);
      if (cameraScript != null)
      {
        HashSet<GameObject> currentPhotographedFingerprints = cameraScript.GetPhotographedFingerprints();
        foreach (GameObject fingerprint in currentPhotographedFingerprints)
        {
          if (fingerprint != null && fingerprintsInBox.Contains(fingerprint) && !lastKnownPhotographedFingerprints.Contains(fingerprint))
          {
            HandleNewlyPhotographedFingerprint(fingerprint);
          }
        }
      }
      // Periodic full scan to catch any missed entries or objects that became active
      if (Time.frameCount % 50 == 0)
      {
        ScanFingerprintsInBox();
      }
    }
  }

  // - EVIDENCE TRACKING
  public void MarkEvidenceAsPhotographed(string evidenceId)
  {
    // Mark regular evidence as photographed
    if (evidenceId.StartsWith("Fingerprint_"))
    {
      return;
    }
    if (evidenceNameToIndex.TryGetValue(evidenceId, out int index))
    {
      if (!evidencePhotographed[index])
      {
        evidencePhotographed[index] = true;
        evidenceFound[index] = true;
        UpdateChecklistUI();
      }
    }
  }

  public void MarkEvidenceAsFound(string evidenceId)
  {
    // Mark evidence as found
    if (evidenceNameToIndex.TryGetValue(evidenceId, out int index))
    {
      if (!evidenceFound[index])
      {
        evidenceFound[index] = true;
        UpdateChecklistUI();
      }
    }
  }

  private void CheckForNewlyFoundEvidence()
  {
    // Placeholder for game-specific logic to check if non-fingerprint evidence is found
  }

  // - UI CREATION SYSTEM
  private void CreateChecklistUI()
  {
    // Destroy existing UI
    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }
    fingerprintInfoTextComponent = null;

    // Create main checklist container
    checklistInstance = new GameObject("EvidenceChecklist_" + GetInstanceID());
    checklistInstance.transform.position = initialSpawnPosition;

    // Setup canvas
    Canvas canvas = checklistInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;
    CanvasScaler canvasScaler = checklistInstance.AddComponent<CanvasScaler>();
    canvasScaler.dynamicPixelsPerUnit = 300;
    checklistInstance.AddComponent<GraphicRaycaster>();
    RectTransform canvasRect = checklistInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(500, 750);

    // Create background panel
    GameObject panel = new GameObject("Panel");
    panel.transform.SetParent(checklistInstance.transform, false);
    RectTransform panelRect = panel.AddComponent<RectTransform>();
    panelRect.anchorMin = Vector2.zero;
    panelRect.anchorMax = Vector2.one;
    panelRect.sizeDelta = Vector2.zero;
    Image panelImage = panel.AddComponent<Image>();
    panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

    // Create title
    GameObject titleGO = new GameObject("Title");
    titleGO.transform.SetParent(checklistInstance.transform, false);
    RectTransform titleRect = titleGO.AddComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0, 1);
    titleRect.anchorMax = new Vector2(1, 1);
    titleRect.sizeDelta = new Vector2(0, 70);
    titleRect.anchoredPosition = Vector2.zero;
    TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
    titleText.text = enableMysteryMode ? "EVIDENCE SEARCH" : "EVIDENCE CHECKLIST";
    titleText.color = Color.white;
    titleText.fontSize = 32;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.fontStyle = FontStyles.Bold;

    float fingerprintInfoYOffset = -75;

    // Create fingerprint info display
    if (trackFingerprints)
    {
      GameObject fingerprintInfoGO = new GameObject("FingerprintInfo");
      fingerprintInfoGO.transform.SetParent(checklistInstance.transform, false);
      RectTransform fingerprintInfoRect = fingerprintInfoGO.AddComponent<RectTransform>();
      fingerprintInfoRect.anchorMin = new Vector2(0, 1);
      fingerprintInfoRect.anchorMax = new Vector2(1, 1);
      fingerprintInfoRect.sizeDelta = new Vector2(0, 50);
      fingerprintInfoRect.anchoredPosition = new Vector2(0, fingerprintInfoYOffset);
      fingerprintInfoTextComponent = fingerprintInfoGO.AddComponent<TextMeshProUGUI>();
      fingerprintInfoTextComponent.color = Color.cyan;
      fingerprintInfoTextComponent.fontSize = 24;
      fingerprintInfoTextComponent.alignment = TextAlignmentOptions.Center;
      fingerprintInfoTextComponent.fontStyle = FontStyles.Bold;
      fingerprintInfoTextComponent.text = "FINGERPRINTS: 0/0 (0%)";
    }

    // Create scroll view for checklist items
    GameObject scrollViewGO = new GameObject("ScrollView");
    scrollViewGO.transform.SetParent(checklistInstance.transform, false);
    RectTransform scrollRect = scrollViewGO.AddComponent<RectTransform>();
    scrollRect.anchorMin = new Vector2(0, 0);
    scrollRect.anchorMax = new Vector2(1, 1);
    scrollRect.offsetMin = new Vector2(20, 20);
    scrollRect.offsetMax = new Vector2(-20, fingerprintInfoYOffset - 20);

    // Add ScrollRect component
    ScrollRect scrollRectComp = scrollViewGO.AddComponent<ScrollRect>();

    // Create content panel for items
    GameObject contentGO = new GameObject("Content");
    contentGO.transform.SetParent(scrollViewGO.transform, false);
    RectTransform contentRect = contentGO.AddComponent<RectTransform>();
    contentRect.anchorMin = new Vector2(0, 1);
    contentRect.anchorMax = new Vector2(1, 1);
    contentRect.pivot = new Vector2(0.5f, 1);
    contentRect.sizeDelta = new Vector2(0, evidenceNames.Count * itemHeight + (evidenceNames.Count - 1) * itemSpacing);

    // Add VerticalLayoutGroup for item arrangement
    VerticalLayoutGroup layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
    layoutGroup.childAlignment = TextAnchor.UpperCenter;
    layoutGroup.spacing = itemSpacing;
    layoutGroup.childForceExpandHeight = false;
    layoutGroup.childControlHeight = false;

    // Assign content to scroll view
    scrollRectComp.content = contentRect;
    scrollRectComp.vertical = true;
    scrollRectComp.horizontal = false;
    scrollRectComp.elasticity = 0.1f;

    // Clear previous UI elements lists
    checklistTexts.Clear();
    checklistImages.Clear();

    // Create UI elements for each evidence item
    for (int i = 0; i < evidenceNames.Count; i++)
    {
      // Create item container
      GameObject itemGO = new GameObject("EvidenceItem_" + i);
      itemGO.transform.SetParent(contentGO.transform, false);
      RectTransform itemRect = itemGO.AddComponent<RectTransform>();
      itemRect.sizeDelta = new Vector2(contentRect.rect.width, itemHeight);

      // Add background image to item
      Image itemImage = itemGO.AddComponent<Image>();
      itemImage.color = itemBackgroundColor;
      checklistImages.Add(itemImage);

      // Create TextMeshProUGUI for item name and status
      GameObject textGO = new GameObject("ItemText");
      textGO.transform.SetParent(itemGO.transform, false);
      RectTransform textRect = textGO.AddComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.sizeDelta = Vector2.zero;
      textRect.offsetMin = new Vector2(10, 0);
      textRect.offsetMax = new Vector2(-10, 0);

      TextMeshProUGUI itemText = textGO.AddComponent<TextMeshProUGUI>();
      itemText.enableWordWrapping = true;
      itemText.overflowMode = TextOverflowModes.Truncate;
      itemText.fontSize = 22;
      itemText.alignment = TextAlignmentOptions.Left;
      checklistTexts.Add(itemText);
    }

    // Hide checklist initially
    checklistInstance.SetActive(false);
    isChecklistVisible = false;

    // Update the UI with initial data
    UpdateChecklistUI();
  }

  // - UI UPDATE SYSTEM
  public void UpdateChecklistUI()
  {
    // Update the display of all checklist items
    if (checklistInstance == null) return;

    for (int i = 0; i < evidenceNames.Count; i++)
    {
      string evidenceId = evidenceNames[i];
      string displayName = evidenceId;

      // Handle fingerprint display names
      if (evidenceId.StartsWith("Fingerprint_") && fingerprintUniqueIdToDisplayName.ContainsKey(evidenceId))
      {
        displayName = fingerprintUniqueIdToDisplayName[evidenceId];
      }
      else if (evidenceId.StartsWith("Fingerprint_"))
      {
        displayName = "Fingerprint";
      }

      bool found = evidenceFound[i];
      bool photographed = evidencePhotographed[i];

      // Determine text color and content
      Color textColor;
      string statusSymbol;
      string itemTextContent;

      if (enableMysteryMode && !found)
      {
        textColor = mysteryColor;
        statusSymbol = mysteryCheckSymbol;
        itemTextContent = bulletPointSymbol + mysterySymbol;
      }
      else
      {
        textColor = found ? foundColor : notFoundColor;
        statusSymbol = found ? checkmarkSymbol : uncheckSymbol;
        itemTextContent = bulletPointSymbol + displayName;
      }

      // Add photographed symbol if applicable
      if (photographed)
      {
        itemTextContent += " " + photographSymbol;
        textColor = photographedColor;
      }

      // Update text and color
      if (i < checklistTexts.Count)
      {
        checklistTexts[i].text = $"{statusSymbol} {itemTextContent}";
        checklistTexts[i].color = textColor;
      }
    }

    // Update fingerprint info text
    if (trackFingerprints && fingerprintInfoTextComponent != null)
    {
      string percentage = (totalFingerprints > 0) ? $"({(float)photographedFingerprintCount / totalFingerprints:P0})" : "(0%)";
      fingerprintInfoTextComponent.text = $"FINGERPRINTS: {photographedFingerprintCount}/{totalFingerprints} {percentage}";
    }
  }

  private void UpdateChecklistTransform()
  {
    // Update checklist UI transform to follow attach point
    if (attachPoint == null || checklistInstance == null) return;

    Vector3 targetPosition = attachPoint.position + attachPoint.TransformDirection(positionOffset);
    Quaternion targetRotation = attachPoint.rotation * Quaternion.Euler(rotationOffset);

    checklistInstance.transform.position = targetPosition;
    checklistInstance.transform.rotation = targetRotation;
    checklistInstance.transform.localScale = Vector3.one * checklistScale;
  }

  // - CHECKLIST VISIBILITY TOGGLE
  public void ToggleChecklist()
  {
    // Toggle checklist visibility
    isChecklistVisible = !isChecklistVisible;
    if (checklistInstance != null)
    {
      checklistInstance.SetActive(isChecklistVisible);
      if (isChecklistVisible)
      {
        UpdateChecklistTransform();
        UpdateChecklistUI();
        if (audioSource != null && openChecklistSound != null)
        {
          audioSource.PlayOneShot(openChecklistSound);
        }
      }
    }
  }

  // - PUBLIC REFRESH METHODS
  public void RefreshEvidenceList()
  {
    // Call InitializeEvidence to re-populate all lists and re-scan fingerprints
    InitializeEvidence();

    // Recreate UI to reflect potentially changed number of items
    CreateChecklistUI();

    // Ensure UI is updated
    if (isChecklistVisible)
    {
      checklistInstance.SetActive(true);
      UpdateChecklistTransform();
      UpdateChecklistUI();
    }
    else
    {
      checklistInstance.SetActive(false);
    }
  }

  public void OnSceneGenerated()
  {
    // Handle scene generation event
    // Clear all previous fingerprint data
    lastKnownPhotographedFingerprints.Clear();
    photographedFingerprintCount = 0;

    // Reset CameraScript photographed fingerprints
    if (cameraScript != null)
    {
      cameraScript.ResetPhotographedFingerprints();
    }

    // Perform a full re-initialization which includes re-scanning fingerprints
    InitializeEvidence();
    CreateChecklistUI();

    // Update UI state
    if (isChecklistVisible)
    {
      UpdateChecklistTransform();
    }
    UpdateChecklistUI();
  }

  // - VR INPUT HANDLING
  private bool CheckVRRightControllerAButton()
  {
    // Check VR controller A button
    if (rightControllers.Count == 0)
    {
      InputDevices.GetDevicesWithCharacteristics(
          InputDeviceCharacteristics.Right |
          InputDeviceCharacteristics.Controller, rightControllers);
    }

    if (rightControllers.Count > 0)
    {
      foreach (var device in rightControllers)
      {
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButtonValue) && aButtonValue)
        {
          return true;
        }
      }
    }
    return false;
  }
}