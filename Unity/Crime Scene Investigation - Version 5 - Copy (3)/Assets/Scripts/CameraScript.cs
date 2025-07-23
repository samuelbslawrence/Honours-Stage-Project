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
  [SerializeField] private float detectionFOVNarrow = 0.3f;
  [SerializeField] private LayerMask evidenceLayerMask = -1;

  [Header("Fingerprint Detection - SIMPLIFIED")]
  [SerializeField] private DustBrush dustBrush;
  [SerializeField] private bool autoFindDustBrush = true;
  [SerializeField] private float fingerprintRevealThreshold = 50f;
  [SerializeField] private Color photographedFingerprintColor = Color.white;
  [SerializeField] private bool debugFingerprintDetection = true;

  [Header("Real-Time Detection")]
  [SerializeField] private bool enableRealTimeDetection = true;
  [SerializeField] private float evidenceRefreshRate = 1f;
  [SerializeField] private bool debugRealTimeUpdates = true;

  [Header("Scene Generator Integration")]
  [SerializeField] private CrimeSceneGenerator sceneGenerator;
  [SerializeField] private bool autoFindSceneGenerator = true;

  [Header("Marker Settings")]
  [SerializeField] private GameObject markerPrefab;
  [SerializeField] private float markerSize = 0.3f;
  [SerializeField] private float markerOffset = 0.2f;
  [SerializeField] private float markerHeight = 0.1f;
  [SerializeField] private float scaleDuration = 1.0f;
  [SerializeField] private bool placeOnlyOneMarker = true;
  [SerializeField] private bool placeMarkersOnFingerprints = false;

  [Header("Ground Settings")]
  [SerializeField] private Transform groundObject;
  [SerializeField] private bool useGroundHeight = false;
  [SerializeField] private float groundHeightOffset = 0.1f;

  [Header("Movement Detection")]
  [SerializeField] private float movementThreshold = 0.00001f;
  [SerializeField] private float listenDuration = 5.0f;
  [SerializeField] private float triggerDebounceTime = 0.5f;

  // NEW: Manual Evidence List
  [Header("Manual Evidence List")]
  [SerializeField] private GameObject[] manualEvidenceObjects;
  [SerializeField] private bool useManualEvidenceList = false; // Toggle to enable/disable this list

  // NEW: Debug Options for listing all seen objects and pausing
  [Header("Debug - Custom Additions")]
  [SerializeField] private bool debugListAllSeenObjects = true;

  // State tracking
  private bool isTakingPhoto = false;
  private bool isMoving = false;
  private bool canTakePicture = false;
  private Vector3 lastPosition;
  private Quaternion lastRotation;
  private float lastMovementTime = 0f;
  private float lastPhotoTime = 0f;

  // Evidence tracking
  private List<GameObject> evidenceObjects = new List<GameObject>();
  private List<GameObject> markers = new List<GameObject>();
  private Dictionary<GameObject, GameObject> markerByObject = new Dictionary<GameObject, GameObject>();
  private Dictionary<GameObject, string> evidenceObjectNames = new Dictionary<GameObject, string>();

  // Fingerprint tracking - SIMPLIFIED
  private HashSet<GameObject> photographedFingerprints = new HashSet<GameObject>();
  private Dictionary<GameObject, Material> fingerprintOriginalMaterials = new Dictionary<GameObject, Material>();

  // Real-time tracking
  private int lastKnownEvidenceCount = 0;
  private HashSet<string> lastKnownEvidenceNames = new HashSet<string>();

  // Special trigger detection for Oculus
  private bool wasButtonPressed = false;

  void Start()
  {
    lastPosition = transform.position;
    lastRotation = transform.rotation;

    FindDustBrush();

    if (autoFindSceneGenerator && sceneGenerator == null)
    {
      sceneGenerator = FindObjectOfType<CrimeSceneGenerator>();
      if (sceneGenerator != null)
      {
        //Debug.Log("CameraScript: Found CrimeSceneGenerator automatically");
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

    PopulateEvidenceObjects();

    if (enableRealTimeDetection)
    {
      StartCoroutine(RealTimeEvidenceMonitoring());
    }
  }

  void FindDustBrush()
  {
    if (autoFindDustBrush && dustBrush == null)
    {
      dustBrush = FindObjectOfType<DustBrush>();
      if (dustBrush != null)
      {
        //Debug.Log("CameraScript: Found DustBrush automatically");
      }
      else
      {
        Debug.LogWarning("CameraScript: No DustBrush found! Fingerprint detection will be limited.");
      }
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
          //Debug.Log("Found camera in child: " + cameraTransform.name);
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
        //Debug.Log("Found ground object: " + groundObj.name);
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
            //Debug.Log("Found potential ground object: " + obj.name);
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

  IEnumerator RealTimeEvidenceMonitoring()
  {
    while (enableRealTimeDetection)
    {
      yield return new WaitForSeconds(evidenceRefreshRate);

      bool evidenceChanged = CheckForEvidenceChanges();

      if (evidenceChanged)
      {
        if (debugRealTimeUpdates)
        {
          //Debug.Log("CameraScript: Evidence changed in scene - refreshing detection list");
        }

        PopulateEvidenceObjects();
        NotifyEvidenceListChanged();
      }
    }
  }

  bool CheckForEvidenceChanges()
  {
    int currentCount = 0;
    // Prioritize manual list if enabled
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

    if (currentCount != lastKnownEvidenceCount)
    {
      lastKnownEvidenceCount = currentCount;
      return true;
    }

    if (evidenceObjects.RemoveAll(obj => obj == null) > 0)
    {
      return true;
    }

    // Check names only if using SceneGenerator
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
      if (debugRealTimeUpdates)
      {
        //Debug.Log("CameraScript: Notified EvidenceChecklist of changes");
      }
    }
  }

  void PopulateEvidenceObjects()
  {
    evidenceObjects.Clear();
    evidenceObjectNames.Clear();

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

    if (debugRealTimeUpdates)
    {
      //Debug.Log("CameraScript: Found " + evidenceObjects.Count + " evidence objects");
      foreach (GameObject obj in evidenceObjects)
      {
        if (obj != null)
        {
          string displayName = evidenceObjectNames.ContainsKey(obj) ? evidenceObjectNames[obj] : obj.name;
          //Debug.Log("  " + displayName + " (" + obj.name + ") at " + obj.transform.position);
        }
      }
    }
  }

  // NEW: Method to get evidence from manual list
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
    if (debugRealTimeUpdates)
    {
      //Debug.Log("CameraScript: Got " + evidenceObjects.Count + " evidence objects from Manual List");
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

        if (debugRealTimeUpdates)
        {
          //Debug.Log("CameraScript: Got " + evidenceObjects.Count + " evidence objects from Scene Generator");
          //Debug.Log("Evidence names: " + string.Join(", ", generatorEvidenceNames ?? new string[0]));
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

    if (debugRealTimeUpdates && taggedEvidence.Length > 0)
    {
      //Debug.Log("CameraScript: Found " + taggedEvidence.Length + " objects with Evidence tag (fallback method)");
    }
  }

  public void OnSceneGenerated()
  {
    //Debug.Log("CameraScript: Scene regenerated - updating evidence detection");

    ClearMarkers();
    evidenceObjectNames.Clear();

    // Clear fingerprint tracking
    photographedFingerprints.Clear();
    fingerprintOriginalMaterials.Clear();

    PopulateEvidenceObjects();

    lastKnownEvidenceCount = 0;
    lastKnownEvidenceNames.Clear();

    // Notify EvidenceChecklist about scene change
    EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
    if (checklist != null)
    {
      checklist.OnSceneGenerated();
    }

    //Debug.Log("CameraScript: Updated to track " + evidenceObjects.Count + " evidence objects");
  }

  public void UpdateEvidenceList(string[] newEvidenceNames)
  {
    //Debug.Log("CameraScript: Evidence list update requested, refreshing from scene generator instead");
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

    //Debug.Log("Taking photo");
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

    CheckEvidenceAndPlaceMarkers(); // Main logic call

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

    //Debug.Log("CHECKING FOR EVIDENCE IN VIEW");

    PopulateEvidenceObjects();

    Ray ray = secondCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    RaycastHit[] hits = Physics.RaycastAll(ray, maxDetectionDistance, evidenceLayerMask);

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    //Debug.Log("Raycast found " + hits.Length + " potential evidence items");

    if (debugListAllSeenObjects)
    {
      //Debug.Log("--- Camera Script: All Objects Detected in View Raycast ---");
      //Debug.Log($"Total objects hit by raycast: {hits.Length}");
    }

    if (hits.Length > 0)
    {
      foreach (RaycastHit hit in hits)
      {
        GameObject obj = hit.collider.gameObject;

        if (debugListAllSeenObjects)
        {
          //Debug.Log($"- Seen Object: {obj.name} (Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)}, Distance: {hit.distance:F2}m)");
        }

        GameObject rootObject = GetRootEvidenceObject(obj);

        if (rootObject != null)
        {
          if (markerByObject.ContainsKey(rootObject))
          {
            //Debug.Log("Marker already exists for: " + rootObject.name);
            continue;
          }

          if (IsValidEvidenceObject(rootObject))
          {
            string displayName = GetEvidenceDisplayName(rootObject);
            //Debug.Log("EVIDENCE FOUND: " + displayName + " (" + rootObject.name + ")");

            bool isValidFingerprint = IsStrictValidFingerprint(rootObject);
            if (isValidFingerprint)
            {
              HandleFingerprintPhotography(rootObject, hit.collider, hit.point);
            }
            else
            {
              PlaceMarkerBesideCollider(rootObject, hit.collider, hit.point);
              // NEW: Notify EvidenceChecklist for non-fingerprint evidence
              EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
              if (checklist != null)
              {
                checklist.MarkEvidenceAsPhotographed(GetEvidenceDisplayName(rootObject));
              }
            }

            if (placeOnlyOneMarker) break;
          }
        }
      }
    }
    else
    {
      FallbackEvidenceCheck();
    }

    if (debugListAllSeenObjects)
    {
      //Debug.Log("--- End Camera Script: All Objects Detected ---");
    }

    //Debug.Log("Evidence check complete");
  }

  private bool IsStrictValidFingerprint(GameObject obj)
  {
    if (obj == null) return false;

    bool hasCorrectTag = obj.CompareTag("Fingerprint");
    int uvLayerIndex = LayerMask.NameToLayer("UV");
    bool onCorrectLayer = uvLayerIndex != -1 && obj.layer == uvLayerIndex;
    string objName = obj.name;
    bool hasCorrectName = objName == "Fingerprint" ||
                          (objName.StartsWith("Fingerprint (") && objName.EndsWith(")"));

    bool isValid = hasCorrectTag && onCorrectLayer && hasCorrectName;

    if (debugFingerprintDetection)
    {
      //Debug.Log($"CameraScript: Strict validation for {objName}:");
      //Debug.Log($"  Tag 'Fingerprint': {hasCorrectTag} (actual: '{obj.tag}')");
      //Debug.Log($"  Layer: {onCorrectLayer} (actual: '{LayerMask.LayerToName(obj.layer)}')");
      //Debug.Log($"  Name pattern: {hasCorrectName}");
      //Debug.Log($"  RESULT: {(isValid ? "✓ VALID" : "✗ INVALID")}");
    }

    return isValid;
  }

  void HandleFingerprintPhotography(GameObject fingerprint, Collider hitCollider, Vector3 hitPoint)
  {
    if (debugFingerprintDetection)
    {
      //Debug.Log("CameraScript: Checking fingerprint: " + fingerprint.name);
    }

    if (!IsStrictValidFingerprint(fingerprint))
    {
      if (debugFingerprintDetection)
      {
        //Debug.Log("CameraScript: Object does not meet strict fingerprint criteria: " + fingerprint.name);
      }
      return;
    }

    if (dustBrush != null)
    {
      float revealPercentage = dustBrush.GetFingerprintRevealPercentage(fingerprint);

      if (debugFingerprintDetection)
      {
        //Debug.Log("CameraScript: Fingerprint reveal percentage: " + revealPercentage.ToString("F1") + "%");
      }

      if (revealPercentage >= fingerprintRevealThreshold)
      {
        if (!photographedFingerprints.Contains(fingerprint))
        {
          photographedFingerprints.Add(fingerprint);
          TurnFingerprintGreen(fingerprint);

          // NEW: Notify EvidenceChecklist for photographed fingerprint
          EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
          if (checklist != null)
          {
            checklist.MarkFingerprintAsPhotographedPublic(fingerprint);
          }

          if (debugFingerprintDetection)
          {
            //Debug.Log("CameraScript: ✓ FINGERPRINT PHOTOGRAPHED: " + fingerprint.name);
            //Debug.Log("CameraScript: Total photographed fingerprints: " + photographedFingerprints.Count);
          }
        }
        else
        {
          if (debugFingerprintDetection)
          {
            //Debug.Log("CameraScript: Fingerprint already photographed: " + fingerprint.name);
          }
        }

        if (placeMarkersOnFingerprints)
        {
          PlaceMarkerBesideCollider(fingerprint, hitCollider, hitPoint);
        }
      }
      else
      {
        if (debugFingerprintDetection)
        {
          //Debug.Log("CameraScript: Fingerprint not sufficiently revealed (" + revealPercentage.ToString("F1") + "% < " + fingerprintRevealThreshold + "%)");
        }
      }
    }
    else
    {
      Debug.LogWarning("CameraScript: DustBrush not found - cannot check fingerprint reveal percentage!");
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
      if (!fingerprintOriginalMaterials.ContainsKey(fingerprint))
      {
        fingerprintOriginalMaterials[fingerprint] = renderer.material;
      }

      Material greenMaterial = new Material(renderer.material);
      greenMaterial.color = photographedFingerprintColor;

      if (greenMaterial.HasProperty("_BaseColor"))
      {
        greenMaterial.SetColor("_BaseColor", photographedFingerprintColor);
      }

      renderer.material = greenMaterial;

      if (debugFingerprintDetection)
      {
        //Debug.Log("Turned fingerprint green: " + fingerprint.name);
      }
    }
    else
    {
      Debug.LogWarning("No renderer found on fingerprint: " + fingerprint.name);
    }
  }

  bool IsValidEvidenceObject(GameObject obj)
  {
    if (obj == null) return false;

    // Check if it's in our tracked list (manual, scene generator, or tagged)
    if (evidenceObjects.Contains(obj))
    {
      return true;
    }

    // Fallback to tag for dynamically added objects not in our initial lists
    if (obj.CompareTag("Evidence"))
    {
      return true;
    }

    // Check if it's a strictly valid fingerprint
    if (IsStrictValidFingerprint(obj))
    {
      return true;
    }

    return false;
  }

  private GameObject GetRootEvidenceObject(GameObject colliderObject)
  {
    GameObject current = colliderObject;

    // Check the object itself
    if (IsValidEvidenceObject(current))
    {
      return current;
    }

    // Check parents
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
    //Debug.Log("Placed marker at: " + markerPosition + " for evidence: " + displayName + " (" + evidenceObject.name + ")");
  }

  void FallbackEvidenceCheck()
  {
    if (evidenceObjects.Count == 0) return;

    //Debug.Log("Using fallback evidence detection method");

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
      //Debug.Log("EVIDENCE FOUND (fallback): " + displayName + " (" + closestEvidence.name + ")");

      bool isValidFingerprint = IsStrictValidFingerprint(closestEvidence);
      if (isValidFingerprint)
      {
        Collider collider = closestEvidence.GetComponentInChildren<Collider>();
        if (collider != null)
        {
          HandleFingerprintPhotography(closestEvidence, collider, position);
        }
        else
        {
          if (dustBrush != null)
          {
            float revealPercentage = dustBrush.GetFingerprintRevealPercentage(closestEvidence);
            if (revealPercentage >= fingerprintRevealThreshold)
            {
              if (!photographedFingerprints.Contains(closestEvidence))
              {
                photographedFingerprints.Add(closestEvidence);
                TurnFingerprintGreen(closestEvidence);
              }
            }
          }
          if (placeMarkersOnFingerprints)
          {
            PlaceMarker(closestEvidence, position);
          }
        }
      }
      else
      {
        Collider collider = closestEvidence.GetComponentInChildren<Collider>();
        if (collider != null)
        {
          PlaceMarkerBesideCollider(closestEvidence, collider, position);
        }
        else
        {
          PlaceMarker(closestEvidence, position);
        }
        // NEW: Notify EvidenceChecklist for non-fingerprint evidence (fallback)
        EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
        if (checklist != null)
        {
          checklist.MarkEvidenceAsPhotographed(GetEvidenceDisplayName(closestEvidence));
        }
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

    //Debug.Log("Placed marker at: " + markerPosition);
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
    //Debug.Log("CameraScript: All markers cleared");
  }

  public void ResetFingerprints()
  {
    foreach (var kvp in fingerprintOriginalMaterials)
    {
      GameObject fingerprint = kvp.Key;
      Material originalMaterial = kvp.Value;

      if (fingerprint != null && originalMaterial != null)
      {
        Renderer renderer = fingerprint.GetComponent<Renderer>();
        if (renderer == null)
        {
          renderer = fingerprint.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
          renderer.material = originalMaterial;
        }
      }
    }

    photographedFingerprints.Clear();
    fingerprintOriginalMaterials.Clear();
    //Debug.Log("CameraScript: Reset all fingerprints to original state");
  }

  // Public API methods
  public int GetEvidenceObjectCount() => evidenceObjects.Count;
  public int GetMarkerCount() => markers.Count;
  public Dictionary<GameObject, GameObject> GetMarkerByObject() => new Dictionary<GameObject, GameObject>(markerByObject);
  public Dictionary<GameObject, string> GetEvidenceObjectNames() => new Dictionary<GameObject, string>(evidenceObjectNames);

  // Fingerprint API - CRITICAL FOR EVIDENCECHECKLIST
  public int GetPhotographedFingerprintCount() => photographedFingerprints.Count;
  public bool IsFingerprintPhotographed(GameObject fingerprint) => photographedFingerprints.Contains(fingerprint);
  public HashSet<GameObject> GetPhotographedFingerprints() => new HashSet<GameObject>(photographedFingerprints);

  public string GetEvidenceDisplayName(GameObject evidenceObj)
  {
    if (evidenceObjectNames.ContainsKey(evidenceObj))
    {
      return evidenceObjectNames[evidenceObj];
    }
    // If not in our dictionary, try to clean the name directly
    return CleanObjectName(evidenceObj.name);
  }

  public string[] GetCurrentTargetNames()
  {
    // Prioritize manual list if enabled
    if (useManualEvidenceList && manualEvidenceObjects != null)
    {
      return manualEvidenceObjects.Where(obj => obj != null).Select(obj => CleanObjectName(obj.name)).ToArray();
    }
    else if (sceneGenerator != null)
    {
      try
      {
        string[] names = sceneGenerator.GetCurrentSceneEvidenceNames();
        return names ?? new string[0];
      }
      catch (System.Exception e)
      {
        Debug.LogWarning("CameraScript: Error getting evidence names: " + e.Message);
      }
    }

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

  [ContextMenu("Force Refresh Evidence")]
  public void ForceRefreshEvidence()
  {
    PopulateEvidenceObjects();
    //Debug.Log("Force refreshed - found " + evidenceObjects.Count + " evidence objects");
  }

  [ContextMenu("Show Current Evidence")]
  public void ShowCurrentEvidence()
  {
    //Debug.Log("=== CURRENT EVIDENCE STATUS ===");
    //Debug.Log("Evidence Objects: " + evidenceObjects.Count);
    foreach (GameObject item in evidenceObjects)
    {
      if (item != null)
      {
        string displayName = GetEvidenceDisplayName(item);
        bool isPhotographed = photographedFingerprints.Contains(item);
        //Debug.Log("  - " + displayName + " (" + item.name + ") (Active: " + item.gameObject.activeInHierarchy + ") (Photographed: " + isPhotographed + ")");
      }
      else
      {
        //Debug.Log("  - NULL");
      }
    }
    //Debug.Log("Markers Placed: " + markers.Count);
    //Debug.Log("Fingerprints Photographed: " + photographedFingerprints.Count);
    //Debug.Log("=================================");
  }

  // DEBUG METHOD: Test fingerprint detection
  [ContextMenu("Debug: Test Fingerprint Detection")]
  public void DebugTestFingerprintDetection()
  {
    //Debug.Log("=== TESTING FINGERPRINT DETECTION ===");

    // Find all objects that might be fingerprints
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    int validFingerprints = 0;

    foreach (GameObject obj in allObjects)
    {
      if (obj != null && obj.activeInHierarchy)
      {
        // Check if it has any fingerprint-related properties
        bool hasTag = obj.CompareTag("Fingerprint");
        bool hasName = obj.name.ToLower().Contains("fingerprint");
        bool onUVLayer = obj.layer == LayerMask.NameToLayer("UV");

        if (hasTag || hasName || onUVLayer)
        {
          bool isValid = IsStrictValidFingerprint(obj);
          if (isValid) validFingerprints++;

          //Debug.Log($"{(isValid ? "✓ VALID" : "✗ INVALID")}: {obj.name}");
          //Debug.Log($"  Tag: {hasTag} ('{obj.tag}')");
          //Debug.Log($"  Layer: {onUVLayer} ('{LayerMask.LayerToName(obj.layer)}')");
          //Debug.Log($"  Name contains 'fingerprint': {hasName}");

          if (isValid)
          {
            bool isPhotographed = photographedFingerprints.Contains(obj);
            // Original code ended here, no change needed as per user request.
          }
        }
      }
    }
  }

  private string CleanObjectName(string objectName)
  {
    if (string.IsNullOrEmpty(objectName)) return "Unknown";

    string cleanName = objectName.Replace("(Clone)", "").Trim();
    cleanName = cleanName.Replace("_", " ");
    if (cleanName.Length > 0)
    {
      cleanName = char.ToUpper(cleanName[0]) + cleanName.Substring(1);
    }
    return cleanName;
  }
}
