using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// - MAIN CRIME SCENE GENERATOR CLASS WITH EVIDENCE QUANTITY SLIDERS
public class CrimeSceneGenerator : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("RANDOM SEED SETTINGS")]
  [Tooltip("Use a custom seed for reproducible random generation")]
  public bool useCustomSeed = false;
  [Tooltip("Custom seed value for random generation")]
  public int customSeed = 12345;
  [Space(10)]

  [Header("Real-Time Update Settings")]
  [Tooltip("Enable automatic scene updates when configuration changes")]
  [SerializeField] private bool enableRealTimeUpdates = true;
  [Tooltip("How often to check for configuration changes (in seconds)")]
  [SerializeField] private float updateCheckInterval = 0.2f;
  [Tooltip("Debounce rapid updates to prevent excessive regeneration")]
  [SerializeField] private bool debounceUpdates = true;
  [Tooltip("Delay before applying debounced updates")]
  [SerializeField] private float debounceDelay = 0.1f;
  [Space(10)]

  [Header("Central Object Settings")]
  [Tooltip("Transform to use as the central point for generation")]
  [SerializeField] private Transform centralObject;
  [Tooltip("Automatically find central object using predefined names")]
  [SerializeField] private bool autoFindCentralObject = true;
  [Tooltip("Names to search for when finding central object")]
  [SerializeField] private string[] centralObjectNames = { "Bar Counter", "Desk", "Table", "Central Point" };
  [Tooltip("Fallback position when no central object is found")]
  [SerializeField] private Vector3 fallbackCentralPosition = Vector3.zero;

  [Header("Environment Bounding Box")]
  [Tooltip("Minimum bounds for object spawning area")]
  [SerializeField] private Vector3 environmentBoundsMin = new Vector3(-10f, 0f, -10f);
  [Tooltip("Maximum bounds for object spawning area")]
  [SerializeField] private Vector3 environmentBoundsMax = new Vector3(10f, 5f, 10f);
  [Tooltip("Show environment bounds visualization in scene view")]
  [SerializeField] public bool visualizeEnvironmentBounds = true;
  [Tooltip("Color for environment bounds visualization")]
  [SerializeField] public Color environmentBoundsColor = Color.yellow;

  [Header("Debug Settings")]
  [Tooltip("Enable debug visualization of spawned objects")]
  public bool enableDebugVisualization = true;
  [Tooltip("Log detailed information about spawn operations")]
  public bool logSpawnDetails = false;

  [Header("Object Type Configurations")]
  [Tooltip("Configuration for different types of objects to spawn")]
  [SerializeField]
  private ObjectTypeConfig[] objectTypes = new ObjectTypeConfig[]
  {
    new ObjectTypeConfig()
    {
      typeName = "Tables",
      category = ObjectCategory.Tables,
      minSpawnRadius = 2f,
      maxSpawnRadius = 4f,
      spawnLevel = 0f,
      useRingSpawning = true,
      tableRingRadius = 4f,
      tablePositionsOnRing = 6,
      chairRingRadius = 1.5f,
      chairsPerTable = 4,
      cupRingRadius = 0.8f,
      cupsPerTable = 3,
      boundingBoxSize = new Vector3(2f, 1f, 2f),
      minCount = 1,
      maxCount = 3
    },
    new ObjectTypeConfig()
    {
      typeName = "Bar Objects",
      category = ObjectCategory.BarObjects,
      minSpawnRadius = 1f,
      maxSpawnRadius = 3f,
      spawnLevel = 0.8f,
      boundingBoxSize = new Vector3(3f, 1f, 1f),
      minCount = 0,
      maxCount = 2
    },
    new ObjectTypeConfig()
    {
      typeName = "Chairs",
      category = ObjectCategory.Chairs,
      minSpawnRadius = 3f,
      maxSpawnRadius = 7f,
      spawnLevel = 0f,
      boundingBoxSize = new Vector3(1f, 1f, 1f),
      minCount = 1,
      maxCount = 4
    },
    new ObjectTypeConfig()
    {
      typeName = "Bottles",
      category = ObjectCategory.Bottles,
      minSpawnRadius = 1f,
      maxSpawnRadius = 6f,
      spawnLevel = 1f,
      boundingBoxSize = new Vector3(0.3f, 1f, 0.3f),
      minCount = 2,
      maxCount = 5
    },
    new ObjectTypeConfig()
    {
      typeName = "Glasses",
      category = ObjectCategory.Glasses,
      minSpawnRadius = 1f,
      maxSpawnRadius = 5f,
      spawnLevel = 1f,
      boundingBoxSize = new Vector3(0.2f, 0.5f, 0.2f),
      minCount = 1,
      maxCount = 4
    },
    new ObjectTypeConfig()
    {
      typeName = "Furniture",
      category = ObjectCategory.Furniture,
      minSpawnRadius = 4f,
      maxSpawnRadius = 8f,
      spawnLevel = 0f,
      boundingBoxSize = new Vector3(1.5f, 2f, 1.5f),
      minCount = 0,
      maxCount = 2
    }
  };

  [Header("Evidence Integration")]
  [Tooltip("Prefabs for evidence items to spawn")]
  [SerializeField] private GameObject[] evidencePrefabs;
  [Tooltip("Names for evidence items")]
  [SerializeField] private string[] evidenceNames;

  [Header("Evidence Quantities")]
  [Tooltip("Maximum quantity for each evidence item (0-30)")]
  [SerializeField] private int[] evidenceMaxQuantities;

  [Tooltip("Configuration settings for evidence spawning")]
  [SerializeField]
  private ObjectTypeConfig evidenceConfig = new ObjectTypeConfig()
  {
    typeName = "Evidence",
    minSpawnRadius = 0.5f,
    maxSpawnRadius = 8f,
    spawnLevel = 0f,
    boundingBoxSize = new Vector3(0.3f, 0.5f, 0.3f),
    minCount = 3,
    maxCount = 6,
    spawnProbability = 1f
  };

  [Header("Generation Settings")]
  [Tooltip("Generate scene automatically on Start")]
  public bool generateOnStart = true;
  [Tooltip("Clear previous objects before generating new scene")]
  public bool clearPreviousScene = true;
  [Tooltip("Maximum attempts to spawn an object before giving up")]
  public int globalMaxSpawnAttempts = 100;
  [Tooltip("Additional buffer space for collision detection")]
  public float collisionCheckBuffer = 0.1f;

  [Header("Integration Settings")]
  [Tooltip("Reference to evidence checklist component")]
  public EvidenceChecklist evidenceChecklist;
  [Tooltip("Automatically find evidence checklist in scene")]
  public bool autoFindEvidenceChecklist = true;
  [Tooltip("Notify evidence checklist when scene is generated")]
  public bool notifyChecklistOnGeneration = true;

  [Tooltip("Reference to camera script component")]
  public CameraScript cameraScript;
  [Tooltip("Automatically find camera script in scene")]
  public bool autoFindCameraScript = true;
  [Tooltip("Notify camera script when scene is generated")]
  public bool notifyCameraOnGeneration = true;

  [Header("Real-Time Integration")]
  [Tooltip("Enable real-time notifications to other systems")]
  public bool enableRealTimeNotifications = true;
  [Tooltip("Delay before sending notifications after generation")]
  public float notificationDelay = 0.2f;

  [Header("Visualization System")]
  [Tooltip("Visualization system for scene generation debug display")]
  public SceneGeneratorVisualization visualizationSystem;

  // - PRIVATE STATE VARIABLES
  // Component references
  private SceneGeneratorSpawning spawningSystem;
  private SceneGeneratorHierarchy hierarchySystem;
  private SceneGeneratorDespawn despawnSystem;
  private SceneGeneratorCategories categorySystem;

  // Generation state
  private System.Random randomizer;
  private Vector3 centralPosition;
  private List<SpawnedObjectData> allSpawnedObjects = new List<SpawnedObjectData>();
  private List<string> currentSceneEvidenceNames = new List<string>();
  private List<GameObject> currentSceneEvidenceObjects = new List<GameObject>();
  private int lastGenerationFrame = -1;

  // Real-time update tracking
  private ObjectTypeConfig[] lastObjectTypesSnapshot;
  private Vector3 lastCentralPosition;
  private Vector3 lastEnvironmentBoundsMin;
  private Vector3 lastEnvironmentBoundsMax;
  private bool lastUseCustomSeed;
  private int lastCustomSeed;
  private float lastUpdateCheckTime;
  private Coroutine debouncedUpdateCoroutine;
  private bool isInitialized = false;

  // - UNITY LIFECYCLE METHODS
  void Start()
  {
    InitializeRandomizer();
    InitializeComponents();
    FindIntegrationComponents();
    SetupCentralObject();
    ValidateEvidenceQuantities();
    CreateConfigurationSnapshot();
    isInitialized = true;

    if (generateOnStart)
    {
      GenerateScene();
    }
  }

  void Update()
  {
    if (!isInitialized)
    {
      InitializeComponents();
      ValidateEvidenceQuantities();
      CreateConfigurationSnapshot();
      isInitialized = true;
    }

    if (visualizationSystem != null)
    {
      visualizationSystem.HandleUpdate();
    }

    if (enableRealTimeUpdates && Application.isPlaying && isInitialized)
    {
      CheckForConfigurationChanges();
    }
  }

  void OnValidate()
  {
    ValidateEvidenceQuantities();

    if (Application.isPlaying && isInitialized && enableRealTimeUpdates)
    {
      CheckForConfigurationChangesImmediate();
    }

    if (visualizationSystem == null)
    {
      InitializeComponents();
    }

    if (visualizationSystem != null)
    {
      visualizationSystem.HandleValidate();
    }
  }

  void OnDrawGizmos()
  {
    if (visualizationSystem == null)
    {
      InitializeComponents();
    }

    if (visualizationSystem != null)
    {
      visualizationSystem.HandleDrawGizmos();
    }
  }

  void OnDrawGizmosSelected()
  {
    if (visualizationSystem == null)
    {
      InitializeComponents();
    }

    if (visualizationSystem != null)
    {
      visualizationSystem.HandleDrawGizmosSelected();
    }
  }

  void OnDestroy()
  {
    if (visualizationSystem != null)
    {
      visualizationSystem.HandleDestroy();
    }

    if (debouncedUpdateCoroutine != null)
    {
      StopCoroutine(debouncedUpdateCoroutine);
    }
  }

  void OnDisable()
  {
    if (visualizationSystem != null)
    {
      visualizationSystem.HandleDisable();
    }

    if (debouncedUpdateCoroutine != null)
    {
      StopCoroutine(debouncedUpdateCoroutine);
    }
  }

  // - EVIDENCE QUANTITY VALIDATION
  private void ValidateEvidenceQuantities()
  {
    if (evidencePrefabs == null) return;

    // Ensure evidence quantities array matches prefabs array
    if (evidenceMaxQuantities == null || evidenceMaxQuantities.Length != evidencePrefabs.Length)
    {
      int[] newQuantities = new int[evidencePrefabs.Length];

      // Copy existing values
      if (evidenceMaxQuantities != null)
      {
        for (int i = 0; i < Mathf.Min(evidenceMaxQuantities.Length, newQuantities.Length); i++)
        {
          newQuantities[i] = evidenceMaxQuantities[i];
        }
      }

      // Set default values for new entries
      for (int i = (evidenceMaxQuantities?.Length ?? 0); i < newQuantities.Length; i++)
      {
        newQuantities[i] = 1;
      }

      evidenceMaxQuantities = newQuantities;
    }

    // Clamp all quantities to valid range
    for (int i = 0; i < evidenceMaxQuantities.Length; i++)
    {
      evidenceMaxQuantities[i] = Mathf.Clamp(evidenceMaxQuantities[i], 0, 30);
    }
  }

  // - REAL-TIME CONFIGURATION MONITORING
  private void CheckForConfigurationChanges()
  {
    if (Time.time - lastUpdateCheckTime < updateCheckInterval)
      return;

    lastUpdateCheckTime = Time.time;

    bool hasChanges = false;

    // Check object type configuration changes
    var configChanges = HasObjectTypeConfigurationsChangedDetailed();
    if (configChanges.hasChanges)
    {
      hasChanges = true;
    }

    // Check central position changes
    Vector3 currentCentralPos = GetCentralPosition();
    if (Vector3.Distance(currentCentralPos, lastCentralPosition) > 0.01f)
    {
      lastCentralPosition = currentCentralPos;
      hasChanges = true;
    }

    // Check environment bounds changes
    if (lastEnvironmentBoundsMin != environmentBoundsMin || lastEnvironmentBoundsMax != environmentBoundsMax)
    {
      lastEnvironmentBoundsMin = environmentBoundsMin;
      lastEnvironmentBoundsMax = environmentBoundsMax;
      hasChanges = true;
    }

    // Check seed changes
    if (lastUseCustomSeed != useCustomSeed || (useCustomSeed && lastCustomSeed != customSeed))
    {
      lastUseCustomSeed = useCustomSeed;
      lastCustomSeed = customSeed;
      hasChanges = true;
    }

    if (hasChanges)
    {
      TriggerDebouncedUpdate();
    }
  }

  private void CheckForConfigurationChangesImmediate()
  {
    bool hasChanges = false;

    var configChanges = HasObjectTypeConfigurationsChangedDetailed();
    if (configChanges.hasChanges)
    {
      hasChanges = true;
    }

    Vector3 currentCentralPos = GetCentralPosition();
    if (Vector3.Distance(currentCentralPos, lastCentralPosition) > 0.01f)
    {
      hasChanges = true;
    }

    if (lastEnvironmentBoundsMin != environmentBoundsMin || lastEnvironmentBoundsMax != environmentBoundsMax)
    {
      hasChanges = true;
    }

    if (lastUseCustomSeed != useCustomSeed || (useCustomSeed && lastCustomSeed != customSeed))
    {
      hasChanges = true;
    }

    if (hasChanges)
    {
      TriggerDebouncedUpdate();
    }
  }

  // - CONFIGURATION CHANGE DETECTION
  private struct ConfigurationChangeResult
  {
    public bool hasChanges;
    public string details;
  }

  private ConfigurationChangeResult HasObjectTypeConfigurationsChangedDetailed()
  {
    var result = new ConfigurationChangeResult();
    result.hasChanges = false;
    result.details = "";

    if (lastObjectTypesSnapshot == null || lastObjectTypesSnapshot.Length != objectTypes.Length)
    {
      result.hasChanges = true;
      return result;
    }

    for (int i = 0; i < objectTypes.Length; i++)
    {
      var changes = CompareObjectTypeConfigDetailed(objectTypes[i], lastObjectTypesSnapshot[i], i);
      if (changes.hasChanges)
      {
        result.hasChanges = true;
        result.details += changes.details;
      }
    }

    return result;
  }

  private ConfigurationChangeResult CompareObjectTypeConfigDetailed(ObjectTypeConfig current, ObjectTypeConfig snapshot, int index)
  {
    var result = new ConfigurationChangeResult();
    result.hasChanges = false;
    result.details = "";

    if (current == null || snapshot == null)
    {
      result.hasChanges = true;
      return result;
    }

    // Check basic properties
    if (current.enableThisType != snapshot.enableThisType ||
        current.minCount != snapshot.minCount ||
        current.maxCount != snapshot.maxCount ||
        Mathf.Abs(current.spawnProbability - snapshot.spawnProbability) > 0.001f ||
        Mathf.Abs(current.minSpawnRadius - snapshot.minSpawnRadius) > 0.001f ||
        Mathf.Abs(current.maxSpawnRadius - snapshot.maxSpawnRadius) > 0.001f ||
        Mathf.Abs(current.spawnLevel - snapshot.spawnLevel) > 0.001f)
    {
      result.hasChanges = true;
    }

    // Check revolution configuration
    var revolutionChanges = CompareRevolutionConfigDetailed(current.revolution, snapshot.revolution, current.typeName);
    if (revolutionChanges.hasChanges)
    {
      result.hasChanges = true;
      result.details += revolutionChanges.details;
    }

    // Check table-specific properties
    if (current.category == ObjectCategory.Tables)
    {
      if (current.useRingSpawning != snapshot.useRingSpawning ||
          Mathf.Abs(current.tableRingRadius - snapshot.tableRingRadius) > 0.001f ||
          current.tablePositionsOnRing != snapshot.tablePositionsOnRing ||
          Mathf.Abs(current.chairRingRadius - snapshot.chairRingRadius) > 0.001f ||
          current.chairsPerTable != snapshot.chairsPerTable ||
          Mathf.Abs(current.cupRingRadius - snapshot.cupRingRadius) > 0.001f ||
          current.cupsPerTable != snapshot.cupsPerTable)
      {
        result.hasChanges = true;
      }
    }

    // Check bounding box properties
    if (Vector3.Distance(current.boundingBoxSize, snapshot.boundingBoxSize) > 0.001f ||
        Mathf.Abs(current.boundingBoxPadding - snapshot.boundingBoxPadding) > 0.001f)
    {
      result.hasChanges = true;
    }

    return result;
  }

  private ConfigurationChangeResult CompareRevolutionConfigDetailed(RevolutionConfig current, RevolutionConfig snapshot, string typeName)
  {
    var result = new ConfigurationChangeResult();
    result.hasChanges = false;
    result.details = "";

    if (current == null || snapshot == null)
    {
      if (current != snapshot)
      {
        result.hasChanges = true;
      }
      return result;
    }

    // Check all revolution configuration properties
    if (current.target != snapshot.target ||
        current.specificObjectType != snapshot.specificObjectType ||
        current.customTargetName != snapshot.customTargetName ||
        Mathf.Abs(current.minimumDistance - snapshot.minimumDistance) > 0.001f ||
        Mathf.Abs(current.maximumDistance - snapshot.maximumDistance) > 0.001f ||
        current.minimumAmount != snapshot.minimumAmount ||
        current.maximumAmount != snapshot.maximumAmount ||
        current.randomPlacement != snapshot.randomPlacement ||
        current.evenDistribution != snapshot.evenDistribution ||
        Mathf.Abs(current.angleVariation - snapshot.angleVariation) > 0.001f ||
        current.rotationDirection != snapshot.rotationDirection ||
        Mathf.Abs(current.rotationOffset - snapshot.rotationOffset) > 0.001f ||
        current.enableCollisionDetection != snapshot.enableCollisionDetection ||
        current.canOverlapWithTarget != snapshot.canOverlapWithTarget)
    {
      result.hasChanges = true;
    }

    return result;
  }

  // - CONFIGURATION SNAPSHOT MANAGEMENT
  private void CreateConfigurationSnapshot()
  {
    lastObjectTypesSnapshot = new ObjectTypeConfig[objectTypes.Length];
    for (int i = 0; i < objectTypes.Length; i++)
    {
      lastObjectTypesSnapshot[i] = CloneObjectTypeConfig(objectTypes[i]);
    }

    lastCentralPosition = GetCentralPosition();
    lastEnvironmentBoundsMin = environmentBoundsMin;
    lastEnvironmentBoundsMax = environmentBoundsMax;
    lastUseCustomSeed = useCustomSeed;
    lastCustomSeed = customSeed;
  }

  private ObjectTypeConfig CloneObjectTypeConfig(ObjectTypeConfig original)
  {
    var cloned = new ObjectTypeConfig()
    {
      typeName = original.typeName,
      category = original.category,
      enableThisType = original.enableThisType,
      prefabs = original.prefabs,
      objectNames = original.objectNames,
      minCount = original.minCount,
      maxCount = original.maxCount,
      spawnProbability = original.spawnProbability,
      minSpawnRadius = original.minSpawnRadius,
      maxSpawnRadius = original.maxSpawnRadius,
      spawnLevel = original.spawnLevel,
      useRingSpawning = original.useRingSpawning,
      tableRingRadius = original.tableRingRadius,
      tablePositionsOnRing = original.tablePositionsOnRing,
      chairRingRadius = original.chairRingRadius,
      chairsPerTable = original.chairsPerTable,
      cupRingRadius = original.cupRingRadius,
      cupsPerTable = original.cupsPerTable,
      boundingBoxSize = original.boundingBoxSize,
      boundingBoxPadding = original.boundingBoxPadding,
      maxSpawnAttempts = original.maxSpawnAttempts,
      canSpawnOnOtherObjects = original.canSpawnOnOtherObjects,
      validSpawnLayers = original.validSpawnLayers
    };

    if (original.revolution != null)
    {
      cloned.revolution = CloneRevolutionConfig(original.revolution);
    }
    else
    {
      cloned.revolution = new RevolutionConfig();
    }

    return cloned;
  }

  private RevolutionConfig CloneRevolutionConfig(RevolutionConfig original)
  {
    return new RevolutionConfig()
    {
      target = original.target,
      specificObjectType = original.specificObjectType,
      customTargetName = original.customTargetName,
      minimumDistance = original.minimumDistance,
      maximumDistance = original.maximumDistance,
      spawnLevel = original.spawnLevel,
      minimumAmount = original.minimumAmount,
      maximumAmount = original.maximumAmount,
      randomPlacement = original.randomPlacement,
      evenDistribution = original.evenDistribution,
      angleVariation = original.angleVariation,
      rotationDirection = original.rotationDirection,
      rotationOffset = original.rotationOffset,
      enableCollisionDetection = original.enableCollisionDetection,
      boundingBoxSize = original.boundingBoxSize,
      boundingBoxPadding = original.boundingBoxPadding,
      maxSpawnAttempts = original.maxSpawnAttempts,
      canOverlapWithTarget = original.canOverlapWithTarget,
      validSpawnLayers = original.validSpawnLayers
    };
  }

  // - DEBOUNCED UPDATE SYSTEM
  private void TriggerDebouncedUpdate()
  {
    if (!debounceUpdates)
    {
      PerformRealTimeUpdate();
      return;
    }

    if (debouncedUpdateCoroutine != null)
    {
      StopCoroutine(debouncedUpdateCoroutine);
    }

    debouncedUpdateCoroutine = StartCoroutine(DebouncedUpdateCoroutine());
  }

  private IEnumerator DebouncedUpdateCoroutine()
  {
    yield return new WaitForSeconds(debounceDelay);
    PerformRealTimeUpdate();
    debouncedUpdateCoroutine = null;
  }

  private void PerformRealTimeUpdate()
  {
    if (!enableRealTimeUpdates || !Application.isPlaying)
      return;

    if (lastUseCustomSeed != useCustomSeed || (useCustomSeed && lastCustomSeed != customSeed))
    {
      InitializeRandomizer();
    }

    SetupCentralObject();
    GenerateScene();
    CreateConfigurationSnapshot();
  }

  // - REAL-TIME UPDATE CONTROL API
  public void ToggleRealTimeUpdates()
  {
    enableRealTimeUpdates = !enableRealTimeUpdates;
    if (enableRealTimeUpdates && Application.isPlaying)
    {
      CreateConfigurationSnapshot();
    }
  }

  public void ForceRealTimeUpdate()
  {
    if (Application.isPlaying)
    {
      PerformRealTimeUpdate();
    }
  }

  public void SetRealTimeUpdatesEnabled(bool enabled)
  {
    enableRealTimeUpdates = enabled;
    if (enabled && Application.isPlaying && isInitialized)
    {
      CreateConfigurationSnapshot();
    }
  }

  public void SetUpdateCheckInterval(float interval)
  {
    updateCheckInterval = Mathf.Max(0.1f, interval);
  }

  public void SetDebounceDelay(float delay)
  {
    debounceDelay = Mathf.Max(0.1f, delay);
  }

  public bool IsRealTimeUpdatesEnabled()
  {
    return enableRealTimeUpdates;
  }

  // - INITIALIZATION
  private void InitializeRandomizer()
  {
    if (useCustomSeed)
    {
      randomizer = new System.Random(customSeed);
    }
    else
    {
      randomizer = new System.Random();
    }
  }

  private void InitializeComponents()
  {
    spawningSystem = new SceneGeneratorSpawning(this);
    hierarchySystem = new SceneGeneratorHierarchy(this);
    despawnSystem = new SceneGeneratorDespawn(this);
    categorySystem = new SceneGeneratorCategories(this);

    if (visualizationSystem == null)
    {
      visualizationSystem = new SceneGeneratorVisualization(this);
    }
    else
    {
      visualizationSystem.SetGenerator(this);
    }

    spawningSystem.SetDependencies(hierarchySystem, despawnSystem);
    visualizationSystem.SetDependencies(spawningSystem, hierarchySystem);
  }

  private void FindIntegrationComponents()
  {
    if (autoFindEvidenceChecklist && evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
    }

    if (autoFindCameraScript && cameraScript == null)
    {
      cameraScript = FindObjectOfType<CameraScript>();
    }
  }

  private void SetupCentralObject()
  {
    if (centralObject == null && autoFindCentralObject)
    {
      FindCentralObject();
    }

    centralPosition = centralObject != null ? centralObject.position : fallbackCentralPosition;
  }

  private void FindCentralObject()
  {
    // Search for objects by exact name match
    foreach (string name in centralObjectNames)
    {
      GameObject found = GameObject.Find(name);
      if (found != null)
      {
        centralObject = found.transform;
        return;
      }
    }

    // Search for objects by partial name match
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      foreach (string name in centralObjectNames)
      {
        if (obj.name.ToLower().Contains(name.ToLower()))
        {
          centralObject = obj.transform;
          return;
        }
      }
    }
  }

  // - SEED MANAGEMENT
  public void SetNewRandomSeed()
  {
    customSeed = System.Environment.TickCount;
    useCustomSeed = true;
    InitializeRandomizer();
  }

  public void SetSeed(int seed)
  {
    customSeed = seed;
    useCustomSeed = true;
    InitializeRandomizer();
  }

  public void RegenerateWithNewSeed()
  {
    SetNewRandomSeed();
    GenerateScene();
  }

  // - SCENE GENERATION
  public void GenerateScene()
  {
    if (randomizer == null)
      InitializeRandomizer();

    lastGenerationFrame = Time.frameCount;

    // Reset fingerprints from previous scene
    DustBrush dustBrush = FindObjectOfType<DustBrush>();
    if (dustBrush != null)
    {
      dustBrush.ResetAllFingerprints();
    }

    // Clear previous scene if requested
    if (clearPreviousScene)
    {
      ClearScene();
    }

    // Reset tracking collections
    currentSceneEvidenceNames.Clear();
    currentSceneEvidenceObjects.Clear();
    allSpawnedObjects.Clear();

    // Setup generation parameters
    SetupCentralObject();
    ValidateEvidenceQuantities();

    // Manage existing objects
    if (despawnSystem != null)
    {
      despawnSystem.ManageExistingObjects();
    }

    // Generate new objects
    if (spawningSystem != null)
    {
      spawningSystem.GenerateObjects();
    }

    // Generate evidence items
    GenerateEvidence();

    // Update configuration snapshot
    if (Application.isPlaying && isInitialized)
    {
      CreateConfigurationSnapshot();
    }

    // Notify other systems
    if (enableRealTimeNotifications)
    {
      StartCoroutine(NotifySystemsAfterGeneration());
    }
  }

  // - EVIDENCE GENERATION WITH QUANTITY SLIDERS
  private void GenerateEvidence()
  {
    if (evidencePrefabs == null || evidencePrefabs.Length == 0)
    {
      return;
    }

    ValidateEvidenceQuantities();
    int totalEvidenceSpawned = 0;

    // Generate evidence for each prefab type
    for (int i = 0; i < evidencePrefabs.Length; i++)
    {
      if (evidencePrefabs[i] == null) continue;
      if (i >= evidenceMaxQuantities.Length) continue;

      int maxQuantity = evidenceMaxQuantities[i];
      if (maxQuantity <= 0) continue;

      // Random quantity up to maximum
      int spawnCount = randomizer.Next(0, maxQuantity + 1);
      if (spawnCount == 0) continue;

      string evidenceName = evidenceNames != null && i < evidenceNames.Length ? evidenceNames[i] : evidencePrefabs[i].name;

      // Spawn individual evidence items
      for (int j = 0; j < spawnCount; j++)
      {
        if (SpawnEvidenceItem(evidencePrefabs[i], evidenceName, j, spawnCount))
        {
          totalEvidenceSpawned++;
        }
      }
    }
  }

  private bool SpawnEvidenceItem(GameObject prefab, string baseName, int instanceIndex, int totalCount)
  {
    for (int attempt = 0; attempt < evidenceConfig.maxSpawnAttempts; attempt++)
    {
      Vector3 spawnPosition = CalculateEvidencePosition();

      if (IsValidEvidencePosition(spawnPosition))
      {
        GameObject spawnedEvidence = Instantiate(prefab, spawnPosition, GetEvidenceRotation());

        // Generate unique name for multiple instances
        string evidenceName = baseName;
        if (totalCount > 1)
          evidenceName += $" ({instanceIndex + 1})";
        spawnedEvidence.name = evidenceName;

        // Create spawn data tracking
        Bounds boundingBox = new Bounds(spawnPosition, evidenceConfig.boundingBoxSize);
        SpawnedObjectData spawnData = new SpawnedObjectData(
          spawnedEvidence, spawnPosition, boundingBox,
          evidenceConfig, -1
        );

        // Add to tracking collections
        allSpawnedObjects.Add(spawnData);
        currentSceneEvidenceObjects.Add(spawnedEvidence);
        currentSceneEvidenceNames.Add(evidenceName);

        return true;
      }
    }

    return false;
  }

  private Vector3 CalculateEvidencePosition()
  {
    Vector3 centralPos = GetCentralPosition();

    // Random distance within spawn radius
    float distance = evidenceConfig.minSpawnRadius +
      ((float)randomizer.NextDouble() * (evidenceConfig.maxSpawnRadius - evidenceConfig.minSpawnRadius));

    // Random angle for circular distribution
    float angle = (float)randomizer.NextDouble() * 360f * Mathf.Deg2Rad;

    Vector3 offset = new Vector3(
      Mathf.Cos(angle) * distance,
      0f,
      Mathf.Sin(angle) * distance
    );

    Vector3 position = centralPos + offset;
    position.y = centralPos.y + evidenceConfig.spawnLevel;

    // Clamp to environment bounds
    Vector3 boundsMin = GetEnvironmentBoundsMin();
    Vector3 boundsMax = GetEnvironmentBoundsMax();

    position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
    position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
    position.z = Mathf.Clamp(position.z, boundsMin.z, boundsMax.z);

    return position;
  }

  private bool IsValidEvidencePosition(Vector3 position)
  {
    Vector3 boundsMin = GetEnvironmentBoundsMin();
    Vector3 boundsMax = GetEnvironmentBoundsMax();

    // Check environment bounds
    if (position.x < boundsMin.x || position.x > boundsMax.x ||
        position.y < boundsMin.y || position.y > boundsMax.y ||
        position.z < boundsMin.z || position.z > boundsMax.z)
    {
      return false;
    }

    // Check collision with existing objects
    Bounds evidenceBounds = new Bounds(position, evidenceConfig.boundingBoxSize);

    foreach (SpawnedObjectData existingObject in allSpawnedObjects)
    {
      if (existingObject.gameObject == null) continue;

      if (evidenceBounds.Intersects(existingObject.boundingBox))
      {
        return false;
      }
    }

    return true;
  }

  private Quaternion GetEvidenceRotation()
  {
    return Quaternion.Euler(0, randomizer.Next(0, 360), 0);
  }

  // - SCENE CLEARING
  public void ClearScene()
  {
    // Destroy all spawned objects
    foreach (SpawnedObjectData spawnData in allSpawnedObjects)
    {
      if (spawnData.gameObject != null)
      {
        if (Application.isPlaying)
          Destroy(spawnData.gameObject);
        else
          DestroyImmediate(spawnData.gameObject);
      }
    }

    // Clear tracking collections
    allSpawnedObjects.Clear();
    currentSceneEvidenceNames.Clear();
    currentSceneEvidenceObjects.Clear();

    // Clear hierarchy system
    if (hierarchySystem != null)
    {
      hierarchySystem.ClearHierarchy();
    }

    // Notify other systems of clearing
    if (enableRealTimeNotifications)
    {
      if (notifyChecklistOnGeneration && evidenceChecklist != null)
      {
        evidenceChecklist.OnSceneGenerated();
      }

      if (notifyCameraOnGeneration && cameraScript != null)
      {
        cameraScript.ClearMarkers();
      }
    }
  }

  // - SYSTEM NOTIFICATION
  private IEnumerator NotifySystemsAfterGeneration()
  {
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(notificationDelay);

    // Notify camera script
    if (notifyCameraOnGeneration && cameraScript != null)
    {
      string[] evidenceNamesArray = currentSceneEvidenceNames.ToArray();
      cameraScript.UpdateEvidenceList(evidenceNamesArray);
      cameraScript.OnSceneGenerated();
    }

    // Notify evidence checklist
    if (notifyChecklistOnGeneration && evidenceChecklist != null)
    {
      evidenceChecklist.OnSceneGenerated();
    }
  }

  // - VISUALIZATION CONTROL METHODS
  public void SetVisualizationSceneIndex(int index)
  {
    if (visualizationSystem != null)
    {
      visualizationSystem.SetCurrentScene(index);
    }
  }

  public string GetCurrentVisualizationSceneName()
  {
    return visualizationSystem?.GetCurrentSceneName() ?? "Unknown";
  }

  public int GetVisualizationSceneCount()
  {
    return visualizationSystem?.GetSceneCount() ?? 0;
  }

  public int GetCurrentVisualizationSceneIndex()
  {
    return visualizationSystem?.GetCurrentSceneIndex() ?? 0;
  }

  public void SetSceneChangeInterval(float interval)
  {
    if (visualizationSystem != null)
    {
      visualizationSystem.SetSceneChangeInterval(interval);
    }
  }

  public void SetManualSceneSelection(bool manual)
  {
    if (visualizationSystem != null)
    {
      visualizationSystem.SetManualSceneSelection(manual);
    }
  }

  // - PUBLIC API METHODS
  // Evidence tracking
  public string[] GetCurrentSceneEvidenceNames()
  {
    return currentSceneEvidenceNames.ToArray();
  }

  public GameObject[] GetCurrentSceneEvidenceObjects()
  {
    return currentSceneEvidenceObjects.ToArray();
  }

  public int GetSpawnedObjectCount() => allSpawnedObjects.Count;
  public int GetCurrentEvidenceCount() => currentSceneEvidenceNames.Count;

  // Position and configuration access
  public Vector3 GetCentralPosition() => centralPosition;
  public System.Random GetRandomizer() => randomizer;
  public ObjectTypeConfig[] GetObjectTypes() => objectTypes;
  public List<SpawnedObjectData> GetAllSpawnedObjects() => allSpawnedObjects;

  public void SetCentralPosition(Vector3 position)
  {
    centralPosition = position;
    fallbackCentralPosition = position;
  }

  public void SetEnvironmentBounds(Vector3 min, Vector3 max)
  {
    environmentBoundsMin = min;
    environmentBoundsMax = max;
  }

  public Vector3 GetEnvironmentBoundsMin() => environmentBoundsMin;
  public Vector3 GetEnvironmentBoundsMax() => environmentBoundsMax;

  // - LEGACY COMPATIBILITY METHODS
  public void GenerateRandomScene()
  {
    GenerateScene();
  }

  public void GenerateRandomLocation()
  {
    GenerateScene();
  }

  public void OnSceneGenerated()
  {
    // Legacy method - functionality moved to notification system
  }

  public string[] GetEvidenceNames()
  {
    return GetCurrentSceneEvidenceNames();
  }

  public GameObject[] GetAllFingerprintsInScene()
  {
    List<GameObject> allFingerprints = new List<GameObject>();

    // Check spawned objects for fingerprints
    foreach (SpawnedObjectData spawnData in allSpawnedObjects)
    {
      if (spawnData.gameObject != null)
      {
        // Check if object itself is a fingerprint
        if (spawnData.gameObject.name.Contains("Fingerprint") || spawnData.gameObject.tag == "Fingerprint")
        {
          allFingerprints.Add(spawnData.gameObject);
        }

        // Check all children for fingerprints
        Transform[] children = spawnData.gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
          if (child != spawnData.gameObject.transform &&
              (child.name.Contains("Fingerprint") || child.tag == "Fingerprint"))
          {
            allFingerprints.Add(child.gameObject);
          }
        }
      }
    }

    return allFingerprints.ToArray();
  }

  public int GetFingerprintCount()
  {
    return GetAllFingerprintsInScene().Length;
  }

  // - SUBSYSTEM ACCESS METHODS
  public SceneGeneratorSpawning GetSpawningSystem() => spawningSystem;
  public SceneGeneratorVisualization GetVisualizationSystem() => visualizationSystem;
  public SceneGeneratorHierarchy GetHierarchySystem() => hierarchySystem;
  public SceneGeneratorDespawn GetDespawnSystem() => despawnSystem;
  public SceneGeneratorCategories GetCategorySystem() => categorySystem;

  // - EVIDENCE TRACKING METHODS
  public void AddEvidenceToScene(GameObject evidenceObject, string evidenceName)
  {
    currentSceneEvidenceObjects.Add(evidenceObject);
    currentSceneEvidenceNames.Add(evidenceName);
  }

  public void AddSpawnedObject(SpawnedObjectData spawnData)
  {
    allSpawnedObjects.Add(spawnData);
  }
}