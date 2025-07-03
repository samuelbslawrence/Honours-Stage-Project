using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class TeleportSystem : MonoBehaviour
{
  [Header("Teleport Settings")]
  [SerializeField] private Transform tpPointsParent; // Parent containing all TP Points
  [SerializeField] private float raycastDistance = 20f; // How far to raycast for TP Points
  [SerializeField] private float raycastRadius = 0.1f; // Radius for spherecast
  [SerializeField] private LayerMask tpLayerMask = -1; // Layer mask for TP Points
  [SerializeField] private string alphaThresholdProperty = "_Cutoff"; // Shader property name
  [SerializeField] private bool teleportHeldObjects = true; // Whether to teleport objects player is holding
  [SerializeField] private string[] grabLayerNames = new string[] { "Grab", "Grabbable", "Held" }; // Layer names for grabbable objects

  [Header("Visual Settings")]
  [SerializeField] private float revealDelay = 0.05f; // Delay between revealing each point
  [SerializeField] private float revealDuration = 0.3f; // How long it takes to reveal a point
  [SerializeField] private float hideDuration = 0.2f; // How long it takes to hide a point

  [Header("Beam Settings")]
  [SerializeField] private bool showBeam = true; // Whether to show the teleport beam
  [SerializeField] private GameObject beamPrefab; // Optional custom beam prefab
  [SerializeField] private float beamWidth = 0.02f; // Width of the teleport beam
  [SerializeField] private float beamEndWidth = 0.005f; // Width at the end of the beam (tapered)
  [SerializeField] private Color beamStartColor = new Color(0.2f, 0.6f, 1f, 0.8f); // Color at start of beam
  [SerializeField] private Color beamEndColor = new Color(0.2f, 0.6f, 1f, 0.0f); // Color at end of beam (transparent)
  [SerializeField] private Material beamMaterial; // Material for the beam line renderer
  [SerializeField] private int beamSegments = 10; // Number of segments in the beam (for curved beam)

  // State tracking
  private bool isTeleporterActive = false;
  private bool wasButtonPressed = false;
  private List<TPPoint> tpPoints = new List<TPPoint>();
  private TPPoint currentTargetPoint = null;
  private Coroutine revealCoroutine = null;
  private Camera mainCamera;

  // Beam rendering - NEW APPROACH
  private Transform rightControllerTransform;
  private GameObject[] beamSegmentObjects;
  private Material beamMaterialInstance;

  void Start()
  {
    // Find main camera
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

    // Try to find TP Points parent if not assigned
    if (tpPointsParent == null)
    {
      GameObject tpParent = GameObject.Find("TP Points");
      if (tpParent != null)
      {
        tpPointsParent = tpParent.transform;
        Debug.Log("Found TP Points parent: " + tpParent.name);
      }
    }

    // Initialize TP Points
    InitializeTPPoints();

    // Set default layer mask if not specified
    if (tpLayerMask.value == -1)
    {
      tpLayerMask = Physics.DefaultRaycastLayers;
    }

    // Create beam material instance
    if (beamMaterial == null)
    {
      beamMaterial = new Material(Shader.Find("Sprites/Default"));
      beamMaterial.color = beamStartColor;
    }

    // Create a unique instance of the material so we don't modify the original
    beamMaterialInstance = new Material(beamMaterial);

    // Setup beam segments
    SetupBeamSegments();

    // Hide beam initially
    SetBeamActive(false);
  }

  void SetupBeamSegments()
  {
    // Create beam segment objects
    beamSegmentObjects = new GameObject[beamSegments - 1];

    for (int i = 0; i < beamSegments - 1; i++)
    {
      if (beamPrefab != null)
      {
        // Use the prefab if provided
        beamSegmentObjects[i] = Instantiate(beamPrefab);
      }
      else
      {
        // Create a simple quad for each segment
        beamSegmentObjects[i] = CreateBeamSegment();
      }

      // Don't show initially
      beamSegmentObjects[i].SetActive(false);
    }

    Debug.Log("Created " + (beamSegments - 1) + " beam segments");
  }

  GameObject CreateBeamSegment()
  {
    GameObject segmentObj = new GameObject("BeamSegment");

    // Add a quad mesh
    MeshFilter meshFilter = segmentObj.AddComponent<MeshFilter>();
    MeshRenderer meshRenderer = segmentObj.AddComponent<MeshRenderer>();

    // Create a simple quad mesh
    Mesh mesh = new Mesh();

    // Vertices (simple quad)
    Vector3[] vertices = new Vector3[4]
    {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
    };

    // Triangles
    int[] triangles = new int[6]
    {
            0, 2, 1,
            2, 3, 1
    };

    // UVs
    Vector2[] uvs = new Vector2[4]
    {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
    };

    // Assign to mesh
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.RecalculateNormals();

    // Assign mesh and material
    meshFilter.mesh = mesh;
    meshRenderer.material = beamMaterialInstance;

    return segmentObj;
  }

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

  void Update()
  {
    // Check for B button press on controller
    bool isButtonPressed = CheckTeleportButtonPressed();

    // Button just pressed
    if (isButtonPressed && !wasButtonPressed)
    {
      ActivateTeleporter();
    }
    // Button just released
    else if (!isButtonPressed && wasButtonPressed)
    {
      TriggerTeleport();
    }

    // Update teleporter ray if active
    if (isTeleporterActive)
    {
      UpdateTeleporterTarget();
    }

    // Update button state
    wasButtonPressed = isButtonPressed;
  }

  void InitializeTPPoints()
  {
    tpPoints.Clear();

    // If we have a parent, get all TP Points from it
    if (tpPointsParent != null)
    {
      for (int i = 0; i < tpPointsParent.childCount; i++)
      {
        Transform child = tpPointsParent.GetChild(i);
        TPPoint tpPoint = child.GetComponent<TPPoint>();

        // If the point doesn't have a TPPoint component, add one
        if (tpPoint == null)
        {
          tpPoint = child.gameObject.AddComponent<TPPoint>();
        }

        tpPoints.Add(tpPoint);
        Debug.Log("Added TP Point: " + child.name);
      }
    }
    else
    {
      // If no parent, try to find all TP Points by name in the scene
      TPPoint[] allTPPoints = GameObject.FindObjectsOfType<TPPoint>();
      if (allTPPoints.Length > 0)
      {
        tpPoints.AddRange(allTPPoints);
        Debug.Log("Found " + allTPPoints.Length + " TP Points in scene");
      }
      else
      {
        // Try to find objects containing "TP Point" in their name
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
            Debug.Log("Added TP Point by name: " + obj.name);
          }
        }
      }
    }

    // Initialize all TP Points
    foreach (TPPoint point in tpPoints)
    {
      point.Initialize(alphaThresholdProperty);
    }

    Debug.Log("Initialized " + tpPoints.Count + " TP Points");
  }

  bool CheckTeleportButtonPressed()
  {
    // Check for VR controller B button specifically
    if (XRSettings.isDeviceActive)
    {
      try
      {
        var inputDevices = new List<InputDevice>();

        // Try to get the right controller first
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);

        foreach (var device in inputDevices)
        {
          // Check specifically for secondary button (B on Oculus)
          if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue) && secondaryButtonValue)
          {
            return true;
          }

          // Some controllers may use primary button for B
          if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
          {
            // For Oculus, only the right controller's primary button should be A, not B
            // But for other controllers, this might be B
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
        // Ignore errors
      }
    }

    // Keyboard fallback for testing - only B key
    if (Input.GetKey(KeyCode.B))
    {
      return true;
    }

    return false;
  }

  void ActivateTeleporter()
  {
    if (isTeleporterActive)
      return;

    Debug.Log("Activating teleporter");
    isTeleporterActive = true;

    // Show beam
    if (showBeam)
    {
      SetBeamActive(true);
    }

    // Stop any ongoing animations
    if (revealCoroutine != null)
    {
      StopCoroutine(revealCoroutine);
    }

    // Sort TP Points by distance to player
    SortTPPointsByDistance();

    // Start revealing TP Points sequentially
    revealCoroutine = StartCoroutine(RevealTPPointsSequentially());
  }

  void SortTPPointsByDistance()
  {
    // Get player position (using this transform as the player reference)
    Vector3 playerPosition = transform.position;

    // Sort TP Points by distance to player (closest first)
    tpPoints.Sort((a, b) =>
        Vector3.Distance(a.transform.position, playerPosition)
        .CompareTo(Vector3.Distance(b.transform.position, playerPosition)));
  }

  IEnumerator RevealTPPointsSequentially()
  {
    // Reveal each point one by one, closest first
    foreach (TPPoint point in tpPoints)
    {
      point.RevealPoint(revealDuration);
      yield return new WaitForSeconds(revealDelay);
    }
  }

  void UpdateTeleporterTarget()
  {
    // Cast a ray from the camera/controller to find a TP Point
    Ray ray;
    Vector3 rayOrigin = mainCamera.transform.position;
    Vector3 rayDirection = mainCamera.transform.forward;
    Quaternion rayRotation = Quaternion.identity;

    // Try to use the right controller if available
    bool usingController = false;

    // First, try to find RightHandAnchor specifically
    GameObject rightHandAnchor = GameObject.Find("RightHandAnchor");
    if (rightHandAnchor != null)
    {
      rayOrigin = rightHandAnchor.transform.position;
      rayDirection = rightHandAnchor.transform.forward;
      rayRotation = rightHandAnchor.transform.rotation;
      rightControllerTransform = rightHandAnchor.transform;
      usingController = true;

      Debug.Log("Using RightHandAnchor for teleport beam");
    }
    // Fall back to XR Input system if RightHandAnchor not found
    else if (XRSettings.isDeviceActive)
    {
      try
      {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);

        if (inputDevices.Count > 0)
        {
          // Use right controller for pointing
          InputDevice rightController = inputDevices[0];

          if (rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
              rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
          {
            rayOrigin = position;
            rayDirection = rotation * Vector3.forward;
            rayRotation = rotation;
            usingController = true;

            // Store controller transform for beam attachment
            if (rightControllerTransform == null)
            {
              // Try to find the controller object with specific names
              string[] possibleControllerNames = new string[] {
                                "RightHandAnchor", "RightHand", "RightController", "Right Hand Controller",
                                "OVRControllerRight", "XRRightController", "Right Controller"
                            };

              foreach (string controllerName in possibleControllerNames)
              {
                GameObject controllerObj = GameObject.Find(controllerName);
                if (controllerObj != null)
                {
                  rightControllerTransform = controllerObj.transform;
                  Debug.Log("Found right controller transform: " + controllerObj.name);
                  break;
                }
              }

              // If still not found, try to search by component names
              if (rightControllerTransform == null)
              {
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                  // Look for common controller names
                  if (obj.name.Contains("Right") &&
                  (obj.name.Contains("Controller") || obj.name.Contains("Hand")))
                  {
                    rightControllerTransform = obj.transform;
                    Debug.Log("Found right controller transform: " + obj.name);
                    break;
                  }
                }
              }
            }
          }
        }
      }
      catch (System.Exception)
      {
        // Ignore errors
      }
    }

    ray = new Ray(rayOrigin, rayDirection);

    // Update beam visualization
    if (showBeam && beamSegmentObjects != null)
    {
      UpdateBeamVisual(rayOrigin, rayDirection, rayRotation);
    }

    // Debug ray
    Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.yellow);

    RaycastHit hit;
    TPPoint hitPoint = null;
    float hitDistance = raycastDistance;

    // Use spherecast to make it easier to hit TP Points
    if (Physics.SphereCast(ray, raycastRadius, out hit, raycastDistance, tpLayerMask))
    {
      // Store hit distance for beam visualization
      hitDistance = hit.distance;

      // Check if we hit a TP Point
      hitPoint = hit.transform.GetComponent<TPPoint>();
      if (hitPoint == null)
      {
        // Try to find parent with TP Point
        hitPoint = hit.transform.GetComponentInParent<TPPoint>();
      }

      // Log what we hit
      if (hitPoint != null)
      {
        Debug.Log("Pointing at teleporter: " + hitPoint.name + " (distance: " + hit.distance.ToString("F2") + "m)");
      }
      else
      {
        Debug.Log("Hit object: " + hit.transform.name + " (not a teleporter)");
      }
    }
    else
    {
      Debug.Log("Not pointing at any teleporter");
    }

    // If target changed
    if (hitPoint != currentTargetPoint)
    {
      // Hide previous target
      if (currentTargetPoint != null)
      {
        currentTargetPoint.RevealPoint(revealDuration);
      }

      // Update current target
      currentTargetPoint = hitPoint;

      // If we have a new target, highlight it and hide others
      if (currentTargetPoint != null)
      {
        // Keep current target visible
        currentTargetPoint.HighlightPoint();
        Debug.Log("TARGET SELECTED: " + currentTargetPoint.name);

        // Hide all other points
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
        // If no target, reveal all points again
        foreach (TPPoint point in tpPoints)
        {
          point.RevealPoint(revealDuration);
        }
      }
    }
  }

  // NEW APPROACH: Use individual beam segments that are positioned in world space
  private void UpdateBeamVisual(Vector3 origin, Vector3 direction, Quaternion rotation)
  {
    if (beamSegmentObjects == null || beamSegmentObjects.Length == 0)
      return;

    float beamLength = raycastDistance;

    // Place and orient each beam segment
    for (int i = 0; i < beamSegmentObjects.Length; i++)
    {
      GameObject segment = beamSegmentObjects[i];
      if (segment == null) continue;

      // Calculate segment position (center of segment)
      float segStart = (float)i / beamSegmentObjects.Length;
      float segEnd = (float)(i + 1) / beamSegmentObjects.Length;
      float segCenter = (segStart + segEnd) / 2;

      // Position at center of segment
      Vector3 segmentPosition = origin + direction * (beamLength * segCenter);
      segment.transform.position = segmentPosition;

      // Orient towards the ray direction
      segment.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);

      // Scale based on width and segment length
      float segmentLength = beamLength / beamSegmentObjects.Length;
      float segmentWidth = Mathf.Lerp(beamWidth, beamEndWidth, segCenter);
      segment.transform.localScale = new Vector3(segmentWidth, segmentLength, 1);

      // Update color - fade to transparent
      if (segment.GetComponent<Renderer>() != null)
      {
        Renderer renderer = segment.GetComponent<Renderer>();
        Color segColor = Color.Lerp(beamStartColor, beamEndColor, segCenter);
        renderer.material.color = segColor;
      }
    }
  }

  void TriggerTeleport()
  {
    if (!isTeleporterActive)
      return;

    Debug.Log("Teleport triggered");

    // Hide beam
    if (showBeam)
    {
      SetBeamActive(false);
    }

    // Teleport to the target point if one is selected
    if (currentTargetPoint != null)
    {
      Vector3 targetPosition = currentTargetPoint.transform.position;

      // Keep original Y position of the player to avoid changing height
      targetPosition.y = transform.position.y;

      Debug.Log("TELEPORTING to: " + currentTargetPoint.name + " at position " + targetPosition);

      // Find held objects to teleport with the player
      List<Transform> heldObjects = new List<Transform>();

      if (teleportHeldObjects)
      {
        // Find objects held by the player
        FindHeldObjects(heldObjects);

        if (heldObjects.Count > 0)
        {
          Debug.Log("Found " + heldObjects.Count + " held objects to teleport");
        }
      }

      // Get the original positions relative to the player
      List<Vector3> relativePositions = new List<Vector3>();
      foreach (Transform heldObj in heldObjects)
      {
        relativePositions.Add(heldObj.position - transform.position);
      }

      // Store original position for debugging
      Vector3 oldPosition = transform.position;

      // Teleport the player (this transform)
      transform.position = targetPosition;

      // Move held objects with the player
      for (int i = 0; i < heldObjects.Count; i++)
      {
        if (heldObjects[i] != null)
        {
          // Maintain the relative position to the player
          heldObjects[i].position = transform.position + relativePositions[i];
          Debug.Log("Teleported held object: " + heldObjects[i].name);
        }
      }

      Debug.Log("Teleportation complete: Player moved from " + oldPosition + " to " + transform.position);
    }
    else
    {
      Debug.Log("No teleport target selected - teleportation canceled");
    }

    // Ensure ALL points are fully hidden when teleporting ends
    // This fixes the bug where some points might stay visible when not teleporting
    foreach (TPPoint point in tpPoints)
    {
      // Force immediate hiding instead of animation to ensure they're hidden
      point.ForceHidePoint();
    }

    // Reset state
    currentTargetPoint = null;
    isTeleporterActive = false;
  }

  // Find objects the player is holding
  private void FindHeldObjects(List<Transform> heldObjects)
  {
    heldObjects.Clear();

    // Try to find directly held objects through physics grabbing
    Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 1.5f);

    foreach (Collider col in nearbyObjects)
    {
      bool isHeld = false;

      // Check if the object is on a grabbable layer
      foreach (string layerName in grabLayerNames)
      {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex != -1 && col.gameObject.layer == layerIndex)
        {
          isHeld = true;
          break;
        }
      }

      // Alternative: Check if the object has a joint connected to the player or controllers
      if (!isHeld)
      {
        Joint[] joints = col.GetComponentsInChildren<Joint>();
        foreach (Joint joint in joints)
        {
          if (joint.connectedBody != null)
          {
            // Check if connected to player or controller
            if (joint.connectedBody.transform.IsChildOf(transform) ||
                joint.connectedBody.transform == transform)
            {
              isHeld = true;
              break;
            }
          }
        }
      }

      // Check for any XR interaction system markers
      if (!isHeld)
      {
        // Check for common XR interaction component names
        string[] interactionComponentNames = new string[] {
                    "XRGrabInteractable", "OVRGrabbable", "Grabbable", "GrabPoint"
                };

        foreach (string compName in interactionComponentNames)
        {
          Component comp = col.GetComponent(compName);
          if (comp != null)
          {
            // Try to use reflection to check if it's grabbed
            try
            {
              System.Reflection.PropertyInfo isGrabbedProp = comp.GetType().GetProperty("isGrabbed");
              if (isGrabbedProp != null)
              {
                bool grabbed = (bool)isGrabbedProp.GetValue(comp);
                if (grabbed)
                {
                  isHeld = true;
                  break;
                }
              }
            }
            catch (System.Exception)
            {
              // Ignore reflection errors
            }

            // If we can't check dynamically, assume it might be held
            isHeld = true;
            break;
          }
        }
      }

      // If any check passed, add to held objects
      if (isHeld && !heldObjects.Contains(col.transform))
      {
        heldObjects.Add(col.transform);
        Debug.Log("Found held object: " + col.name);
      }
    }
  }

  // Called when the script is destroyed
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

    // Clean up materials
    if (beamMaterialInstance != null)
    {
      Destroy(beamMaterialInstance);
    }
  }
}