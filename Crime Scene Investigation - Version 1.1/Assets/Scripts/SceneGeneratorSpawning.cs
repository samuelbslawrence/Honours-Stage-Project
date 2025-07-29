using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// - REVOLUTION-BASED SCENE GENERATOR SPAWNING SYSTEM
public class SceneGeneratorSpawning
{
  // - PRIVATE STATE VARIABLES
  private CrimeSceneGenerator generator;
  private SceneGeneratorHierarchy hierarchySystem;
  private SceneGeneratorDespawn despawnSystem;

  private List<SpawnedObjectData> allSpawnedObjects = new List<SpawnedObjectData>();
  private Dictionary<GameObject, RevolutionRelationship> revolutionRelationships = new Dictionary<GameObject, RevolutionRelationship>();
  private List<Vector3> centerPoints = new List<Vector3>();

  // Lookup dictionary for fast object category queries
  private Dictionary<GameObject, ObjectCategory> objectCategoryLookup = new Dictionary<GameObject, ObjectCategory>();

  // - CONSTRUCTOR
  public SceneGeneratorSpawning(CrimeSceneGenerator generator)
  {
    this.generator = generator;
    centerPoints.Add(generator.GetCentralPosition());
  }

  public void SetDependencies(SceneGeneratorHierarchy hierarchy, SceneGeneratorDespawn despawn)
  {
    this.hierarchySystem = hierarchy;
    this.despawnSystem = despawn;
  }

  // - MAIN GENERATION SYSTEM
  // Two-phase generation: center objects first, then objects revolving around them
  public void GenerateObjects()
  {
    allSpawnedObjects.Clear();
    revolutionRelationships.Clear();
    objectCategoryLookup.Clear();
    centerPoints.Clear();
    centerPoints.Add(generator.GetCentralPosition());

    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();

    GenerateCenterPointObjects(objectTypes);
    GenerateRevolvingObjects(objectTypes);
  }

  // Phase 1: Generate objects that serve as revolution centers
  private void GenerateCenterPointObjects(ObjectTypeConfig[] objectTypes)
  {
    foreach (ObjectTypeConfig config in objectTypes)
    {
      if (!config.enableThisType) continue;

      if (config.revolution.target == RevolutionTarget.CenterPoint)
      {
        GenerateObjectsForConfig(config, centerPoints);
      }
    }
  }

  // Phase 2: Generate objects that revolve around existing objects
  private void GenerateRevolvingObjects(ObjectTypeConfig[] objectTypes)
  {
    foreach (ObjectTypeConfig config in objectTypes)
    {
      if (!config.enableThisType) continue;

      if (config.revolution.target != RevolutionTarget.CenterPoint)
      {
        List<Vector3> targets = config.GetRevolutionTargets(allSpawnedObjects);
        if (targets.Count > 0)
        {
          GenerateObjectsForConfig(config, targets);
        }
      }
    }
  }

  // Generate objects of specific type around provided revolution targets
  private void GenerateObjectsForConfig(ObjectTypeConfig config, List<Vector3> revolutionTargets)
  {
    if (config.prefabs == null || config.prefabs.Length == 0) return;

    foreach (Vector3 target in revolutionTargets)
    {
      int spawnCount = config.revolution.GetSpawnCount(generator.GetRandomizer());

      for (int i = 0; i < spawnCount; i++)
      {
        SpawnObjectAroundTarget(config, target, i, spawnCount);
      }
    }
  }

  // Attempt to spawn individual object with collision checking and retry logic
  private bool SpawnObjectAroundTarget(ObjectTypeConfig config, Vector3 target, int index, int totalCount)
  {
    for (int attempt = 0; attempt < config.maxSpawnAttempts; attempt++)
    {
      Vector3 spawnPosition = CalculateRevolutionPosition(config, target, index, totalCount);

      if (IsValidSpawnPosition(spawnPosition, config, target))
      {
        GameObject prefab = config.prefabs[generator.GetRandomizer().Next(config.prefabs.Length)];
        GameObject spawnedObject = Object.Instantiate(prefab, spawnPosition, GetRevolutionRotation(spawnPosition, target, config));

        Bounds boundingBox = CalculateBoundingBox(spawnPosition, config);
        SpawnedObjectData spawnData = new SpawnedObjectData(
          spawnedObject,
          spawnPosition,
          boundingBox,
          config,
          GetTypeIndex(config),
          target
        );

        allSpawnedObjects.Add(spawnData);
        generator.AddSpawnedObject(spawnData);

        // Update category lookup for fast queries
        objectCategoryLookup[spawnedObject] = config.category;

        TrackRevolutionRelationship(spawnedObject, target, config);

        return true;
      }
    }

    return false;
  }

