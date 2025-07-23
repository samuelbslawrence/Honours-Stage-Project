using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // Required for OrderBy
using UnityEngine.XR; // Required for XR Input

public class EvidenceChecklist : MonoBehaviour
{
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
  [SerializeField] private bool debugBoxDetection = true;
  [SerializeField] private float monitoringRate = 0.2f;
  [SerializeField] private string requiredFingerprintTag = "Fingerprint";
  [SerializeField] private string requiredLayerName = "UV";
  [SerializeField] private SceneFingerprintDetector sceneFingerprintDetector;
  [SerializeField] private bool autoFindBoxDetector = true;
  [SerializeField] private float fingerprintDetectionRadius = 100f; // NEW: Radius for fingerprint detection

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
  [SerializeField] private bool excludeFingerprintsFromList = false; // NEW: Toggle to exclude fingerprints from the main list

  [Header("Audio Settings")] //
  [SerializeField] private AudioSource audioSource; //
  [SerializeField] private AudioClip openChecklistSound; //

  private GameObject checklistInstance;
  private bool isChecklistVisible = false;
  private List<string> evidenceNames = new List<string>();
  private List<bool> evidenceFound = new List<bool>();
  private List<bool> evidencePhotographed = new List<bool>();
  private Dictionary<string, int> evidenceNameToIndex = new Dictionary<string, int>();
  private List<TextMeshProUGUI> checklistTexts = new List<TextMeshProUGUI>();
  private List<Image> checklistImages = new List<Image>();

  private TextMeshProUGUI fingerprintInfoTextComponent;

  private HashSet<GameObject> fingerprintsInBox = new HashSet<GameObject>();
  private HashSet<GameObject> lastKnownPhotographedFingerprints = new HashSet<GameObject>();
  private Dictionary<GameObject, string> fingerprintGameObjectToUniqueId = new Dictionary<GameObject, string>();
  private Dictionary<string, string> fingerprintUniqueIdToDisplayName = new Dictionary<string, string>();

  private int totalFingerprints = 0;
  private int photographedFingerprintCount = 0;

  private List<UnityEngine.XR.InputDevice> rightControllers = new List<UnityEngine.XR.InputDevice>();
  private bool wasAPressed = false;

  private readonly Vector3 initialSpawnPosition = new Vector3(0, -1000, 0);

  private static EvidenceChecklist instance;
  public static EvidenceChecklist Instance => instance;

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
    if (instance != this) return;

    FindRequiredComponents();
    FindSceneGenerator();
    FindToolSpawner();
    FindBoxDetector();
    InitializeEvidence();
    CreateChecklistUI();

    // NEW: Find or add AudioSource
    if (audioSource == null) //
    {
      audioSource = GetComponent<AudioSource>(); //
      if (audioSource == null) //
      {
        audioSource = gameObject.AddComponent<AudioSource>(); //
      }
    }
    // Set some default AudioSource settings (optional)
    audioSource.playOnAwake = false; //
    audioSource.spatialBlend = 0.0f; // Make it 2D UI sound

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

