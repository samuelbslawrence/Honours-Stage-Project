using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Utility script to prevent VR reference errors during scene transitions
/// Attach this to your main VR rig or OVR Manager
/// </summary>
public class VRCleanupUtility : MonoBehaviour
{
  [Header("Cleanup Settings")]
  [SerializeField] private bool enableAutoCleanup = true;
  [SerializeField] private bool debugCleanup = true;
  [SerializeField] private float cleanupDelay = 0.1f;

  [Header("Scene Transition Detection")]
  [SerializeField] private bool detectSceneChanges = true;
  [SerializeField] private string[] menuSceneNames = { "MainMenu", "Menu", "Start" };
  [SerializeField] private string[] gameSceneNames = { "Roling", "Game", "Investigation" };

  private bool isCleaningUp = false;
  private string currentSceneName;

  void Start()
  {
    currentSceneName = SceneManager.GetActiveScene().name;

    if (enableAutoCleanup)
    {
      // Listen for scene changes
      SceneManager.sceneLoaded += OnSceneLoaded;
      SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    if (debugCleanup)
    {
      Debug.Log($"VRCleanupUtility: Initialized in scene '{currentSceneName}'");
    }
  }

  void OnDestroy()
  {
    if (enableAutoCleanup)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Destroyed");
    }
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (debugCleanup)
    {
      Debug.Log($"VRCleanupUtility: Scene loaded - {scene.name}");
    }

    currentSceneName = scene.name;

    // Small delay to allow scene to fully load
    StartCoroutine(DelayedSceneSetup());
  }

  private void OnSceneUnloaded(Scene scene)
  {
    if (debugCleanup)
    {
      Debug.Log($"VRCleanupUtility: Scene unloaded - {scene.name}");
    }

    // Perform cleanup when scene is unloaded
    if (!isCleaningUp)
    {
      PerformVRCleanup();
    }
  }

  private IEnumerator DelayedSceneSetup()
  {
    yield return new WaitForSeconds(cleanupDelay);

    // Ensure VR systems are properly initialized in the new scene
    EnsureVRSystemsAreValid();
  }

  /// <summary>
  /// Call this before transitioning to a new scene
  /// </summary>
  public void PrepareForSceneTransition()
  {
    if (isCleaningUp) return;

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Preparing for scene transition...");
    }

    StartCoroutine(SafeSceneTransitionCleanup());
  }

  private IEnumerator SafeSceneTransitionCleanup()
  {
    isCleaningUp = true;

    // Step 1: Disable VR input updates
    DisableVRInputUpdates();

    // Step 2: Wait for current frame to complete
    yield return new WaitForEndOfFrame();

    // Step 3: Clean up VR references
    PerformVRCleanup();

    // Step 4: Wait for cleanup to complete
    yield return new WaitForSeconds(cleanupDelay);

    isCleaningUp = false;

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Scene transition cleanup complete");
    }
  }

  private void DisableVRInputUpdates()
  {
    SafelyDisableVRComponents();

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Disabled VR input updates");
    }
  }

  private void SafelyDisableVRComponents()
  {
    try
    {
      // Disable OVR components that might cause the error
      var ovrControllerHelpers = FindObjectsOfType<OVRControllerHelper>();
      foreach (var helper in ovrControllerHelpers)
      {
        if (helper != null)
        {
          helper.enabled = false;
        }
      }

      // Disable OVR Camera Rig temporarily
      var cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig != null)
      {
        cameraRig.enabled = false;
      }

      // Disable any Oculus Interaction components
      DisableOculusInteractionComponents();
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRCleanupUtility: Error disabling VR components: {e.Message}");
    }
  }

  private void DisableOculusInteractionComponents()
  {
    try
    {
      // Find and disable common Oculus Interaction components
      var allComponents = FindObjectsOfType<MonoBehaviour>();

      foreach (var component in allComponents)
      {
        if (component != null && component.GetType().Namespace != null &&
            component.GetType().Namespace.Contains("Oculus.Interaction"))
        {
          component.enabled = false;
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRCleanupUtility: Error disabling Oculus Interaction components: {e.Message}");
    }
  }

  private void PerformVRCleanup()
  {
    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Performing VR cleanup...");
    }

    CleanupVRReferences();

    // Force garbage collection
    System.GC.Collect();
    System.GC.WaitForPendingFinalizers();

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: VR cleanup complete");
    }
  }

  private void CleanupVRReferences()
  {
    // Clear any static references that might hold onto destroyed VR objects
    ClearSingletonInstances();
    ClearControllerReferences();
  }

  private void ClearSingletonInstances()
  {
    try
    {
      // Clear EvidenceChecklist singleton
      var evidenceChecklistType = typeof(EvidenceChecklist);
      var instanceField = evidenceChecklistType.GetField("instance",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

      if (instanceField != null)
      {
        instanceField.SetValue(null, null);
      }

      // Add other singleton cleanup here as needed
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRCleanupUtility: Error clearing singleton instances: {e.Message}");
    }
  }

  private void ClearControllerReferences()
  {
    try
    {
      // Clear any cached controller references in your scripts
      var toolSpawner = FindObjectOfType<ToolSpawner>();
      if (toolSpawner != null)
      {
        // Use reflection to clear private controller references
        var type = toolSpawner.GetType();
        var controllerFields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in controllerFields)
        {
          if (field.Name.ToLower().Contains("controller"))
          {
            field.SetValue(toolSpawner, null);
          }
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRCleanupUtility: Error clearing controller references: {e.Message}");
    }
  }

  private void EnsureVRSystemsAreValid()
  {
    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: Ensuring VR systems are valid...");
    }

    ReenableVRSystems();

    if (debugCleanup)
    {
      Debug.Log("VRCleanupUtility: VR systems validation complete");
    }
  }

  private void ReenableVRSystems()
  {
    try
    {
      // Re-enable VR systems if they were disabled
      var cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig != null && !cameraRig.enabled)
      {
        cameraRig.enabled = true;
      }

      var ovrManager = FindObjectOfType<OVRManager>();
      if (ovrManager != null && !ovrManager.enabled)
      {
        ovrManager.enabled = true;
      }

      // Re-enable controller helpers
      var controllerHelpers = FindObjectsOfType<OVRControllerHelper>();
      foreach (var helper in controllerHelpers)
      {
        if (helper != null && !helper.enabled)
        {
          helper.enabled = true;
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRCleanupUtility: Error re-enabling VR systems: {e.Message}");
    }
  }

  /// <summary>
  /// Public method to manually trigger VR cleanup
  /// </summary>
  [ContextMenu("Perform VR Cleanup")]
  public void ManualVRCleanup()
  {
    PerformVRCleanup();
  }

  /// <summary>
  /// Public method to safely disable VR systems
  /// </summary>
  [ContextMenu("Disable VR Systems")]
  public void DisableVRSystems()
  {
    DisableVRInputUpdates();
  }

  /// <summary>
  /// Public method to validate VR systems
  /// </summary>
  [ContextMenu("Validate VR Systems")]
  public void ValidateVRSystems()
  {
    EnsureVRSystemsAreValid();
  }

  // Application lifecycle events
  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus && enableAutoCleanup)
    {
      PerformVRCleanup();
    }
  }

  private void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus && enableAutoCleanup)
    {
      PerformVRCleanup();
    }
    else if (hasFocus)
    {
      // Small delay to allow systems to re-initialize
      StartCoroutine(DelayedSceneSetup());
    }
  }
}