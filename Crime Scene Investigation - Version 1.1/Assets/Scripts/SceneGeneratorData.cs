using UnityEngine;
using System.Collections.Generic;

// - REVOLUTION TARGET ENUM
[System.Serializable]
public enum RevolutionTarget
{
  CenterPoint,
  SpecificObject,
  AnyTable,
  AnyChair,
  AnyBarObject,
  AnyFurniture,
  Custom
}

// - ROTATION DIRECTION ENUM
[System.Serializable]
public enum RotationDirection
{
  UseVariation,
  FaceCenter,
  FaceAway,
  FaceRandom,
  AlignWithRadius
}

// - OBJECT CATEGORY ENUM
[System.Serializable]
public enum ObjectCategory
{
  Tables,
  BarObjects,
  Chairs,
  Bottles,
  Glasses,
  Furniture,
  Decorations,
  Evidence,
  Custom
}

// - REVOLUTION CONFIGURATION CLASS
[System.Serializable]
public class RevolutionConfig
{
  [Header("Revolution Target")]
  public RevolutionTarget target = RevolutionTarget.CenterPoint;
  public ObjectCategory specificObjectType = ObjectCategory.Tables;
  public string customTargetName = "";

  [Header("Revolution Parameters")]
  [Tooltip("Closest point the item can spawn")]
  public float minimumDistance = 1f;

  [Tooltip("Furthest point the item can spawn")]
  public float maximumDistance = 3f;

  [Tooltip("Height of the object")]
  public float spawnLevel = 0f;

  [Header("Spawn Amount")]
  [Tooltip("Minimum number to spawn")]
  [Range(0, 50)]
  public int minimumAmount = 0;

  [Tooltip("Maximum number to spawn")]
  [Range(0, 50)]
  public int maximumAmount = 3;

  [Header("Revolution Settings")]
  [Tooltip("Allow random placement within min/max distance")]
  public bool randomPlacement = true;

  [Tooltip("Distribute evenly around the target")]
  public bool evenDistribution = true;

  [Tooltip("Random angle variation (degrees)")]
  [Range(0f, 180f)]
  public float angleVariation = 30f;

  [Header("Rotation Settings")]
  [Tooltip("Control object rotation relative to revolution center")]
  public RotationDirection rotationDirection = RotationDirection.UseVariation;

  [Tooltip("Additional rotation offset (degrees)")]
  [Range(-180f, 180f)]
  public float rotationOffset = 0f;

  [Header("Collision Settings")]
  public bool enableCollisionDetection = true;
  public Vector3 boundingBoxSize = new Vector3(1f, 1f, 1f);
  public float boundingBoxPadding = 0.1f;

  [Header("Advanced Options")]
  public int maxSpawnAttempts = 50;
  public bool canOverlapWithTarget = false;
  public LayerMask validSpawnLayers = -1;

  // Generate random spawn count within configured range
  public int GetSpawnCount(System.Random randomizer)
  {
    return randomizer.Next(minimumAmount, maximumAmount + 1);
  }

  // Calculate spawn distance based on placement settings
  public float GetRandomDistance(System.Random randomizer)
  {
    if (randomPlacement)
    {
      return minimumDistance + ((float)randomizer.NextDouble() * (maximumDistance - minimumDistance));
    }
    else
    {
      return (minimumDistance + maximumDistance) * 0.5f;
    }
  }

  // Calculate angle for object placement with variation
  public float GetAngleForIndex(int index, int totalCount, System.Random randomizer)
  {
    float baseAngle = 0f;

    if (evenDistribution && totalCount > 0)
    {
      baseAngle = (360f / totalCount) * index;
    }
    else
    {
      baseAngle = (float)randomizer.NextDouble() * 360f;
    }

    float variation = ((float)randomizer.NextDouble() - 0.5f) * angleVariation * 2f;
    return baseAngle + variation;
  }
}

// - OBJECT TYPE CONFIGURATION WITH LEGACY COMPATIBILITY
[System.Serializable]
public class ObjectTypeConfig
{
  [Header("Basic Settings")]
  public string typeName = "Object Type";
  public ObjectCategory category = ObjectCategory.Tables;
  public bool enableThisType = true;
  public GameObject[] prefabs;
  public string[] objectNames;

  [Header("Revolution Configuration")]
  public RevolutionConfig revolution = new RevolutionConfig();

  [Header("Legacy Ring Settings (Tables Only)")]
  [HideInInspector] public bool useRingSpawning = false;
  [HideInInspector] public float tableRingRadius = 4f;
  [HideInInspector] public int tablePositionsOnRing = 6;
  [HideInInspector] public float chairRingRadius = 2.0f;
  [HideInInspector] public int chairsPerTable = 4;
  [HideInInspector] public float cupRingRadius = 0.8f;
  [HideInInspector] public int cupsPerTable = 3;

