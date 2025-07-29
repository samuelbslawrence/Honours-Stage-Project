using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// - CAMERA SCRIPT MAIN CLASS
public class CameraScript : MonoBehaviour
{
  // - INSPECTOR CONFIGURATION
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
  [SerializeField] private float detectionFOVNarrow = 60;
  [SerializeField] private LayerMask evidenceLayerMask = -1;

  [Header("Fingerprint Detection - SIMPLIFIED")]
  [SerializeField] private DustBrush dustBrush;
  [SerializeField] private bool autoFindDustBrush = true;
  [SerializeField] private float fingerprintRevealThreshold = 50f;
  [SerializeField] private Color photographedFingerprintColor = Color.white;

  [Header("Real-Time Detection")]
  [SerializeField] private bool enableRealTimeDetection = true;
  [SerializeField] private float evidenceRefreshRate = 1f;

  [Header("Scene Generator Integration")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;

  [Header("Marker Settings")]
  [SerializeField] private GameObject markerPrefab;
  [SerializeField] private float markerSize = 0.3f;
  [SerializeField] private float markerOffset = 0.2f;
  [SerializeField] private float markerHeight = 0.1f;
  [SerializeField] private float scaleDuration = 1.0f;
  [SerializeField] private bool placeOnlyOneMarker = false;
  [SerializeField] private bool placeMarkersOnFingerprints = true;

  [Header("Ground Settings")]
  [SerializeField] private Transform groundObject;
  [SerializeField] private bool useGroundHeight = false;
  [SerializeField] private float groundHeightOffset = 0.1f;

  [Header("Movement Detection")]
  [SerializeField] private float movementThreshold = 0.00001f;
  [SerializeField] private float listenDuration = 5.0f;
  [SerializeField] private float triggerDebounceTime = 0.5f;

  [Header("Manual Evidence List")]
  [SerializeField] private GameObject[] manualEvidenceObjects;
  [SerializeField] private bool useManualEvidenceList = false;

  // - PRIVATE STATE VARIABLES
  // Movement and photo state tracking
  private bool isTakingPhoto = false;
  private bool isMoving = false;
  private bool canTakePicture = false;
  private Vector3 lastPosition;
  private Quaternion lastRotation;
  private float lastMovementTime = 0f;
  private float lastPhotoTime = 0f;

  // Evidence and marker tracking collections
  private List<GameObject> evidenceObjects = new List<GameObject>();
  private List<GameObject> markers = new List<GameObject>();
  private Dictionary<GameObject, GameObject> markerByObject = new Dictionary<GameObject, GameObject>();
  private Dictionary<GameObject, string> evidenceObjectNames = new Dictionary<GameObject, string>();

  // Fingerprint photography tracking
  private HashSet<GameObject> photographedFingerprints = new HashSet<GameObject>();
  private Dictionary<GameObject, Material> fingerprintOriginalMaterials = new Dictionary<GameObject, Material>();

  // Real-time monitoring state
  private int lastKnownEvidenceCount = 0;
  private HashSet<string> lastKnownEvidenceNames = new HashSet<string>();

  // VR input handling
  private bool wasButtonPressed = false;

  // - UNITY LIFECYCLE METHODS
  void Start()
  {
    // Initialize position and rotation tracking
    lastPosition = transform.position;
    lastRotation = transform.rotation;

    // Setup all camera components and dependencies
    FindDustBrush();
    InitializeSceneGenerator();
    SetupCamera();
    SetupRenderTexture();
    FindFlashLight();
    FindGroundObject();
    CreateDefaultMarkerPrefab();

    // Populate initial evidence list
    PopulateEvidenceObjects();

    // Start real-time monitoring if enabled
    if (enableRealTimeDetection)
    {
      StartCoroutine(RealTimeEvidenceMonitoring());
    }
  }

  void Update()
  {
    if (isTakingPhoto) return;

    // Update movement and photo state
    CheckMovement();
    UpdateCanTakePicture();

    // Handle photo input
    if (canTakePicture)
    {
      // Keyboard and mouse input
      if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
      {
        TakePhoto();
      }

      // VR trigger input
      bool triggerPressed = CheckAllTriggerMethods();
      if (triggerPressed && Time.time - lastPhotoTime > triggerDebounceTime)
      {
        TakePhoto();
        lastPhotoTime = Time.time;
      }
    }
  }

  // - COMPONENT SETUP
  void FindDustBrush()
  {
    if (autoFindDustBrush && dustBrush == null)
    {
      dustBrush = FindObjectOfType<DustBrush>();
      if (dustBrush == null)
      {
        Debug.LogWarning("CameraScript: No DustBrush found! Fingerprint detection will be limited.");
      }
    }
  }

  void InitializeSceneGenerator()
  {
    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator == null)
      {
        Debug.LogWarning("CameraScript: No CrimeSceneGenerator found! Evidence detection will be limited.");
      }
    }
  }

  void SetupCamera()
  {
    if (secondCamera == null)
    {
      // Try to find camera in children first
      secondCamera = GetComponentInChildren<Camera>();
      if (secondCamera == null)
      {
        // Look for specific camera child
        Transform cameraTransform = transform.Find("Camera");
        if (cameraTransform != null)
        {
          secondCamera = cameraTransform.GetComponent<Camera>();
        }
      }
      // Fallback to camera on same object
      if (secondCamera == null)
      {
        secondCamera = GetComponent<Camera>();
      }
    }

    // Validate camera was found
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
      }
      else
      {
        // Try common ground object names
        string[] groundNames = new string[] { "Ground", "Floor", "Terrain", "Plane" };
        foreach (string name in groundNames)
        {
          GameObject obj = GameObject.Find(name);
          if (obj != null)
          {
            groundObject = obj.transform;
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

      // Create yellow cube marker
      GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cubeObj.transform.SetParent(markerObj.transform);
      cubeObj.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
      cubeObj.transform.localPosition = new Vector3(0, 0.05f, 0);

      // Remove collider to prevent interference
      Destroy(cubeObj.GetComponent<Collider>());

      // Setup yellow material
      MeshRenderer renderer = cubeObj.GetComponent<MeshRenderer>();
      Material mat = new Material(Shader.Find("Standard"));
      mat.color = Color.yellow;
      mat.SetFloat("_Glossiness", 0.0f);
      renderer.material = mat;

      markerPrefab = markerObj;
      markerObj.SetActive(false);
    }
  }

  // - REAL-TIME MONITORING
  IEnumerator RealTimeEvidenceMonitoring()
  {
    while (enableRealTimeDetection)
    {
      yield return new WaitForSeconds(evidenceRefreshRate);

      bool evidenceChanged = CheckForEvidenceChanges();

      if (evidenceChanged)
      {
        PopulateEvidenceObjects();
        NotifyEvidenceListChanged();
      }
    }
  }

  bool CheckForEvidenceChanges()
  {
    int currentCount = 0;

    // Get current evidence count based on active source
    if (useManualEvidenceList && manualEvidenceObjects != null)
    {
      currentCount = manualEvidenceObjects.Count(obj => obj != null && obj.activeInHierarchy);
    }
    else if (sceneGenerator != null)
    {
      currentCount = sceneGenerator.GetCurrentEvidenceCount();
    }
    else
    {
      GameObject[] taggedEvidence = GameObject.FindGameObjectsWithTag("Evidence");
      currentCount = taggedEvidence.Length;
    }

    // Check for count changes
    if (currentCount != lastKnownEvidenceCount)
    {
      lastKnownEvidenceCount = currentCount;
      return true;
    }

    // Remove null objects and check if any were removed
    if (evidenceObjects.RemoveAll(obj => obj == null) > 0)
    {
      return true;
    }

    // Check for name changes when using scene generator
    if (!useManualEvidenceList && sceneGenerator != null)
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
        Debug.LogWarning("CameraScript: Error checking evidence names: " + e.Message);
      }
    }

    return false;
  }

  void NotifyEvidenceListChanged()
  {
    EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
    if (checklist != null)
    {
      checklist.RefreshEvidenceList();
    }
  }

  // - EVIDENCE DETECTION SYSTEM
  void PopulateEvidenceObjects()
  {
    evidenceObjects.Clear();
    evidenceObjectNames.Clear();

    // Use appropriate evidence source
    if (useManualEvidenceList && manualEvidenceObjects != null)
    {
      GetEvidenceFromManualList();
    }
    else if (sceneGenerator != null)
    {
      GetEvidenceFromSceneGenerator();
    }
    else
    {
      ScanEvidenceByTag();
    }
  }

  void GetEvidenceFromManualList()
  {
    if (manualEvidenceObjects == null) return;

    foreach (GameObject evidenceObj in manualEvidenceObjects)
    {
      if (evidenceObj != null && evidenceObj.activeInHierarchy)
      {
        evidenceObjects.Add(evidenceObj);
        evidenceObjectNames[evidenceObj] = CleanObjectName(evidenceObj.name);
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

            // Use generator names if available, otherwise clean object name
            if (generatorEvidenceNames != null && i < generatorEvidenceNames.Length)
            {
              evidenceObjectNames[evidenceObj] = generatorEvidenceNames[i];
            }
            else
            {
              evidenceObjectNames[evidenceObj] = CleanObjectName(evidenceObj.name);
            }
          }
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning("CameraScript: Error getting evidence objects from scene generator: " + e.Message);
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
        evidenceObjectNames[obj] = CleanObjectName(obj.name);
      }
    }
  }

  // - PUBLIC EVENT HANDLERS
  public void OnSceneGenerated()
  {
    // Clear existing state
    ClearMarkers();
    evidenceObjectNames.Clear();

    // Reset fingerprint tracking
    photographedFingerprints.Clear();
    fingerprintOriginalMaterials.Clear();

    // Refresh evidence detection
    PopulateEvidenceObjects();

    // Reset monitoring state
    lastKnownEvidenceCount = 0;
    lastKnownEvidenceNames.Clear();

    // Notify evidence checklist
    EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
    if (checklist != null)
    {
      checklist.OnSceneGenerated();
    }
  }

  public void UpdateEvidenceList(string[] newEvidenceNames)
  {
    PopulateEvidenceObjects();
  }

  // - MOVEMENT DETECTION
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

  // - INPUT HANDLING
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

  // - PHOTO CAPTURE SYSTEM
  void TakePhoto()
  {
    if (isTakingPhoto) return;

    StartCoroutine(TakePhotoSequence());
  }

  IEnumerator TakePhotoSequence()
  {
    isTakingPhoto = true;

    // Enable flash
    if (flashLight != null)
    {
      flashLight.enabled = true;
    }

    // Play shutter sound
    if (cameraShutterSound != null)
    {
      cameraShutterSound.Play();
    }

    // Process evidence detection and marker placement
    CheckEvidenceAndPlaceMarkers();

    // Wait for flash duration
    yield return new WaitForSeconds(flashDuration);

    // Disable flash
    if (flashLight != null)
    {
      flashLight.enabled = false;
    }

    isTakingPhoto = false;
  }

  // - EVIDENCE DETECTION AND MARKER PLACEMENT
  void CheckEvidenceAndPlaceMarkers()
  {
    if (secondCamera == null) return;

    // Refresh evidence list
    PopulateEvidenceObjects();

    bool evidencePhotographedInThisShot = false;
    HashSet<GameObject> processedObjects = new HashSet<GameObject>();

    // Cast multiple rays for much wider detection FOV
    List<RaycastHit> allHits = new List<RaycastHit>();

    // Center ray (original behavior)
    Ray centerRay = secondCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    RaycastHit[] centerHits = Physics.RaycastAll(centerRay, maxDetectionDistance, evidenceLayerMask);
    allHits.AddRange(centerHits);

    // Much denser ray grid for maximum sensitivity
    int raysPerAxis = 7;
    float fovMultiplier = 3.0f;
    float expandedFOV = detectionFOVNarrow * fovMultiplier;

    // Main grid pattern
    for (int x = 0; x < raysPerAxis; x++)
    {
      for (int y = 0; y < raysPerAxis; y++)
      {
        // Skip center ray (already cast)
        if (x == raysPerAxis / 2 && y == raysPerAxis / 2) continue;

        // Calculate viewport coordinates with much wider spread
        float normalizedX = (float)x / (raysPerAxis - 1);
        float normalizedY = (float)y / (raysPerAxis - 1);

        // Expand from center with the wider FOV
        float viewportX = 0.5f + (normalizedX - 0.5f) * (expandedFOV / 60f);
        float viewportY = 0.5f + (normalizedY - 0.5f) * (expandedFOV / 60f);

        Ray ray = secondCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDetectionDistance, evidenceLayerMask);
        allHits.AddRange(hits);
      }
    }

    // Add circular pattern for even better coverage around edges
    int circleRays = 16;
    float circleRadius = expandedFOV / 80f;

    for (int i = 0; i < circleRays; i++)
    {
      float angle = (float)i / circleRays * Mathf.PI * 2f;
      float viewportX = 0.5f + Mathf.Cos(angle) * circleRadius;
      float viewportY = 0.5f + Mathf.Sin(angle) * circleRadius;

      Ray ray = secondCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0));
      RaycastHit[] hits = Physics.RaycastAll(ray, maxDetectionDistance, evidenceLayerMask);
      allHits.AddRange(hits);
    }

    // Add diagonal rays for corner coverage
    float[] diagonalOffsets = { 0.3f, 0.6f, 0.9f, 1.2f };
    Vector2[] diagonalDirections = {
        new Vector2(1, 1), new Vector2(-1, 1),
        new Vector2(1, -1), new Vector2(-1, -1)
    };

    foreach (float offset in diagonalOffsets)
    {
      foreach (Vector2 direction in diagonalDirections)
      {
        float viewportX = 0.5f + direction.x * offset * (expandedFOV / 100f);
        float viewportY = 0.5f + direction.y * offset * (expandedFOV / 100f);

        Ray ray = secondCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDetectionDistance, evidenceLayerMask);
        allHits.AddRange(hits);
      }
    }

    // Sort all hits by distance for consistent processing
    allHits.Sort((a, b) => a.distance.CompareTo(b.distance));

    // Process all raycast hits
    foreach (RaycastHit hit in allHits)
    {
      GameObject obj = hit.collider.gameObject;
      GameObject rootObject = GetRootEvidenceObject(obj);

      if (rootObject != null && !processedObjects.Contains(rootObject))
      {
        processedObjects.Add(rootObject);

        // Check if valid evidence (including fingerprints)
        if (IsValidEvidenceObject(rootObject))
        {
          bool isFingerprint = IsStrictValidFingerprint(rootObject);
          if (isFingerprint)
          {
            HandleFingerprintPhotography(rootObject, hit.collider, hit.point);
            evidencePhotographedInThisShot = true;
          }
          else
          {
            // Handle non-fingerprint evidence
            if (!markerByObject.ContainsKey(rootObject) && (!placeOnlyOneMarker || markerByObject.Count == 0))
            {
              PlaceMarkerBesideCollider(rootObject, hit.collider, hit.point);
              EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
              if (checklist != null)
              {
                checklist.MarkEvidenceAsPhotographed(GetEvidenceDisplayName(rootObject));
              }
              evidencePhotographedInThisShot = true;
            }
          }
        }
      }
    }

    // Fallback detection if nothing was found
    if (!evidencePhotographedInThisShot)
    {
      FallbackEvidenceCheck();
    }
  }

  // - FINGERPRINT DETECTION SYSTEM
  private bool IsStrictValidFingerprint(GameObject obj)
  {
    if (obj == null) return false;
    bool hasCorrectTag = obj.CompareTag("Fingerprint");
    int uvLayerIndex = LayerMask.NameToLayer("UV");
    bool onCorrectLayer = uvLayerIndex != -1 && obj.layer == uvLayerIndex;
    string objName = obj.name;
    bool hasCorrectName = objName == "Fingerprint" || (objName.StartsWith("Fingerprint (") && objName.EndsWith(")"));
    bool isValid = hasCorrectTag && onCorrectLayer && hasCorrectName;
    return isValid;
  }

  void HandleFingerprintPhotography(GameObject fingerprint, Collider hitCollider, Vector3 hitPoint)
  {
    // Validate fingerprint meets strict criteria
    if (!IsStrictValidFingerprint(fingerprint))
    {
      return;
    }

    // Check reveal percentage through dust brush
    if (dustBrush != null)
    {
      float revealPercentage = dustBrush.GetFingerprintRevealPercentage(fingerprint);
      // Process if sufficiently revealed and not already photographed
      if (revealPercentage >= fingerprintRevealThreshold && !photographedFingerprints.Contains(fingerprint))
      {
        photographedFingerprints.Add(fingerprint);
        TurnFingerprintGreen(fingerprint);
        // Notify evidence checklist
        EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
        if (checklist != null)
        {
          checklist.MarkFingerprintAsPhotographedPublic(fingerprint);
        }
        // Place marker if enabled (for each photographed fingerprint)
        if (placeMarkersOnFingerprints)
        {
          // Ensure a marker is not placed multiple times for the same fingerprint
          if (!markerByObject.ContainsKey(fingerprint))
          {
            PlaceMarkerBesideCollider(fingerprint, hitCollider, hitPoint);
          }
        }
      }
    }
    else
    {
      Debug.LogWarning("CameraScript: DustBrush not found - cannot check fingerprint reveal percentage!");
      if (!photographedFingerprints.Contains(fingerprint))
      {
        photographedFingerprints.Add(fingerprint);
        TurnFingerprintGreen(fingerprint);
        EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
        if (checklist != null)
        {
          checklist.MarkFingerprintAsPhotographedPublic(fingerprint);
        }
        if (placeMarkersOnFingerprints)
        {
          if (!markerByObject.ContainsKey(fingerprint))
          {
            PlaceMarkerBesideCollider(fingerprint, hitCollider, hitPoint);
          }
        }
      }
    }
  }

  void TurnFingerprintGreen(GameObject fingerprint)
  {
    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }
    if (renderer != null)
    {
      // Store original material
      if (!fingerprintOriginalMaterials.ContainsKey(fingerprint))
      {
        fingerprintOriginalMaterials[fingerprint] = renderer.material;
      }
      // Create and apply green material
      Material greenMaterial = new Material(renderer.material);
      greenMaterial.color = photographedFingerprintColor;
      if (greenMaterial.HasProperty("_BaseColor"))
      {
        greenMaterial.SetColor("_BaseColor", photographedFingerprintColor);
      }
      renderer.material = greenMaterial;
    }
    else
    {
      Debug.LogWarning("No renderer found on fingerprint: " + fingerprint.name);
    }
  }

  // - EVIDENCE VALIDATION
  bool IsValidEvidenceObject(GameObject obj)
  {
    if (obj == null) return false;
    // Check if in tracked evidence list
    if (evidenceObjects.Contains(obj))
    {
      return true;
    }
    // Fallback to tag check
    if (obj.CompareTag("Evidence"))
    {
      return true;
    }
    // Check for valid fingerprint
    if (IsStrictValidFingerprint(obj))
    {
      return true;
    }
    return false;
  }

  private GameObject GetRootEvidenceObject(GameObject colliderObject)
  {
    GameObject current = colliderObject;
    // Check current object
    if (IsValidEvidenceObject(current))
    {
      return current;
    }
    // Check parent hierarchy
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

  // - MARKER PLACEMENT SYSTEM
  void PlaceMarkerBesideCollider(GameObject evidenceObject, Collider hitCollider, Vector3 hitPoint)
  {
    if (markerPrefab == null)
    {
      Debug.LogError("No marker prefab assigned!");
      return;
    }
    // Prevent placing multiple markers on the same object if placeOnlyOneMarker is true for non-fingerprints
    if (markerByObject.ContainsKey(evidenceObject)) return;
    // Calculate marker position based on collider bounds
    Bounds bounds = hitCollider.bounds;
    Vector3 markerPosition = bounds.center;
    // Offset to side of evidence
    float sideOffset = bounds.extents.x + markerOffset;
    markerPosition.x += sideOffset;
    // Set height based on ground or evidence position
    if (useGroundHeight && groundObject != null)
    {
      markerPosition.y = groundObject.position.y + groundHeightOffset;
    }
    else
    {
      markerPosition.y = bounds.min.y + markerHeight;
    }
    // Create and configure marker
    GameObject marker = Instantiate(markerPrefab, markerPosition, Quaternion.identity);
    marker.SetActive(true);
    marker.transform.localScale = Vector3.zero;
    markers.Add(marker);
    markerByObject[evidenceObject] = marker;

    // Play marker scale-up animation
    StartCoroutine(ScaleMarker(marker.transform));
  }

  IEnumerator ScaleMarker(Transform markerTransform)
  {
    float timer = 0f;
    Vector3 initialScale = Vector3.zero;
    Vector3 targetScale = Vector3.one * markerSize;

    while (timer < scaleDuration)
    {
      timer += Time.deltaTime;
      markerTransform.localScale = Vector3.Lerp(initialScale, targetScale, timer / scaleDuration);
      yield return null;
    }
    markerTransform.localScale = targetScale;
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
  }

  // - PUBLIC API METHODS
  public string GetEvidenceDisplayName(GameObject obj)
  {
    if (obj == null) return "Unknown";
    if (evidenceObjectNames.TryGetValue(obj, out string displayName))
    {
      return displayName;
    }
    return CleanObjectName(obj.name);
  }

  public string[] GetUniqueEvidenceDisplayNames()
  {
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

  public GameObject[] GetCurrentEvidenceObjects()
  {
    return evidenceObjects.ToArray();
  }

  public HashSet<GameObject> GetPhotographedFingerprints()
  {
    return photographedFingerprints;
  }

  public bool IsFingerprintPhotographed(GameObject fingerprint)
  {
    return photographedFingerprints.Contains(fingerprint);
  }

  public void RemovePhotographedFingerprint(GameObject fingerprint)
  {
    if (photographedFingerprints.Contains(fingerprint))
    {
      photographedFingerprints.Remove(fingerprint);
      // Optionally revert material here if needed
      if (fingerprintOriginalMaterials.ContainsKey(fingerprint))
      {
        Renderer renderer = fingerprint.GetComponent<Renderer>();
        if (renderer == null)
        {
          renderer = fingerprint.GetComponentInChildren<Renderer>();
        }
        if (renderer != null)
        {
          renderer.material = fingerprintOriginalMaterials[fingerprint];
        }
        fingerprintOriginalMaterials.Remove(fingerprint);
      }
    }
  }

  public void ResetPhotographedFingerprints()
  {
    foreach (GameObject fp in photographedFingerprints)
    {
      if (fp != null && fingerprintOriginalMaterials.ContainsKey(fp))
      {
        Renderer renderer = fp.GetComponent<Renderer>();
        if (renderer == null)
        {
          renderer = fp.GetComponentInChildren<Renderer>();
        }
        if (renderer != null)
        {
          renderer.material = fingerprintOriginalMaterials[fp];
        }
      }
    }
    photographedFingerprints.Clear();
    fingerprintOriginalMaterials.Clear();
  }

  // - EDITOR CONTEXT MENU METHODS
  [ContextMenu("Force Refresh Evidence")]
  public void ForceRefreshEvidence()
  {
    PopulateEvidenceObjects();
  }

  [ContextMenu("Show Current Evidence")]
  public void ShowCurrentEvidence()
  {
    foreach (GameObject item in evidenceObjects)
    {
      if (item != null)
      {
        string displayName = GetEvidenceDisplayName(item);
        bool isPhotographed = photographedFingerprints.Contains(item);
        Debug.Log($"Evidence: {displayName}, Photographed: {isPhotographed}, Marked: {markerByObject.ContainsKey(item)}");
      }
    }
  }

  // - UTILITY METHODS
  private string CleanObjectName(string objectName)
  {
    if (string.IsNullOrEmpty(objectName)) return "Unknown";

    // Remove clone suffix and clean formatting
    string cleanName = objectName.Replace("(Clone)", "").Trim();
    cleanName = cleanName.Replace("_", " ");

    // Capitalize first letter
    if (cleanName.Length > 0)
    {
      cleanName = char.ToUpper(cleanName[0]) + cleanName.Substring(1);
    }

    return cleanName;
  }

  void FallbackEvidenceCheck()
  {
    // If no direct hit is found, target the nearest valid evidence
  }
}