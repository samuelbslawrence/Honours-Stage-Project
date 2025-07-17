using UnityEngine;
using System.Collections;

/// <summary>
/// Prevents VR controller helpers from being destroyed during game startup
/// Attach this to your OVRCameraRig and set execution order to -100 (very early)
/// </summary>
[DefaultExecutionOrder(-100)]
public class VRStartupProtector : MonoBehaviour
{
  [Header("Protection Settings")]
  [SerializeField] private bool enableStartupProtection = true;
  [SerializeField] private bool debugProtection = true;
  [SerializeField] private float protectionDuration = 5f; // Protect for first 5 seconds

  [Header("Controller References")]
  [SerializeField] private OVRCameraRig cameraRig;

  private OVRControllerHelper[] allControllerHelpers;
  private bool protectionActive = false;

  void Awake()
  {
    if (enableStartupProtection)
    {
      // This runs very early due to DefaultExecutionOrder(-100)
      StartVRControllerProtection();
    }
  }

  void StartVRControllerProtection()
  {
    if (debugProtection)
    {
      Debug.Log("🛡️ VRStartupProtector: Protecting VR controllers during startup");
    }

    // Find camera rig
    if (cameraRig == null)
    {
      cameraRig = GetComponent<OVRCameraRig>();
      if (cameraRig == null)
      {
        cameraRig = FindObjectOfType<OVRCameraRig>();
      }
    }

    if (cameraRig == null)
    {
      Debug.LogError("VRStartupProtector: No OVRCameraRig found!");
      return;
    }

    // Find all controller helpers
    allControllerHelpers = FindObjectsOfType<OVRControllerHelper>();

    if (debugProtection)
    {
      Debug.Log($"🛡️ VRStartupProtector: Found {allControllerHelpers.Length} controller helpers to protect");
      foreach (var helper in allControllerHelpers)
      {
        if (helper != null)
        {
          Debug.Log($"  - {helper.name} on {helper.transform.parent?.name}");
        }
      }
    }

    // Protect each controller helper
    foreach (var helper in allControllerHelpers)
    {
      ApplyControllerProtection(helper);
    }

    protectionActive = true;

    // Start protection duration timer
    StartCoroutine(StartupProtectionTimer());
  }

