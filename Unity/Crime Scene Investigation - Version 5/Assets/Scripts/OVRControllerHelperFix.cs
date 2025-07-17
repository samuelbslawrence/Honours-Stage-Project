using UnityEngine;
using System.Collections;

/// <summary>
/// Fixes the missing OVRControllerHelper issue on the left controller
/// Attach this to your OVRCameraRig or LeftHandAnchor
/// </summary>
public class OVRControllerHelperFix : MonoBehaviour
{
  [Header("Fix Settings")]
  [SerializeField] private bool enableFix = true;
  [SerializeField] private bool debugFix = true;
  [SerializeField] private float checkInterval = 0.5f;
  [SerializeField] private int maxRecreationAttempts = 5;

  [Header("Controller References")]
  [SerializeField] private OVRCameraRig cameraRig;
  [SerializeField] private bool autoFindCameraRig = true;

  [Header("Controller Helper Prefabs (Optional)")]
  [SerializeField] private GameObject leftControllerHelperPrefab;
  [SerializeField] private GameObject rightControllerHelperPrefab;

  // Tracking variables
  private OVRControllerHelper leftControllerHelper;
  private OVRControllerHelper rightControllerHelper;
  private int leftRecreationAttempts = 0;
  private int rightRecreationAttempts = 0;
  private bool isFixing = false;

  void Start()
  {
    if (enableFix)
    {
      FindCameraRig();
      FindExistingControllerHelpers();
      StartCoroutine(ControllerHelperMonitor());

      if (debugFix)
      {
        Debug.Log("🎮 OVRControllerHelperFix: Started monitoring controller helpers");
      }
    }
  }

  void FindCameraRig()
  {
    if (autoFindCameraRig && cameraRig == null)
    {
      cameraRig = FindObjectOfType<OVRCameraRig>();
      if (cameraRig == null)
      {
        cameraRig = GetComponentInParent<OVRCameraRig>();
      }
    }

    if (cameraRig == null)
    {
      Debug.LogError("OVRControllerHelperFix: No OVRCameraRig found! Cannot fix controller helpers.");
      enabled = false;
    }
    else if (debugFix)
    {
      Debug.Log($"🎮 OVRControllerHelperFix: Found OVRCameraRig: {cameraRig.name}");
    }
  }

  void FindExistingControllerHelpers()
  {
    if (cameraRig == null) return;

    // Find left controller helper
    if (cameraRig.leftHandAnchor != null)
    {
      leftControllerHelper = cameraRig.leftHandAnchor.GetComponentInChildren<OVRControllerHelper>();
    }

    // Find right controller helper
    if (cameraRig.rightHandAnchor != null)
    {
      rightControllerHelper = cameraRig.rightHandAnchor.GetComponentInChildren<OVRControllerHelper>();
    }

    if (debugFix)
    {
      Debug.Log($"🎮 Found existing controller helpers - Left: {(leftControllerHelper != null ? "✅" : "❌")}, Right: {(rightControllerHelper != null ? "✅" : "❌")}");
    }
  }

  IEnumerator ControllerHelperMonitor()
  {
    while (enableFix)
    {
      yield return new WaitForSeconds(checkInterval);

      if (!isFixing)
      {
        CheckAndFixControllerHelpers();
      }
    }
  }

  void CheckAndFixControllerHelpers()
  {
    if (cameraRig == null) return;

    // Check left controller helper
    if (leftControllerHelper == null || !IsControllerHelperValid(leftControllerHelper))
    {
      if (debugFix)
      {
        Debug.LogWarning("🎮 OVRControllerHelperFix: Left controller helper is missing or invalid!");
      }

      StartCoroutine(FixLeftControllerHelper());
    }

    // Check right controller helper
    if (rightControllerHelper == null || !IsControllerHelperValid(rightControllerHelper))
    {
      if (debugFix)
      {
        Debug.LogWarning("🎮 OVRControllerHelperFix: Right controller helper is missing or invalid!");
      }

      StartCoroutine(FixRightControllerHelper());
    }
  }

