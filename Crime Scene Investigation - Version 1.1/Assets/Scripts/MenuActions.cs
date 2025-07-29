using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuActions : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
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

  // - PRIVATE STATE VARIABLES
  // Transition state tracking
  private bool isTransitioning = false;

  // - INITIALIZATION
  void Start()
  {
    // Setup audio component if not assigned
    if (audioSource == null)
    {
      audioSource = GetComponent<AudioSource>();
    }
  }

  // - PUBLIC MENU ACTION METHODS
  // Start investigation scene with VR-safe transition
  public void StartInvestigation()
  {
    if (isTransitioning)
    {
      return;
    }

    PlayButtonSound();
    StartCoroutine(SafeSceneTransition(investigationSceneName));
  }

  // Quit application with VR-safe cleanup
  public void QuitApplication()
  {
    if (isTransitioning)
    {
      return;
    }

    PlayButtonSound();
    StartCoroutine(SafeApplicationQuit());
  }

  // Set investigation scene name programmatically
  public void SetInvestigationSceneName(string sceneName)
  {
    investigationSceneName = sceneName;
  }

  // - SCENE TRANSITION SYSTEM
  // Safe scene transition with VR cleanup
  private IEnumerator SafeSceneTransition(string sceneName)
  {
    isTransitioning = true;

    // Play transition audio
    PlayTransitionSound();

    // Perform VR cleanup if enabled
    if (enableVRCleanup)
    {
      yield return StartCoroutine(PerformVRCleanup());
    }

    // Standard transition delay
    yield return new WaitForSeconds(transitionDelay);

    // Load scene with error handling
    LoadSceneWithFallback(sceneName);
  }

  // Load scene with fallback error handling
  private void LoadSceneWithFallback(string sceneName)
  {
    try
    {
      SceneManager.LoadScene(sceneName);
    }
    catch (System.Exception e)
    {
      // Attempt fallback scene load
      StartCoroutine(FallbackSceneLoad(sceneName));
    }
  }

  // Fallback scene loading coroutine
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

  // - APPLICATION QUIT SYSTEM
  // Safe application quit with VR cleanup
  private IEnumerator SafeApplicationQuit()
  {
    isTransitioning = true;

    // Perform VR cleanup if enabled
    if (enableVRCleanup)
    {
      yield return StartCoroutine(PerformVRCleanup());
    }

    // Standard quit delay
    yield return new WaitForSeconds(transitionDelay);

    // Quit with error handling
    QuitApplicationWithFallback();
  }

  // Quit application with fallback handling
  private void QuitApplicationWithFallback()
  {
    try
    {
      Application.Quit();

#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    catch (System.Exception e)
    {
      // Fallback quit attempt
      Application.Quit();
    }
  }

  // - VR CLEANUP SYSTEM
  // Perform VR system cleanup
  private IEnumerator PerformVRCleanup()
  {
    // Disable VR input systems
    DisableVRInputSystems();

    // Wait for VR systems to process disable
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(vrCleanupDelay);

    // Clean up VR references
    CleanupVRReferences();

    // Final cleanup wait
    yield return new WaitForEndOfFrame();
  }

  // Disable VR input systems to prevent updates during transition
  private void DisableVRInputSystems()
  {
    try
    {
      // Disable OVR components
      DisableOVRComponents();

      // Disable XR interaction systems
      DisableXRInteractionSystems();

      // Disable custom VR scripts
      DisableCustomVRScripts();
    }
    catch (System.Exception e)
    {
      // Silent error handling for VR cleanup
    }
  }

  // Disable OVR-specific components
  private void DisableOVRComponents()
  {
    // Disable OVR Manager
    OVRManager ovrManager = FindObjectOfType<OVRManager>();
    if (ovrManager != null)
    {
      ovrManager.enabled = false;
    }

    // Disable OVR Camera Rig
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
  }

  // Disable XR Interaction Toolkit components
  private void DisableXRInteractionSystems()
  {
    try
    {
      // Disable XR Interaction Manager
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

  // Disable custom VR scripts that might cause issues
  private void DisableCustomVRScripts()
  {
    try
    {
      // Disable specific custom VR components
      TeleportSystem teleportSystem = FindObjectOfType<TeleportSystem>();
      if (teleportSystem != null)
      {
        teleportSystem.enabled = false;
      }

      ToolSpawner toolSpawner = FindObjectOfType<ToolSpawner>();
      if (toolSpawner != null)
      {
        toolSpawner.enabled = false;
      }

      // Disable VR-related scripts by name pattern
      DisableVRScriptsByPattern();
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  // Disable scripts with VR-related names
  private void DisableVRScriptsByPattern()
  {
    MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
    foreach (MonoBehaviour script in allScripts)
    {
      if (script != null && script != this)
      {
        // Check for VR-related keywords in script name
        string scriptName = script.GetType().Name.ToLower();
        if (scriptName.Contains("vr") || scriptName.Contains("ovr") ||
            scriptName.Contains("oculus") || scriptName.Contains("controller"))
        {
          script.enabled = false;
        }
      }
    }
  }

  // Clean up VR object references
  private void CleanupVRReferences()
  {
    try
    {
      // Force garbage collection
      System.GC.Collect();
      System.GC.WaitForPendingFinalizers();

      // Clear static references
      ClearStaticReferences();
    }
    catch (System.Exception e)
    {
      // Silent error handling
    }
  }

  // Clear static references that might hold destroyed objects
  private void ClearStaticReferences()
  {
    try
    {
      // Clear EvidenceChecklist singleton reference
      var evidenceChecklistInstance = typeof(EvidenceChecklist).GetField("instance",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
      if (evidenceChecklistInstance != null)
      {
        evidenceChecklistInstance.SetValue(null, null);
      }

      // Clear ToolSpawner singleton reference
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

  // - AUDIO SYSTEM
  // Play button click sound effect
  private void PlayButtonSound()
  {
    if (audioSource != null && buttonClickSound != null)
    {
      audioSource.PlayOneShot(buttonClickSound);
    }
  }

  // Play transition sound effect
  private void PlayTransitionSound()
  {
    if (audioSource != null && transitionSound != null)
    {
      audioSource.PlayOneShot(transitionSound);
    }
  }

  // - MANUAL VR CLEANUP
  // Manually trigger VR cleanup for testing
  [ContextMenu("Manual VR Cleanup")]
  public void ManualVRCleanup()
  {
    if (!isTransitioning)
    {
      StartCoroutine(PerformManualVRCleanupCoroutine());
    }
  }

  // Manual VR cleanup coroutine
  private IEnumerator PerformManualVRCleanupCoroutine()
  {
    yield return StartCoroutine(PerformVRCleanup());
  }

  // - CLEANUP
  void OnDestroy()
  {
    // Stop all running coroutines
    StopAllCoroutines();
  }
}