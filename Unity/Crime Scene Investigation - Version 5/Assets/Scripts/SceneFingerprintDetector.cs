using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for OrderBy

public class SceneFingerprintDetector : MonoBehaviour
{
  [Header("Fingerprint Settings")]
  [SerializeField] private string fingerprintTag = "Fingerprint";
  [SerializeField] private string fingerprintLayer = "UV";

  [Header("Debug Settings")]
  [SerializeField] private bool debugOutput = true;

  // Reference to your EvidenceChecklist (if needed for communication)
  // Drag and drop your EvidenceChecklist GameObject here in the Inspector
  [SerializeField] private EvidenceChecklist evidenceChecklist;

  private List<GameObject> detectedFingerprints = new List<GameObject>();

  void Start()
  {
    // Automatically find EvidenceChecklist if not assigned
    if (evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
      if (evidenceChecklist != null && debugOutput)
      {
        //Debug.Log("SceneFingerprintDetector: Found EvidenceChecklist automatically.");
      }
    }

    // Perform an initial detection scan when the scene starts
    DetectAllFingerprintsInScene();
  }

  /// <summary>
  /// Detects all active fingerprint objects in the entire scene.
  /// </summary>
  public void DetectAllFingerprintsInScene()
  {
    detectedFingerprints.Clear(); // Clear previous detections

    // Find all GameObjects in the scene
    // NOTE: This can be performance-heavy in scenes with thousands of objects.
    // For production, consider object pooling or more targeted search if performance is an issue.
    GameObject[] allActiveGameObjects = FindObjectsOfType<GameObject>();

    if (debugOutput)
    {
      //Debug.Log("SceneFingerprintDetector: Starting full scene fingerprint scan...");
    }

    foreach (GameObject obj in allActiveGameObjects)
    {
      if (obj != null && IsFingerprint(obj))
      {
        detectedFingerprints.Add(obj);
        // Communicate to EvidenceChecklist if it exists
        if (evidenceChecklist != null)
        {
          try
          {
            evidenceChecklist.OnFingerprintEnteredBox(obj); // Reusing this method, assuming it handles "found in scene"
          }
          catch (System.Exception e)
          {
            Debug.LogError($"SceneFingerprintDetector: Error communicating with EvidenceChecklist for {obj.name}: {e.Message}");
          }
        }
      }
    }

    if (debugOutput)
    {
      OutputDetectedFingerprints();
    }
  }

  /// <summary>
  /// Checks if a given GameObject is considered a fingerprint based on tag, layer, and name pattern.
  /// This logic is taken directly from your original script's robust IsFingerprint method.
  /// </summary>
  private bool IsFingerprint(GameObject obj)
  {
    if (obj == null) return false;

    // Check tag
    bool hasCorrectTag = obj.CompareTag(fingerprintTag);

    // Check layer
    int layerIndex = LayerMask.NameToLayer(fingerprintLayer);
    bool onCorrectLayer = layerIndex != -1 && obj.layer == layerIndex;

    // Check name pattern - EXACT SAME LOGIC AS YOUR ORIGINAL SCRIPT
    string objName = obj.name;
    bool hasCorrectName = objName == "Fingerprint" ||
                          (objName.StartsWith("Fingerprint (") && objName.EndsWith(")"));

    bool isValid = hasCorrectTag && onCorrectLayer && hasCorrectName;

    if (debugOutput && objName.Contains("Fingerprint")) // Only log details for potential fingerprints
    {
      //Debug.Log($"SceneFingerprintDetector: Validation for {objName}: Tag={hasCorrectTag}, Layer={onCorrectLayer}, NamePattern={hasCorrectName} -> RESULT: {isValid}");
    }

    return isValid;
  }

  /// <summary>
  /// Outputs the list of detected fingerprints to the console in alphabetical order.
  /// </summary>
  public void OutputDetectedFingerprints()
  {
    // Clean up null entries in case objects were destroyed
    detectedFingerprints.RemoveAll(obj => obj == null);

    if (detectedFingerprints.Count == 0)
    {
      //Debug.Log("=== SCENE FINGERPRINTS ===\nNo fingerprints detected in the scene.\n=== END SCENE FINGERPRINTS ===");
      return;
    }

    var sortedFingerprints = detectedFingerprints.OrderBy(obj => obj.name).ToList();

    //Debug.Log("=== SCENE FINGERPRINTS (ALPHABETICAL) ===");
    //Debug.Log($"Total fingerprints detected: {sortedFingerprints.Count}");
    //Debug.Log("---");

    for (int i = 0; i < sortedFingerprints.Count; i++)
    {
      GameObject fp = sortedFingerprints[i];
      //Debug.Log($"{i + 1:D3}. {fp.name}");
    }

    //Debug.Log("=== END SCENE FINGERPRINTS ===");
  }

  // Public API to get the detected fingerprints
  public List<GameObject> GetDetectedFingerprints()
  {
    // Return a copy to prevent external modification of the internal list
    return new List<GameObject>(detectedFingerprints);
  }

  // Context Menu for easy testing in the editor
  [ContextMenu("Detect All Fingerprints Now")]
  public void EditorDetectAllFingerprintsNow()
  {
    DetectAllFingerprintsInScene();
  }

  [ContextMenu("Output Detected Fingerprints")]
  public void EditorOutputDetectedFingerprints()
  {
    OutputDetectedFingerprints();
  }
}