  void ApplyControllerProtection(OVRControllerHelper helper)
  {
    if (helper == null) return;

    try
    {
      // Add a protection component
      var protection = helper.gameObject.GetComponent<ControllerHelperProtection>();
      if (protection == null)
      {
        protection = helper.gameObject.AddComponent<ControllerHelperProtection>();
        protection.Initialize(this, debugProtection);
      }

      // Ensure the helper is enabled and active
      if (!helper.enabled)
      {
        helper.enabled = true;
        if (debugProtection)
        {
          Debug.Log($"🛡️ VRStartupProtector: Enabled controller helper {helper.name}");
        }
      }

      if (!helper.gameObject.activeInHierarchy)
      {
        helper.gameObject.SetActive(true);
        if (debugProtection)
        {
          Debug.Log($"🛡️ VRStartupProtector: Activated controller helper {helper.name}");
        }
      }

      // Make sure it has proper parent
      FixControllerParenting(helper);
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"VRStartupProtector: Error protecting {helper.name}: {e.Message}");
    }
  }

  void FixControllerParenting(OVRControllerHelper helper)
  {
    if (helper == null || cameraRig == null) return;

    Transform currentParent = helper.transform.parent;

    // Check if it's properly parented to a hand anchor
    bool isProperlyParented = false;

    if (currentParent != null)
    {
      // Check if parent is LeftHandAnchor or RightHandAnchor or their children
      Transform checkParent = currentParent;
      while (checkParent != null)
      {
        if (checkParent == cameraRig.leftHandAnchor || checkParent == cameraRig.rightHandAnchor)
        {
          isProperlyParented = true;
          break;
        }
        checkParent = checkParent.parent;
      }
    }

    if (!isProperlyParented)
    {
      // Try to determine which hand this should belong to
      string helperName = helper.name.ToLower();

      if (helperName.Contains("left") && cameraRig.leftHandAnchor != null)
      {
        helper.transform.SetParent(cameraRig.leftHandAnchor);
        helper.transform.localPosition = Vector3.zero;
        helper.transform.localRotation = Quaternion.identity;

        if (debugProtection)
        {
          Debug.Log($"🛡️ VRStartupProtector: Reparented {helper.name} to LeftHandAnchor");
        }
      }
      else if (helperName.Contains("right") && cameraRig.rightHandAnchor != null)
      {
        helper.transform.SetParent(cameraRig.rightHandAnchor);
        helper.transform.localPosition = Vector3.zero;
        helper.transform.localRotation = Quaternion.identity;

        if (debugProtection)
        {
          Debug.Log($"🛡️ VRStartupProtector: Reparented {helper.name} to RightHandAnchor");
        }
      }
    }
  }

  IEnumerator StartupProtectionTimer()
  {
    yield return new WaitForSeconds(protectionDuration);

    protectionActive = false;

    if (debugProtection)
    {
      Debug.Log($"🛡️ VRStartupProtector: Protection period ended after {protectionDuration} seconds");
    }

    // Remove protection components
    RemoveStartupProtectionComponents();
  }

  void RemoveStartupProtectionComponents()
  {
    var protectionComponents = FindObjectsOfType<ControllerHelperProtection>();
    foreach (var protection in protectionComponents)
    {
      if (protection != null)
      {
        Destroy(protection);
      }
    }

    if (debugProtection)
    {
      Debug.Log($"🛡️ VRStartupProtector: Removed {protectionComponents.Length} protection components");
    }
  }

  // Public method to manually check controller status
  [ContextMenu("Check Controller Helper Status")]
  public void CheckControllerHelperStatus()
  {
    Debug.Log("=== VR CONTROLLER HELPER STATUS ===");

    var helpers = FindObjectsOfType<OVRControllerHelper>();
    Debug.Log($"Total Controller Helpers Found: {helpers.Length}");

    foreach (var helper in helpers)
    {
      if (helper != null)
      {
        Debug.Log($"✅ {helper.name}:");
        Debug.Log($"  Active: {helper.gameObject.activeInHierarchy}");
        Debug.Log($"  Enabled: {helper.enabled}");
        Debug.Log($"  Parent: {(helper.transform.parent ? helper.transform.parent.name : "None")}");
        Debug.Log($"  Position: {helper.transform.position}");
      }
    }

    if (cameraRig != null)
    {
      Debug.Log($"Left Hand Anchor: {(cameraRig.leftHandAnchor ? cameraRig.leftHandAnchor.name : "NULL")}");
      Debug.Log($"Right Hand Anchor: {(cameraRig.rightHandAnchor ? cameraRig.rightHandAnchor.name : "NULL")}");

      if (cameraRig.leftHandAnchor)
      {
        var leftHelper = cameraRig.leftHandAnchor.GetComponentInChildren<OVRControllerHelper>();
        Debug.Log($"Left Helper in Anchor: {(leftHelper ? "✅ " + leftHelper.name : "❌ MISSING")}");
      }

      if (cameraRig.rightHandAnchor)
      {
        var rightHelper = cameraRig.rightHandAnchor.GetComponentInChildren<OVRControllerHelper>();
        Debug.Log($"Right Helper in Anchor: {(rightHelper ? "✅ " + rightHelper.name : "❌ MISSING")}");
      }
    }

    Debug.Log("=== END STATUS ===");
  }

  // Public method to manually restart protection
  [ContextMenu("Restart Controller Protection")]
  public void RestartControllerProtection()
  {
    if (debugProtection)
    {
      Debug.Log("🛡️ VRStartupProtector: Manually restarting controller protection");
    }

    StopAllCoroutines();
    protectionActive = false;
    StartVRControllerProtection();
  }

  // Public properties
  public bool IsProtectionActive => protectionActive;
  public OVRControllerHelper[] GetAllControllerHelpers() => allControllerHelpers;
}