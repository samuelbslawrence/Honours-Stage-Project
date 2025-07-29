using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class FingerprintMonitor : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("Dependencies")]
  [Tooltip("Reference to the main CameraScript in your scene.")]
  [SerializeField] private CameraScript cameraScript;
  [Tooltip("Reference to the DustBrush script in your scene.")]
  [SerializeField] private DustBrush dustBrush;

  [Header("Update Settings")]
  [Tooltip("How often to update and log the status (in seconds).")]
  [SerializeField] private float updateInterval = 1.0f;

  // - PRIVATE STATE VARIABLES
  // Component references
  private Renderer fingerprintRenderer;
  private Camera mainCamera;

  // Update timing
  private float lastUpdateTime;

  // - INITIALIZATION
  void Start()
  {
    // Get required renderer component
    fingerprintRenderer = GetComponent<Renderer>();
    if (fingerprintRenderer == null)
    {
      Debug.LogError($"FingerprintMonitor: No Renderer found on {gameObject.name}. This script requires a Renderer to check visibility.");
      enabled = false;
      return;
    }

    // Find camera script automatically if not assigned
    if (cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
      if (cameraScript == null)
      {
        Debug.LogWarning("FingerprintMonitor: CameraScript not found automatically. Visibility checks might not function correctly.");
      }
    }

    // Find dust brush automatically if not assigned
    if (dustBrush == null)
    {
      dustBrush = FindObjectOfType<DustBrush>();
      if (dustBrush == null)
      {
        Debug.LogWarning("FingerprintMonitor: DustBrush not found automatically. Brushed status will always be 'false'.");
      }
    }

    // Setup main camera reference
    SetupMainCamera();

    // Initialize update timing and perform first check
    lastUpdateTime = Time.time;
    UpdateStatus();
  }

  // - UPDATE LOOP
  void Update()
  {
    // Update status periodically for performance
    if (Time.time >= lastUpdateTime + updateInterval)
    {
      UpdateStatus();
      lastUpdateTime = Time.time;
    }
  }

  // - CAMERA SETUP
  // Configure main camera reference for visibility checks
  private void SetupMainCamera()
  {
    // Try to get camera from camera script first
    if (cameraScript != null && cameraScript.GetComponent<Camera>() != null)
    {
      mainCamera = cameraScript.GetComponent<Camera>();
    }
    // Fallback to main camera
    else if (Camera.main != null)
    {
      mainCamera = Camera.main;
    }
    else
    {
      Debug.LogError("FingerprintMonitor: No main camera found in the scene. Visibility checks cannot be performed.");
      enabled = false;
    }
  }

  // - STATUS UPDATE SYSTEM
  // Update and process fingerprint status
  private void UpdateStatus()
  {
    bool isVisible = IsVisibleToCamera();
    bool isBrushed = IsBrushed();

    // Status information is available for other systems to query
    // Visual feedback could be added here if needed
  }

  // - VISIBILITY DETECTION
  // Check if fingerprint is visible to assigned camera
  public bool IsVisibleToCamera()
  {
    if (mainCamera == null || fingerprintRenderer == null)
    {
      return false;
    }

    // Simple visibility check using renderer
    return fingerprintRenderer.isVisible;
  }

  // - BRUSH STATUS DETECTION
  // Check if fingerprint has been sufficiently revealed by dust brush
  public bool IsBrushed()
  {
    if (dustBrush == null)
    {
      return false;
    }

    // Check if fingerprint is sufficiently revealed
    return dustBrush.IsFingerprintSufficientlyRevealed(gameObject);
  }

  // - PUBLIC API METHODS
  // Manual status check for testing
  [ContextMenu("Check Status Now")]
  public void CheckStatusNow()
  {
    UpdateStatus();
  }

  // Public properties for external access
  public bool CurrentIsVisibleToCamera => IsVisibleToCamera();
  public bool CurrentIsBrushed => IsBrushed();
}