  // - LEGACY COMPATIBILITY PROPERTIES
  // These properties maintain backwards compatibility with old systems
  public float minSpawnRadius
  {
    get { return revolution.minimumDistance; }
    set { revolution.minimumDistance = value; }
  }

  public float maxSpawnRadius
  {
    get { return revolution.maximumDistance; }
    set { revolution.maximumDistance = value; }
  }

  public float spawnLevel
  {
    get { return revolution.spawnLevel; }
    set { revolution.spawnLevel = value; }
  }

  public int minCount
  {
    get { return revolution.minimumAmount; }
    set { revolution.minimumAmount = value; }
  }

  public int maxCount
  {
    get { return revolution.maximumAmount; }
    set { revolution.maximumAmount = value; }
  }

  public float spawnProbability
  {
    get { return 1f; }
    set { /* Legacy property, ignored */ }
  }

  public Vector3 boundingBoxSize
  {
    get { return revolution.boundingBoxSize; }
    set { revolution.boundingBoxSize = value; }
  }

  public float boundingBoxPadding
  {
    get { return revolution.boundingBoxPadding; }
    set { revolution.boundingBoxPadding = value; }
  }

  public int maxSpawnAttempts
  {
    get { return revolution.maxSpawnAttempts; }
    set { revolution.maxSpawnAttempts = value; }
  }

  public bool canSpawnOnOtherObjects
  {
    get { return revolution.canOverlapWithTarget; }
    set { revolution.canOverlapWithTarget = value; }
  }

  public LayerMask validSpawnLayers
  {
    get { return revolution.validSpawnLayers; }
    set { revolution.validSpawnLayers = value; }
  }

  // - CONSTRUCTOR AND INITIALIZATION
  public ObjectTypeConfig()
  {
    SetDefaultsForCategory();
  }

  // Set appropriate defaults based on object category
  public void SetDefaultsForCategory()
  {
    revolution = new RevolutionConfig();

    switch (category)
    {
      case ObjectCategory.Tables:
        typeName = "Tables";
        revolution.target = RevolutionTarget.CenterPoint;
        revolution.minimumDistance = 2f;
        revolution.maximumDistance = 4f;
        revolution.spawnLevel = 0f;
        revolution.minimumAmount = 1;
        revolution.maximumAmount = 6;
        revolution.boundingBoxSize = new Vector3(2f, 1f, 2f);
        revolution.enableCollisionDetection = true;
        revolution.rotationDirection = RotationDirection.UseVariation;
        revolution.rotationOffset = 0f;
        useRingSpawning = true;
        chairRingRadius = 2.0f;
        chairsPerTable = 4;
        cupRingRadius = 0.8f;
        cupsPerTable = 3;
        break;

      case ObjectCategory.Chairs:
        typeName = "Chairs";
        revolution.target = RevolutionTarget.AnyTable;
        revolution.minimumDistance = 1.8f;
        revolution.maximumDistance = 2.2f;
        revolution.spawnLevel = 0f;
        revolution.minimumAmount = 2;
        revolution.maximumAmount = 4;
        revolution.boundingBoxSize = new Vector3(0.8f, 1f, 0.8f);
        revolution.enableCollisionDetection = true;
        revolution.rotationDirection = RotationDirection.FaceCenter;
        revolution.rotationOffset = 0f;
        break;

      case ObjectCategory.Glasses:
        typeName = "Glasses";
        revolution.target = RevolutionTarget.AnyTable;
        revolution.minimumDistance = 0.6f;
        revolution.maximumDistance = 1.0f;
        revolution.spawnLevel = 1f;
        revolution.minimumAmount = 1;
        revolution.maximumAmount = 3;
        revolution.boundingBoxSize = new Vector3(0.1f, 0.2f, 0.1f);
        revolution.enableCollisionDetection = false;
        revolution.canOverlapWithTarget = true;
        revolution.rotationDirection = RotationDirection.FaceRandom;
        revolution.rotationOffset = 0f;
        break;

      case ObjectCategory.Bottles:
        typeName = "Bottles";
        revolution.target = RevolutionTarget.AnyTable;
        revolution.minimumDistance = 0.7f;
        revolution.maximumDistance = 1.2f;
        revolution.spawnLevel = 1f;
        revolution.minimumAmount = 1;
        revolution.maximumAmount = 3;
        revolution.boundingBoxSize = new Vector3(0.1f, 0.3f, 0.1f);
        revolution.enableCollisionDetection = false;
        revolution.canOverlapWithTarget = true;
        revolution.rotationDirection = RotationDirection.FaceRandom;
        revolution.rotationOffset = 0f;
        break;

      case ObjectCategory.BarObjects:
        typeName = "Bar Objects";
        revolution.target = RevolutionTarget.CenterPoint;
        revolution.minimumDistance = 1f;
        revolution.maximumDistance = 3f;
        revolution.spawnLevel = 0f;
        revolution.minimumAmount = 0;
        revolution.maximumAmount = 2;
        revolution.boundingBoxSize = new Vector3(3f, 1f, 1f);
        revolution.enableCollisionDetection = true;
        revolution.rotationDirection = RotationDirection.FaceCenter;
        revolution.rotationOffset = 0f;
        break;

      case ObjectCategory.Furniture:
        typeName = "Furniture";
        revolution.target = RevolutionTarget.CenterPoint;
        revolution.minimumDistance = 4f;
        revolution.maximumDistance = 8f;
        revolution.spawnLevel = 0f;
        revolution.minimumAmount = 0;
        revolution.maximumAmount = 2;
        revolution.boundingBoxSize = new Vector3(1.5f, 2f, 1.5f);
        revolution.enableCollisionDetection = true;
        revolution.rotationDirection = RotationDirection.UseVariation;
        revolution.rotationOffset = 0f;
        break;

      case ObjectCategory.Decorations:
        typeName = "Decorations";
        revolution.target = RevolutionTarget.AnyTable;
        revolution.minimumDistance = 0.5f;
        revolution.maximumDistance = 1.5f;
        revolution.spawnLevel = 1f;
        revolution.minimumAmount = 0;
        revolution.maximumAmount = 2;
        revolution.boundingBoxSize = new Vector3(0.3f, 0.5f, 0.3f);
        revolution.enableCollisionDetection = false;
        revolution.canOverlapWithTarget = true;
        revolution.rotationDirection = RotationDirection.FaceRandom;
        revolution.rotationOffset = 0f;
        break;

      default:
        typeName = "Custom Object";
        revolution.target = RevolutionTarget.CenterPoint;
        revolution.minimumDistance = 1f;
        revolution.maximumDistance = 3f;
        revolution.spawnLevel = 0f;
        revolution.minimumAmount = 1;
        revolution.maximumAmount = 3;
        revolution.enableCollisionDetection = true;
        revolution.rotationDirection = RotationDirection.UseVariation;
        revolution.rotationOffset = 0f;
        break;
    }
  }