  // - POSITION AND ROTATION CALCULATION SYSTEM
  // Calculate position in circular pattern around target using polar coordinates
  private Vector3 CalculateRevolutionPosition(ObjectTypeConfig config, Vector3 target, int index, int totalCount)
  {
    float distance = config.revolution.GetRandomDistance(generator.GetRandomizer());
    float angle = config.revolution.GetAngleForIndex(index, totalCount, generator.GetRandomizer());

    float angleRad = angle * Mathf.Deg2Rad;

    Vector3 offset = new Vector3(
      Mathf.Cos(angleRad) * distance,
      0f,
      Mathf.Sin(angleRad) * distance
    );

    Vector3 position = target + offset;
    position.y = target.y + config.spawnLevel;

    // Clamp to environment bounds
    Vector3 boundsMin = generator.GetEnvironmentBoundsMin();
    Vector3 boundsMax = generator.GetEnvironmentBoundsMax();

    position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
    position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
    position.z = Mathf.Clamp(position.z, boundsMin.z, boundsMax.z);

    return position;
  }

  // Calculate object rotation based on revolution configuration
  private Quaternion GetRevolutionRotation(Vector3 position, Vector3 target, ObjectTypeConfig config)
  {
    switch (config.revolution.rotationDirection)
    {
      case RotationDirection.FaceCenter:
        return GetRotationFacingTarget(position, target, config.revolution.rotationOffset);

      case RotationDirection.FaceAway:
        return GetRotationFacingAwayFromTarget(position, target, config.revolution.rotationOffset);

      case RotationDirection.FaceRandom:
        return GetRandomRotation(config.revolution.rotationOffset);

      case RotationDirection.AlignWithRadius:
        return GetRotationAlignedWithRadius(position, target, config.revolution.rotationOffset);

      case RotationDirection.UseVariation:
      default:
        return GetRotationWithVariation(position, target, config);
    }
  }

  private Quaternion GetRotationFacingTarget(Vector3 position, Vector3 target, float offset)
  {
    Vector3 directionToTarget = (target - position).normalized;

    if (directionToTarget != Vector3.zero)
    {
      float angle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
      return Quaternion.Euler(0, angle + offset, 0);
    }

    return Quaternion.Euler(0, offset, 0);
  }

  private Quaternion GetRotationFacingAwayFromTarget(Vector3 position, Vector3 target, float offset)
  {
    Vector3 directionAwayFromTarget = (position - target).normalized;

    if (directionAwayFromTarget != Vector3.zero)
    {
      float angle = Mathf.Atan2(directionAwayFromTarget.x, directionAwayFromTarget.z) * Mathf.Rad2Deg;
      return Quaternion.Euler(0, angle + offset, 0);
    }

    return Quaternion.Euler(0, offset + 180f, 0);
  }

  private Quaternion GetRandomRotation(float offset)
  {
    float randomAngle = generator.GetRandomizer().Next(0, 360);
    return Quaternion.Euler(0, randomAngle + offset, 0);
  }

  // Calculate rotation tangent to the revolution circle
  private Quaternion GetRotationAlignedWithRadius(Vector3 position, Vector3 target, float offset)
  {
    Vector3 radius = position - target;
    radius.y = 0;

    if (radius != Vector3.zero)
    {
      Vector3 tangent = Vector3.Cross(Vector3.up, radius).normalized;
      float angle = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;
      return Quaternion.Euler(0, angle + offset, 0);
    }

    return Quaternion.Euler(0, offset, 0);
  }

  private Quaternion GetRotationWithVariation(Vector3 position, Vector3 target, ObjectTypeConfig config)
  {
    Vector3 directionToTarget = (target - position).normalized;

    if (directionToTarget != Vector3.zero)
    {
      float angle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
      float variation = ((float)generator.GetRandomizer().NextDouble() - 0.5f) * config.revolution.angleVariation * 2f;
      return Quaternion.Euler(0, angle + variation + config.revolution.rotationOffset, 0);
    }

    return Quaternion.Euler(0, generator.GetRandomizer().Next(0, 360) + config.revolution.rotationOffset, 0);
  }

  // - VALIDATION SYSTEM
  // Check if position is valid considering bounds, collisions, and overlap rules
  private bool IsValidSpawnPosition(Vector3 position, ObjectTypeConfig config, Vector3 target)
  {
    if (!IsWithinEnvironmentBounds(position))
      return false;

    if (!config.revolution.enableCollisionDetection)
      return true;

    Bounds newBounds = CalculateBoundingBox(position, config);

    foreach (SpawnedObjectData existingObject in allSpawnedObjects)
    {
      if (config.revolution.canOverlapWithTarget)
      {
        float distanceToTarget = Vector3.Distance(existingObject.position, target);
        if (distanceToTarget < 0.5f)
          continue;
      }

      if (!existingObject.typeConfig.revolution.enableCollisionDetection)
        continue;

      if (newBounds.Intersects(existingObject.boundingBox))
      {
        if (CanOverlapWithObject(config, existingObject.typeConfig))
        {
          float distance = Vector3.Distance(position, existingObject.position);
          if (distance < 0.3f)
            return false;
        }
        else
        {
          return false;
        }
      }
    }

    return true;
  }

