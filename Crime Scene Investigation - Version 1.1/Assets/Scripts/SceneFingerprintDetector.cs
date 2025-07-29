using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// - SCENE FINGERPRINT DETECTOR MAIN CLASS
public class SceneFingerprintDetector : MonoBehaviour
{
  // - INSPECTOR CONFIGURATION
  [Header("Fingerprint Settings")]
  [SerializeField] private string fingerprintTag = "Fingerprint";
  [SerializeField] private string fingerprintLayer = "UV";
  [SerializeField] private Mesh fingerprintMeshAsset;

  [Header("Evidence Checklist Integration")]
  [SerializeField] private EvidenceChecklist evidenceChecklist;

  // - STATE VARIABLES
  // Detected fingerprint tracking
  private List<GameObject> detectedFingerprints = new List<GameObject>();

  // - UNITY LIFECYCLE METHODS
  void Start()
  {
    // Automatically find evidence checklist if not assigned
    if (evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
    }

    // Perform initial scene scan
    DetectAllFingerprintsInScene();
  }

  // - FINGERPRINT DETECTION SYSTEM
  public void DetectAllFingerprintsInScene()
  {
    // Scan entire scene for fingerprint objects
    detectedFingerprints.Clear();

    GameObject[] allActiveGameObjects = FindObjectsOfType<GameObject>();

    foreach (GameObject obj in allActiveGameObjects)
    {
      // Only process active game objects
      if (obj != null && obj.activeInHierarchy && IsFingerprint(obj))
      {
        detectedFingerprints.Add(obj);

        // Notify evidence checklist if available
        if (evidenceChecklist != null)
        {
          evidenceChecklist.OnFingerprintEnteredBox(obj);
        }
      }
    }
  }

  // - FINGERPRINT VALIDATION
  private bool IsFingerprint(GameObject obj)
  {
    // Check if object meets fingerprint criteria
    if (obj == null) return false;

    // Ensure the object is active in the hierarchy
    if (!obj.activeInHierarchy) return false;

    // Check for MeshRenderer component
    MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
    if (meshRenderer == null) return false;

    // Check for correct mesh
    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
    if (meshFilter == null || meshFilter.sharedMesh == null) return false;

    // Compare the sharedMesh with the serialized fingerprintMeshAsset
    if (fingerprintMeshAsset == null || meshFilter.sharedMesh != fingerprintMeshAsset)
    {
      return false;
    }

    // Validate fingerprint tag
    bool hasCorrectTag = obj.CompareTag(fingerprintTag);
    if (!hasCorrectTag) return false;

    // Validate fingerprint layer
    int layerIndex = LayerMask.NameToLayer(fingerprintLayer);
    bool onCorrectLayer = layerIndex != -1 && obj.layer == layerIndex;
    if (!onCorrectLayer) return false;

    // Validate fingerprint name pattern
    string objName = obj.name;
    bool hasCorrectName = objName == "Fingerprint" ||
                          (objName.StartsWith("Fingerprint (") && objName.EndsWith(")"));
    if (!hasCorrectName) return false;

    return true;
  }

  // - PUBLIC API METHODS
  public List<GameObject> GetDetectedFingerprints()
  {
    // Get copy of detected fingerprints list
    return new List<GameObject>(detectedFingerprints);
  }

  public void OutputDetectedFingerprints()
  {
    // Output detected fingerprints information
    detectedFingerprints.RemoveAll(obj => obj == null);

    if (detectedFingerprints.Count == 0)
    {
      return;
    }

    // Sort fingerprints alphabetically
    var sortedFingerprints = detectedFingerprints.OrderBy(obj => obj.name).ToList();

    foreach (var fingerprint in sortedFingerprints)
    {
      // Output fingerprint details
    }
  }
}