using UnityEngine;
using System.Collections.Generic;

// - SCENE GENERATOR HIERARCHICAL SPAWNING SYSTEM
public class SceneGeneratorHierarchy
{
  // - HIERARCHY SETTINGS
  public bool enableHierarchicalSpawning = false;
  public bool enableSmartPlacement = true;
  public float parentChildRadius = 2f;
  public int maxChildrenPerParent = 3;
  public float childAngleRandomness = 60f;
  public bool maintainChildOrientation = false;
  public bool adjustChildHeightBasedOnParent = true;
  public float childHeightOffset = 0.1f;

  // Defines parent-child relationships between object categories
  [System.Serializable]
  public class SpawnHierarchy
  {
    public ObjectCategory parentCategory;
    public ObjectCategory[] childCategories;
    public float childSpawnRadius = 2f;
    public int maxChildrenPerParent = 3;
    public float childAngleRandomness = 60f;
  }

  [Header("Hierarchy Rules")]
  public SpawnHierarchy[] hierarchyRules = new SpawnHierarchy[]
  {
    new SpawnHierarchy()
    {
      parentCategory = ObjectCategory.Tables,
      childCategories = new ObjectCategory[] { ObjectCategory.Glasses, ObjectCategory.Bottles },
      childSpawnRadius = 1.5f,
      maxChildrenPerParent = 2,
      childAngleRandomness = 45f
    },
    new SpawnHierarchy()
    {
      parentCategory = ObjectCategory.BarObjects,
      childCategories = new ObjectCategory[] { ObjectCategory.Glasses, ObjectCategory.Bottles },
      childSpawnRadius = 2f,
      maxChildrenPerParent = 4,
      childAngleRandomness = 90f
    },
    new SpawnHierarchy()
    {
      parentCategory = ObjectCategory.Furniture,
      childCategories = new ObjectCategory[] { ObjectCategory.Decorations },
      childSpawnRadius = 1f,
      maxChildrenPerParent = 1,
      childAngleRandomness = 30f
    }
  };

  // - PRIVATE STATE VARIABLES
  private CrimeSceneGenerator generator;
  private Dictionary<GameObject, List<GameObject>> parentChildRelations = new Dictionary<GameObject, List<GameObject>>();
  private List<GameObject> hierarchyParents = new List<GameObject>();
  private List<GameObject> hierarchyChildren = new List<GameObject>();

  // - CONSTRUCTOR
  public SceneGeneratorHierarchy(CrimeSceneGenerator generator)
  {
    this.generator = generator;
  }

  // - HIERARCHY CONTROL METHODS
  public bool IsHierarchicalSpawningEnabled()
  {
    return enableHierarchicalSpawning;
  }

  public void SetHierarchicalSpawning(bool enabled)
  {
    enableHierarchicalSpawning = enabled;
    Debug.Log($"Hierarchical spawning {(enabled ? "enabled" : "disabled")}");
  }

  public void ClearHierarchy()
  {
    parentChildRelations.Clear();
    hierarchyParents.Clear();
    hierarchyChildren.Clear();
  }

  // - HIERARCHICAL GENERATION SYSTEM
  // Generate objects using two-pass hierarchical approach
  public void GenerateObjectsHierarchically()
  {
    Debug.Log("Starting hierarchical object generation");

    ClearHierarchy();

    // Two-pass generation: parents first, then children around parents
    GenerateParentObjects();
    GenerateChildrenForParents();

    Debug.Log($"Hierarchical generation complete: {hierarchyParents.Count} parents, {hierarchyChildren.Count} children");
  }

