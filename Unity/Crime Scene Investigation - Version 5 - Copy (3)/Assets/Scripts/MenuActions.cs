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
  }

  /// <summary>
  /// Starts the investigation scene with VR-safe transition
  /// </summary>
  public void StartInvestigation()
  {
    if (isTransitioning)
    {
      return;
    }

    PlayButtonSound();
    StartCoroutine(SafeSceneTransition(investigationSceneName));
  }

  /// <summary>
  /// Quits the application with VR-safe cleanup
  /// </summary>
  public void QuitApplication()
  {
    if (isTransitioning)
    {
      return;
    }

    PlayButtonSound();
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
      SceneManager.LoadScene(sceneName);
    }
    catch (System.Exception e)
    {
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
      SceneManager.LoadScene(sceneName);
    }
    catch (System.Exception fallbackError)
    {
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
      Application.Quit();

      // For editor testing
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    catch (System.Exception e)
    {
      // Fallback quit
      Application.Quit();
    }
  }

  /// <summary>
  /// Disable VR input systems to prevent updates during transition
  /// </summary>
  private void DisableVRInputSystems()
  {
    try
    {
      // Disable OVR Manager if present
      OVRManager ovrManager = FindObjectOfType<OVRManager>();
      if (ovrManager != null)
      {
        ovrManager.enabled = false;
      }

      // Disable OVR Camera Rig if present
      OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig != null)
      {
        cameraRig.enabled = false;
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

      // Disable XR interaction systems
      DisableXRInteractionSystems();

      // Disable custom VR scripts
      DisableCustomVRScripts();
    }
    catch (System.Exception e)
    {
      // Silent error handling
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

      // Disable XR Interactables
      var interactables = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable>();
      foreach (var interactable in interactables)
      {
        if (interactable != null)
        {
          interactable.enabled = false;
        }
      }
    }
    catch (System.Exception e)
    {
      // Silent error handling
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
      }

      // Disable ToolSpawner
      ToolSpawner toolSpawner = FindObjectOfType<ToolSpawner>();
      if (toolSpawner != null)
      {
        toolSpawner.enabled = false;
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
          }
        }
      }
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  /// <summary>
  /// Clean up VR object references
  /// </summary>
  private void CleanupVRReferences()
  {
    try
    {
      // Force garbage collection to clean up references
      System.GC.Collect();
      System.GC.WaitForPendingFinalizers();

      // Clear any static references if your scripts use them
      ClearStaticReferences();
    }
    catch (System.Exception e)
    {
      // Silent error handling
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

      // Clear ToolSpawner singleton
      var toolSpawnerInstance = typeof(ToolSpawner).GetField("instance",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
      if (toolSpawnerInstance != null)
      {
        toolSpawnerInstance.SetValue(null, null);
      }
    }
    catch (System.Exception e)
    {
      // Silent error handling
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
  }

  // Lifecycle methods
  void OnDestroy()
  {
    StopAllCoroutines();
  }
}