  bool IsControllerHelperValid(OVRControllerHelper helper)
  {
    if (helper == null) return false;
    if (helper.gameObject == null) return false;
    if (!helper.gameObject.activeInHierarchy) return false;
    if (!helper.enabled) return false;

    return true;
  }

  IEnumerator FixLeftControllerHelper()
  {
    if (isFixing || leftRecreationAttempts >= maxRecreationAttempts) yield break;

    isFixing = true;
    leftRecreationAttempts++;

    if (debugFix)
    {
      Debug.Log($"🔧 OVRControllerHelperFix: Attempting to fix left controller helper (attempt {leftRecreationAttempts})");
    }

    // Wait a frame to ensure scene is stable
    yield return new WaitForEndOfFrame();

    // Try to find existing helper first
    if (cameraRig.leftHandAnchor != null)
    {
      leftControllerHelper = cameraRig.leftHandAnchor.GetComponentInChildren<OVRControllerHelper>();

      if (leftControllerHelper != null)
      {
        // Found existing helper, try to reactivate it
        if (!leftControllerHelper.gameObject.activeInHierarchy)
        {
          leftControllerHelper.gameObject.SetActive(true);
        }

        if (!leftControllerHelper.enabled)
        {
          leftControllerHelper.enabled = true;
        }

        if (debugFix)
        {
          Debug.Log("🔧 OVRControllerHelperFix: Reactivated existing left controller helper");
        }
      }
      else
      {
        // No existing helper found, create a new one
        leftControllerHelper = CreateControllerHelper(cameraRig.leftHandAnchor, OVRInput.Controller.LTouch, "LeftControllerHelper");

        if (debugFix)
        {
          Debug.Log("🔧 OVRControllerHelperFix: Created new left controller helper");
        }
      }
    }

    isFixing = false;
  }

  IEnumerator FixRightControllerHelper()
  {
    if (isFixing || rightRecreationAttempts >= maxRecreationAttempts) yield break;

    isFixing = true;
    rightRecreationAttempts++;

    if (debugFix)
    {
      Debug.Log($"🔧 OVRControllerHelperFix: Attempting to fix right controller helper (attempt {rightRecreationAttempts})");
    }

    yield return new WaitForEndOfFrame();

    if (cameraRig.rightHandAnchor != null)
    {
      rightControllerHelper = cameraRig.rightHandAnchor.GetComponentInChildren<OVRControllerHelper>();

      if (rightControllerHelper != null)
      {
        // Found existing helper, try to reactivate it
        if (!rightControllerHelper.gameObject.activeInHierarchy)
        {
          rightControllerHelper.gameObject.SetActive(true);
        }

        if (!rightControllerHelper.enabled)
        {
          rightControllerHelper.enabled = true;
        }

        if (debugFix)
        {
          Debug.Log("🔧 OVRControllerHelperFix: Reactivated existing right controller helper");
        }
      }
      else
      {
        // No existing helper found, create a new one
        rightControllerHelper = CreateControllerHelper(cameraRig.rightHandAnchor, OVRInput.Controller.RTouch, "RightControllerHelper");

        if (debugFix)
        {
          Debug.Log("🔧 OVRControllerHelperFix: Created new right controller helper");
        }
      }
    }

    isFixing = false;
  }

  OVRControllerHelper CreateControllerHelper(Transform parent, OVRInput.Controller controllerType, string name)
  {
    GameObject controllerHelperObj;

    // Use prefab if available
    if (controllerType == OVRInput.Controller.LTouch && leftControllerHelperPrefab != null)
    {
      controllerHelperObj = Instantiate(leftControllerHelperPrefab, parent);
    }
    else if (controllerType == OVRInput.Controller.RTouch && rightControllerHelperPrefab != null)
    {
      controllerHelperObj = Instantiate(rightControllerHelperPrefab, parent);
    }
    else
    {
      // Create from scratch
      controllerHelperObj = new GameObject(name);
      controllerHelperObj.transform.SetParent(parent);
      controllerHelperObj.transform.localPosition = Vector3.zero;
      controllerHelperObj.transform.localRotation = Quaternion.identity;
      controllerHelperObj.transform.localScale = Vector3.one;
    }

    // Add or configure OVRControllerHelper component
    OVRControllerHelper helper = controllerHelperObj.GetComponent<OVRControllerHelper>();
    if (helper == null)
    {
      helper = controllerHelperObj.AddComponent<OVRControllerHelper>();
    }

    // Configure the controller helper
    ConfigureControllerHelper(helper, controllerType);

    return helper;
  }