  // First pass: create parent objects using standard spawning rules
  private void GenerateParentObjects()
  {
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();
    SceneGeneratorCategories categorySystem = generator.GetCategorySystem();

    foreach (SpawnHierarchy rule in hierarchyRules)
    {
      ObjectTypeConfig parentConfig = FindConfigForCategory(rule.parentCategory);
      if (parentConfig == null || !parentConfig.enableThisType)
      {
        continue;
      }

      if (!categorySystem.IsCategoryEnabled(rule.parentCategory))
      {
        continue;
      }

      int parentCount = generator.GetRandomizer().Next(parentConfig.minCount, parentConfig.maxCount + 1);

      for (int i = 0; i < parentCount; i++)
      {
        GameObject parentObject = SpawnParentObject(parentConfig, rule);
        if (parentObject != null)
        {
          hierarchyParents.Add(parentObject);
          parentChildRelations[parentObject] = new List<GameObject>();
        }
      }
    }
  }

  // Second pass: create children positioned around each parent
  private void GenerateChildrenForParents()
  {
    foreach (GameObject parent in hierarchyParents)
    {
      if (parent == null) continue;

      SpawnHierarchy rule = FindHierarchyRuleForParent(parent);
      if (rule == null) continue;

      int childCount = generator.GetRandomizer().Next(1, rule.maxChildrenPerParent + 1);

      for (int i = 0; i < childCount; i++)
      {
        GameObject child = SpawnChildObject(parent, rule, i);
        if (child != null)
        {
          hierarchyChildren.Add(child);
          parentChildRelations[parent].Add(child);
        }
      }
    }
  }

  // Spawn parent object using existing spawning system
  private GameObject SpawnParentObject(ObjectTypeConfig config, SpawnHierarchy rule)
  {
    SceneGeneratorSpawning spawningSystem = generator.GetSpawningSystem();
    if (spawningSystem == null) return null;

    List<SpawnedObjectData> beforeSpawn = new List<SpawnedObjectData>(generator.GetAllSpawnedObjects());

    int typeIndex = FindTypeIndexForConfig(config);
    spawningSystem.SpawnObjectOfType(config, typeIndex);

    // Find the newly spawned object by comparing before/after lists
    List<SpawnedObjectData> afterSpawn = generator.GetAllSpawnedObjects();
    if (afterSpawn.Count > beforeSpawn.Count)
    {
      SpawnedObjectData newSpawn = afterSpawn[afterSpawn.Count - 1];
      return newSpawn.gameObject;
    }

    return null;
  }

  // Spawn child object positioned relative to parent
  private GameObject SpawnChildObject(GameObject parent, SpawnHierarchy rule, int childIndex)
  {
    if (parent == null || rule.childCategories == null || rule.childCategories.Length == 0)
      return null;

    ObjectCategory childCategory = rule.childCategories[generator.GetRandomizer().Next(rule.childCategories.Length)];
    ObjectTypeConfig childConfig = FindConfigForCategory(childCategory);

    if (childConfig == null || !childConfig.enableThisType)
      return null;

    if (childConfig.prefabs == null || childConfig.prefabs.Length == 0)
      return null;

    Vector3 childPosition = CalculateChildPosition(parent, rule, childIndex);

    if (!IsValidChildPosition(childPosition, childConfig, parent))
    {
      return null;
    }

    GameObject prefab = childConfig.prefabs[generator.GetRandomizer().Next(childConfig.prefabs.Length)];
    GameObject childObject = Object.Instantiate(prefab, childPosition, GetChildRotation(parent, rule));

    Bounds childBounds = CalculateChildBounds(childPosition, childConfig);
    SpawnedObjectData spawnData = new SpawnedObjectData(childObject, childPosition, childBounds, childConfig, FindTypeIndexForConfig(childConfig));
    generator.AddSpawnedObject(spawnData);

    Debug.Log($"Spawned child {childConfig.typeName} at {childPosition} for parent {parent.name}");

    return childObject;
  }

