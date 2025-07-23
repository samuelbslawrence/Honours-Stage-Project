using UnityEngine;
using System.Collections; // Required for Coroutines if you want to add delayed checks

// Ensure this GameObject has a Renderer component, as visibility checks rely on it.
[RequireComponent(typeof(Renderer))]
public class FingerprintMonitor : MonoBehaviour
{
  [Header("Dependencies")]
  [Tooltip("Reference to the main CameraScript in your scene.")]
  [SerializeField] private CameraScript cameraScript;
  [Tooltip("Reference to the DustBrush script in your scene.")]
  [SerializeField] private DustBrush dustBrush;

  [Header("Debug Settings")]
  [Tooltip("Enable to log visibility and brushed status to the console.")]
  [SerializeField] private bool debugOutput = true;
  [Tooltip("How often to update and log the status (in seconds).")]
  [SerializeField] private float updateInterval = 1.0f;

  private Renderer fingerprintRenderer;
  private Camera mainCamera; // The actual camera used for visibility checks
  private float lastUpdateTime;

  void Start()
  {
    fingerprintRenderer = GetComponent<Renderer>();
    if (fingerprintRenderer == null)
    {
      Debug.LogError($"FingerprintMonitor: No Renderer found on {gameObject.name}. This script requires a Renderer to check visibility.");
      enabled = false; // Disable script if no renderer
      return;
    }

    // Attempt to find CameraScript and DustBrush automatically if not assigned
    if (cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
      if (cameraScript == null && debugOutput)
      {
        Debug.LogWarning("FingerprintMonitor: CameraScript not found automatically. Visibility checks might not function correctly.");
      }
    }

    if (dustBrush == null)
    {
      dustBrush = FindObjectOfType<DustBrush>();
      if (dustBrush == null && debugOutput)
      {
        Debug.LogWarning("FingerprintMonitor: DustBrush not found automatically. Brushed status will always be 'false'.");
      }
    }

    // Get the camera from CameraScript if available, otherwise fall back to Camera.main
    if (cameraScript != null && cameraScript.GetComponent<Camera>() != null)
    {
      mainCamera = cameraScript.GetComponent<Camera>();
    }
    else if (Camera.main != null)
    {
      mainCamera = Camera.main;
      if (debugOutput)
      {
        //Debug.LogWarning("FingerprintMonitor: Using Camera.main for visibility checks, as CameraScript's camera could not be determined.");
      }
    }
    else
    {
      Debug.LogError("FingerprintMonitor: No main camera found in the scene. Visibility checks cannot be performed.");
      enabled = false;
    }

    lastUpdateTime = Time.time;
    // Perform an initial check immediately
    UpdateStatus();
  }

  void Update()
  {
    // Update status periodically instead of every frame for performance
    if (Time.time >= lastUpdateTime + updateInterval)
    {
      UpdateStatus();
      lastUpdateTime = Time.time;
    }
  }

  private void UpdateStatus()
  {
    bool isVisible = IsVisibleToCamera();
    bool isBrushed = IsBrushed();

    if (debugOutput)
    {
      //Debug.Log($"[{gameObject.name}] - Visible: {isVisible}, Brushed: {isBrushed}");
    }

    // You could add visual feedback here, e.g., change color based on status
    // Example:
    // if (isVisible && isBrushed) {
    //     fingerprintRenderer.material.color = Color.blue;
    // } else if (isVisible) {
    //     fingerprintRenderer.material.color = Color.yellow;
    // } else {
    //     fingerprintRenderer.material.color = Color.grey;
    // }
  }

  /// <summary>
  /// Checks if the fingerprint's renderer is currently visible by the assigned camera.
  /// </summary>
  /// <returns>True if visible, false otherwise.</returns>
  public bool IsVisibleToCamera()
  {
    if (mainCamera == null || fingerprintRenderer == null)
    {
      return false;
    }

    // This is a simple check if the renderer is within any camera's frustum.
    // For a more precise check against a specific camera, you would use
    // GeometryUtility.TestPlanesAABB or WorldToViewportPoint.
    // For debugging purposes, renderer.isVisible is often sufficient.
    return fingerprintRenderer.isVisible;

    // More precise check against a specific camera (uncomment if needed):
    // Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
    // return GeometryUtility.TestPlanesAABB(planes, fingerprintRenderer.bounds);
  }

  /// <summary>
  /// Checks if the fingerprint has been sufficiently revealed by the DustBrush.
  /// </summary>
  /// <returns>True if brushed/revealed, false otherwise.</returns>
  public bool IsBrushed()
  {
    if (dustBrush == null)
    {
      return false; // Cannot check if DustBrush is not assigned
    }
    // Assuming DustBrush's IsFingerprintSufficientlyRevealed uses its own threshold
    return dustBrush.IsFingerprintSufficientlyRevealed(gameObject);
  }

  // You can add ContextMenu items for manual testing in the editor
  [ContextMenu("Check Status Now")]
  public void CheckStatusNow()
  {
    UpdateStatus();
  }

  // Public properties to access status from other scripts if needed
  public bool CurrentIsVisibleToCamera => IsVisibleToCamera();
  public bool CurrentIsBrushed => IsBrushed();
}
