using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
  [Header("Camera Settings")]
  [SerializeField] private Camera secondCamera;
  [SerializeField] private RenderTexture renderTexture;
  [SerializeField] private Material screenMaterial;
  [SerializeField] private int textureWidth = 128;
  [SerializeField] private int textureHeight = 105;

  [Header("Photo Capture")]
  [SerializeField] private Light flashLight;
  [SerializeField] private AudioSource cameraShutterSound;
  [SerializeField] private float flashDuration = 0.1f;

  [Header("Evidence Detection")]
  [SerializeField] private float maxDetectionDistance = 10f;
  [SerializeField] private float detectionFOVNarrow = 0.3f; // Narrow FOV for detection (0-1 range, 0.5 = middle 50% of screen)
  [SerializeField] private LayerMask evidenceLayerMask = -1; // Layer mask for raycast detection

  [Header("Real-Time Detection")]
  [SerializeField] private bool enableRealTimeDetection = true;
  [SerializeField] private float evidenceRefreshRate = 1f; // How often to scan for new evidence
  [SerializeField] private bool debugRealTimeUpdates = true;

  [Header("Scene Generator Integration")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;

  [Header("Marker Settings")]
  [SerializeField] private GameObject markerPrefab; // Marker prefab
  [SerializeField] private float markerSize = 0.3f; // Size of the marker
  [SerializeField] private float markerOffset = 0.2f; // Offset from evidence
  [SerializeField] private float markerHeight = 0.1f; // Height above ground
  [SerializeField] private float scaleDuration = 1.0f; // Duration of scale animation
  [SerializeField] private bool placeOnlyOneMarker = true; // Whether to place only one marker at a time

  [Header("Ground Settings")]
  [SerializeField] private Transform groundObject; // Reference to ground object
  [SerializeField] private bool useGroundHeight = false; // Toggle for using ground height
  [SerializeField] private float groundHeightOffset = 0.1f; // Height above ground level

  [Header("Movement Detection")]
  [SerializeField] private float movementThreshold = 0.00001f;
  [SerializeField] private float listenDuration = 5.0f;
  [SerializeField] private float triggerDebounceTime = 0.5f;

  // State tracking
  private bool isTakingPhoto = false;
  private bool isMoving = false;
  private bool canTakePicture = false;
  private Vector3 lastPosition;
  private Quaternion lastRotation;
  private float lastMovementTime = 0f;
  private float lastPhotoTime = 0f;

  // Evidence tracking - now using actual GameObjects from scene generator
  private List<GameObject> evidenceObjects = new List<GameObject>();
  private List<GameObject> markers = new List<GameObject>();
  private Dictionary<GameObject, GameObject> markerByObject = new Dictionary<GameObject, GameObject>();
  private Dictionary<GameObject, string> evidenceObjectNames = new Dictionary<GameObject, string>();

  // Real-time tracking
  private int lastKnownEvidenceCount = 0;
  private HashSet<string> lastKnownEvidenceNames = new HashSet<string>();

  // Special trigger detection for Oculus
  private bool wasButtonPressed = false;

  void Start()
  {
    // Initialize position tracking
    lastPosition = transform.position;
    lastRotation = transform.rotation;

    // Find scene generator if auto-find is enabled
    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator != null)
      {
        Debug.Log("CameraScript: Found CrimeSceneGenerator automatically");
      }
      else
      {
        Debug.LogWarning("CameraScript: No CrimeSceneGenerator found! Evidence detection will be limited.");
      }
    }

    SetupCamera();
    SetupRenderTexture();
    FindFlashLight();
    FindGroundObject();
    CreateDefaultMarkerPrefab();

    // Initialize evidence detection
    PopulateEvidenceObjects();

    // Start real-time monitoring
    if (enableRealTimeDetection)
    {
      StartCoroutine(RealTimeEvidenceMonitoring());
    }
  }

  void SetupCamera()
  {
    if (secondCamera == null)
    {
      secondCamera = GetComponentInChildren<Camera>();
      if (secondCamera == null)
      {
        Transform cameraTransform = transform.Find("Camera");
        if (cameraTransform != null)
        {
          secondCamera = cameraTransform.GetComponent<Camera>();
          Debug.Log("Found camera in child: " + cameraTransform.name);
        }
      }
      if (secondCamera == null)
      {
        secondCamera = GetComponent<Camera>();
      }
    }

    if (secondCamera == null)
    {
      Debug.LogError("CameraScript: No camera found! Cannot operate without a camera.");
      enabled = false;
      return;
    }
  }

  void SetupRenderTexture()
  {
    if (renderTexture == null)
    {
      renderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
      renderTexture.Create();
    }

    secondCamera.targetTexture = renderTexture;

    if (screenMaterial != null)
    {
      screenMaterial.mainTexture = renderTexture;
    }
  }

  void FindFlashLight()
  {
    if (flashLight == null)
    {
      Transform flashTransform = transform.Find("flash");
      if (flashTransform != null)
      {
        flashLight = flashTransform.GetComponent<Light>();
      }
    }
  }

  void FindGroundObject()
  {
    if (groundObject == null && useGroundHeight)
    {
      GameObject groundObj = GameObject.Find("Ground");
      if (groundObj != null)
      {
        groundObject = groundObj.transform;
        Debug.Log("Found ground object: " + groundObj.name);
      }
      else
      {
        string[] groundNames = new string[] { "Ground", "Floor", "Terrain", "Plane" };
        foreach (string name in groundNames)
        {
          GameObject obj = GameObject.Find(name);
          if (obj != null)
          {
            groundObject = obj.transform;
            Debug.Log("Found potential ground object: " + obj.name);
            break;
          }
        }
      }

      if (groundObject == null)
      {
        Debug.LogWarning("No ground object found. Will use evidence position + markerHeight instead.");
      }
    }
  }

  void CreateDefaultMarkerPrefab()
  {
    if (markerPrefab == null)
    {
      GameObject markerObj = new GameObject("MarkerPrefab");

      GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cubeObj.transform.SetParent(markerObj.transform);
      cubeObj.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
      cubeObj.transform.localPosition = new Vector3(0, 0.05f, 0);

      Destroy(cubeObj.GetComponent<Collider>());

      MeshRenderer renderer = cubeObj.GetComponent<MeshRenderer>();
      Material mat = new Material(Shader.Find("Standard"));
      mat.color = Color.yellow;
      mat.SetFloat("_Glossiness", 0.0f);
      renderer.material = mat;

      markerPrefab = markerObj;
      markerObj.SetActive(false);
    }
  }

  // REAL-TIME EVIDENCE MONITORING COROUTINE
  IEnumerator RealTimeEvidenceMonitoring()
  {
    while (enableRealTimeDetection)
    {
      yield return new WaitForSeconds(evidenceRefreshRate);

      // Check if evidence in scene has changed
      bool evidenceChanged = CheckForEvidenceChanges();

      if (evidenceChanged)
      {
        if (debugRealTimeUpdates)
        {
          Debug.Log("🔄 CameraScript: Evidence changed in scene - refreshing detection list");
        }

        PopulateEvidenceObjects();

        // Notify other systems if needed
        NotifyEvidenceListChanged();
      }
    }
  }

  bool CheckForEvidenceChanges()
  {
    // Get current evidence count from scene generator
    int currentCount = 0;
    if (sceneGenerator != null)
    {
      currentCount = sceneGenerator.GetCurrentEvidenceCount();
    }
    else
    {
      // Fallback to tag-based counting
      GameObject[] taggedEvidence = GameObject.FindGameObjectsWithTag("Evidence");
      currentCount = taggedEvidence.Length;
    }

    // Check if count changed
    if (currentCount != lastKnownEvidenceCount)
    {
      lastKnownEvidenceCount = currentCount;
      return true;
    }

    // Check if actual objects changed (some might have been destroyed/created)
    if (evidenceObjects.RemoveAll(obj => obj == null) > 0)
    {
      return true;
    }

    // Check if evidence names changed
    if (sceneGenerator != null)
    {
      try
      {
        string[] currentNames = sceneGenerator.GetCurrentSceneEvidenceNames();
        HashSet<string> currentNameSet = new HashSet<string>(currentNames ?? new string[0]);

        if (!currentNameSet.SetEquals(lastKnownEvidenceNames))
        {
          lastKnownEvidenceNames = currentNameSet;
          return true;
        }
      }
      catch (System.Exception e)
      {
        Debug.LogWarning($"CameraScript: Error checking evidence names: {e.Message}");
      }
    }

    return false;
  }

  void NotifyEvidenceListChanged()
  {
    // Find and notify evidence checklist
    EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
    if (checklist != null)
    {
      checklist.RefreshEvidenceList();
      if (debugRealTimeUpdates)
      {
        Debug.Log("🔔 CameraScript: Notified EvidenceChecklist of changes");
      }
    }
  }

  void PopulateEvidenceObjects()
  {
    evidenceObjects.Clear();
    evidenceObjectNames.Clear();

    // Primary method: Get actual spawned objects from Scene Generator
    if (sceneGenerator != null)
    {
      GetEvidenceFromSceneGenerator();
    }

    // Fallback method: Scan by Evidence tag
    if (evidenceObjects.Count == 0)
    {
      ScanEvidenceByTag();
    }

    if (debugRealTimeUpdates)
    {
      Debug.Log($"🎯 CameraScript: Found {evidenceObjects.Count} evidence objects");
      foreach (GameObject obj in evidenceObjects)
      {
        if (obj != null)
        {
          string displayName = evidenceObjectNames.ContainsKey(obj) ? evidenceObjectNames[obj] : obj.name;
          Debug.Log($"  📍 {displayName} ({obj.name}) at {obj.transform.position}");
        }
      }
    }
  }

  void GetEvidenceFromSceneGenerator()
  {
    try
    {
      GameObject[] generatorEvidence = sceneGenerator.GetCurrentSceneEvidenceObjects();
      string[] generatorEvidenceNames = sceneGenerator.GetCurrentSceneEvidenceNames();

      if (generatorEvidence != null && generatorEvidence.Length > 0)
      {
        for (int i = 0; i < generatorEvidence.Length; i++)
        {
          GameObject evidenceObj = generatorEvidence[i];
          if (evidenceObj != null && evidenceObj.activeInHierarchy)
          {
            evidenceObjects.Add(evidenceObj);

            // Map the spawned object to its proper display name from the generator
            if (generatorEvidenceNames != null && i < generatorEvidenceNames.Length)
            {
              evidenceObjectNames[evidenceObj] = generatorEvidenceNames[i];
            }
            else
            {
              // Fallback to cleaning up the object name
              evidenceObjectNames[evidenceObj] = CleanObjectName(evidenceObj.name);
            }
          }
        }

        if (debugRealTimeUpdates)
        {
          Debug.Log($"🎬 CameraScript: Got {evidenceObjects.Count} evidence objects from Scene Generator");
          Debug.Log($"📋 Evidence names: {string.Join(", ", generatorEvidenceNames ?? new string[0])}");
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"CameraScript: Error getting evidence objects from scene generator: {e.Message}");
    }
  }

  void ScanEvidenceByTag()
  {
    GameObject[] taggedEvidence = GameObject.FindGameObjectsWithTag("Evidence");
    foreach (GameObject obj in taggedEvidence)
    {
      if (obj != null && obj.activeInHierarchy && !evidenceObjects.Contains(obj))
      {
        evidenceObjects.Add(obj);
        // For fallback objects, clean up the name
        evidenceObjectNames[obj] = CleanObjectName(obj.name);
      }
    }

    if (debugRealTimeUpdates && taggedEvidence.Length > 0)
    {
      Debug.Log($"🏷️ CameraScript: Found {taggedEvidence.Length} objects with Evidence tag (fallback method)");
    }
  }

  // Called by CrimeSceneGenerator when a new scene is generated
  public void OnSceneGenerated()
  {
    Debug.Log("🔄 CameraScript: Scene regenerated - updating evidence detection");

    // Clear existing markers
    ClearMarkers();

    // Clear evidence mappings
    evidenceObjectNames.Clear();

    // Force immediate refresh
    PopulateEvidenceObjects();

    // Reset monitoring
    lastKnownEvidenceCount = 0;
    lastKnownEvidenceNames.Clear();

    Debug.Log($"🎯 CameraScript: Updated to track {evidenceObjects.Count} evidence objects");
  }

  // Public method to update evidence list (can be called by other scripts)
  public void UpdateEvidenceList(string[] newEvidenceNames)
  {
    // This method is kept for compatibility but now we get objects directly from scene generator
    Debug.Log($"🔄 CameraScript: Evidence list update requested, refreshing from scene generator instead");
    PopulateEvidenceObjects();
  }

  void Update()
  {
    if (isTakingPhoto) return;

    CheckMovement();
    UpdateCanTakePicture();

    if (canTakePicture)
    {
      if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
      {
        TakePhoto();
      }

      bool triggerPressed = CheckAllTriggerMethods();

      if (triggerPressed && Time.time - lastPhotoTime > triggerDebounceTime)
      {
        TakePhoto();
        lastPhotoTime = Time.time;
      }
    }
  }

  void CheckMovement()
  {
    float positionDelta = Vector3.Distance(transform.position, lastPosition);
    float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

    isMoving = positionDelta > movementThreshold || rotationDelta > movementThreshold * 100;

    lastPosition = transform.position;
    lastRotation = transform.rotation;

    if (isMoving)
    {
      lastMovementTime = Time.time;
    }
  }

  void UpdateCanTakePicture()
  {
    float timeSinceMovement = Time.time - lastMovementTime;
    canTakePicture = timeSinceMovement < listenDuration;
  }

  bool CheckAllTriggerMethods()
  {
    if (UnityEngine.XR.XRSettings.isDeviceActive)
    {
      try
      {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(inputDevices);

        foreach (var device in inputDevices)
        {
          if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerValue) &&
              triggerValue && !wasButtonPressed)
          {
            wasButtonPressed = true;
            return true;
          }
          else if (!triggerValue)
          {
            wasButtonPressed = false;
          }
        }
      }
      catch (System.Exception) { }
    }

    return false;
  }

  void TakePhoto()
  {
    if (isTakingPhoto) return;

    Debug.Log("📸 Taking photo");
    StartCoroutine(TakePhotoSequence());
  }

  IEnumerator TakePhotoSequence()
  {
    isTakingPhoto = true;

    if (flashLight != null)
    {
      flashLight.enabled = true;
    }

    if (cameraShutterSound != null)
    {
      cameraShutterSound.Play();
    }

    CheckEvidenceAndPlaceMarkers();

    yield return new WaitForSeconds(flashDuration);

    if (flashLight != null)
    {
      flashLight.enabled = false;
    }

    isTakingPhoto = false;
  }

  void CheckEvidenceAndPlaceMarkers()
  {
    if (secondCamera == null) return;

    Debug.Log("==== 🔍 CHECKING FOR EVIDENCE IN VIEW ====");

    // Make sure we have the latest evidence list
    PopulateEvidenceObjects();

    Ray ray = secondCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    RaycastHit[] hits = Physics.RaycastAll(ray, maxDetectionDistance, evidenceLayerMask);

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    Debug.Log($"🎯 Raycast found {hits.Length} potential evidence items");
    Debug.Log($"📋 Current evidence list has {evidenceObjects.Count} objects");

    if (hits.Length > 0)
    {
      foreach (RaycastHit hit in hits)
      {
        GameObject rootObject = GetRootEvidenceObject(hit.collider.gameObject);

        if (rootObject != null)
        {
          if (markerByObject.ContainsKey(rootObject))
          {
            Debug.Log($"⏭️ Marker already exists for: {rootObject.name}");
            continue;
          }

          if (IsValidEvidenceObject(rootObject))
          {
            string displayName = GetEvidenceDisplayName(rootObject);
            Debug.Log($"✅ EVIDENCE FOUND: {displayName} ({rootObject.name})");
            Debug.Log($"📍 HIT POINT: {hit.point}");
            Debug.Log($"🎯 COLLIDER: {hit.collider.name}");

            PlaceMarkerBesideCollider(rootObject, hit.collider, hit.point);

            if (placeOnlyOneMarker) break;
          }
          else
          {
            Debug.Log($"❌ Not valid evidence: {rootObject.name}");
          }
        }
      }
    }
    else
    {
      FallbackEvidenceCheck();
    }

    Debug.Log("======================================");
  }

  bool IsValidEvidenceObject(GameObject obj)
  {
    if (obj == null) return false;

    // Check if it's in our evidence objects list (from scene generator)
    if (evidenceObjects.Contains(obj))
    {
      return true;
    }

    // Check if it has Evidence tag
    if (obj.CompareTag("Evidence"))
    {
      return true;
    }

    return false;
  }

  private GameObject GetRootEvidenceObject(GameObject colliderObject)
  {
    GameObject current = colliderObject;

    // First check if the collider object itself is evidence
    if (IsValidEvidenceObject(current))
    {
      return current;
    }

    // Then check parent hierarchy
    Transform parent = current.transform.parent;
    while (parent != null)
    {
      if (IsValidEvidenceObject(parent.gameObject))
      {
        return parent.gameObject;
      }
      parent = parent.parent;
    }

    return null;
  }

  void PlaceMarkerBesideCollider(GameObject evidenceObject, Collider hitCollider, Vector3 hitPoint)
  {
    if (markerPrefab == null)
    {
      Debug.LogError("No marker prefab assigned!");
      return;
    }

    Bounds bounds = hitCollider.bounds;
    Vector3 markerPosition = bounds.center;

    float sideOffset = bounds.extents.x + markerOffset;
    markerPosition.x += sideOffset;

    if (useGroundHeight && groundObject != null)
    {
      markerPosition.y = groundObject.position.y + groundHeightOffset;
    }
    else
    {
      markerPosition.y = bounds.min.y + markerHeight;
    }

    GameObject marker = Instantiate(markerPrefab, markerPosition, Quaternion.identity);
    marker.SetActive(true);
    marker.transform.localScale = Vector3.zero;

    markers.Add(marker);
    markerByObject[evidenceObject] = marker;

    DontDestroyOnLoad(marker);

    StartCoroutine(AnimateMarkerScale(marker.transform));

    string displayName = GetEvidenceDisplayName(evidenceObject);
    Debug.Log($"📍 Placed marker at: {markerPosition} for evidence: {displayName} ({evidenceObject.name})");
  }

  void FallbackEvidenceCheck()
  {
    if (evidenceObjects.Count == 0) return;

    Debug.Log("🔄 Using fallback evidence detection method");

    float xMin = 0.5f - (detectionFOVNarrow / 2);
    float xMax = 0.5f + (detectionFOVNarrow / 2);
    float yMin = 0.5f - (detectionFOVNarrow / 2);
    float yMax = 0.5f + (detectionFOVNarrow / 2);

    GameObject closestEvidence = null;
    float closestDistance = float.MaxValue;

    foreach (GameObject evidence in evidenceObjects)
    {
      if (evidence == null || !evidence.gameObject.activeInHierarchy) continue;

      Vector3 screenPoint = secondCamera.WorldToViewportPoint(evidence.transform.position);
      bool inNarrowView = screenPoint.z > 0 &&
                         screenPoint.x > xMin && screenPoint.x < xMax &&
                         screenPoint.y > yMin && screenPoint.y < yMax;

      if (!inNarrowView) continue;
      if (markerByObject.ContainsKey(evidence)) continue;

      float distance = Vector3.Distance(secondCamera.transform.position, evidence.transform.position);

      if (distance < closestDistance)
      {
        closestDistance = distance;
        closestEvidence = evidence;
      }
    }

    if (closestEvidence != null)
    {
      Vector3 position = closestEvidence.transform.position;
      string displayName = GetEvidenceDisplayName(closestEvidence);
      Debug.Log($"✅ EVIDENCE FOUND (fallback): {displayName} ({closestEvidence.name})");

      Collider collider = closestEvidence.GetComponentInChildren<Collider>();
      if (collider != null)
      {
        PlaceMarkerBesideCollider(closestEvidence, collider, position);
      }
      else
      {
        PlaceMarker(closestEvidence, position);
      }
    }
  }

  void PlaceMarker(GameObject evidence, Vector3 position)
  {
    if (markerPrefab == null)
    {
      Debug.LogError("No marker prefab assigned!");
      return;
    }

    Vector3 markerPosition = position;
    markerPosition.x += markerOffset;

    if (useGroundHeight && groundObject != null)
    {
      markerPosition.y = groundObject.position.y + groundHeightOffset;
    }
    else
    {
      markerPosition.y += markerHeight;
    }

    GameObject marker = Instantiate(markerPrefab, markerPosition, Quaternion.identity);
    marker.SetActive(true);
    marker.transform.localScale = Vector3.zero;

    markers.Add(marker);
    markerByObject[evidence] = marker;

    DontDestroyOnLoad(marker);

    StartCoroutine(AnimateMarkerScale(marker.transform));

    Debug.Log($"📍 Placed marker at: {markerPosition}");
  }

  IEnumerator AnimateMarkerScale(Transform markerTransform)
  {
    if (markerTransform == null) yield break;

    Vector3 targetScale = Vector3.one * markerSize;
    float startTime = Time.time;

    while (Time.time < startTime + scaleDuration && markerTransform != null)
    {
      float t = (Time.time - startTime) / scaleDuration;
      float smoothT = Mathf.SmoothStep(0, 1, t);
      markerTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, smoothT);
      yield return null;
    }

    if (markerTransform != null)
    {
      markerTransform.localScale = targetScale;
    }
  }

  public void ClearMarkers()
  {
    foreach (GameObject marker in markers)
    {
      if (marker != null)
      {
        Destroy(marker);
      }
    }

    markers.Clear();
    markerByObject.Clear();
    Debug.Log("🗑️ CameraScript: All markers cleared");
  }

  // Public API methods
  public int GetEvidenceObjectCount() => evidenceObjects.Count;
  public int GetMarkerCount() => markers.Count;
  public Dictionary<GameObject, GameObject> GetMarkerByObject() => new Dictionary<GameObject, GameObject>(markerByObject);
  public Dictionary<GameObject, string> GetEvidenceObjectNames() => new Dictionary<GameObject, string>(evidenceObjectNames);

  // Get the proper display name for an evidence object
  public string GetEvidenceDisplayName(GameObject evidenceObj)
  {
    if (evidenceObjectNames.ContainsKey(evidenceObj))
    {
      return evidenceObjectNames[evidenceObj];
    }
    return CleanObjectName(evidenceObj.name);
  }

  // Compatibility method for EvidenceChecklist - returns current evidence names
  public string[] GetCurrentTargetNames()
  {
    if (sceneGenerator != null)
    {
      try
      {
        string[] names = sceneGenerator.GetCurrentSceneEvidenceNames();
        return names ?? new string[0];
      }
      catch (System.Exception e)
      {
        Debug.LogWarning($"CameraScript: Error getting evidence names: {e.Message}");
      }
    }

    // Fallback: return unique display names from our current evidence objects
    HashSet<string> uniqueNames = new HashSet<string>();
    foreach (GameObject obj in evidenceObjects)
    {
      if (obj != null)
      {
        uniqueNames.Add(GetEvidenceDisplayName(obj));
      }
    }
    return uniqueNames.ToArray();
  }

  // Get current evidence objects (for EvidenceChecklist compatibility)
  public GameObject[] GetCurrentEvidenceObjects()
  {
    return evidenceObjects.ToArray();
  }

  // Force refresh methods
  [ContextMenu("🔄 Force Refresh Evidence")]
  public void ForceRefreshEvidence()
  {
    PopulateEvidenceObjects();
    Debug.Log($"🔄 Force refreshed - found {evidenceObjects.Count} evidence objects");
  }

  [ContextMenu("🎯 Show Current Evidence")]
  public void ShowCurrentEvidence()
  {
    Debug.Log("=== 📋 CURRENT EVIDENCE STATUS ===");
    Debug.Log($"Evidence Objects: {evidenceObjects.Count}");
    foreach (GameObject item in evidenceObjects)
    {
      if (item != null)
      {
        string displayName = GetEvidenceDisplayName(item);
        Debug.Log($"  - {displayName} ({item.name}) (Active: {item.gameObject.activeInHierarchy})");
      }
      else
      {
        Debug.Log($"  - NULL");
      }
    }
    Debug.Log($"Markers Placed: {markers.Count}");
    Debug.Log("=================================");
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

  void OnDestroy()
  {
    if (renderTexture != null)
    {
      renderTexture.Release();
    }
  }
}