  // - POSITION CALCULATION SYSTEM
  // Calculate child position in circular pattern around parent
  private Vector3 CalculateChildPosition(GameObject parent, SpawnHierarchy rule, int childIndex)
  {
    Vector3 parentPos = parent.transform.position;

    // Calculate angle with randomness
    float baseAngle = (360f / rule.maxChildrenPerParent) * childIndex;
    float randomOffset = ((float)generator.GetRandomizer().NextDouble() - 0.5f) * rule.childAngleRandomness;
    float finalAngle = (baseAngle + randomOffset) * Mathf.Deg2Rad;

    // Position on circle around parent with distance variation
    float distance = rule.childSpawnRadius * (0.7f + 0.3f * (float)generator.GetRandomizer().NextDouble());
    Vector3 offset = new Vector3(
      Mathf.Cos(finalAngle) * distance,
      adjustChildHeightBasedOnParent ? childHeightOffset : 0f,
      Mathf.Sin(finalAngle) * distance
    );

    Vector3 childPosition = parentPos + offset;

    // Clamp to environment bounds
    Vector3 boundsMin = generator.GetEnvironmentBoundsMin();
    Vector3 boundsMax = generator.GetEnvironmentBoundsMax();

    childPosition.x = Mathf.Clamp(childPosition.x, boundsMin.x, boundsMax.x);
    childPosition.y = Mathf.Clamp(childPosition.y, boundsMin.y, boundsMax.y);
    childPosition.z = Mathf.Clamp(childPosition.z, boundsMin.z, boundsMax.z);

    return childPosition;
  }

  // Validate child position for bounds and collision
  private bool IsValidChildPosition(Vector3 position, ObjectTypeConfig config, GameObject parent)
  {
    Vector3 boundsMin = generator.GetEnvironmentBoundsMin();
    Vector3 boundsMax = generator.GetEnvironmentBoundsMax();

    if (position.x < boundsMin.x || position.x > boundsMax.x ||
        position.y < boundsMin.y || position.y > boundsMax.y ||
        position.z < boundsMin.z || position.z > boundsMax.z)
    {
      return false;
    }

    // Check collision with existing objects (except parent)
    Bounds childBounds = CalculateChildBounds(position, config);

    foreach (SpawnedObjectData existingObject in generator.GetAllSpawnedObjects())
    {
      if (existingObject.gameObject == parent) continue;

      if (childBounds.Intersects(existingObject.boundingBox))
      {
        return false;
      }
    }

    return true;
  }

  private Bounds CalculateChildBounds(Vector3 position, ObjectTypeConfig config)
  {
    Vector3 size = config.boundingBoxSize + Vector3.one * config.boundingBoxPadding;
    return new Bounds(position, size);
  }

  private Quaternion GetChildRotation(GameObject parent, SpawnHierarchy rule)
  {
    if (maintainChildOrientation && parent != null)
    {
      return parent.transform.rotation;
    }
    else
    {
      return Quaternion.Euler(0, generator.GetRandomizer().Next(0, 360), 0);
    }
  }

  // - UTILITY METHODS
  private ObjectTypeConfig FindConfigForCategory(ObjectCategory category)
  {
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();

    foreach (ObjectTypeConfig config in objectTypes)
    {
      if (config.category == category)
      {
        return config;
      }
    }

    return null;
  }

  private int FindTypeIndexForConfig(ObjectTypeConfig targetConfig)
  {
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();

    for (int i = 0; i < objectTypes.Length; i++)
    {
      if (objectTypes[i] == targetConfig)
      {
        return i;
      }
    }

    return -1;
  }

  private SpawnHierarchy FindHierarchyRuleForParent(GameObject parent)
  {
    if (parent == null) return null;

    ObjectCategory parentCategory = DetermineObjectCategory(parent);

    foreach (SpawnHierarchy rule in hierarchyRules)
    {
      if (rule.parentCategory == parentCategory)
      {
        return rule;
      }
    }

    return null;
  }

