using UnityEngine;

/// <summary>
/// Temporary protection component added to controller helpers during startup
/// </summary>
public class ControllerHelperProtection : MonoBehaviour
{
  private VRStartupProtector parentProtector;
  private bool debugEnabled;

  public void Initialize(VRStartupProtector protector, bool debug)
  {
    parentProtector = protector;
    debugEnabled = debug;
  }

  void OnDestroy()
  {
    if (debugEnabled && parentProtector != null)
    {
      Debug.LogWarning($"🛡️ ControllerHelperProtection: {gameObject.name} protection component destroyed!");

      // If the protection is still active and this is being destroyed, something's wrong
      if (parentProtector.IsProtectionActive)
      {
        Debug.LogError($"💥 Controller helper {gameObject.name} is being destroyed during startup protection period!");
        Debug.LogError($"Destruction stack trace:\n{System.Environment.StackTrace}");
      }
    }
  }

  void OnDisable()
  {
    if (debugEnabled && parentProtector != null && parentProtector.IsProtectionActive)
    {
      Debug.LogWarning($"⚠️ ControllerHelperProtection: {gameObject.name} was disabled during protection period!");
    }
  }
}