  // Check if legacy ring settings should be displayed
  public bool ShouldShowRingSettings()
  {
    return category == ObjectCategory.Tables && useRingSpawning;
  }

  // Resolve revolution targets from spawned objects
  public List<Vector3> GetRevolutionTargets(List<SpawnedObjectData> allSpawnedObjects)
  {
    List<Vector3> targets = new List<Vector3>();

    switch (revolution.target)
    {
      case RevolutionTarget.CenterPoint:
        targets.Add(Vector3.zero);
        break;

      case RevolutionTarget.AnyTable:
        foreach (var obj in allSpawnedObjects)
        {
          if (obj.typeConfig.category == ObjectCategory.Tables)
            targets.Add(obj.position);
        }
        break;

      case RevolutionTarget.AnyChair:
        foreach (var obj in allSpawnedObjects)
        {
          if (obj.typeConfig.category == ObjectCategory.Chairs)
            targets.Add(obj.position);
        }
        break;

      case RevolutionTarget.AnyBarObject:
        foreach (var obj in allSpawnedObjects)
        {
          if (obj.typeConfig.category == ObjectCategory.BarObjects)
            targets.Add(obj.position);
        }
        break;

      case RevolutionTarget.AnyFurniture:
        foreach (var obj in allSpawnedObjects)
        {
          if (obj.typeConfig.category == ObjectCategory.Furniture)
            targets.Add(obj.position);
        }
        break;

      case RevolutionTarget.SpecificObject:
        foreach (var obj in allSpawnedObjects)
        {
          if (obj.typeConfig.category == revolution.specificObjectType)
            targets.Add(obj.position);
        }
        break;

      case RevolutionTarget.Custom:
        if (!string.IsNullOrEmpty(revolution.customTargetName))
        {
          foreach (var obj in allSpawnedObjects)
          {
            if (obj.gameObject.name.Contains(revolution.customTargetName))
              targets.Add(obj.position);
          }
        }
        break;
    }

    // Fallback to center point if no targets found
    if (targets.Count == 0)
    {
      targets.Add(Vector3.zero);
    }

    return targets;
  }
}