  // Define special overlap rules between object categories
  private bool CanOverlapWithObject(ObjectTypeConfig spawningConfig, ObjectTypeConfig existingConfig)
  {
    // Glasses and bottles can overlap with tables
    if ((spawningConfig.category == ObjectCategory.Glasses || spawningConfig.category == ObjectCategory.Bottles) &&
        existingConfig.category == ObjectCategory.Tables)
    {
      return true;
    }

    // Decorations can overlap with furniture
    if (spawningConfig.category == ObjectCategory.Decorations &&
        existingConfig.category == ObjectCategory.Furniture)
    {
      return true;
    }

    return false;
  }

  private bool IsWithinEnvironmentBounds(Vector3 position)
  {
    Vector3 boundsMin = generator.GetEnvironmentBoundsMin();
    Vector3 boundsMax = generator.GetEnvironmentBoundsMax();

    return position.x >= boundsMin.x && position.x <= boundsMax.x &&
           position.y >= boundsMin.y && position.y <= boundsMax.y &&
           position.z >= boundsMin.z && position.z <= boundsMax.z;
  }

  // - RELATIONSHIP TRACKING SYSTEM
  // Track which objects revolve around which centers for visualization and logic
  private void TrackRevolutionRelationship(GameObject spawnedObject, Vector3 target, ObjectTypeConfig config)
  {
    GameObject centerObject = FindObjectAtPosition(target);

    if (centerObject != null)
    {
      if (!revolutionRelationships.ContainsKey(centerObject))
      {
        revolutionRelationships[centerObject] = new RevolutionRelationship(centerObject, config);
      }

      revolutionRelationships[centerObject].orbitingObjects.Add(spawnedObject);
    }
  }

  private GameObject FindObjectAtPosition(Vector3 target)
  {
    float closestDistance = float.MaxValue;
    GameObject closestObject = null;

    foreach (SpawnedObjectData obj in allSpawnedObjects)
    {
      float distance = Vector3.Distance(obj.position, target);
      if (distance < closestDistance && distance < 1f)
      {
        closestDistance = distance;
        closestObject = obj.gameObject;
      }
    }

    return closestObject;
  }

  // - EVIDENCE SPAWNING
  public void SpawnEvidence(ObjectTypeConfig evidenceConfig)
  {
    if (evidenceConfig.prefabs == null || evidenceConfig.prefabs.Length == 0)
    {
      return;
    }

    if (evidenceConfig.revolution.target == RevolutionTarget.CenterPoint)
    {
      List<Vector3> targets = new List<Vector3> { generator.GetCentralPosition() };
      GenerateObjectsForConfig(evidenceConfig, targets);
    }
    else
    {
      List<Vector3> targets = evidenceConfig.GetRevolutionTargets(allSpawnedObjects);
      GenerateObjectsForConfig(evidenceConfig, targets);
    }
  }

  // - UTILITY METHODS
  private Bounds CalculateBoundingBox(Vector3 position, ObjectTypeConfig config)
  {
    Vector3 size = config.boundingBoxSize + Vector3.one * config.boundingBoxPadding;
    return new Bounds(position, size);
  }

  private int GetTypeIndex(ObjectTypeConfig targetConfig)
  {
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();
    for (int i = 0; i < objectTypes.Length; i++)
    {
      if (objectTypes[i] == targetConfig)
        return i;
    }
    return 0;
  }

  // Helper methods for efficient category checking
  private bool IsObjectOfCategory(GameObject obj, ObjectCategory category)
  {
    return objectCategoryLookup.TryGetValue(obj, out ObjectCategory objCategory) && objCategory == category;
  }

  private bool IsChair(GameObject obj)
  {
    return IsObjectOfCategory(obj, ObjectCategory.Chairs);
  }

  private bool IsDrinkware(GameObject obj)
  {
    return IsObjectOfCategory(obj, ObjectCategory.Glasses) || IsObjectOfCategory(obj, ObjectCategory.Bottles);
  }

  // - PUBLIC API METHODS
  public List<SpawnedObjectData> GetAllSpawnedObjects()
  {
    return new List<SpawnedObjectData>(allSpawnedObjects);
  }

