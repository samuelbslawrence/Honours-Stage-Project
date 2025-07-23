using UnityEngine;
using System.Collections.Generic;

public class DustBrush : MonoBehaviour
{
  [Header("Brush Settings")]
  [Tooltip("How quickly the fingerprint becomes visible (0-1 per second)")]
  public float revealSpeed = 2.0f;

  [Tooltip("The white color to apply to revealed fingerprints")]
  public Color revealedColor = Color.white;

  [Tooltip("Material to swap to when fingerprint is revealed")]
  public Material revealedMaterial;

  [Header("Debug")]
  [Tooltip("Show debug information in console")]
  public bool debugMode = false;

  [Header("Testing")]
  [Tooltip("Instantly reveal fingerprints for testing")]
  public bool instantRevealMode = false;

  // Keep track of fingerprints we're currently revealing
  private HashSet<GameObject> fingerprintsInContact = new HashSet<GameObject>();

  // Track original materials and revealed fingerprints
  private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
  private Dictionary<GameObject, float> revealProgress = new Dictionary<GameObject, float>();
  private HashSet<GameObject> fullyRevealedFingerprints = new HashSet<GameObject>();

  void Start()
  {
    // Ensure the brush has a collider and it's set as a trigger
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

    // Create a default revealed material if none assigned
    if (revealedMaterial == null)
    {
      CreateDefaultRevealedMaterial();
    }
  }

  void CreateDefaultRevealedMaterial()
  {
    // Create a simple white material for revealed fingerprints
    revealedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    revealedMaterial.SetColor("_BaseColor", revealedColor);
    revealedMaterial.SetFloat("_Surface", 1); // Transparent
    revealedMaterial.SetFloat("_Blend", 0); // Alpha blend
    revealedMaterial.name = "DustBrush_RevealedMaterial";

    if (debugMode)
    {
      Debug.Log("DustBrush: Created default revealed material");
    }
  }

  void OnTriggerEnter(Collider other)
  {
    // Check if the object we're touching is a fingerprint
    if (other.gameObject.name.Contains("Fingerprint") || other.gameObject.tag == "Fingerprint")
    {
      fingerprintsInContact.Add(other.gameObject);

      // Store original material if not already stored
      StoreOriginalMaterial(other.gameObject);

      // If instant reveal mode is on, immediately reveal the fingerprint
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

      if (debugMode)
      {
        Debug.Log("DustBrush: Started revealing fingerprint on " + other.gameObject.name);
      }
    }
  }

  void OnTriggerExit(Collider other)
  {
    // Stop revealing this fingerprint when we're no longer touching it
    if (other.gameObject.name.Contains("Fingerprint") || other.gameObject.tag == "Fingerprint")
    {
      fingerprintsInContact.Remove(other.gameObject);

      if (debugMode)
      {
        Debug.Log("DustBrush: Stopped revealing fingerprint on " + other.gameObject.name);
      }
    }
  }

  void Update()
  {
    // Reveal all fingerprints we're currently in contact with
    foreach (GameObject fingerprint in fingerprintsInContact)
    {
      if (fingerprint != null && !fullyRevealedFingerprints.Contains(fingerprint))
      {
        RevealFingerprint(fingerprint);
      }
    }

    // Clean up any null references (in case fingerprints were destroyed)
    fingerprintsInContact.RemoveWhere(fp => fp == null);
  }

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

