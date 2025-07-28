using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FingerprintCleanupTool : EditorWindow
{
  [Header("Detection Settings")]
  [SerializeField] private float duplicateThreshold = 0.1f; // Distance threshold for duplicates
  [SerializeField] private bool includeInactiveObjects = true;
  [SerializeField] private bool autoSelectDuplicates = true;

  [Header("Filter Options")]
  [SerializeField] private bool onlyFingerprintTag = true;
  [SerializeField] private bool onlyUVLayer = true;
  [SerializeField] private bool strictNameCheck = true;

  [Header("Occlusion Detection")]
  [SerializeField] private bool detectOccludedFingerprints = true;
  [SerializeField] private int occlusionRayCount = 16; // Rays cast from different angles
  [SerializeField] private float raycastDistance = 50f; // Max distance for occlusion check
  [SerializeField] private LayerMask occlusionLayerMask = -1; // What objects can occlude fingerprints
  [SerializeField] private float occlusionThreshold = 0.8f; // 80% of rays blocked = occluded

  private List<GameObject> allFingerprints = new List<GameObject>();
  private List<FingerprintGroup> duplicateGroups = new List<FingerprintGroup>();
  private List<GameObject> occludedFingerprints = new List<GameObject>();
  private Vector2 scrollPosition;
  private bool showAdvancedOptions = false;

  [System.Serializable]
  public class FingerprintGroup
  {
    public Vector3 position;
    public List<GameObject> fingerprints;
    public bool isExpanded = false;
    public bool[] selectedForDeletion;

    public FingerprintGroup(Vector3 pos)
    {
      position = pos;
      fingerprints = new List<GameObject>();
    }

    public void InitializeSelection()
    {
      selectedForDeletion = new bool[fingerprints.Count];
      // By default, select all except the first one (keep the original)
      for (int i = 1; i < selectedForDeletion.Length; i++)
      {
        selectedForDeletion[i] = true;
      }
    }
  }

  [MenuItem("Tools/Fingerprint Cleanup Tool")]
  public static void ShowWindow()
  {
    FingerprintCleanupTool window = GetWindow<FingerprintCleanupTool>("Fingerprint Cleanup");
    window.minSize = new Vector2(400, 300);
    window.Show();
  }

  void OnGUI()
  {
    EditorGUILayout.LabelField("Fingerprint Cleanup Tool", EditorStyles.boldLabel);
    EditorGUILayout.Space();

    // Settings Section
    EditorGUILayout.LabelField("Detection Settings", EditorStyles.boldLabel);
    duplicateThreshold = EditorGUILayout.FloatField("Duplicate Distance Threshold", duplicateThreshold);
    includeInactiveObjects = EditorGUILayout.Toggle("Include Inactive Objects", includeInactiveObjects);
    autoSelectDuplicates = EditorGUILayout.Toggle("Auto-Select Duplicates for Deletion", autoSelectDuplicates);

    EditorGUILayout.Space();

    // Advanced Options
    showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Filter Options");
    if (showAdvancedOptions)
    {
      EditorGUI.indentLevel++;
      onlyFingerprintTag = EditorGUILayout.Toggle("Only 'Fingerprint' Tag", onlyFingerprintTag);
      onlyUVLayer = EditorGUILayout.Toggle("Only UV Layer", onlyUVLayer);
      strictNameCheck = EditorGUILayout.Toggle("Strict Name Check", strictNameCheck);
      EditorGUI.indentLevel--;
    }

    // Occlusion Detection Options
    EditorGUILayout.LabelField("Occlusion Detection", EditorStyles.boldLabel);
    detectOccludedFingerprints = EditorGUILayout.Toggle("Detect Covered Fingerprints", detectOccludedFingerprints);
    if (detectOccludedFingerprints)
    {
      EditorGUI.indentLevel++;
      occlusionRayCount = EditorGUILayout.IntSlider("Detection Rays", occlusionRayCount, 4, 32);
      raycastDistance = EditorGUILayout.FloatField("Raycast Distance", raycastDistance);
      occlusionLayerMask = EditorGUILayout.MaskField("Occlusion Layers", occlusionLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
      occlusionThreshold = EditorGUILayout.Slider("Occlusion Threshold", occlusionThreshold, 0.1f, 1.0f);
      EditorGUILayout.HelpBox("Threshold: 0.5 = 50% blocked, 1.0 = completely blocked", MessageType.Info);
      EditorGUI.indentLevel--;
    }

    EditorGUILayout.Space();

    // Action Buttons
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Scan for Duplicates", GUILayout.Height(30)))
    {
      ScanForDuplicateFingerprints();
    }

    if (GUILayout.Button("Find Covered Fingerprints", GUILayout.Height(30)))
    {
      FindOccludedFingerprints();
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Scan All Issues", GUILayout.Height(25)))
    {
      ScanForDuplicateFingerprints();
      if (detectOccludedFingerprints)
      {
        FindOccludedFingerprints();
      }
    }

    if (GUILayout.Button("Refresh Scene", GUILayout.Height(25)))
    {
      RefreshFingerprintList();
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space();

    // Results Section
    if (allFingerprints.Count > 0)
    {
      EditorGUILayout.LabelField($"Total Fingerprints Found: {allFingerprints.Count}", EditorStyles.helpBox);
    }

    // Covered Fingerprints Section
    if (occludedFingerprints.Count > 0)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("🚫 COVERED FINGERPRINTS", EditorStyles.boldLabel);
      EditorGUILayout.LabelField($"Found {occludedFingerprints.Count} fingerprints that are completely covered by objects", EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Select All Covered"))
      {
        Selection.objects = occludedFingerprints.ToArray();
        SceneView.FrameLastActiveSceneView();
      }
      if (GUILayout.Button("Delete All Covered", GUILayout.Width(120)))
      {
        DeleteOccludedFingerprints();
      }
      EditorGUILayout.EndHorizontal();

      // List covered fingerprints
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Covered Fingerprints:", EditorStyles.miniBoldLabel);

      for (int i = 0; i < occludedFingerprints.Count; i++)
      {
        GameObject fp = occludedFingerprints[i];
        if (fp == null) continue;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField($"#{i + 1}", fp, typeof(GameObject), true);

        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
          Selection.activeGameObject = fp;
          SceneView.FrameLastActiveSceneView();
        }

        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
          if (EditorUtility.DisplayDialog("Delete Covered Fingerprint",
              $"Delete '{fp.name}' at {fp.transform.position:F2}?", "Delete", "Cancel"))
          {
            DestroyImmediate(fp);
            occludedFingerprints.RemoveAt(i);
            i--; // Adjust index after removal
          }
        }
        EditorGUILayout.EndHorizontal();
      }
      EditorGUILayout.EndVertical();
    }

    // Duplicates Section
    if (duplicateGroups.Count > 0)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("🔄 DUPLICATE FINGERPRINTS", EditorStyles.boldLabel);
      EditorGUILayout.LabelField($"Duplicate Groups Found: {duplicateGroups.Count}", EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Select All Duplicates"))
      {
        SelectAllDuplicates();
      }
      if (GUILayout.Button("Delete Selected", GUILayout.Width(120)))
      {
        DeleteSelectedDuplicates();
      }
      if (GUILayout.Button("Clear Selection", GUILayout.Width(120)))
      {
        ClearAllSelections();
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space();

      // Scrollable list of duplicate groups
      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

      for (int groupIndex = 0; groupIndex < duplicateGroups.Count; groupIndex++)
      {
        DrawDuplicateGroup(duplicateGroups[groupIndex], groupIndex);
      }

      EditorGUILayout.EndScrollView();
    }
    else if (allFingerprints.Count > 0 && duplicateGroups.Count == 0)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("✓ No duplicates found!", EditorStyles.helpBox);
    }

    // Show summary if both scans completed
    if (allFingerprints.Count > 0)
    {
      int totalIssues = duplicateGroups.Sum(g => g.fingerprints.Count - 1) + occludedFingerprints.Count;
      if (totalIssues == 0)
      {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎉 All fingerprints look good! No issues found.", EditorStyles.helpBox);
      }
    }
  }

  void DrawDuplicateGroup(FingerprintGroup group, int groupIndex)
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

    // Group header
    EditorGUILayout.BeginHorizontal();
    group.isExpanded = EditorGUILayout.Foldout(group.isExpanded,
        $"Group {groupIndex + 1}: {group.fingerprints.Count} fingerprints at {group.position:F2}");

    if (GUILayout.Button("Select All in Scene", GUILayout.Width(130)))
    {
      Selection.objects = group.fingerprints.ToArray();
      SceneView.FrameLastActiveSceneView();
    }
    EditorGUILayout.EndHorizontal();

    if (group.isExpanded)
    {
      EditorGUI.indentLevel++;

      for (int i = 0; i < group.fingerprints.Count; i++)
      {
        GameObject fingerprint = group.fingerprints[i];
        if (fingerprint == null) continue;

        EditorGUILayout.BeginHorizontal();

        // Checkbox for deletion
        group.selectedForDeletion[i] = EditorGUILayout.Toggle(group.selectedForDeletion[i], GUILayout.Width(20));

        // Object field (clickable)
        EditorGUILayout.ObjectField($"#{i + 1}", fingerprint, typeof(GameObject), true);

        // Quick select button
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
          Selection.activeGameObject = fingerprint;
          SceneView.FrameLastActiveSceneView();
        }

        // Distance from first object
        if (i > 0)
        {
          float distance = Vector3.Distance(group.fingerprints[0].transform.position, fingerprint.transform.position);
          EditorGUILayout.LabelField($"({distance:F3}m)", GUILayout.Width(60));
        }
        else
        {
          EditorGUILayout.LabelField("(Original)", GUILayout.Width(60));
        }

        EditorGUILayout.EndHorizontal();
      }

      EditorGUI.indentLevel--;

      // Group actions
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Keep First Only"))
      {
        for (int i = 0; i < group.selectedForDeletion.Length; i++)
        {
          group.selectedForDeletion[i] = (i > 0);
        }
      }
      if (GUILayout.Button("Keep Last Only"))
      {
        for (int i = 0; i < group.selectedForDeletion.Length; i++)
        {
          group.selectedForDeletion[i] = (i < group.selectedForDeletion.Length - 1);
        }
      }
      if (GUILayout.Button("Select All"))
      {
        for (int i = 0; i < group.selectedForDeletion.Length; i++)
        {
          group.selectedForDeletion[i] = true;
        }
      }
      if (GUILayout.Button("Select None"))
      {
        for (int i = 0; i < group.selectedForDeletion.Length; i++)
        {
          group.selectedForDeletion[i] = false;
        }
      }
      EditorGUILayout.EndHorizontal();
    }

    EditorGUILayout.EndVertical();
    EditorGUILayout.Space();
  }

  void RefreshFingerprintList()
  {
    allFingerprints.Clear();
    duplicateGroups.Clear();
    occludedFingerprints.Clear();

    // Find all potential fingerprint objects
    GameObject[] allObjects = includeInactiveObjects ?
        Resources.FindObjectsOfTypeAll<GameObject>() :
        FindObjectsOfType<GameObject>();

    foreach (GameObject obj in allObjects)
    {
      if (IsValidFingerprint(obj))
      {
        // Make sure it's in the current scene, not a prefab
        if (obj.scene.IsValid() || !includeInactiveObjects)
        {
          allFingerprints.Add(obj);
        }
      }
    }

    Debug.Log($"Found {allFingerprints.Count} fingerprint objects in the scene.");
  }

  bool IsValidFingerprint(GameObject obj)
  {
    if (obj == null) return false;

    bool isValid = true;

    // Check tag
    if (onlyFingerprintTag && !obj.CompareTag("Fingerprint"))
    {
      isValid = false;
    }

    // Check layer
    if (onlyUVLayer)
    {
      int uvLayerIndex = LayerMask.NameToLayer("UV");
      if (uvLayerIndex != -1 && obj.layer != uvLayerIndex)
      {
        isValid = false;
      }
    }

    // Check name
    if (strictNameCheck)
    {
      string objName = obj.name;
      bool hasCorrectName = objName == "Fingerprint" ||
                           (objName.StartsWith("Fingerprint (") && objName.EndsWith(")")) ||
                           objName.StartsWith("Fingerprint");
      if (!hasCorrectName)
      {
        isValid = false;
      }
    }

    return isValid;
  }

  void ScanForDuplicateFingerprints()
  {
    RefreshFingerprintList();
    duplicateGroups.Clear();

    if (allFingerprints.Count == 0)
    {
      Debug.LogWarning("No fingerprints found to scan!");
      return;
    }

    // Group fingerprints by proximity
    List<GameObject> processedFingerprints = new List<GameObject>();

    foreach (GameObject fingerprint in allFingerprints)
    {
      if (processedFingerprints.Contains(fingerprint)) continue;

      List<GameObject> nearbyFingerprints = new List<GameObject>();
      nearbyFingerprints.Add(fingerprint);
      processedFingerprints.Add(fingerprint);

      // Find all fingerprints within threshold distance
      foreach (GameObject otherFingerprint in allFingerprints)
      {
        if (otherFingerprint == fingerprint || processedFingerprints.Contains(otherFingerprint))
          continue;

        float distance = Vector3.Distance(fingerprint.transform.position, otherFingerprint.transform.position);
        if (distance <= duplicateThreshold)
        {
          nearbyFingerprints.Add(otherFingerprint);
          processedFingerprints.Add(otherFingerprint);
        }
      }

      // If we found duplicates, create a group
      if (nearbyFingerprints.Count > 1)
      {
        FingerprintGroup group = new FingerprintGroup(fingerprint.transform.position);
        group.fingerprints = nearbyFingerprints.OrderBy(f => Vector3.Distance(fingerprint.transform.position, f.transform.position)).ToList();
        group.InitializeSelection();
        duplicateGroups.Add(group);
      }
    }

    Debug.Log($"Found {duplicateGroups.Count} groups of duplicate fingerprints.");

    if (autoSelectDuplicates && duplicateGroups.Count > 0)
    {
      SelectAllDuplicates();
    }
  }

  void SelectAllDuplicates()
  {
    List<GameObject> allDuplicates = new List<GameObject>();

    foreach (FingerprintGroup group in duplicateGroups)
    {
      for (int i = 0; i < group.fingerprints.Count; i++)
      {
        if (group.selectedForDeletion[i])
        {
          allDuplicates.Add(group.fingerprints[i]);
        }
      }
    }

    Selection.objects = allDuplicates.ToArray();
    Debug.Log($"Selected {allDuplicates.Count} duplicate fingerprints.");
  }

  void DeleteSelectedDuplicates()
  {
    if (!EditorUtility.DisplayDialog("Delete Duplicates",
        "Are you sure you want to delete the selected duplicate fingerprints? This cannot be undone!",
        "Delete", "Cancel"))
    {
      return;
    }

    int deletedCount = 0;

    foreach (FingerprintGroup group in duplicateGroups)
    {
      for (int i = group.fingerprints.Count - 1; i >= 0; i--)
      {
        if (group.selectedForDeletion[i] && group.fingerprints[i] != null)
        {
          DestroyImmediate(group.fingerprints[i]);
          deletedCount++;
        }
      }
    }

    Debug.Log($"Deleted {deletedCount} duplicate fingerprints.");

    // Refresh the list
    ScanForDuplicateFingerprints();
  }

  void ClearAllSelections()
  {
    foreach (FingerprintGroup group in duplicateGroups)
    {
      for (int i = 0; i < group.selectedForDeletion.Length; i++)
      {
        group.selectedForDeletion[i] = false;
      }
    }

    Selection.objects = new Object[0];
  }

  void FindOccludedFingerprints()
  {
    RefreshFingerprintList();
    occludedFingerprints.Clear();

    if (allFingerprints.Count == 0)
    {
      Debug.LogWarning("No fingerprints found to check for occlusion!");
      return;
    }

    EditorUtility.DisplayProgressBar("Checking Occlusion", "Analyzing fingerprint visibility...", 0f);

    try
    {
      for (int i = 0; i < allFingerprints.Count; i++)
      {
        GameObject fingerprint = allFingerprints[i];
        EditorUtility.DisplayProgressBar("Checking Occlusion",
            $"Checking {fingerprint.name} ({i + 1}/{allFingerprints.Count})",
            (float)i / allFingerprints.Count);

        if (IsFingerprintOccluded(fingerprint))
        {
          occludedFingerprints.Add(fingerprint);
        }
      }
    }
    finally
    {
      EditorUtility.ClearProgressBar();
    }

    Debug.Log($"Found {occludedFingerprints.Count} occluded fingerprints out of {allFingerprints.Count} total.");
  }

  bool IsFingerprintOccluded(GameObject fingerprint)
  {
    if (fingerprint == null) return false;

    // Get fingerprint bounds
    Renderer renderer = fingerprint.GetComponent<Renderer>();
    if (renderer == null)
    {
      renderer = fingerprint.GetComponentInChildren<Renderer>();
    }

    if (renderer == null)
    {
      Debug.LogWarning($"No renderer found on fingerprint: {fingerprint.name}");
      return false;
    }

    Bounds bounds = renderer.bounds;
    Vector3 center = bounds.center;
    float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

    // Cast rays from multiple directions to check visibility
    int blockedRays = 0;
    int totalRays = occlusionRayCount;

    // Generate ray directions in a sphere around the fingerprint
    for (int i = 0; i < totalRays; i++)
    {
      Vector3 direction = GetSphereDirection(i, totalRays);

      // Cast ray from fingerprint outward
      Ray ray = new Ray(center + direction * (size * 0.1f), direction);

      if (Physics.Raycast(ray, raycastDistance, occlusionLayerMask))
      {
        blockedRays++;
      }
    }

    // Also check rays toward common camera positions (top-down, eye-level, etc.)
    Vector3[] cameraDirections = {
            Vector3.up,           // From above
            Vector3.down,         // From below  
            Vector3.forward,      // From front
            Vector3.back,         // From behind
            Vector3.left,         // From left
            Vector3.right,        // From right
            new Vector3(0, 1, 1).normalized,   // Diagonal up-forward
            new Vector3(0, -1, 1).normalized,  // Diagonal down-forward
            new Vector3(1, 0, 1).normalized,   // Diagonal right-forward
            new Vector3(-1, 0, 1).normalized   // Diagonal left-forward
        };

    int viewBlockedRays = 0;
    foreach (Vector3 direction in cameraDirections)
    {
      Ray ray = new Ray(center + direction * (size * 0.1f), direction);
      if (Physics.Raycast(ray, raycastDistance, occlusionLayerMask))
      {
        viewBlockedRays++;
      }
    }

    // Consider occluded if most rays are blocked AND all common view directions are blocked
    float sphereOcclusionRatio = (float)blockedRays / totalRays;
    float viewOcclusionRatio = (float)viewBlockedRays / cameraDirections.Length;

    // Fingerprint is occluded if either:
    // 1. High percentage of sphere rays blocked
    // 2. All common viewing angles blocked
    return sphereOcclusionRatio >= occlusionThreshold || viewOcclusionRatio >= 0.8f;
  }

  Vector3 GetSphereDirection(int index, int total)
  {
    // Generate evenly distributed points on a sphere using golden spiral
    float phi = Mathf.Acos(1 - 2 * (float)index / total);
    float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * index;

    float x = Mathf.Sin(phi) * Mathf.Cos(theta);
    float y = Mathf.Sin(phi) * Mathf.Sin(theta);
    float z = Mathf.Cos(phi);

    return new Vector3(x, y, z).normalized;
  }

  void DeleteOccludedFingerprints()
  {
    if (occludedFingerprints.Count == 0)
    {
      Debug.LogWarning("No occluded fingerprints to delete!");
      return;
    }

    if (!EditorUtility.DisplayDialog("Delete Covered Fingerprints",
        $"Are you sure you want to delete {occludedFingerprints.Count} covered fingerprints? This cannot be undone!",
        "Delete All", "Cancel"))
    {
      return;
    }

    int deletedCount = 0;

    foreach (GameObject fingerprint in occludedFingerprints)
    {
      if (fingerprint != null)
      {
        DestroyImmediate(fingerprint);
        deletedCount++;
      }
    }

    occludedFingerprints.Clear();
    Debug.Log($"Deleted {deletedCount} covered fingerprints.");

    // Refresh the list
    RefreshFingerprintList();
  }
}