  // Determine object category based on name and tag patterns
  private ObjectCategory DetermineObjectCategory(GameObject obj)
  {
    if (obj == null) return ObjectCategory.Custom;

    string objName = obj.name.ToLower();
    string objTag = obj.tag.ToLower();

    // Check object against each category pattern
    if (objName.Contains("table") || objName.Contains("desk") || objTag.Contains("table"))
    {
      return ObjectCategory.Tables;
    }

    if (objName.Contains("bar") || objName.Contains("counter") || objTag.Contains("bar"))
    {
      return ObjectCategory.BarObjects;
    }

    if (objName.Contains("chair") || objName.Contains("seat") || objName.Contains("stool") || objTag.Contains("chair"))
    {
      return ObjectCategory.Chairs;
    }

    if (objName.Contains("bottle") || objName.Contains("wine") || objName.Contains("beer") || objTag.Contains("bottle"))
    {
      return ObjectCategory.Bottles;
    }

    if (objName.Contains("glass") || objName.Contains("cup") || objName.Contains("mug") || objTag.Contains("glass"))
    {
      return ObjectCategory.Glasses;
    }

    if (objName.Contains("shelf") || objName.Contains("cabinet") || objName.Contains("furniture") || objTag.Contains("furniture"))
    {
      return ObjectCategory.Furniture;
    }

    if (objName.Contains("decoration") || objName.Contains("plant") || objName.Contains("picture") || objTag.Contains("decoration"))
    {
      return ObjectCategory.Decorations;
    }

    return ObjectCategory.Custom;
  }

  // - PUBLIC API METHODS
  public Dictionary<GameObject, List<GameObject>> GetParentChildRelations()
  {
    return new Dictionary<GameObject, List<GameObject>>(parentChildRelations);
  }

  public List<GameObject> GetHierarchyParents()
  {
    return new List<GameObject>(hierarchyParents);
  }

  public List<GameObject> GetHierarchyChildren()
  {
    return new List<GameObject>(hierarchyChildren);
  }

  public List<GameObject> GetChildrenOfParent(GameObject parent)
  {
    if (parentChildRelations.ContainsKey(parent))
    {
      return new List<GameObject>(parentChildRelations[parent]);
    }
    return new List<GameObject>();
  }

  public GameObject GetParentOfChild(GameObject child)
  {
    foreach (var kvp in parentChildRelations)
    {
      if (kvp.Value.Contains(child))
      {
        return kvp.Key;
      }
    }
    return null;
  }

  public bool IsParentObject(GameObject obj)
  {
    return hierarchyParents.Contains(obj);
  }

  public bool IsChildObject(GameObject obj)
  {
    return hierarchyChildren.Contains(obj);
  }

  // - HIERARCHY RULE MANAGEMENT
  public void AddHierarchyRule(SpawnHierarchy rule)
  {
    List<SpawnHierarchy> rulesList = new List<SpawnHierarchy>(hierarchyRules);
    rulesList.Add(rule);
    hierarchyRules = rulesList.ToArray();
  }

  public void RemoveHierarchyRule(ObjectCategory parentCategory)
  {
    List<SpawnHierarchy> rulesList = new List<SpawnHierarchy>();

    foreach (SpawnHierarchy rule in hierarchyRules)
    {
      if (rule.parentCategory != parentCategory)
      {
        rulesList.Add(rule);
      }
    }

    hierarchyRules = rulesList.ToArray();
  }

  public SpawnHierarchy GetHierarchyRule(ObjectCategory parentCategory)
  {
    foreach (SpawnHierarchy rule in hierarchyRules)
    {
      if (rule.parentCategory == parentCategory)
      {
        return rule;
      }
    }
    return null;
  }

  // - STATISTICS AND DEBUG
  public int GetTotalHierarchyObjectCount()
  {
    return hierarchyParents.Count + hierarchyChildren.Count;
  }

  public int GetParentCount()
  {
    return hierarchyParents.Count;
  }

  public int GetChildCount()
  {
    return hierarchyChildren.Count;
  }

  public void LogHierarchyStatistics()
  {
    Debug.Log($"Hierarchy Statistics:");
    Debug.Log($"- Total Parents: {hierarchyParents.Count}");
    Debug.Log($"- Total Children: {hierarchyChildren.Count}");
    Debug.Log($"- Parent-Child Relations: {parentChildRelations.Count}");

    foreach (var kvp in parentChildRelations)
    {
      if (kvp.Key != null)
      {
        Debug.Log($"  {kvp.Key.name} has {kvp.Value.Count} children");
      }
    }
  }
}