  void ConfigureControllerHelper(OVRControllerHelper helper, OVRInput.Controller controllerType)
  {
    if (helper == null) return;

    try
    {
      // Set controller type using reflection to access private fields
      var controllerField = typeof(OVRControllerHelper).GetField("m_controller",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      if (controllerField != null)
      {
        controllerField.SetValue(helper, controllerType);
      }

      // Enable the helper
      helper.enabled = true;

      if (debugFix)
      {
        Debug.Log($"🔧 OVRControllerHelperFix: Configured controller helper for {controllerType}");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"OVRControllerHelperFix: Error configuring controller helper: {e.Message}");
    }
  }

  // Public methods for manual control
  [ContextMenu("Force Fix Left Controller")]
  public void ForceFixLeftController()
  {
    if (enableFix)
    {
      leftRecreationAttempts = 0;
      StartCoroutine(FixLeftControllerHelper());
    }
  }

  [ContextMenu("Force Fix Right Controller")]
  public void ForceFixRightController()
  {
    if (enableFix)
    {
      rightRecreationAttempts = 0;
      StartCoroutine(FixRightControllerHelper());
    }
  }

  [ContextMenu("Force Fix Both Controllers")]
  public void ForceFixBothControllers()
  {
    ForceFixLeftController();
    ForceFixRightController();
  }

  [ContextMenu("Check Controller Status")]
  public void CheckControllerStatus()
  {
    Debug.Log("=== CONTROLLER HELPER STATUS ===");

    if (cameraRig != null)
    {
      Debug.Log($"OVRCameraRig: {cameraRig.name}");
      Debug.Log($"Left Hand Anchor: {(cameraRig.leftHandAnchor != null ? cameraRig.leftHandAnchor.name : "NULL")}");
      Debug.Log($"Right Hand Anchor: {(cameraRig.rightHandAnchor != null ? cameraRig.rightHandAnchor.name : "NULL")}");
    }

    Debug.Log($"Left Controller Helper: {(leftControllerHelper != null ? $"✅ {leftControllerHelper.name}" : "❌ NULL")}");
    Debug.Log($"Right Controller Helper: {(rightControllerHelper != null ? $"✅ {rightControllerHelper.name}" : "❌ NULL")}");

    if (leftControllerHelper != null)
    {
      Debug.Log($"  Left Active: {leftControllerHelper.gameObject.activeInHierarchy}");
      Debug.Log($"  Left Enabled: {leftControllerHelper.enabled}");
    }

    if (rightControllerHelper != null)
    {
      Debug.Log($"  Right Active: {rightControllerHelper.gameObject.activeInHierarchy}");
      Debug.Log($"  Right Enabled: {rightControllerHelper.enabled}");
    }

    Debug.Log($"Recreation Attempts - Left: {leftRecreationAttempts}, Right: {rightRecreationAttempts}");
    Debug.Log("=== END STATUS ===");
  }

  // Reset recreation attempts
  public void ResetRecreationAttempts()
  {
    leftRecreationAttempts = 0;
    rightRecreationAttempts = 0;

    if (debugFix)
    {
      Debug.Log("🔄 OVRControllerHelperFix: Reset recreation attempts");
    }
  }

  void OnDestroy()
  {
    if (debugFix)
    {
      Debug.Log("🎮 OVRControllerHelperFix: Component destroyed");
    }
  }
}