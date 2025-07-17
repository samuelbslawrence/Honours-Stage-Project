using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles main menu actions with VR-safe scene transitions
/// This replaces your original MenuButtons.cs content
/// </summary>
public class MenuActions : MonoBehaviour
{
  [Header("Scene Transition Settings")]
  [SerializeField] private float transitionDelay = 0.5f;
  [SerializeField] private bool debugTransitions = true;
  [SerializeField] private string investigationSceneName = "Roling";

  [Header("VR Cleanup Settings")]
  [SerializeField] private bool enableVRCleanup = true;
  [SerializeField] private float vrCleanupDelay = 0.2f;

  [Header("Audio (Optional)")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip buttonClickSound;
  [SerializeField] private AudioClip transitionSound;

  private bool isTransitioning = false;

  void Start()
  {
    // Setup audio if not assigned
    if (audioSource == null)
    {
      audioSource = GetComponent<AudioSource>();
    }

    if (debugTransitions)
    {
      Debug.Log("MenuActions: Initialized with VR-safe transitions");
    }
  }

  /// <summary>
  /// Starts the investigation scene with VR-safe transition
  /// </summary>
  public void StartInvestigation()
  {
    if (isTransitioning)
    {
      Debug.LogWarning("MenuActions: Already transitioning, ignoring request");
      return;
    }

    PlayButtonSound();

    if (debugTransitions)
    {
      Debug.Log($"MenuActions: Starting investigation scene transition to '{investigationSceneName}'");
    }

    StartCoroutine(SafeSceneTransition(investigationSceneName));
  }

  /// <summary>
  /// Quits the application with VR-safe cleanup
  /// </summary>
  public void QuitApplication()
  {
    if (isTransitioning)
    {
      Debug.LogWarning("MenuActions: Already transitioning, ignoring quit request");
      return;
    }

    PlayButtonSound();

    if (debugTransitions)
    {
      Debug.Log("MenuActions: Quitting application with VR cleanup");
    }

    StartCoroutine(SafeApplicationQuit());
  }

  /// <summary>
  /// Safe scene transition that properly handles VR cleanup
  /// </summary>
  private IEnumerator SafeSceneTransition(string sceneName)
  {
    isTransitioning = true;

    // Play transition sound
    PlayTransitionSound();

    // Step 1: Disable VR input systems if VR cleanup is enabled
    if (enableVRCleanup)
    {
      DisableVRInputSystems();

      // Step 2: Wait for VR systems to process the disable
      yield return new WaitForEndOfFrame();
      yield return new WaitForSeconds(vrCleanupDelay);

      // Step 3: Clean up any remaining VR references
      CleanupVRReferences();

      // Step 4: Wait for cleanup to complete
      yield return new WaitForEndOfFrame();
    }

    // Step 5: Standard transition delay
    yield return new WaitForSeconds(transitionDelay);

    // Step 6: Load the new scene with error handling
    LoadSceneWithFallback(sceneName);
  }

  /// <summary>
  /// Load scene with error handling (separated from coroutine)
  /// </summary>
  private void LoadSceneWithFallback(string sceneName)
  {
    try
    {
      if (debugTransitions)
      {
        Debug.Log($"MenuActions: Loading scene: {sceneName}");
      }

      SceneManager.LoadScene(sceneName);
    }
    catch (System.Exception e)
    {
      Debug.LogError($"MenuActions: Error during scene transition: {e.Message}");

      // Fallback: try direct scene load
      StartCoroutine(FallbackSceneLoad(sceneName));
    }
  }

  /// <summary>
  /// Fallback scene loading coroutine
  /// </summary>
  private IEnumerator FallbackSceneLoad(string sceneName)
  {
    yield return new WaitForSeconds(0.1f);

    try
    {
      if (debugTransitions)
      {
        Debug.Log("MenuActions: Attempting fallback scene load");
      }
      SceneManager.LoadScene(sceneName);
    }
    catch (System.Exception fallbackError)
    {
      Debug.LogError($"MenuActions: Fallback scene load failed: {fallbackError.Message}");
      isTransitioning = false;
    }
  }

  /// <summary>
  /// Safe application quit that handles VR cleanup
  /// </summary>
  private IEnumerator SafeApplicationQuit()
  {
    isTransitioning = true;

    // Step 1: Disable VR systems if cleanup is enabled
    if (enableVRCleanup)
    {
      DisableVRInputSystems();

      // Step 2: Wait for cleanup
      yield return new WaitForEndOfFrame();
      yield return new WaitForSeconds(vrCleanupDelay);

      // Step 3: Clean up VR references
      CleanupVRReferences();

      // Step 4: Final wait
      yield return new WaitForEndOfFrame();
    }

    // Step 5: Standard quit delay
    yield return new WaitForSeconds(transitionDelay);

    // Step 6: Quit application with error handling
    QuitApplicationWithFallback();
  }

  /// <summary>
  /// Quit application with error handling (separated from coroutine)
  /// </summary>
  private void QuitApplicationWithFallback()
  {
    try
    {
      if (debugTransitions)
      {
        Debug.Log("MenuActions: Application quit requested");
      }

      Application.Quit();

      // For editor testing
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    catch (System.Exception e)
    {
      Debug.LogError($"MenuActions: Error during application quit: {e.Message}");

      // Fallback quit
      Application.Quit();
    }
  }

  /// <summary>
  /// Disable VR input systems to prevent updates during transition
  /// </summary>
  private void DisableVRInputSystems()
  {
    if (debugTransitions)
    {
      Debug.Log("MenuActions: Disabling VR input systems...");
    }

    try
    {
      // Disable OVR Manager if present
      OVRManager ovrManager = FindObjectOfType<OVRManager>();
      if (ovrManager != null)
      {
        ovrManager.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled OVRManager");
        }
      }

      // Disable OVR Camera Rig if present
      OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig != null)
      {
        cameraRig.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled OVRCameraRig");
        }
      }

      // Disable controller helpers
      OVRControllerHelper[] controllerHelpers = FindObjectsOfType<OVRControllerHelper>();
      foreach (OVRControllerHelper helper in controllerHelpers)
      {
        if (helper != null)
        {
          helper.enabled = false;
        }
      }

      if (controllerHelpers.Length > 0 && debugTransitions)
      {
        Debug.Log($"MenuActions: Disabled {controllerHelpers.Length} controller helpers");
      }

      // Disable XR interaction systems
      DisableXRInteractionSystems();

      // Disable custom VR scripts
      DisableCustomVRScripts();
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"MenuActions: Error disabling VR systems: {e.Message}");
    }
  }

  /// <summary>
  /// Disable XR Interaction Toolkit components
  /// </summary>
  private void DisableXRInteractionSystems()
  {
    try
    {
      // Find and disable XR Interaction Manager
      var interactionManager = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
      if (interactionManager != null)
      {
        interactionManager.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled XR Interaction Manager");
        }
      }

      // Disable XR Controllers
      var controllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRController>();
      foreach (var controller in controllers)
      {
        if (controller != null)
        {
          controller.enabled = false;
        }
      }

      if (controllers.Length > 0 && debugTransitions)
      {
        Debug.Log($"MenuActions: Disabled {controllers.Length} XR controllers");
      }

      // Disable XR Interactables
      var interactables = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable>();
      foreach (var interactable in interactables)
      {
        if (interactable != null)
        {
          interactable.enabled = false;
        }
      }

      if (interactables.Length > 0 && debugTransitions)
      {
        Debug.Log($"MenuActions: Disabled {interactables.Length} XR interactables");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"MenuActions: Error disabling XR Interaction systems: {e.Message}");
    }
  }

  /// <summary>
  /// Disable custom VR scripts that might cause issues
  /// </summary>
  private void DisableCustomVRScripts()
  {
    try
    {
      // Disable TeleportSystem
      TeleportSystem teleportSystem = FindObjectOfType<TeleportSystem>();
      if (teleportSystem != null)
      {
        teleportSystem.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled TeleportSystem");
        }
      }

      // Disable ToolSpawner
      ToolSpawner toolSpawner = FindObjectOfType<ToolSpawner>();
      if (toolSpawner != null)
      {
        toolSpawner.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled ToolSpawner");
        }
      }

      // Disable VRCleanupUtility to prevent conflicts
      VRCleanupUtility vrCleanup = FindObjectOfType<VRCleanupUtility>();
      if (vrCleanup != null)
      {
        vrCleanup.enabled = false;
        if (debugTransitions)
        {
          Debug.Log("MenuActions: Disabled VRCleanupUtility");
        }
      }

      // Disable any other VR-related scripts
      MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
      foreach (MonoBehaviour script in allScripts)
      {
        if (script != null && script != this)
        {
          // Check if the script name contains VR-related keywords
          string scriptName = script.GetType().Name.ToLower();
          if (scriptName.Contains("vr") || scriptName.Contains("ovr") ||
              scriptName.Contains("oculus") || scriptName.Contains("controller"))
          {
            script.enabled = false;
            if (debugTransitions)
            {
              Debug.Log($"MenuActions: Disabled VR script: {script.GetType().Name}");
            }
          }
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"MenuActions: Error disabling custom VR scripts: {e.Message}");
    }
  }

  /// <summary>
  /// Clean up VR object references
  /// </summary>
  private void CleanupVRReferences()
  {
    if (debugTransitions)
    {
      Debug.Log("MenuActions: Cleaning up VR references...");
    }

    try
    {
      // Force garbage collection to clean up references
      System.GC.Collect();
      System.GC.WaitForPendingFinalizers();

      // Clear any static references if your scripts use them
      ClearStaticReferences();

      if (debugTransitions)
      {
        Debug.Log("MenuActions: VR cleanup complete");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"MenuActions: Error during VR cleanup: {e.Message}");
    }
  }

  /// <summary>
  /// Clear static references that might hold onto destroyed objects
  /// </summary>
  private void ClearStaticReferences()
  {
    try
    {
      // Clear EvidenceChecklist singleton
      var evidenceChecklistInstance = typeof(EvidenceChecklist).GetField("instance",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
      if (evidenceChecklistInstance != null)
      {
        evidenceChecklistInstance.SetValue(null, null);
      }

      // Add any other static references your scripts might have
      // Example: YourScript.staticReference = null;

      if (debugTransitions)
      {
        Debug.Log("MenuActions: Static references cleared");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"MenuActions: Error clearing static references: {e.Message}");
    }
  }

  /// <summary>
  /// Play button click sound
  /// </summary>
  private void PlayButtonSound()
  {
    if (audioSource != null && buttonClickSound != null)
    {
      audioSource.PlayOneShot(buttonClickSound);
    }
  }

  /// <summary>
  /// Play transition sound
  /// </summary>
  private void PlayTransitionSound()
  {
    if (audioSource != null && transitionSound != null)
    {
      audioSource.PlayOneShot(transitionSound);
    }
  }

  /// <summary>
  /// Public method to change investigation scene name
  /// </summary>
  public void SetInvestigationSceneName(string sceneName)
  {
    investigationSceneName = sceneName;
    if (debugTransitions)
    {
      Debug.Log($"MenuActions: Investigation scene name changed to: {sceneName}");
    }
  }

  /// <summary>
  /// Public method to trigger VR cleanup manually
  /// </summary>
  [ContextMenu("Manual VR Cleanup")]
  public void ManualVRCleanup()
  {
    if (!isTransitioning)
    {
      StartCoroutine(PerformManualVRCleanup());
    }
  }

  private IEnumerator PerformManualVRCleanup()
  {
    DisableVRInputSystems();
    yield return new WaitForSeconds(vrCleanupDelay);
    CleanupVRReferences();

    if (debugTransitions)
    {
      Debug.Log("MenuActions: Manual VR cleanup complete");
    }
  }

  // Lifecycle methods
  void OnDestroy()
  {
    StopAllCoroutines();
    if (debugTransitions)
    {
      Debug.Log("MenuActions: OnDestroy called");
    }
  }

  void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus && debugTransitions)
    {
      Debug.Log("MenuActions: Application paused");
    }
  }

  void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus && debugTransitions)
    {
      Debug.Log("MenuActions: Application lost focus");
    }
  }
}