  void OnDestroy()
  {
    if (instance == this)
    {
      instance = null;
    }

    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }
  }

  void Update()
  {
    if (instance != this) return;

    if (!syncWithToolSpawner)
    {
      if (Input.GetKeyDown(toggleKey)) ToggleChecklist();

      bool isAPressed = CheckVRRightControllerAButton();
      if (isAPressed && !wasAPressed) ToggleChecklist();
      wasAPressed = isAPressed;
    }

    if (checklistInstance != null && isChecklistVisible)
    {
      UpdateChecklistTransform();
    }

    if (enableRealTimeUpdates && Time.time > updateCheckRate)
    {
      CheckForNewlyFoundEvidence();
    }
  }

  private void FindBoxDetector()
  {
    if (autoFindBoxDetector && sceneFingerprintDetector == null)
    {
      sceneFingerprintDetector = FindObjectOfType<SceneFingerprintDetector>();
      if (sceneFingerprintDetector == null)
      {
        useManualEvidenceList = true;
      }
    }
  }

  private IEnumerator BoxTriggerMonitoring()
  {
    while (trackFingerprints && instance == this)
    {
      yield return new WaitForSeconds(monitoringRate);

      if (cameraScript != null)
      {
        HashSet<GameObject> currentPhotographedFingerprints = cameraScript.GetPhotographedFingerprints();

        foreach (GameObject fingerprint in currentPhotographedFingerprints)
        {
          if (fingerprint != null &&
              fingerprintsInBox.Contains(fingerprint) &&
              !lastKnownPhotographedFingerprints.Contains(fingerprint))
          {
            HandleNewlyPhotographedFingerprint(fingerprint);
          }
        }
      }

      if (Time.frameCount % 50 == 0)
      {
        ScanFingerprintsInBox();
      }
    }
  }

  public void HandleNewlyPhotographedFingerprint(GameObject fingerprint)
  {
    if (fingerprint == null)
    {
      return;
    }

    // Ensure the fingerprint is valid and within the tracking box (which now includes radius check)
    if (IsValidFingerprint(fingerprint) && fingerprintsInBox.Contains(fingerprint))
    {
      string uniqueFingerprintId = GetUniqueFingerprintId(fingerprint);

      if (!lastKnownPhotographedFingerprints.Contains(fingerprint))
      {
        lastKnownPhotographedFingerprints.Add(fingerprint);
        photographedFingerprintCount++;

        if (evidenceNameToIndex.TryGetValue(uniqueFingerprintId, out int index))
        {
          if (!evidencePhotographed[index])
          {
            evidencePhotographed[index] = true;
          }
        }

        if (isChecklistVisible)
        {
          UpdateChecklistUI();
        }

        if (fingerprintInfoTextComponent != null)
        {
          string percentage = (totalFingerprints > 0)
              ? $"({(float)photographedFingerprintCount / totalFingerprints:P0})"
              : "(0%)";
          fingerprintInfoTextComponent.text = $"FINGERPRINTS: {photographedFingerprintCount}/{totalFingerprints} {percentage}";
        }
      }
    }
  }

  public void MarkEvidenceAsPhotographed(string evidenceId)
  {
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

  private void ScanFingerprintsInBox()
  {
    fingerprintsInBox.Clear();
    fingerprintGameObjectToUniqueId.Clear();
    fingerprintUniqueIdToDisplayName.Clear();

    // Determine the origin for radius check. If sceneGenerator is null, use world origin.
    Vector3 originPoint = (sceneGenerator != null) ? sceneGenerator.transform.position : Vector3.zero;

    // Use FindObjectsOfType<FingerprintMonitor>() for efficiency if useFingerprintMonitorScript is true
    // Otherwise, iterate through all GameObjects (less efficient but covers more cases)
    if (useFingerprintMonitorScript)
    {
      FingerprintMonitor[] monitors = FindObjectsOfType<FingerprintMonitor>();

      foreach (FingerprintMonitor monitor in monitors)
      {
        GameObject obj = monitor.gameObject;
        // Apply all checks here: IsValidFingerprint (now includes MeshRenderer and activeInHierarchy)
        // AND radius check
        if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
        {
          fingerprintsInBox.Add(obj);
          string uniqueId = GetUniqueFingerprintId(obj);
          fingerprintGameObjectToUniqueId[obj] = uniqueId;
          fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(obj);
        }
      }
    }
    else
    {
      List<GameObject> objectsToCheck = new List<GameObject>();

      if (sceneFingerprintDetector != null)
      {
        // This path uses SceneFingerprintDetector.GetDetectedFingerprints()
        // We will still apply the MeshRenderer check and radius check on its output.
        List<GameObject> objectsFromDetector = sceneFingerprintDetector.GetDetectedFingerprints();
        objectsToCheck.AddRange(objectsFromDetector);
      }
      else
      {
        // Fallback: search all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
          objectsToCheck.Add(obj);
        }
      }

      foreach (GameObject obj in objectsToCheck)
      {
        // Apply all checks here: IsValidFingerprint (now includes MeshRenderer and activeInHierarchy)
        // AND radius check
        if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
        {
          fingerprintsInBox.Add(obj);
          string uniqueId = GetUniqueFingerprintId(obj);
          fingerprintGameObjectToUniqueId[obj] = uniqueId;
          fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(obj);
        }
      }
    }

    totalFingerprints = fingerprintsInBox.Count;

    if (isChecklistVisible)
    {
      UpdateChecklistUI();
    }
  }

  /// <summary>
  /// Checks if a GameObject is a valid fingerprint based on tag, layer, script, and active state.
  /// Does NOT include radius check, as that depends on sceneGenerator.
  /// </summary>
  /// <param name="obj">The GameObject to check.</param>
  /// <returns>True if the GameObject meets the criteria for a valid fingerprint, false otherwise.</returns>
  private bool IsValidFingerprint(GameObject obj)
  {
    if (obj == null || !obj.activeInHierarchy) return false; // Must be active

    // Must have a MeshRenderer component
    if (obj.GetComponent<MeshRenderer>() == null) return false;

    // Check for FingerprintMonitor script if that option is enabled
    if (useFingerprintMonitorScript)
    {
      if (obj.GetComponent<FingerprintMonitor>() == null) return false;
    }
    else // Fallback to tag/layer/name checks if not using FingerprintMonitor script
    {
      bool hasCorrectTag = obj.CompareTag(requiredFingerprintTag);
      int uvLayerIndex = LayerMask.NameToLayer(requiredLayerName);
      bool onCorrectLayer = uvLayerIndex != -1 && obj.layer == uvLayerIndex;
      string objName = obj.name;
      bool hasCorrectName = objName == "Fingerprint" ||
                            (objName.StartsWith("Fingerprint (") && objName.EndsWith(")") && objName.Length > "Fingerprint (".Length + 1);

      if (!(hasCorrectTag && onCorrectLayer && hasCorrectName)) return false;
    }

    return true;
  }

  private string GetUniqueFingerprintId(GameObject fingerprint)
  {
    if (fingerprint == null) return "NullFingerprint";
    return $"Fingerprint_{fingerprint.name}_{fingerprint.GetInstanceID()}";
  }

  private string GetFingerprintDisplayName(GameObject fingerprint)
  {
    if (fingerprint == null) return "Unknown Fingerprint";

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

  public void OnFingerprintEnteredBox(GameObject fingerprint)
  {
    // This method is likely called by an external trigger box.
    // We still need to validate the fingerprint against all criteria.
    Vector3 originPoint = (sceneGenerator != null) ? sceneGenerator.transform.position : Vector3.zero;

    if (IsValidFingerprint(fingerprint) && Vector3.Distance(fingerprint.transform.position, originPoint) <= fingerprintDetectionRadius)
    {
      if (!fingerprintsInBox.Contains(fingerprint))
      {
        fingerprintsInBox.Add(fingerprint);
        string uniqueId = GetUniqueFingerprintId(fingerprint);
        fingerprintGameObjectToUniqueId[fingerprint] = uniqueId;
        fingerprintUniqueIdToDisplayName[uniqueId] = GetFingerprintDisplayName(fingerprint);

        totalFingerprints = fingerprintsInBox.Count; // Recalculate total after adding

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
    // When a fingerprint exits the box, we simply remove it from our tracking,
    // regardless of its validity or distance, as it's no longer "in the box".
    if (fingerprintsInBox.Contains(fingerprint))
    {
      fingerprintsInBox.Remove(fingerprint);
      string uniqueId = GetUniqueFingerprintId(fingerprint);
      fingerprintGameObjectToUniqueId.Remove(fingerprint);
      fingerprintUniqueIdToDisplayName.Remove(uniqueId);
      lastKnownPhotographedFingerprints.Remove(fingerprint);

      totalFingerprints = fingerprintsInBox.Count; // Recalculate total after removing

      if (isChecklistVisible)
      {
        UpdateChecklistUI();
      }
    }
  }

  private void FindToolSpawner()
  {
    if (autoFindToolSpawner && toolSpawner == null)
    {
      toolSpawner = FindObjectOfType<ToolSpawner>();
    }
  }

  IEnumerator RealTimeFoundEvidenceMonitoring()
  {
    while (enableRealTimeUpdates && instance == this)
    {
      yield return new WaitForSeconds(updateCheckRate);
      CheckForNewlyFoundEvidence();
    }
  }

  private void FindSceneGenerator()
  {
    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator == null)
      {
        useManualEvidenceList = true;
      }
    }
  }

  private IEnumerator MonitorSceneGeneration()
  {
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

  public void OnSceneGenerated()
  {
    RefreshEvidenceList();
  }

  public void RefreshEvidenceList()
  {
    if (instance != this) return;

    InitializeEvidence();

    if (trackFingerprints)
    {
      ScanFingerprintsInBox(); // Re-scan based on new criteria
      lastKnownPhotographedFingerprints.Clear();
      photographedFingerprintCount = 0;

      if (cameraScript != null)
      {
        // Re-evaluate photographed fingerprints against the new valid set
        HashSet<GameObject> currentPhotographedFingerprints = cameraScript.GetPhotographedFingerprints();
        foreach (GameObject fp in currentPhotographedFingerprints)
        {
          // Only re-add if it's still considered "in the box" (i.e., valid and within radius)
          if (fp != null && fingerprintsInBox.Contains(fp))
          {
            HandleNewlyPhotographedFingerprint(fp);
          }
        }
      }
    }

    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }
    CreateChecklistUI();
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

  private void FindRequiredComponents()
  {
    if (cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
    }
    if (attachPoint == null)
    {
      GameObject leftController = GameObject.Find("LeftHandAnchor") ?? GameObject.Find("LeftHand");
      attachPoint = leftController?.transform ?? Camera.main.transform;
    }
  }

  private void InitializeEvidence()
  {
    evidenceNames.Clear();
    evidenceFound.Clear();
    evidencePhotographed.Clear();
    evidenceNameToIndex.Clear();

    if (trackFingerprints)
    {
      HashSet<GameObject> allFingerprintsInScene = new HashSet<GameObject>();
      // Determine the origin for radius check. If sceneGenerator is null, use world origin.
      Vector3 originPoint = (sceneGenerator != null) ? sceneGenerator.transform.position : Vector3.zero;

      // Use FindObjectsOfType<FingerprintMonitor>() for efficiency if useFingerprintMonitorScript is true
      // Otherwise, iterate through all GameObjects (less efficient but covers more cases)
      if (useFingerprintMonitorScript)
      {
        FingerprintMonitor[] monitors = FindObjectsOfType<FingerprintMonitor>();
        foreach (FingerprintMonitor monitor in monitors)
        {
          GameObject obj = monitor.gameObject;
          // Apply all checks: IsValidFingerprint (includes MeshRenderer and activeInHierarchy)
          // AND radius check
          if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
          {
            allFingerprintsInScene.Add(obj);
          }
        }
      }
      else // Fallback to general GameObject search if not using FingerprintMonitor script
      {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
          // Apply all checks: IsValidFingerprint (includes MeshRenderer and activeInHierarchy)
          // AND radius check
          if (IsValidFingerprint(obj) && Vector3.Distance(obj.transform.position, originPoint) <= fingerprintDetectionRadius)
          {
            allFingerprintsInScene.Add(obj);
          }
        }
      }

      totalFingerprints = allFingerprintsInScene.Count; // Count all fingerprints regardless of list display

      if (!excludeFingerprintsFromList) // Only add fingerprints to the list if not excluded
      {
        foreach (GameObject fp in allFingerprintsInScene)
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
    }

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

    // Fallback if no evidence found at all (including filtered fingerprints)
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
        // Fallback to manual list if sceneGenerator fails or doesn't provide names
      }
    }
    if (useManualEvidenceList)
    {
      return evidenceItemNames;
    }
    return new string[0];
  }

  private void CreateChecklistUI()
  {
    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }

    fingerprintInfoTextComponent = null;

    checklistInstance = new GameObject("EvidenceChecklist_" + GetInstanceID());
    checklistInstance.transform.position = initialSpawnPosition;
    Canvas canvas = checklistInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;
    CanvasScaler canvasScaler = checklistInstance.AddComponent<CanvasScaler>();
    canvasScaler.dynamicPixelsPerUnit = 300;
    checklistInstance.AddComponent<GraphicRaycaster>();
    RectTransform canvasRect = checklistInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(500, 750);

    GameObject panel = new GameObject("Panel");
    panel.transform.SetParent(checklistInstance.transform, false);
    RectTransform panelRect = panel.AddComponent<RectTransform>();
    panelRect.anchorMin = Vector2.zero;
    panelRect.anchorMax = Vector2.one;
    panelRect.sizeDelta = Vector2.zero;
    Image panelImage = panel.AddComponent<Image>();
    panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

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
      fingerprintInfoTextComponent.name = "FingerprintInfoText";
      fingerprintInfoTextComponent.text = $"FINGERPRINTS: {photographedFingerprintCount}/{totalFingerprints} (0%)";
      fingerprintInfoTextComponent.color = Color.cyan;
      fingerprintInfoTextComponent.fontSize = 20;
      fingerprintInfoTextComponent.alignment = TextAlignmentOptions.Center;
      fingerprintInfoTextComponent.fontStyle = FontStyles.Bold;
    }

    GameObject contentGO = new GameObject("Content");
    contentGO.transform.SetParent(checklistInstance.transform, false);
    RectTransform contentRect = contentGO.AddComponent<RectTransform>();
    contentRect.anchorMin = new Vector2(0, 0);
    contentRect.anchorMax = new Vector2(1, 1);
    float contentStartY = trackFingerprints ? fingerprintInfoYOffset - 50 : -70;
    contentRect.offsetMin = new Vector2(0, 0);
    contentRect.offsetMax = new Vector2(0, contentStartY);

    GameObject scrollViewGO = new GameObject("ScrollView");
    scrollViewGO.transform.SetParent(contentGO.transform, false);
    RectTransform scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
    scrollViewRect.anchorMin = Vector2.zero;
    scrollViewRect.anchorMax = Vector2.one;
    scrollViewRect.sizeDelta = Vector2.zero;

    ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
    scrollRect.horizontal = false;
    scrollRect.vertical = true;
    scrollRect.movementType = ScrollRect.MovementType.Clamped;

    GameObject viewportGO = new GameObject("Viewport");
    viewportGO.transform.SetParent(scrollViewGO.transform, false);
    RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
    viewportRect.anchorMin = Vector2.zero;
    viewportRect.anchorMax = Vector2.one;
    viewportRect.sizeDelta = Vector2.zero;
    viewportRect.pivot = new Vector2(0, 1);
    viewportGO.AddComponent<RectMask2D>();
    scrollRect.viewport = viewportRect;

    GameObject itemsPanelGO = new GameObject("ItemsPanel");
    itemsPanelGO.transform.SetParent(viewportGO.transform, false);
    RectTransform itemsPanelRect = itemsPanelGO.AddComponent<RectTransform>();
    itemsPanelRect.anchorMin = new Vector2(0, 1);
    itemsPanelRect.anchorMax = new Vector2(1, 1);
    itemsPanelRect.pivot = new Vector2(0.5f, 1);
    // Set sizeDelta Y to 0, let layout group and content fitter handle it
    itemsPanelRect.sizeDelta = new Vector2(0, 0);
    itemsPanelRect.anchoredPosition = new Vector2(0, 0);
    scrollRect.content = itemsPanelRect;

    // Add VerticalLayoutGroup to itemsPanelGO
    VerticalLayoutGroup layoutGroup = itemsPanelGO.AddComponent<VerticalLayoutGroup>();
    layoutGroup.childAlignment = TextAnchor.UpperLeft;
    layoutGroup.spacing = itemSpacing; // Use existing itemSpacing for vertical spacing
    layoutGroup.padding = new RectOffset(20, 20, (int)itemSpacing, (int)itemSpacing); // Add padding around the items
    layoutGroup.childControlWidth = true; // Children will control their own width
    layoutGroup.childForceExpandWidth = true; // Children will expand to fill width
    layoutGroup.childControlHeight = false; // Children will use their specified height
    layoutGroup.childForceExpandHeight = false; // Do not force expand height

    // Add ContentSizeFitter to itemsPanelGO to make it resize based on its content
    ContentSizeFitter contentFitter = itemsPanelGO.AddComponent<ContentSizeFitter>();
    contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


    checklistTexts.Clear();
    // checklistImages.Clear(); // Not used, but good practice if we were storing these

    for (int i = 0; i < evidenceNames.Count; i++)
    {
      GameObject itemGO = new GameObject("EvidenceItem_" + i);
      itemGO.transform.SetParent(itemsPanelGO.transform, false);
      RectTransform itemRect = itemGO.AddComponent<RectTransform>();
      itemRect.anchorMin = new Vector2(0, 0.5f); // Center vertically within the item
      itemRect.anchorMax = new Vector2(1, 0.5f); // Center vertically within the item
      itemRect.sizeDelta = new Vector2(0, itemHeight);
      itemRect.anchoredPosition = Vector2.zero; // Layout group handles positioning

      // Add background box for each item
      GameObject itemBackgroundGO = new GameObject("ItemBackground");
      itemBackgroundGO.transform.SetParent(itemGO.transform, false); // Make it a child of the itemGO
      RectTransform itemBackgroundRect = itemBackgroundGO.AddComponent<RectTransform>();
      itemBackgroundRect.anchorMin = Vector2.zero;
      itemBackgroundRect.anchorMax = Vector2.one;
      itemBackgroundRect.sizeDelta = Vector2.zero; // Make it fill the parent itemGO
      Image itemBackgroundImage = itemBackgroundGO.AddComponent<Image>();
      itemBackgroundImage.color = itemBackgroundColor; // Use the new serialized color

      TextMeshProUGUI itemText = itemGO.AddComponent<TextMeshProUGUI>();
      itemText.name = "ItemText_" + i;
      itemText.color = Color.white;
      itemText.fontSize = 24;
      itemText.alignment = TextAlignmentOptions.Left;
      itemText.rectTransform.offsetMin = new Vector2(20, 0);
      itemText.rectTransform.offsetMax = new Vector2(-20, 0);
      checklistTexts.Add(itemText);
    }
    checklistInstance.SetActive(false);
  }

  public void ToggleChecklist()
  {
    isChecklistVisible = !isChecklistVisible;
    if (checklistInstance != null)
    {
      checklistInstance.SetActive(isChecklistVisible);
      if (isChecklistVisible)
      {
        UpdateChecklistTransform();
        UpdateChecklistUI();
        // NEW: Play sound when checklist opens
        if (audioSource != null && openChecklistSound != null) //
        {
          audioSource.PlayOneShot(openChecklistSound); //
        }
      }
    }
  }

  public void ShowChecklist()
  {
    isChecklistVisible = true;
    if (checklistInstance != null)
    {
      checklistInstance.SetActive(true);
      UpdateChecklistTransform();
      UpdateChecklistUI();
    }
  }

  public void HideChecklist()
  {
    isChecklistVisible = false;
    if (checklistInstance != null)
    {
      checklistInstance.SetActive(false);
    }
  }

  private void UpdateChecklistTransform()
  {
    if (attachPoint != null)
    {
      checklistInstance.transform.position = attachPoint.position + attachPoint.TransformDirection(positionOffset);
      checklistInstance.transform.rotation = attachPoint.rotation * Quaternion.Euler(rotationOffset);
      checklistInstance.transform.localScale = Vector3.one * checklistScale;
    }
  }

  private void UpdateChecklistUI()
  {
    if (checklistInstance == null) return;

    // Re-find fingerprintInfoTextComponent if it's null (can happen after Destroy/Create cycle)
    if (fingerprintInfoTextComponent == null)
    {
      GameObject fingerprintInfoGO = checklistInstance.transform.Find("FingerprintInfo")?.gameObject;
      if (fingerprintInfoGO != null)
      {
        fingerprintInfoTextComponent = fingerprintInfoGO.GetComponent<TextMeshProUGUI>();
      }

      if (fingerprintInfoTextComponent == null)
      {
        // Fallback: search all TextMeshProUGUI components if direct find fails
        TextMeshProUGUI[] allTexts = checklistInstance.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in allTexts)
        {
          if (text.name == "FingerprintInfoText" || text.gameObject.name == "FingerprintInfo")
          {
            fingerprintInfoTextComponent = text;
            break;
          }
        }
      }
    }

    if (fingerprintInfoTextComponent != null)
    {
      string percentage = (totalFingerprints > 0)
          ? $"({(float)photographedFingerprintCount / totalFingerprints:P0})"
          : "(0%)";

      fingerprintInfoTextComponent.text = $"FINGERPRINTS: {photographedFingerprintCount}/{totalFingerprints} {percentage}";
    }

    for (int i = 0; i < evidenceNames.Count; i++)
    {
      if (i < checklistTexts.Count)
      {
        TextMeshProUGUI itemText = checklistTexts[i];
        string currentEvidenceName = evidenceNames[i];
        bool isPhotographed = evidencePhotographed[i];
        bool isFound = evidenceFound[i];

        string displayName = currentEvidenceName;
        bool isFingerprint = currentEvidenceName.StartsWith("Fingerprint_");

        if (isFingerprint && fingerprintUniqueIdToDisplayName.ContainsKey(currentEvidenceName))
        {
          displayName = fingerprintUniqueIdToDisplayName[currentEvidenceName];
        }

        string statusSymbol = uncheckSymbol;
        Color textColor = notFoundColor;

        if (isFingerprint)
        {
          if (enableMysteryMode)
          {
            displayName = mysterySymbol;
            statusSymbol = mysteryCheckSymbol;
            textColor = mysteryColor;
          }

          if (isPhotographed)
          {
            statusSymbol = photographSymbol;
            textColor = photographedColor;
            if (!enableMysteryMode)
            {
              statusSymbol = checkmarkSymbol;
            }
          }
        }
        else // Not a fingerprint
        {
          if (enableMysteryMode)
          {
            displayName = mysterySymbol;
            statusSymbol = mysteryCheckSymbol;
            textColor = mysteryColor;
          }

          if (isFound)
          {
            statusSymbol = checkmarkSymbol;
            textColor = foundColor;
          }
          else if (isPhotographed)
          {
            statusSymbol = photographSymbol;
            textColor = photographedColor;
          }
        }

        // Construct the text based on mystery mode, now including the bullet point
        if (enableMysteryMode)
        {
          itemText.text = $"{bulletPointSymbol}{statusSymbol} {displayName}";
        }
        else
        {
          itemText.text = $"{bulletPointSymbol}{statusSymbol} {displayName}";
          // Only add photograph symbol for non-fingerprints if not in mystery mode and photographed
          if (isPhotographed && !isFingerprint)
          {
            itemText.text += $" {photographSymbol}";
          }
        }
        itemText.color = textColor;
      }
    }
  }

  public void MarkEvidenceAsFound(string evidenceId)
  {
    if (evidenceNameToIndex.TryGetValue(evidenceId, out int index))
    {
      if (!evidenceFound[index])
      {
        evidenceFound[index] = true;
        UpdateChecklistUI();
      }
    }
  }

  public void MarkFingerprintAsPhotographedPublic(GameObject fingerprint)
  {
    HandleNewlyPhotographedFingerprint(fingerprint);
  }

  private bool CheckVRRightControllerAButton()
  {
    if (rightControllers.Count == 0)
    {
      InputDevices.GetDevicesWithCharacteristics(
          InputDeviceCharacteristics.Right |
          InputDeviceCharacteristics.Controller, rightControllers);
    }

    if (rightControllers.Count > 0)
    {
      rightControllers[0].TryGetFeatureValue(CommonUsages.primaryButton, out bool aButtonState);
      return aButtonState;
    }
    return false;
  }

  private void CheckForNewlyFoundEvidence()
  {
    if (toolSpawner == null || !enableRealTimeUpdates) return;

    for (int i = 0; i < evidenceNames.Count; i++)
    {
      string evidenceId = evidenceNames[i];
      if (!evidenceId.StartsWith("Fingerprint_") && !evidenceFound[i])
      {
        // Game-specific logic for detecting found evidence would go here
      }
    }
  }
}