  public Dictionary<GameObject, RevolutionRelationship> GetRevolutionRelationships()
  {
    return new Dictionary<GameObject, RevolutionRelationship>(revolutionRelationships);
  }

  public List<GameObject> GetObjectsRevolvingAround(GameObject centerObject)
  {
    if (revolutionRelationships.ContainsKey(centerObject))
    {
      return new List<GameObject>(revolutionRelationships[centerObject].orbitingObjects);
    }
    return new List<GameObject>();
  }

  public GameObject GetRevolutionCenterFor(GameObject orbitingObject)
  {
    foreach (var relationship in revolutionRelationships.Values)
    {
      if (relationship.orbitingObjects.Contains(orbitingObject))
      {
        return relationship.revolutionCenter;
      }
    }
    return null;
  }

  public Vector3 GetRevolutionTargetFor(GameObject obj)
  {
    foreach (SpawnedObjectData spawnData in allSpawnedObjects)
    {
      if (spawnData.gameObject == obj)
      {
        return spawnData.revolutionTarget;
      }
    }
    return Vector3.zero;
  }

  public int GetTotalObjectCount()
  {
    return allSpawnedObjects.Count;
  }

  public int GetRevolutionCenterCount()
  {
    return revolutionRelationships.Count;
  }

  public int GetOrbitingObjectCount()
  {
    int count = 0;
    foreach (var relationship in revolutionRelationships.Values)
    {
      count += relationship.orbitingObjects.Count;
    }
    return count;
  }

  // - MAINTENANCE AND CLEANUP
  // Remove null references and empty relationships from revolution system
  public void CleanupRevolutionSystem()
  {
    var keysToRemove = new List<GameObject>();

    // Check each revolution relationship for validity
    foreach (var kvp in revolutionRelationships)
    {
      if (kvp.Key == null)
      {
        keysToRemove.Add(kvp.Key);
        continue;
      }

      // Remove null orbiting objects
      kvp.Value.orbitingObjects.RemoveAll(obj => obj == null);

      // Mark relationships with no orbiting objects for removal
      if (kvp.Value.orbitingObjects.Count == 0)
      {
        keysToRemove.Add(kvp.Key);
      }
    }

    // Remove empty or invalid relationships
    foreach (GameObject key in keysToRemove)
    {
      revolutionRelationships.Remove(key);
    }

    // Clean up spawned objects list and category lookup
    allSpawnedObjects.RemoveAll(data => data.gameObject == null);

    var categoryKeysToRemove = objectCategoryLookup.Keys.Where(key => key == null).ToList();
    foreach (GameObject key in categoryKeysToRemove)
    {
      objectCategoryLookup.Remove(key);
    }
  }

  public bool IsConfigEnabled(ObjectTypeConfig config)
  {
    return config.enableThisType;
  }

  public void SpawnObjectOfType(ObjectTypeConfig config, int typeIndex)
  {
    List<Vector3> targets = config.GetRevolutionTargets(allSpawnedObjects);
    if (targets.Count == 0)
    {
      targets.Add(generator.GetCentralPosition());
    }

    GenerateObjectsForConfig(config, targets);
  }

  // - ESSENTIAL QUERY METHODS (used by other scripts)
  public List<SpawnedObjectData> GetSpawnedTables()
  {
    return allSpawnedObjects.Where(obj => obj.typeConfig.category == ObjectCategory.Tables).ToList();
  }

  public Dictionary<GameObject, List<GameObject>> GetTableChairs()
  {
    var tableChairs = new Dictionary<GameObject, List<GameObject>>();

    foreach (var relationship in revolutionRelationships)
    {
      GameObject centerObject = relationship.Key;

      if (centerObject != null && relationship.Value.centerConfig.category == ObjectCategory.Tables)
      {
        List<GameObject> chairs = relationship.Value.orbitingObjects
          .Where(obj => obj != null && IsChair(obj))
          .ToList();

        if (chairs.Count > 0)
        {
          tableChairs[centerObject] = chairs;
        }
      }
    }

    return tableChairs;
  }

  public Dictionary<GameObject, List<GameObject>> GetTableCups()
  {
    var tableCups = new Dictionary<GameObject, List<GameObject>>();

    foreach (var relationship in revolutionRelationships)
    {
      GameObject centerObject = relationship.Key;

      if (centerObject != null && relationship.Value.centerConfig.category == ObjectCategory.Tables)
      {
        List<GameObject> drinkware = relationship.Value.orbitingObjects
          .Where(obj => obj != null && IsDrinkware(obj))
          .ToList();

        if (drinkware.Count > 0)
        {
          tableCups[centerObject] = drinkware;
        }
      }
    }

    return tableCups;
  }
}