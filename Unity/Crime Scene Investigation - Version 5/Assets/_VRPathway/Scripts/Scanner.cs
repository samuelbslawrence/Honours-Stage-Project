using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRScanner : MonoBehaviour
{
  [Header("Scanner Components")]
  public Animator animator;
  public LineRenderer laserRenderer;
  public TextMeshProUGUI targetName;
  public TextMeshProUGUI targetPosition;
  public AudioSource scannerSound;
  public Light scannerLight;

  [Header("Scanner Settings")]
  public float scanRange = 1000f;
  public float lightDuration = 0.2f;

  [Header("Movement Detection")]
  [SerializeField] private float movementThreshold = 0.00001f;
  [SerializeField] private float listenDuration = 5.0f;
  [SerializeField] private float triggerDebounceTime = 0.5f;

  // State tracking
  private bool isScanning = false;
  private bool isMoving = false;
  private bool canScan = false;
  private bool isPickedUp = false;
  private Vector3 lastPosition;
  private Quaternion lastRotation;
  private float lastMovementTime = 0f;
  private float lastScanTime = 0f;
  private float pickupDetectionTime = 1.0f; // Reduced time before considering it "put down"

  // Special trigger detection for Oculus
  private bool wasButtonPressed = false;

  void Start()
  {
    Debug.Log("VR SCANNER STARTING!");

    // Initialize position tracking
    lastPosition = transform.position;
    lastRotation = transform.rotation;

    // Hide scanner elements initially
    SetScannerState(false);

    Debug.Log("VR Scanner initialized successfully");
  }

  void Update()
  {
    // Skip if scanning
    if (isScanning)
      return;

    // Check if scanner is moving
    CheckMovement();

    // Update ability to scan based on movement
    UpdateCanScan();

    // Check for scan input if scanner is ready
    if (canScan)
    {
      // Add universal keyboard/mouse trigger for testing
      if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
      {
        PerformScan();
      }

      // Try to detect controller trigger presses
      bool triggerPressed = CheckAllTriggerMethods();

      if (triggerPressed && Time.time - lastScanTime > triggerDebounceTime)
      {
        PerformScan();
        lastScanTime = Time.time;
      }
    }

    // Show laser when ready to scan
    if (canScan)
    {
      UpdateLaserDisplay();
    }
  }

  void CheckMovement()
  {
    // Calculate movement
    float positionDelta = Vector3.Distance(transform.position, lastPosition);
    float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

    // Check if movement exceeds threshold
    bool wasMoving = isMoving;
    isMoving = positionDelta > movementThreshold || rotationDelta > movementThreshold * 100;

    // Update last position/rotation
    lastPosition = transform.position;
    lastRotation = transform.rotation;

    // Update timestamp if moving
    if (isMoving)
    {
      lastMovementTime = Time.time;
      Debug.Log("Scanner is moving!");

      // Check if this is the first time being picked up
      if (!isPickedUp)
      {
        OnScannerPickedUp();
      }
    }

    // Check if scanner should be considered "put down" (stopped moving for a while)
    float timeSinceMovement = Time.time - lastMovementTime;
    if (isPickedUp && timeSinceMovement > pickupDetectionTime)
    {
      Debug.Log("Scanner has been still for " + timeSinceMovement.ToString("F1") + " seconds - triggering put down");
      OnScannerPutDown();
    }
  }

  void OnScannerPickedUp()
  {
    isPickedUp = true;
    Debug.Log("Scanner picked up - opening animation");

    // Trigger opening animation
    if (animator != null)
    {
      animator.SetBool("Opened", true);
      Debug.Log("Scanner animation: Opened = true");
    }
    else
    {
      Debug.Log("No animator found on scanner");
    }
  }

  void OnScannerPutDown()
  {
    isPickedUp = false;
    Debug.Log("Scanner put down - playing closing animation");

    // Trigger reverse/closing animation
    if (animator != null)
    {
      animator.SetBool("Opened", false);
      Debug.Log("Scanner animation: Opened = false (reverse animation)");

      // Optional: Force animation speed for reverse
      // animator.speed = 1.0f; // Normal speed for closing
    }
    else
    {
      Debug.Log("No animator found - cannot play reverse animation");
    }

    // Hide scanner elements when put down
    SetScannerState(false);

    // Reset can scan state
    canScan = false;
  }

  void UpdateCanScan()
  {
    // Can only scan when picked up AND after recent movement
    float timeSinceMovement = Time.time - lastMovementTime;
    bool newCanScan = isPickedUp && (timeSinceMovement < listenDuration);

    if (newCanScan != canScan)
    {
      canScan = newCanScan;
      SetScannerState(canScan);
      Debug.Log("Scanner ready to scan: " + canScan);
    }
  }

  bool CheckAllTriggerMethods()
  {
    // Simple check for VR trigger button (EXACTLY like camera script)
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

  void PerformScan()
  {
    if (isScanning) return;

    Debug.Log("PERFORMING SCAN!");
    StartCoroutine(ScanSequence());
  }

  IEnumerator ScanSequence()
  {
    isScanning = true;

    // Scanner light on
    if (scannerLight != null)
    {
      scannerLight.enabled = true;
    }

    // Play sound
    if (scannerSound != null)
    {
      scannerSound.Play();
    }

    // Perform the scan
    CheckTargetsAndScan();

    // Wait for light duration
    yield return new WaitForSeconds(lightDuration);

    // Scanner light off
    if (scannerLight != null)
    {
      scannerLight.enabled = false;
    }

    // Scan done
    isScanning = false;
  }

  void CheckTargetsAndScan()
  {
    if (laserRenderer == null) return;

    RaycastHit hit;
    Vector3 startPoint = laserRenderer.transform.position;
    Vector3 direction = laserRenderer.transform.forward;

    if (Physics.Raycast(startPoint, direction, out hit, scanRange))
    {
      string objectName = hit.collider.name;
      Vector3 objectPosition = hit.collider.transform.position;
      float distance = hit.distance;

      Debug.Log("=== SCAN COMPLETE ===");
      Debug.Log("SCANNED OBJECT: " + objectName);
      Debug.Log("POSITION: " + objectPosition.ToString("F2"));
      Debug.Log("DISTANCE: " + distance.ToString("F2") + "m");
      Debug.Log("==================");

      // Update target information with scan results
      if (targetName != null)
      {
        targetName.text = "SCANNED: " + objectName;
        targetName.color = Color.black; // Black text
      }
      if (targetPosition != null)
      {
        targetPosition.text = "DIST: " + distance.ToString("F1") + "m\nPOS: " + objectPosition.ToString("F1");
        targetPosition.color = Color.black; // Black text
      }
    }
    else
    {
      Debug.Log("=== SCAN COMPLETE ===");
      Debug.Log("SCAN RESULT: No target found");
      Debug.Log("==================");

      if (targetName != null)
      {
        targetName.text = "SCAN: No Target";
        targetName.color = Color.black; // Black text
      }
      if (targetPosition != null)
      {
        targetPosition.text = "No Object Detected";
        targetPosition.color = Color.black; // Black text
      }
    }
  }

  void UpdateLaserDisplay()
  {
    if (laserRenderer == null) return;

    RaycastHit hit;
    Vector3 startPoint = laserRenderer.transform.position;
    Vector3 direction = laserRenderer.transform.forward;
    Vector3 endPoint = startPoint + direction * scanRange;

    // Perform raycast for laser display
    if (Physics.Raycast(startPoint, direction, out hit, scanRange))
    {
      endPoint = hit.point;

      string objectName = hit.collider.name;
      float distance = hit.distance;

      // Update UI with current target (live preview)
      if (targetName != null)
      {
        targetName.text = "TARGET: " + objectName;
        targetName.color = Color.black; // Black text
      }
      if (targetPosition != null)
      {
        targetPosition.text = "DISTANCE: " + distance.ToString("F1") + "m\nREADY TO SCAN";
        targetPosition.color = Color.black; // Black text
      }
    }
    else
    {
      // No target found
      if (targetName != null)
      {
        targetName.text = "NO TARGET";
        targetName.color = Color.black; // Black text
      }
      if (targetPosition != null)
      {
        targetPosition.text = "POINT AT OBJECT\nTO SCAN";
        targetPosition.color = Color.black; // Black text
      }
    }

    // Update laser line
    laserRenderer.SetPosition(0, Vector3.zero);
    laserRenderer.SetPosition(1, laserRenderer.transform.InverseTransformPoint(endPoint));
  }

  void SetScannerState(bool active)
  {
    Debug.Log("Setting scanner state to: " + active);

    // Show/hide scanner elements
    if (laserRenderer != null)
      laserRenderer.gameObject.SetActive(active);
    if (targetName != null)
      targetName.gameObject.SetActive(active);
    if (targetPosition != null)
      targetPosition.gameObject.SetActive(active);
  }

  // Public test method
  public void TestScanner()
  {
    Debug.Log("TEST SCANNER CALLED!");
    PerformScan();
  }

  // Force pickup for testing
  public void ForcePickup()
  {
    Debug.Log("FORCING SCANNER PICKUP FOR TESTING");
    OnScannerPickedUp();
    canScan = true;
    SetScannerState(true);
  }

  // Force put down for testing
  public void ForcePutDown()
  {
    Debug.Log("FORCING SCANNER PUT DOWN FOR TESTING");
    OnScannerPutDown();
  }

  // Manual scan trigger
  public void TriggerScan()
  {
    if (canScan)
    {
      PerformScan();
    }
    else
    {
      Debug.Log("Scanner not ready - pick it up and move it first!");
    }
  }
}