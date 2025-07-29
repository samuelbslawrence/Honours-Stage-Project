using UnityEngine;
using System.Collections.Generic;

public class DustBrush : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("Brush Settings")]
  [Tooltip("How quickly the fingerprint becomes visible (0-1 per second)")]
  public float revealSpeed = 2.0f;

  [Tooltip("The white color to apply to revealed fingerprints")]
  public Color revealedColor = Color.white;

  [Tooltip("Material to swap to when fingerprint is revealed")]
  public Material revealedMaterial;

  [Header("Testing")]
  [Tooltip("Instantly reveal fingerprints for testing")]
  public bool instantRevealMode = false;

  // - PRIVATE STATE VARIABLES
  // Fingerprint tracking collections
  private HashSet<GameObject> fingerprintsInContact = new HashSet<GameObject>();
  private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
  private Dictionary<GameObject, float> revealProgress = new Dictionary<GameObject, float>();
  private HashSet<GameObject> fullyRevealedFingerprints = new HashSet<GameObject>();

  // - INITIALIZATION
  void Start()
  {
    // Validate collider setup
    ValidateColliderSetup();

    // Create default material if needed
    if (revealedMaterial == null)
    {
      CreateDefaultRevealedMaterial();
    }
  }

  // Validate brush collider configuration
  void ValidateColliderSetup()
  {
    Collider brushCollider = GetComponent<Collider>();
    if (brushCollider == null)
    {
      Debug.LogError("DustBrush: No collider found on " + gameObject.name);
      return;
    }

    if (!brushCollider.isTrigger)
    {
      Debug.LogWarning("DustBrush: Collider on " + gameObject.name + " should be set as a trigger for proper detection.");
    }
  }

  // Create default revealed material if none assigned
  void CreateDefaultRevealedMaterial()
  {
    revealedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    revealedMaterial.SetColor("_BaseColor", revealedColor);
    revealedMaterial.SetFloat("_Surface", 1); // Transparent
    revealedMaterial.SetFloat("_Blend", 0); // Alpha blend
    revealedMaterial.name = "DustBrush_RevealedMaterial";
  }

  // - UPDATE LOOP
  void Update()
  {
    // Reveal all fingerprints currently in contact
    foreach (GameObject fingerprint in fingerprintsInContact)
    {
      if (fingerprint != null && !fullyRevealedFingerprints.Contains(fingerprint))
      {
        RevealFingerprint(fingerprint);
      }
    }

    // Clean up null references
    fingerprintsInContact.RemoveWhere(fp => fp == null);
  }

  // - TRIGGER DETECTION SYSTEM
  // Handle brush entering fingerprint trigger
  void OnTriggerEnter(Collider other)
  {
    // Check if touching a fingerprint
    if (other.gameObject.name.Contains("Fingerprint") || other.gameObject.tag == "Fingerprint")
    {
      fingerprintsInContact.Add(other.gameObject);

      // Store original material for restoration
      StoreOriginalMaterial(other.gameObject);

      // Handle instant reveal mode
      if (instantRevealMode)
      {
        InstantRevealFingerprint(other.gameObject);
      }
      else
      {
        // Initialize reveal progress
        if (!revealProgress.ContainsKey(other.gameObject))
        {
          revealProgress[other.gameObject] = 0f;
        }
      }
    }
  }

  // Handle brush exiting fingerprint trigger
  void OnTriggerExit(Collider other)
  {
    // Stop revealing when no longer touching
    if (other.gameObject.name.Contains("Fingerprint") || other.gameObject.tag == "Fingerprint")
    {
      fingerprintsInContact.Remove(other.gameObject);
    }
  }

  // - MATERIAL MANAGEMENT
  // Store original material for later restoration
  void StoreOriginalMaterial(GameObject fingerprint)
  {
    if (originalMaterials.ContainsKey(fingerprint))
    {
      return; // Already stored
    }

    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }

    if (renderer != null)
    {
      originalMaterials[fingerprint] = renderer.material;
    }
  }

  // - FINGERPRINT REVEAL SYSTEM
  // Gradually reveal fingerprint over time
  void RevealFingerprint(GameObject fingerprint)
  {
    if (!revealProgress.ContainsKey(fingerprint))
    {
      revealProgress[fingerprint] = 0f;
    }

    // Increase reveal progress
    float currentProgress = revealProgress[fingerprint];
    float newProgress = Mathf.MoveTowards(currentProgress, 1.0f, revealSpeed * Time.deltaTime);
    revealProgress[fingerprint] = newProgress;

    // Swap to revealed material when fully revealed
    if (newProgress >= 1.0f && !fullyRevealedFingerprints.Contains(fingerprint))
    {
      SwapToRevealedMaterial(fingerprint);
      fullyRevealedFingerprints.Add(fingerprint);
    }
  }

  // Swap fingerprint to revealed material
  void SwapToRevealedMaterial(GameObject fingerprint)
  {
    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }

    if (renderer != null && revealedMaterial != null)
    {
      renderer.material = revealedMaterial;
    }
  }

  // Instantly reveal fingerprint without animation
  void InstantRevealFingerprint(GameObject fingerprint)
  {
    StoreOriginalMaterial(fingerprint);
    revealProgress[fingerprint] = 1.0f;
    SwapToRevealedMaterial(fingerprint);
    fullyRevealedFingerprints.Add(fingerprint);
  }

  // - PUBLIC API METHODS
  // Manually reveal specific fingerprint
  public void RevealFingerprintManually(GameObject fingerprint)
  {
    if (fingerprint != null)
    {
      StoreOriginalMaterial(fingerprint);
      InstantRevealFingerprint(fingerprint);
    }
  }

  // Reset fingerprint to invisible state
  public void ResetFingerprint(GameObject fingerprint)
  {
    if (fingerprint == null || !originalMaterials.ContainsKey(fingerprint)) return;

    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }

    if (renderer != null)
    {
      // Restore original material
      renderer.material = originalMaterials[fingerprint];

      // Reset tracking data
      revealProgress.Remove(fingerprint);
      fullyRevealedFingerprints.Remove(fingerprint);
    }
  }

  // Reset all fingerprints for new scene
  public void ResetAllFingerprints()
  {
    // Reset tracked fingerprints
    List<GameObject> fingerprintsToReset = new List<GameObject>(originalMaterials.Keys);
    foreach (GameObject fingerprint in fingerprintsToReset)
    {
      if (fingerprint != null)
      {
        ResetFingerprint(fingerprint);
      }
    }

    // Scan scene for any untracked fingerprints
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj.name.Contains("Fingerprint") || obj.tag == "Fingerprint")
      {
        ResetAnyFingerprint(obj);
      }
    }

    // Clear all tracking collections
    fingerprintsInContact.Clear();
    originalMaterials.Clear();
    revealProgress.Clear();
    fullyRevealedFingerprints.Clear();
  }

  // Reset any fingerprint even without stored original material
  void ResetAnyFingerprint(GameObject fingerprint)
  {
    if (fingerprint == null) return;

    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }

    if (renderer != null)
    {
      // Use stored material if available
      if (originalMaterials.ContainsKey(fingerprint))
      {
        renderer.material = originalMaterials[fingerprint];
      }
      else
      {
        // Force current material to transparent
        ForceTransparentMaterial(renderer);
      }
    }
  }

  // Force material to transparent state
  void ForceTransparentMaterial(Renderer renderer)
  {
    Material currentMaterial = renderer.material;

    // Set transparent color
    Color transparentColor = currentMaterial.color;
    transparentColor.a = 0f;
    currentMaterial.color = transparentColor;

    // Handle URP base color
    if (currentMaterial.HasProperty("_BaseColor"))
    {
      currentMaterial.SetColor("_BaseColor", transparentColor);
    }

    // Configure transparency settings
    if (currentMaterial.HasProperty("_Surface"))
    {
      currentMaterial.SetFloat("_Surface", 1); // Transparent
    }
    if (currentMaterial.HasProperty("_Blend"))
    {
      currentMaterial.SetFloat("_Blend", 0); // Alpha blend
    }

    currentMaterial.renderQueue = 3000; // Transparent queue
  }

  // - CAMERA SCRIPT COMPATIBILITY METHODS
  // Get fingerprint reveal percentage
  public float GetFingerprintRevealPercentage(GameObject fingerprint)
  {
    if (fingerprint == null || !revealProgress.ContainsKey(fingerprint))
    {
      return 0f;
    }

    return revealProgress[fingerprint] * 100f;
  }

  // Check if fingerprint is sufficiently revealed
  public bool IsFingerprintSufficientlyRevealed(GameObject fingerprint, float threshold = 50f)
  {
    float percentage = GetFingerprintRevealPercentage(fingerprint);
    return percentage >= threshold;
  }

  // - CONTEXT MENU METHODS
  [ContextMenu("Reset All Fingerprints")]
  public void ResetAllFingerprintsMenu()
  {
    ResetAllFingerprints();
  }

  [ContextMenu("Test Reveal All Fingerprints")]
  public void TestRevealAllFingerprints()
  {
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    int revealedCount = 0;
    foreach (GameObject obj in allObjects)
    {
      if (obj.name.Contains("Fingerprint") || obj.tag == "Fingerprint")
      {
        InstantRevealFingerprint(obj);
        revealedCount++;
      }
    }
  }
}