// - UNITY EDITOR CUSTOM PROPERTY DRAWER
#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ObjectTypeConfig))]
public class ObjectTypeConfigDrawer : UnityEditor.PropertyDrawer
{
  public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
  {
    UnityEditor.EditorGUI.BeginProperty(position, label, property);

    var categoryProp = property.FindPropertyRelative("category");
    ObjectCategory category = (ObjectCategory)categoryProp.enumValueIndex;

    UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("typeName"));
    UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("category"));
    UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("enableThisType"));
    UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("prefabs"));
    UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("objectNames"));

    UnityEditor.EditorGUILayout.Space(10);
    UnityEditor.EditorGUILayout.LabelField("Revolution Configuration", UnityEditor.EditorStyles.boldLabel);

    var revolutionProp = property.FindPropertyRelative("revolution");
    DrawRevolutionConfig(revolutionProp);

    // Show legacy ring settings for tables only
    if (category == ObjectCategory.Tables)
    {
      UnityEditor.EditorGUILayout.Space(10);
      var useRingProp = property.FindPropertyRelative("useRingSpawning");
      UnityEditor.EditorGUILayout.PropertyField(useRingProp, new GUIContent("Use Legacy Ring Spawning"));

      if (useRingProp.boolValue)
      {
        UnityEditor.EditorGUILayout.LabelField("Legacy Ring Settings", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUI.indentLevel++;
        UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("chairRingRadius"));
        UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("chairsPerTable"));
        UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("cupRingRadius"));
        UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("cupsPerTable"));
        UnityEditor.EditorGUI.indentLevel--;
      }
    }

    UnityEditor.EditorGUI.EndProperty();
  }

  // Draw revolution configuration section
  private void DrawRevolutionConfig(UnityEditor.SerializedProperty revolutionProp)
  {
    UnityEditor.EditorGUI.indentLevel++;

    var targetProp = revolutionProp.FindPropertyRelative("target");
    UnityEditor.EditorGUILayout.PropertyField(targetProp);

    RevolutionTarget target = (RevolutionTarget)targetProp.enumValueIndex;

    // Show additional fields based on target type
    if (target == RevolutionTarget.SpecificObject)
    {
      UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("specificObjectType"));
    }
    else if (target == RevolutionTarget.Custom)
    {
      UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("customTargetName"));
    }

    // Draw all revolution configuration sections
    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Revolution Parameters", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("minimumDistance"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("maximumDistance"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("spawnLevel"));

    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Spawn Amount", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("minimumAmount"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("maximumAmount"));

    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Revolution Settings", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("randomPlacement"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("evenDistribution"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("angleVariation"));

    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Rotation Settings", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("rotationDirection"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("rotationOffset"));

    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Collision Settings", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("enableCollisionDetection"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("boundingBoxSize"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("boundingBoxPadding"));

    UnityEditor.EditorGUILayout.Space(5);
    UnityEditor.EditorGUILayout.LabelField("Advanced Options", UnityEditor.EditorStyles.boldLabel);
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("maxSpawnAttempts"));
    UnityEditor.EditorGUILayout.PropertyField(revolutionProp.FindPropertyRelative("canOverlapWithTarget"));

    UnityEditor.EditorGUI.indentLevel--;
  }

  public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
  {
    return 0;
  }
}
#endif

// - SPAWNED OBJECT DATA CLASS
[System.Serializable]
public class SpawnedObjectData
{
  public GameObject gameObject;
  public Vector3 position;
  public Bounds boundingBox;
  public ObjectTypeConfig typeConfig;
  public int typeIndex;
  public Vector3 revolutionTarget;

  public SpawnedObjectData(GameObject obj, Vector3 pos, Bounds bounds, ObjectTypeConfig config, int index, Vector3 target)
  {
    gameObject = obj;
    position = pos;
    boundingBox = bounds;
    typeConfig = config;
    typeIndex = index;
    revolutionTarget = target;
  }

  public SpawnedObjectData(GameObject obj, Vector3 pos, Bounds bounds, ObjectTypeConfig config, int index)
  {
    gameObject = obj;
    position = pos;
    boundingBox = bounds;
    typeConfig = config;
    typeIndex = index;
    revolutionTarget = Vector3.zero;
  }
}

// - REVOLUTION RELATIONSHIP TRACKING CLASS
[System.Serializable]
public class RevolutionRelationship
{
  public GameObject revolutionCenter;
  public List<GameObject> orbitingObjects;
  public ObjectTypeConfig centerConfig;

  public RevolutionRelationship(GameObject center, ObjectTypeConfig config)
  {
    revolutionCenter = center;
    centerConfig = config;
    orbitingObjects = new List<GameObject>();
  }
}

// - PREVIEW OBJECT MARKER COMPONENT
public class PreviewObjectMarker : MonoBehaviour
{
  public ObjectTypeConfig originalConfig;
  public int previewIndex;
}