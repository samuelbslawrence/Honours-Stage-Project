using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class TeleportSystem : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("Teleport Settings")]
  [SerializeField] private Transform tpPointsParent;
  [SerializeField] private float raycastDistance = 20f;
  [SerializeField] private float raycastRadius = 0.1f;
  [SerializeField] private LayerMask tpLayerMask = -1;
  [SerializeField] private string alphaThresholdProperty = "_Cutoff";
  [SerializeField] private bool teleportHeldObjects = true;
  [SerializeField] private string[] grabLayerNames = new string[] { "Grab", "Grabbable", "Held" };

  [Header("Visual Settings")]
  [SerializeField] private float revealDelay = 0.05f;
  [SerializeField] private float revealDuration = 0.3f;
  [SerializeField] private float hideDuration = 0.2f;

  [Header("Beam Settings")]
  [SerializeField] private bool showBeam = true;
  [SerializeField] private GameObject beamPrefab;
  [SerializeField] private float beamWidth = 0.02f;
  [SerializeField] private float beamEndWidth = 0.005f;
  [SerializeField] private Color beamStartColor = new Color(0.2f, 0.6f, 1f, 0.8f);
  [SerializeField] private Color beamEndColor = new Color(0.2f, 0.6f, 1f, 0.0f);
  [SerializeField] private Material beamMaterial;
  [SerializeField] private int beamSegments = 10;

  // - PRIVATE STATE VARIABLES
  // Teleporter state tracking
  private bool isTeleporterActive = false;
  private bool wasButtonPressed = false;

  // TP Point management
  private List<TPPoint> tpPoints = new List<TPPoint>();
  private TPPoint currentTargetPoint = null;
  private Coroutine revealCoroutine = null;

  // Component references
  private Camera mainCamera;
  private Transform rightControllerTransform;

  // Beam rendering system
  private GameObject[] beamSegmentObjects;
  private Material beamMaterialInstance;

  // - INITIALIZATION
  void Start()
  {
    // Setup camera reference
    SetupMainCamera();

    // Setup TP points parent
    SetupTPPointsParent();

    // Initialize teleport points
    InitializeTPPoints();

    // Configure layer mask
    ConfigureLayerMask();

    // Setup beam rendering
    SetupBeamRendering();

    // Hide beam initially
    SetBeamActive(false);
  }

  // Find and validate main camera
  void SetupMainCamera()
  {
    mainCamera = Camera.main;
    if (mainCamera == null)
    {
      mainCamera = GetComponentInChildren<Camera>();
      if (mainCamera == null)
      {
        Debug.LogError("No camera found! Cannot operate teleporter.");
        enabled = false;
        return;
      }
    }
  }

  // Find TP points parent object
  void SetupTPPointsParent()
  {
    if (tpPointsParent == null)
    {
      GameObject tpParent = GameObject.Find("TP Points");
      if (tpParent != null)
      {
        tpPointsParent = tpParent.transform;
      }
    }
  }

  // Configure raycast layer mask
  void ConfigureLayerMask()
  {
    if (tpLayerMask.value == -1)
    {
      tpLayerMask = Physics.DefaultRaycastLayers;
    }
  }

  // Setup beam rendering components
  void SetupBeamRendering()
  {
    // Create beam material instance
    if (beamMaterial == null)
    {
      beamMaterial = new Material(Shader.Find("Sprites/Default"));
      beamMaterial.color = beamStartColor;
    }

    beamMaterialInstance = new Material(beamMaterial);

    // Setup beam segments
    SetupBeamSegments();
  }

  // - UPDATE LOOP
  void Update()
  {
    // Handle teleport button input
    bool isButtonPressed = CheckTeleportButtonPressed();

    // Process button state changes
    if (isButtonPressed && !wasButtonPressed)
    {
      ActivateTeleporter();
    }
    else if (!isButtonPressed && wasButtonPressed)
    {
      TriggerTeleport();
    }

    // Update teleporter targeting if active
    if (isTeleporterActive)
    {
      UpdateTeleporterTarget();
    }

    wasButtonPressed = isButtonPressed;
  }

  // - TP POINT INITIALIZATION
  // Initialize all teleport points in scene
  void InitializeTPPoints()
  {
    tpPoints.Clear();

    // Get TP points from parent if available
    if (tpPointsParent != null)
    {
      GetTPPointsFromParent();
    }
    else
    {
      // Find TP points in scene
      FindTPPointsInScene();
    }

    // Initialize all found TP points
    foreach (TPPoint point in tpPoints)
    {
      point.Initialize(alphaThresholdProperty);
    }
  }

  // Get TP points from designated parent object
  void GetTPPointsFromParent()
  {
    for (int i = 0; i < tpPointsParent.childCount; i++)
    {
      Transform child = tpPointsParent.GetChild(i);
      TPPoint tpPoint = child.GetComponent<TPPoint>();

      // Add TPPoint component if missing
      if (tpPoint == null)
      {
        tpPoint = child.gameObject.AddComponent<TPPoint>();
      }

      tpPoints.Add(tpPoint);
    }
  }

  // Find TP points throughout the scene
  void FindTPPointsInScene()
  {
    // Try finding existing TPPoint components
    TPPoint[] allTPPoints = GameObject.FindObjectsOfType<TPPoint>();
    if (allTPPoints.Length > 0)
    {
      tpPoints.AddRange(allTPPoints);
    }
    else
    {
      // Search for objects with TP Point in name
      GameObject[] tpObjects = GameObject.FindObjectsOfType<GameObject>();
      foreach (GameObject obj in tpObjects)
      {
        if (obj.name.Contains("TP Point"))
        {
          TPPoint tpPoint = obj.GetComponent<TPPoint>();
          if (tpPoint == null)
          {
            tpPoint = obj.AddComponent<TPPoint>();
          }
          tpPoints.Add(tpPoint);
        }
      }
    }
  }

  // - BEAM RENDERING SYSTEM
  // Setup beam segment objects for rendering
  void SetupBeamSegments()
  {
    beamSegmentObjects = new GameObject[beamSegments - 1];

    for (int i = 0; i < beamSegments - 1; i++)
    {
      if (beamPrefab != null)
      {
        beamSegmentObjects[i] = Instantiate(beamPrefab);
      }
      else
      {
        beamSegmentObjects[i] = CreateBeamSegment();
      }

      beamSegmentObjects[i].SetActive(false);
    }
  }

  // Create individual beam segment mesh
  GameObject CreateBeamSegment()
  {
    GameObject segmentObj = new GameObject("BeamSegment");

    // Add mesh components
    MeshFilter meshFilter = segmentObj.AddComponent<MeshFilter>();
    MeshRenderer meshRenderer = segmentObj.AddComponent<MeshRenderer>();

    // Create quad mesh
    Mesh mesh = new Mesh();

    // Define quad vertices
    Vector3[] vertices = new Vector3[4]
    {
      new Vector3(-0.5f, -0.5f, 0),
      new Vector3(0.5f, -0.5f, 0),
      new Vector3(-0.5f, 0.5f, 0),
      new Vector3(0.5f, 0.5f, 0)
    };

    // Define triangles
    int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

    // Define UVs
    Vector2[] uvs = new Vector2[4]
    {
      new Vector2(0, 0),
      new Vector2(1, 0),
      new Vector2(0, 1),
      new Vector2(1, 1)
    };

    // Assign mesh data
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.RecalculateNormals();

    // Configure mesh and material
    meshFilter.mesh = mesh;
    meshRenderer.material = beamMaterialInstance;

    return segmentObj;
  }

  // Control beam visibility
  void SetBeamActive(bool active)
  {
    if (beamSegmentObjects != null)
    {
      foreach (GameObject segment in beamSegmentObjects)
      {
        if (segment != null)
        {
          segment.SetActive(active);
        }
      }
    }
  }

  // Update beam visual appearance
  private void UpdateBeamVisual(Vector3 origin, Vector3 direction, Quaternion rotation)
  {
    if (beamSegmentObjects == null || beamSegmentObjects.Length == 0)
      return;

    float beamLength = raycastDistance;

    // Position and orient each beam segment
    for (int i = 0; i < beamSegmentObjects.Length; i++)
    {
      GameObject segment = beamSegmentObjects[i];
      if (segment == null) continue;

      // Calculate segment position
      float segStart = (float)i / beamSegmentObjects.Length;
      float segEnd = (float)(i + 1) / beamSegmentObjects.Length;
      float segCenter = (segStart + segEnd) / 2;

      // Position segment
      Vector3 segmentPosition = origin + direction * (beamLength * segCenter);
      segment.transform.position = segmentPosition;

      // Orient segment
      segment.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);

      // Scale segment
      float segmentLength = beamLength / beamSegmentObjects.Length;
      float segmentWidth = Mathf.Lerp(beamWidth, beamEndWidth, segCenter);
      segment.transform.localScale = new Vector3(segmentWidth, segmentLength, 1);

      // Update segment color
      if (segment.GetComponent<Renderer>() != null)
      {
        Renderer renderer = segment.GetComponent<Renderer>();
        Color segColor = Color.Lerp(beamStartColor, beamEndColor, segCenter);
        renderer.material.color = segColor;
      }
    }
  }

  // - INPUT HANDLING
  // Check for teleport button press
  bool CheckTeleportButtonPressed()
  {
    // Check VR controller B button
    if (XRSettings.isDeviceActive)
    {
      try
      {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);

        foreach (var device in inputDevices)
        {
          // Check secondary button (B on Oculus)
          if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue) && secondaryButtonValue)
          {
            return true;
          }

          // Check primary button for non-Oculus controllers
          if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
          {
            string deviceName = device.name.ToLower();
            if (!deviceName.Contains("oculus") || !deviceName.Contains("right"))
            {
              return true;
            }
          }
        }
      }
      catch (System.Exception)
      {
        // Ignore input errors
      }
    }

    // Keyboard fallback for testing
    if (Input.GetKey(KeyCode.B))
    {
      return true;
    }

    return false;
  }

  // - TELEPORTER ACTIVATION SYSTEM
  // Activate teleporter and show TP points
  void ActivateTeleporter()
  {
    if (isTeleporterActive)
      return;

    isTeleporterActive = true;

    // Show beam
    if (showBeam)
    {
      SetBeamActive(true);
    }

    // Stop ongoing animations
    if (revealCoroutine != null)
    {
      StopCoroutine(revealCoroutine);
    }

    // Sort and reveal TP points
    SortTPPointsByDistance();
    revealCoroutine = StartCoroutine(RevealTPPointsSequentially());
  }

  // Sort TP points by distance to player
  void SortTPPointsByDistance()
  {
    Vector3 playerPosition = transform.position;

    tpPoints.Sort((a, b) =>
        Vector3.Distance(a.transform.position, playerPosition)
        .CompareTo(Vector3.Distance(b.transform.position, playerPosition)));
  }

  // Reveal TP points one by one
  IEnumerator RevealTPPointsSequentially()
  {
    foreach (TPPoint point in tpPoints)
    {
      point.RevealPoint(revealDuration);
      yield return new WaitForSeconds(revealDelay);
    }
  }

  // - TARGETING SYSTEM
  // Update teleporter target selection
  void UpdateTeleporterTarget()
  {
    // Setup raycast from controller or camera
    Ray ray;
    Vector3 rayOrigin = mainCamera.transform.position;
    Vector3 rayDirection = mainCamera.transform.forward;
    Quaternion rayRotation = Quaternion.identity;

    // Try to use right controller if available
    bool usingController = GetControllerRayInfo(ref rayOrigin, ref rayDirection, ref rayRotation);

    ray = new Ray(rayOrigin, rayDirection);

    // Update beam visualization
    if (showBeam && beamSegmentObjects != null)
    {
      UpdateBeamVisual(rayOrigin, rayDirection, rayRotation);
    }

    // Perform targeting raycast
    ProcessTargetingRaycast(ray);
  }

  // Get ray information from VR controller
  bool GetControllerRayInfo(ref Vector3 rayOrigin, ref Vector3 rayDirection, ref Quaternion rayRotation)
  {
    // Try to find RightHandAnchor first
    GameObject rightHandAnchor = GameObject.Find("RightHandAnchor");
    if (rightHandAnchor != null)
    {
      rayOrigin = rightHandAnchor.transform.position;
      rayDirection = rightHandAnchor.transform.forward;
      rayRotation = rightHandAnchor.transform.rotation;
      rightControllerTransform = rightHandAnchor.transform;
      return true;
    }

    // Fallback to XR Input system
    if (XRSettings.isDeviceActive)
    {
      try
      {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);

        if (inputDevices.Count > 0)
        {
          InputDevice rightController = inputDevices[0];

          if (rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
              rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
          {
            rayOrigin = position;
            rayDirection = rotation * Vector3.forward;
            rayRotation = rotation;

            // Store controller transform reference
            FindControllerTransform();
            return true;
          }
        }
      }
      catch (System.Exception)
      {
        // Ignore controller errors
      }
    }

    return false;
  }

  // Find controller transform for beam attachment
  void FindControllerTransform()
  {
    if (rightControllerTransform != null) return;

    string[] possibleControllerNames = new string[] {
      "RightHandAnchor", "RightHand", "RightController", "Right Hand Controller",
      "OVRControllerRight", "XRRightController", "Right Controller"
    };

    // Search by name
    foreach (string controllerName in possibleControllerNames)
    {
      GameObject controllerObj = GameObject.Find(controllerName);
      if (controllerObj != null)
      {
        rightControllerTransform = controllerObj.transform;
        return;
      }
    }

    // Search by name pattern
    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj.name.Contains("Right") &&
          (obj.name.Contains("Controller") || obj.name.Contains("Hand")))
      {
        rightControllerTransform = obj.transform;
        return;
      }
    }
  }

  // Process targeting raycast results
  void ProcessTargetingRaycast(Ray ray)
  {
    RaycastHit hit;
    TPPoint hitPoint = null;

    // Perform spherecast for easier targeting
    if (Physics.SphereCast(ray, raycastRadius, out hit, raycastDistance, tpLayerMask))
    {
      // Check for TP Point component
      hitPoint = hit.transform.GetComponent<TPPoint>();
      if (hitPoint == null)
      {
        hitPoint = hit.transform.GetComponentInParent<TPPoint>();
      }
    }

    // Update target selection
    UpdateTargetSelection(hitPoint);
  }

  // Update current target selection
  void UpdateTargetSelection(TPPoint hitPoint)
  {
    if (hitPoint != currentTargetPoint)
    {
      // Hide previous target
      if (currentTargetPoint != null)
      {
        currentTargetPoint.RevealPoint(revealDuration);
      }

      // Update current target
      currentTargetPoint = hitPoint;

      // Handle new target
      if (currentTargetPoint != null)
      {
        // Highlight current target
        currentTargetPoint.HighlightPoint();

        // Hide other points
        foreach (TPPoint point in tpPoints)
        {
          if (point != currentTargetPoint)
          {
            point.HidePoint(hideDuration);
          }
        }
      }
      else
      {
        // No target - reveal all points
        foreach (TPPoint point in tpPoints)
        {
          point.RevealPoint(revealDuration);
        }
      }
    }
  }

  // - TELEPORTATION SYSTEM
  // Execute teleportation to selected target
  void TriggerTeleport()
  {
    if (!isTeleporterActive)
      return;

    // Hide beam
    if (showBeam)
    {
      SetBeamActive(false);
    }

    // Execute teleport if target selected
    if (currentTargetPoint != null)
    {
      ExecuteTeleportation();
    }

    // Hide all TP points
    HideAllTPPoints();

    // Reset teleporter state
    ResetTeleporterState();
  }

  // Execute the actual teleportation
  void ExecuteTeleportation()
  {
    Vector3 targetPosition = currentTargetPoint.transform.position;
    targetPosition.y = transform.position.y; // Maintain player height

    // Handle held objects
    List<Transform> heldObjects = new List<Transform>();
    List<Vector3> relativePositions = new List<Vector3>();

    if (teleportHeldObjects)
    {
      FindHeldObjects(heldObjects);

      // Store relative positions
      foreach (Transform heldObj in heldObjects)
      {
        relativePositions.Add(heldObj.position - transform.position);
      }
    }

    // Teleport player
    transform.position = targetPosition;

    // Move held objects
    for (int i = 0; i < heldObjects.Count; i++)
    {
      if (heldObjects[i] != null)
      {
        heldObjects[i].position = transform.position + relativePositions[i];
      }
    }
  }

  // Hide all TP points immediately
  void HideAllTPPoints()
  {
    foreach (TPPoint point in tpPoints)
    {
      point.ForceHidePoint();
    }
  }

  // Reset teleporter to inactive state
  void ResetTeleporterState()
  {
    currentTargetPoint = null;
    isTeleporterActive = false;
  }

  // - HELD OBJECT DETECTION
  // Find objects currently held by player
  private void FindHeldObjects(List<Transform> heldObjects)
  {
    heldObjects.Clear();

    // Search for objects near player
    Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 1.5f);

    foreach (Collider col in nearbyObjects)
    {
      bool isHeld = false;

      // Check grabbable layers
      isHeld = CheckGrabbableLayers(col);

      // Check physics joints
      if (!isHeld)
      {
        isHeld = CheckPhysicsJoints(col);
      }

      // Check XR interaction components
      if (!isHeld)
      {
        isHeld = CheckXRInteractionComponents(col);
      }

      // Add to held objects if detected as held
      if (isHeld && !heldObjects.Contains(col.transform))
      {
        heldObjects.Add(col.transform);
      }
    }
  }

  // Check if object is on grabbable layer
  bool CheckGrabbableLayers(Collider col)
  {
    foreach (string layerName in grabLayerNames)
    {
      int layerIndex = LayerMask.NameToLayer(layerName);
      if (layerIndex != -1 && col.gameObject.layer == layerIndex)
      {
        return true;
      }
    }
    return false;
  }

  // Check for physics joints connecting to player
  bool CheckPhysicsJoints(Collider col)
  {
    Joint[] joints = col.GetComponentsInChildren<Joint>();
    foreach (Joint joint in joints)
    {
      if (joint.connectedBody != null)
      {
        if (joint.connectedBody.transform.IsChildOf(transform) ||
            joint.connectedBody.transform == transform)
        {
          return true;
        }
      }
    }
    return false;
  }

  // Check for XR interaction system components
  bool CheckXRInteractionComponents(Collider col)
  {
    string[] interactionComponentNames = new string[] {
      "XRGrabInteractable", "OVRGrabbable", "Grabbable", "GrabPoint"
    };

    foreach (string compName in interactionComponentNames)
    {
      Component comp = col.GetComponent(compName);
      if (comp != null)
      {
        // Try to check grab state via reflection
        try
        {
          System.Reflection.PropertyInfo isGrabbedProp = comp.GetType().GetProperty("isGrabbed");
          if (isGrabbedProp != null)
          {
            bool grabbed = (bool)isGrabbedProp.GetValue(comp);
            if (grabbed)
            {
              return true;
            }
          }
        }
        catch (System.Exception)
        {
          // Ignore reflection errors
        }

        // Assume might be held if component exists
        return true;
      }
    }
    return false;
  }

  // - CLEANUP
  void OnDestroy()
  {
    // Clean up beam segments
    if (beamSegmentObjects != null)
    {
      foreach (GameObject segment in beamSegmentObjects)
      {
        if (segment != null)
        {
          Destroy(segment);
        }
      }
    }

    // Clean up material instance
    if (beamMaterialInstance != null)
    {
      Destroy(beamMaterialInstance);
    }
  }
}