      if (debugMode)
      {
        Debug.Log("DustBrush: Stored original material for " + fingerprint.name);
      }
    }
  }

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

    if (debugMode && Time.frameCount % 60 == 0)
    {
      Debug.Log("DustBrush: Revealing " + fingerprint.name + " - Progress: " + (newProgress * 100f).ToString("F1") + "%");
    }

    // When fully revealed, swap to the revealed material
    if (newProgress >= 1.0f && !fullyRevealedFingerprints.Contains(fingerprint))
    {
      SwapToRevealedMaterial(fingerprint);
      fullyRevealedFingerprints.Add(fingerprint);

      if (debugMode)
      {
        Debug.Log("DustBrush: Fingerprint " + fingerprint.name + " fully revealed! Swapped material.");
      }
    }
  }

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

      if (debugMode)
      {
        Debug.Log("DustBrush: Swapped " + fingerprint.name + " to revealed material");
      }
    }
  }

  void InstantRevealFingerprint(GameObject fingerprint)
  {
    StoreOriginalMaterial(fingerprint);
    revealProgress[fingerprint] = 1.0f;
    SwapToRevealedMaterial(fingerprint);
    fullyRevealedFingerprints.Add(fingerprint);

    if (debugMode)
    {
      Debug.Log("DustBrush: INSTANTLY revealed " + fingerprint.name);
    }
  }

  // Public method to manually reveal a specific fingerprint (useful for testing)
  public void RevealFingerprintManually(GameObject fingerprint)
  {
    if (fingerprint != null)
    {
      StoreOriginalMaterial(fingerprint);
      InstantRevealFingerprint(fingerprint);
    }
  }

  // Public method to reset a fingerprint to invisible state
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

      // Reset tracking
      revealProgress.Remove(fingerprint);
      fullyRevealedFingerprints.Remove(fingerprint);

      if (debugMode)
      {
        Debug.Log("DustBrush: Reset " + fingerprint.name + " to original material");
      }
    }
  }

  // Reset all for new scene
  public void ResetAllFingerprints()
  {
    if (debugMode)
    {
      Debug.Log("DustBrush: Resetting all fingerprints for new scene");
    }

    // First, reset all fingerprints that we have tracked
    List<GameObject> fingerprintsToReset = new List<GameObject>(originalMaterials.Keys);
    foreach (GameObject fingerprint in fingerprintsToReset)
    {
      if (fingerprint != null)
      {
        ResetFingerprint(fingerprint);
      }
    }

    // Also scan the entire scene for any fingerprints and reset them
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj.name.Contains("Fingerprint") || obj.tag == "Fingerprint")
      {
        // Reset any fingerprint that might not be in our tracking
        ResetAnyFingerprint(obj);
      }
    }

    // Clear all tracking
    fingerprintsInContact.Clear();
    originalMaterials.Clear();
    revealProgress.Clear();
    fullyRevealedFingerprints.Clear();

    if (debugMode)
    {
      Debug.Log("DustBrush: Reset complete - all fingerprints should be transparent");
    }
  }

  // Reset any fingerprint, even if we don't have its original material stored
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
      // If we have the original material, use it
      if (originalMaterials.ContainsKey(fingerprint))
      {
        renderer.material = originalMaterials[fingerprint];
      }
      else
      {
        // Force the current material to be transparent
        Material currentMaterial = renderer.material;

        // Set alpha to 0 on both standard and URP properties
        Color transparentColor = currentMaterial.color;
        transparentColor.a = 0f;
        currentMaterial.color = transparentColor;

        if (currentMaterial.HasProperty("_BaseColor"))
        {
          currentMaterial.SetColor("_BaseColor", transparentColor);
        }

        // Ensure transparency is enabled
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

      if (debugMode)
      {
        
        //Debug.Log("DustBrush: Force reset fingerprint: " + fingerprint.name);
      }
    }
  }

  // Simple methods for camera script compatibility
  public float GetFingerprintRevealPercentage(GameObject fingerprint)
  {
    if (fingerprint == null || !revealProgress.ContainsKey(fingerprint))
    {
      return 0f;
    }

    return revealProgress[fingerprint] * 100f;
  }

  public bool IsFingerprintSufficientlyRevealed(GameObject fingerprint, float threshold = 50f)
  {
    float percentage = GetFingerprintRevealPercentage(fingerprint);
    return percentage >= threshold;
  }

  [ContextMenu("Reset All Fingerprints")]
  public void ResetAllFingerprintsMenu()
  {
    ResetAllFingerprints();
  }

  [ContextMenu("Test Reveal All Fingerprints")]
  public void TestRevealAllFingerprints()
  {
    Debug.Log("DustBrush: Testing reveal all fingerprints...");
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
    Debug.Log("DustBrush: Successfully revealed " + revealedCount + " fingerprints");
  }
}