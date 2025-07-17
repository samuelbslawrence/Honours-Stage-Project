using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
  [SerializeField] private bool notifyToolSpawnerOnToggle = true;

  [Header("Mystery Mode")]
  [SerializeField] private bool enableMysteryMode = true;
  [SerializeField] private string mysterySymbol = "???????";
  [SerializeField] private string mysteryDescription = "Unknown Evidence";

  [Header("Real-Time Updates")]
  [SerializeField] private bool enableRealTimeUpdates = true;
  [SerializeField] private float updateCheckRate = 0.5f;

  [Header("Scene Generator Integration")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;
  [SerializeField] private bool updateOnSceneGeneration = true;

  [Header("Evidence Items (Fallback)")]
  [SerializeField] private string[] evidenceItemNames = new string[] { "Bottle 1", "Bottle 2", "Bottle 3", "Glass", "Knife" };
  [SerializeField] private bool useManualEvidenceList = false;

  [Header("UI Settings")]
  [SerializeField] private Color foundColor = new Color(0.2f, 0.8f, 0.2f);
  [SerializeField] private Color notFoundColor = new Color(0.8f, 0.2f, 0.2f);
  [SerializeField] private Color mysteryColor = new Color(0.7f, 0.7f, 0.3f);
  [SerializeField] private string checkmarkSymbol = "✓";
  [SerializeField] private string uncheckSymbol = "□";
  [SerializeField] private string mysteryCheckSymbol = "?";
  [SerializeField] private float itemSpacing = 15f;
  [SerializeField] private float itemHeight = 60f;

  private GameObject checklistInstance;
  private bool isChecklistVisible = false;
  private List<string> evidenceNames = new List<string>();
  private List<bool> evidenceFound = new List<bool>();
  private Dictionary<string, int> evidenceNameToIndex = new Dictionary<string, int>();
  private List<TextMeshProUGUI> checklistTexts = new List<TextMeshProUGUI>();
  private List<Image> checklistImages = new List<Image>();
  private bool wasButtonPressed = false;
  private readonly Vector3 initialSpawnPosition = new Vector3(0, -1000, 0);
  private List<GameObject> currentSpawnedEvidence = new List<GameObject>();

  // Real-time tracking
  private float lastUpdateCheck = 0f;
  private int lastFoundCount = 0;

  // Singleton pattern to prevent multiple instances
  private static EvidenceChecklist instance;
  public static EvidenceChecklist Instance => instance;

  void Awake()
  {
    // Implement singleton pattern
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }
    instance = this;
  }

  void Start()
  {
    // Additional safety check
    if (instance != this)
    {
      return;
    }

    DestroyExistingChecklistPrefabs();
    FindRequiredComponents();
    FindSceneGenerator();
    FindToolSpawner();
    InitializeEvidence();
    CreateChecklistUI();

    if (updateOnSceneGeneration && sceneGenerator != null)
    {
      StartCoroutine(MonitorSceneGeneration());
    }

    if (enableRealTimeUpdates)
    {
      StartCoroutine(RealTimeFoundEvidenceMonitoring());
    }
  }

  void OnDestroy()
  {
    if (instance == this)
    {
      instance = null;
    }

    // Clean up any remaining checklist instances
    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }
  }

  void Update()
  {
    if (instance != this) return; // Safety check

    if (Input.GetKeyDown(toggleKey)) ToggleChecklist();

    bool isButtonPressed = CheckVRButtonPressed();
    if (isButtonPressed && !wasButtonPressed) ToggleChecklist();
    wasButtonPressed = isButtonPressed;

    if (checklistInstance != null && isChecklistVisible)
    {
      UpdateChecklistTransform();
    }

    // Real-time evidence checking
    if (enableRealTimeUpdates && Time.time - lastUpdateCheck > updateCheckRate)
    {
      CheckForNewlyFoundEvidence();
      lastUpdateCheck = Time.time;
    }
  }

  // Find ToolSpawner for cross-script communication
  private void FindToolSpawner()
  {
    if (autoFindToolSpawner && toolSpawner == null)
    {
      toolSpawner = FindObjectOfType<ToolSpawner>();
    }
  }

  // Notify ToolSpawner when our visibility changes
  private void NotifyToolSpawnerOfVisibilityChange()
  {
    if (notifyToolSpawnerOnToggle && toolSpawner != null)
    {
      if (isChecklistVisible)
      {
        // We're becoming visible - ask ToolSpawner to disable VR controls
        toolSpawner.DisableVRControls();
      }
      else
      {
        // We're becoming hidden - allow ToolSpawner to use VR controls
        toolSpawner.EnableVRControls();
      }
    }
  }

  // REAL-TIME MONITORING FOR FOUND EVIDENCE
  IEnumerator RealTimeFoundEvidenceMonitoring()
  {
    while (enableRealTimeUpdates && instance == this)
    {
      yield return new WaitForSeconds(updateCheckRate);

      CheckForNewlyFoundEvidence();

      // Check if completion status changed
      int currentFoundCount = GetFoundEvidenceCount();
      if (currentFoundCount != lastFoundCount)
      {
        lastFoundCount = currentFoundCount;
        if (isChecklistVisible)
        {
          UpdateChecklistUI();
        }
      }
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

  public void RefreshEvidenceList()
  {
    if (instance != this) return;

    InitializeEvidence();

    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
      CreateChecklistUI();

      if (isChecklistVisible)
      {
        checklistInstance.SetActive(true);
        UpdateChecklistTransform();
        UpdateChecklistUI();
      }
    }
  }

  private void DestroyExistingChecklistPrefabs()
  {
    // More comprehensive cleanup
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj != null && (
          obj.name.Contains("ChecklistPrefab") ||
          obj.name.Contains("DefaultChecklist") ||
          obj.name.Contains("EvidenceChecklist") ||
          (obj.GetComponent<Canvas>() != null && obj.transform.Find("Title") != null &&
           obj.transform.Find("Title").GetComponent<TextMeshProUGUI>() != null &&
           obj.transform.Find("Title").GetComponent<TextMeshProUGUI>().text.Contains("EVIDENCE"))))
      {
        if (obj != checklistInstance) // Don't destroy our own instance
        {
          Destroy(obj);
        }
      }
    }

    // Also check for any Canvas components with checklist-like structure
    Canvas[] canvases = FindObjectsOfType<Canvas>();
    foreach (Canvas canvas in canvases)
    {
      if (canvas != null && canvas.gameObject != checklistInstance)
      {
        Transform titleTransform = canvas.transform.Find("Title");
        if (titleTransform != null)
        {
          TextMeshProUGUI titleText = titleTransform.GetComponent<TextMeshProUGUI>();
          if (titleText != null && (titleText.text.Contains("EVIDENCE") || titleText.text.Contains("CHECKLIST")))
          {
            Destroy(canvas.gameObject);
          }
        }
      }
    }
  }

  private void FindRequiredComponents()
  {
    if (cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
      if (cameraScript == null)
      {
        enabled = false;
        return;
      }
    }

    if (attachPoint == null)
    {
      GameObject leftController = GameObject.Find("LeftHandAnchor");
      attachPoint = leftController?.transform ?? Camera.main.transform;
    }
  }

  private void InitializeEvidence()
  {
    evidenceNames.Clear();
    evidenceFound.Clear();
    evidenceNameToIndex.Clear();
    currentSpawnedEvidence.Clear();

    string[] namesToUse = GetEvidenceNamesFromBestSource();

    // Make sure we have valid evidence names
    if (namesToUse == null || namesToUse.Length == 0)
    {
      namesToUse = new string[] { "Bottle 1", "Bottle 2", "Bottle 3", "Glass", "Knife" };
    }

    for (int i = 0; i < namesToUse.Length; i++)
    {
      string name = namesToUse[i];
      if (string.IsNullOrEmpty(name)) continue;

      evidenceNames.Add(name);
      evidenceFound.Add(false);
      evidenceNameToIndex[name] = evidenceNames.Count - 1;
    }
  }

  private string[] GetEvidenceNamesFromBestSource()
  {
    // Priority 1: Scene Generator current scene evidence
    if (!useManualEvidenceList && sceneGenerator != null)
    {
      try
      {
        string[] currentSceneNames = sceneGenerator.GetCurrentSceneEvidenceNames();
        if (currentSceneNames != null && currentSceneNames.Length > 0)
        {
          return currentSceneNames;
        }

        // Fallback to all evidence names from generator
        string[] allGeneratorNames = sceneGenerator.GetEvidenceNames();
        if (allGeneratorNames != null && allGeneratorNames.Length > 0)
        {
          return allGeneratorNames;
        }
      }
      catch (System.Exception e)
      {
        // Silent error handling
      }
    }

    // Priority 2: Camera Script target names
    if (cameraScript != null)
    {
      string[] cameraNames = cameraScript.GetCurrentTargetNames();
      if (cameraNames != null && cameraNames.Length > 0)
      {
        return cameraNames;
      }
    }

    // Priority 3: Manual evidence list
    if (useManualEvidenceList)
    {
      return evidenceItemNames;
    }

    // Priority 4: Scan scene for Evidence tagged objects and get names from CameraScript
    List<string> scannedNames = new List<string>();
    GameObject[] evidenceObjects = GameObject.FindGameObjectsWithTag("Evidence");

    foreach (GameObject obj in evidenceObjects)
    {
      if (obj != null && obj.activeInHierarchy)
      {
        string displayName = GetEvidenceDisplayName(obj);
        if (!string.IsNullOrEmpty(displayName) && !scannedNames.Contains(displayName))
        {
          scannedNames.Add(displayName);
        }
      }
    }

    if (scannedNames.Count > 0)
    {
      return scannedNames.ToArray();
    }

    return new string[0];
  }

  // Get display name for evidence object - uses CameraScript integration
  private string GetEvidenceDisplayName(GameObject evidenceObj)
  {
    if (evidenceObj == null) return "Unknown";

    // Try to get display name from CameraScript
    if (cameraScript != null)
    {
      string displayName = cameraScript.GetEvidenceDisplayName(evidenceObj);
      if (!string.IsNullOrEmpty(displayName))
      {
        return displayName;
      }
    }

    // Fallback: clean up the object name
    return CleanObjectName(evidenceObj.name);
  }

  // Simple name cleaning - just removes (Clone) and does basic formatting
  private string CleanObjectName(string objectName)
  {
    if (string.IsNullOrEmpty(objectName)) return "Unknown";

    string cleanName = objectName.Replace("(Clone)", "").Trim();

    // Replace underscores with spaces and capitalize first letter
    cleanName = cleanName.Replace("_", " ");
    if (cleanName.Length > 0)
    {
      cleanName = char.ToUpper(cleanName[0]) + cleanName.Substring(1);
    }

    return cleanName;
  }

  private void CreateChecklistUI()
  {
    if (checklistInstance != null)
    {
      Destroy(checklistInstance);
    }

    checklistInstance = new GameObject("DefaultChecklist_" + GetInstanceID());
    checklistInstance.transform.position = initialSpawnPosition;

    Canvas canvas = checklistInstance.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;

    CanvasScaler canvasScaler = checklistInstance.AddComponent<CanvasScaler>();
    canvasScaler.dynamicPixelsPerUnit = 300;

    checklistInstance.AddComponent<GraphicRaycaster>();

    RectTransform canvasRect = checklistInstance.GetComponent<RectTransform>();
    canvasRect.sizeDelta = new Vector2(400, 700);

    // Background panel
    GameObject panel = new GameObject("Panel");
    panel.transform.SetParent(checklistInstance.transform, false);
    RectTransform panelRect = panel.AddComponent<RectTransform>();
    panelRect.anchorMin = Vector2.zero;
    panelRect.anchorMax = Vector2.one;
    panelRect.sizeDelta = Vector2.zero;

    Image panelImage = panel.AddComponent<Image>();
    panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

    // Title
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
    titleText.outlineWidth = 0.2f;
    titleText.outlineColor = new Color(0, 0, 0, 0.7f);

    // VR Controls hint
    GameObject hintGO = new GameObject("VRHint");
    hintGO.transform.SetParent(checklistInstance.transform, false);
    RectTransform hintRect = hintGO.AddComponent<RectTransform>();
    hintRect.anchorMin = new Vector2(0, 1);
    hintRect.anchorMax = new Vector2(1, 1);
    hintRect.sizeDelta = new Vector2(0, 25);
    hintRect.anchoredPosition = new Vector2(0, -75);

    TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
    hintText.text = "VR: Left Stick=Navigate | Y=Toggle | X=Select";
    hintText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    hintText.fontSize = 14;
    hintText.alignment = TextAlignmentOptions.Center;
    hintText.fontStyle = FontStyles.Italic;

    // Content area
    GameObject contentGO = new GameObject("Content");
    contentGO.transform.SetParent(checklistInstance.transform, false);
    RectTransform contentRect = contentGO.AddComponent<RectTransform>();
    contentRect.anchorMin = new Vector2(0, 0);
    contentRect.anchorMax = new Vector2(1, 1);
    contentRect.sizeDelta = new Vector2(0, -110);
    contentRect.anchoredPosition = new Vector2(0, -55);

    SetupChecklistContent(contentGO.transform);

    checklistInstance.SetActive(false);
    isChecklistVisible = false;
  }

  private void SetupChecklistContent(Transform contentArea)
  {
    checklistTexts.Clear();
    checklistImages.Clear();

    for (int i = 0; i < evidenceNames.Count; i++)
    {
      CreateChecklistItem(contentArea, i);
    }
  }

  private void CreateChecklistItem(Transform parent, int index)
  {
    GameObject itemGO = new GameObject($"Item_{index}");
    itemGO.transform.SetParent(parent, false);

    RectTransform rect = itemGO.AddComponent<RectTransform>();
    rect.anchorMin = new Vector2(0, 1);
    rect.anchorMax = new Vector2(1, 1);
    rect.pivot = new Vector2(0.5f, 1);
    rect.sizeDelta = new Vector2(0, itemHeight);

    rect.anchoredPosition = new Vector2(0, -index * (itemHeight + itemSpacing) - 20);

    // Background
    GameObject bgGO = new GameObject("Background");
    bgGO.transform.SetParent(rect, false);

    RectTransform bgRect = bgGO.AddComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.sizeDelta = new Vector2(-20, -10);
    bgRect.anchoredPosition = Vector2.zero;

    Image bgImage = bgGO.AddComponent<Image>();
    // Use mystery color if mystery mode is enabled and evidence not found
    Color bgColor = enableMysteryMode && !evidenceFound[index] ?
                   new Color(mysteryColor.r, mysteryColor.g, mysteryColor.b, 0.2f) :
                   new Color(notFoundColor.r, notFoundColor.g, notFoundColor.b, 0.2f);
    bgImage.color = bgColor;
    checklistImages.Add(bgImage);

    // Text
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(rect, false);

    RectTransform textRect = textGO.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = new Vector2(-30, -5);
    textRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();

    // Set initial text based on mystery mode
    string displayText = GetDisplayTextForEvidence(index);
    tmpText.text = displayText;

    tmpText.color = enableMysteryMode && !evidenceFound[index] ? mysteryColor : Color.white;
    tmpText.fontSize = 24;
    tmpText.alignment = TextAlignmentOptions.Left;
    tmpText.outlineWidth = 0.2f;
    tmpText.outlineColor = new Color(0, 0, 0, 0.5f);
    checklistTexts.Add(tmpText);
  }

  private string GetDisplayTextForEvidence(int index)
  {
    bool found = evidenceFound[index];

    if (enableMysteryMode && !found)
    {
      // Show mystery symbol for unfound evidence
      return $"{mysteryCheckSymbol} {mysterySymbol}";
    }
    else if (found)
    {
      // Show actual name for found evidence
      return $"{checkmarkSymbol} {evidenceNames[index]}";
    }
    else
    {
      // Show actual name with unchecked symbol (non-mystery mode)
      return $"{uncheckSymbol} {evidenceNames[index]}";
    }
  }

  private void UpdateChecklistTransform()
  {
    if (checklistInstance != null && attachPoint != null)
    {
      checklistInstance.transform.position = attachPoint.position + attachPoint.TransformDirection(positionOffset);
      checklistInstance.transform.rotation = attachPoint.rotation * Quaternion.Euler(rotationOffset);
      checklistInstance.transform.localScale = Vector3.one * checklistScale;
    }
  }

  public void ToggleChecklist()
  {
    if (instance != this) return;

    isChecklistVisible = !isChecklistVisible;
    if (checklistInstance != null)
    {
      checklistInstance.SetActive(isChecklistVisible);
      if (isChecklistVisible)
      {
        UpdateChecklistTransform();
        UpdateChecklistUI();
      }
      else
      {
        checklistInstance.transform.position = initialSpawnPosition;
      }
    }

    // Notify ToolSpawner of visibility change
    NotifyToolSpawnerOfVisibilityChange();
  }

  private void CheckForNewlyFoundEvidence()
  {
    if (cameraScript == null) return;

    // Get markers from camera script - now returns Dictionary<GameObject, GameObject>
    var markerByObject = cameraScript.GetMarkerByObject();

    if (markerByObject != null && markerByObject.Count > 0)
    {
      bool foundNew = false;
      foreach (GameObject evidenceObject in markerByObject.Keys)
      {
        if (evidenceObject != null)
        {
          string evidenceName = GetEvidenceDisplayName(evidenceObject);
          if (TryMarkEvidence(evidenceName))
          {
            foundNew = true;
          }
        }
      }

      if (foundNew && isChecklistVisible)
      {
        UpdateChecklistUI();
      }
    }
  }

  private bool TryMarkEvidence(string evidenceName)
  {
    // Direct match
    if (evidenceNameToIndex.TryGetValue(evidenceName, out int index) && !evidenceFound[index])
    {
      evidenceFound[index] = true;
      return true;
    }

    // Partial match
    foreach (string targetName in evidenceNames)
    {
      if ((evidenceName.Contains(targetName) || targetName.Contains(evidenceName)) &&
          evidenceNameToIndex.TryGetValue(targetName, out int targetIndex) && !evidenceFound[targetIndex])
      {
        evidenceFound[targetIndex] = true;
        return true;
      }
    }

    // Special case matching
    string cleanEvidence = evidenceName.ToLower().Replace(" ", "").Replace("_", "");
    foreach (string targetName in evidenceNames)
    {
      string cleanTarget = targetName.ToLower().Replace(" ", "").Replace("_", "");
      if (cleanEvidence.Contains(cleanTarget) || cleanTarget.Contains(cleanEvidence))
      {
        if (evidenceNameToIndex.TryGetValue(targetName, out int targetIndex) && !evidenceFound[targetIndex])
        {
          evidenceFound[targetIndex] = true;
          return true;
        }
      }
    }

    return false;
  }

  private void UpdateChecklistUI()
  {
    if (checklistTexts.Count != evidenceNames.Count || checklistInstance == null) return;

    int foundCount = 0;
    foreach (bool found in evidenceFound) if (found) foundCount++;

    // Update title
    Transform titleTransform = checklistInstance.transform.Find("Title");
    if (titleTransform?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI titleText)
    {
      string titlePrefix = enableMysteryMode ? "EVIDENCE FOUND" : "EVIDENCE";
      titleText.text = $"{titlePrefix}: {foundCount}/{evidenceNames.Count}";
      titleText.color = foundCount == evidenceNames.Count && foundCount > 0 ?
          new Color(0.2f, 1f, 0.2f, 1f) : (foundCount > 0 ? new Color(1f, 1f, 0.2f, 1f) : Color.white);
    }

    // Update items
    for (int i = 0; i < evidenceNames.Count && i < checklistTexts.Count; i++)
    {
      bool found = evidenceFound[i];

      // Update text based on mystery mode and found status
      checklistTexts[i].text = GetDisplayTextForEvidence(i);

      if (found)
      {
        checklistTexts[i].fontStyle = FontStyles.Bold;
        checklistTexts[i].color = new Color(0.2f, 1f, 0.2f, 1f);
      }
      else if (enableMysteryMode)
      {
        checklistTexts[i].fontStyle = FontStyles.Normal;
        checklistTexts[i].color = mysteryColor;
      }
      else
      {
        checklistTexts[i].fontStyle = FontStyles.Normal;
        checklistTexts[i].color = Color.white;
      }

      if (i < checklistImages.Count)
      {
        if (found)
        {
          checklistImages[i].color = new Color(foundColor.r, foundColor.g, foundColor.b, 0.4f);
        }
        else if (enableMysteryMode)
        {
          checklistImages[i].color = new Color(mysteryColor.r, mysteryColor.g, mysteryColor.b, 0.2f);
        }
        else
        {
          checklistImages[i].color = new Color(notFoundColor.r, notFoundColor.g, notFoundColor.b, 0.1f);
        }
      }
    }

    HandleCompletionMessage(foundCount);
  }

  private void HandleCompletionMessage(int foundCount)
  {
    Transform completionTransform = checklistInstance.transform.Find("CompletionMessage");

    if (foundCount == evidenceNames.Count && foundCount > 0)
    {
      if (completionTransform == null)
      {
        GameObject completionGO = new GameObject("CompletionMessage");
        completionGO.transform.SetParent(checklistInstance.transform, false);

        RectTransform completionRect = completionGO.AddComponent<RectTransform>();
        completionRect.anchorMin = new Vector2(0, 0);
        completionRect.anchorMax = new Vector2(1, 0);
        completionRect.sizeDelta = new Vector2(0, 70);
        completionRect.anchoredPosition = new Vector2(0, 20);

        TextMeshProUGUI completionText = completionGO.AddComponent<TextMeshProUGUI>();
        completionText.text = enableMysteryMode ? "ALL EVIDENCE DISCOVERED!" : "ALL EVIDENCE FOUND!";
        completionText.color = new Color(0.2f, 1f, 0.2f, 1f);
        completionText.fontSize = 32;
        completionText.fontStyle = FontStyles.Bold;
        completionText.alignment = TextAlignmentOptions.Center;
        completionText.outlineWidth = 0.2f;
        completionText.outlineColor = new Color(0, 0, 0, 0.7f);
      }
    }
    else if (completionTransform != null)
    {
      Destroy(completionTransform.gameObject);
    }
  }

  private bool CheckVRButtonPressed()
  {
    if (!UnityEngine.XR.XRSettings.isDeviceActive) return false;

    try
    {
      var inputDevices = new List<UnityEngine.XR.InputDevice>();
      UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
          UnityEngine.XR.InputDeviceCharacteristics.Left | UnityEngine.XR.InputDeviceCharacteristics.Controller,
          inputDevices);

      foreach (var device in inputDevices)
      {
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
          return true;
      }
    }
    catch (System.Exception) { }
    return false;
  }

  // Public API for Scene Generator Integration
  public void OnSceneGenerated()
  {
    if (instance != this) return;

    RefreshEvidenceList();
  }

  public void MarkEvidenceAsFound(string evidenceName)
  {
    if (instance != this) return;

    if (TryMarkEvidence(evidenceName))
    {
      if (isChecklistVisible) UpdateChecklistUI();
    }
  }

  public bool IsAllEvidenceFound() => evidenceFound.TrueForAll(found => found);
  public int GetFoundEvidenceCount() => evidenceFound.FindAll(found => found).Count;
  public int GetTotalEvidenceCount() => evidenceNames.Count;

  // Public API for cross-script communication
  public bool IsChecklistVisible() => isChecklistVisible;

  public bool IsUsingVRControls()
  {
    // Evidence checklist doesn't have complex VR navigation like ToolSpawner
    // but we can check if it's visible as an indicator
    return isChecklistVisible;
  }

  public void ForceCloseChecklist()
  {
    if (isChecklistVisible)
    {
      ToggleChecklist();
    }
  }

  public void SetToolSpawnerReference(ToolSpawner spawner)
  {
    toolSpawner = spawner;
  }

  public void ResetChecklist()
  {
    if (instance != this) return;

    for (int i = 0; i < evidenceFound.Count; i++) evidenceFound[i] = false;
    if (isChecklistVisible) UpdateChecklistUI();
  }

  // Mystery Mode Controls
  public void ToggleMysteryMode()
  {
    if (instance != this) return;

    enableMysteryMode = !enableMysteryMode;
    if (isChecklistVisible) UpdateChecklistUI();
  }
}