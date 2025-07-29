using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// - SPAWN LOCATION ENUM
[System.Serializable]
public enum SpawnLocation
{
  Floor,
  Table,
  TableOrFloor,
  Any
}

// - CRIME SCENE GENERATOR MAIN CLASS
public class CrimeSceneGenerator : MonoBehaviour
{
  // - INSPECTOR CONFIGURATION
  [Header("RANDOM SEED SETTINGS")]
  public bool useCustomSeed = false;
  public int customSeed = 12345;
  [Space(10)]

  [Header("Location Probabilities")]
  [Range(0f, 1f)]
  public float barProbability = 0.33f;
  [Range(0f, 1f)]
  public float officeProbability = 0.33f;
  [Range(0f, 1f)]
  public float homeProbability = 0.34f;
  [Space(10)]

  [Header("BAR SCENE CONFIGURATION")]
  [Header("Bar - Main Tables")]
  public GameObject barTable1Object;
  public string barTable1Name = "Bar Counter";
  [Range(0f, 1f)]
  public float barTable1ShowProbability = 0.8f;
  public Transform[] barTable1SpawnPoints;
  public GameObject[] barTable1LinkedObjects;
  [Range(0f, 1f)]
  public float[] barTable1LinkedObjectProbability;

  public GameObject barTable2Object;
  public string barTable2Name = "Bar Table";
  [Range(0f, 1f)]
  public float barTable2ShowProbability = 0.7f;
  public Transform[] barTable2SpawnPoints;
  public GameObject[] barTable2LinkedObjects;
  [Range(0f, 1f)]
  public float[] barTable2LinkedObjectProbability;

  public GameObject barTable3Object;
  public string barTable3Name = "Bar High Table";
  [Range(0f, 1f)]
  public float barTable3ShowProbability = 0.6f;
  public Transform[] barTable3SpawnPoints;
  public GameObject[] barTable3LinkedObjects;
  [Range(0f, 1f)]
  public float[] barTable3LinkedObjectProbability;

  [Header("Bar - Other Objects")]
  public GameObject[] barTableLikeObjects;
  public string[] barTableLikeNames;
  [Range(0f, 1f)]
  public float[] barTableLikeShowProbability;

  public GameObject[] barExistingFurniture;
  public string[] barExistingFurnitureNames;
  [Range(0f, 1f)]
  public float[] barExistingFurnitureShowProbability;

  public GameObject[] barFurnitureObjects;
  public string[] barFurnitureNames;
  [Range(0f, 1f)]
  public float[] barFurnitureShowProbability;

  [Header("Bar - Spawn Points")]
  public Transform[] barFloorSpawnPoints;

  [Header("Bar - Scene Parent")]
  public GameObject barSceneParent;

  [Header("OFFICE SCENE CONFIGURATION")]
  [Header("Office - Main Tables")]
  public GameObject officeTable1Object;
  public string officeTable1Name = "Office Desk 1";
  [Range(0f, 1f)]
  public float officeTable1ShowProbability = 0.8f;
  public Transform[] officeTable1SpawnPoints;
  public GameObject[] officeTable1LinkedObjects;
  [Range(0f, 1f)]
  public float[] officeTable1LinkedObjectProbability;

  public GameObject officeTable2Object;
  public string officeTable2Name = "Office Desk 2";
  [Range(0f, 1f)]
  public float officeTable2ShowProbability = 0.7f;
  public Transform[] officeTable2SpawnPoints;
  public GameObject[] officeTable2LinkedObjects;
  [Range(0f, 1f)]
  public float[] officeTable2LinkedObjectProbability;

  public GameObject officeTable3Object;
  public string officeTable3Name = "Conference Table";
  [Range(0f, 1f)]
  public float officeTable3ShowProbability = 0.6f;
  public Transform[] officeTable3SpawnPoints;
  public GameObject[] officeTable3LinkedObjects;
  [Range(0f, 1f)]
  public float[] officeTable3LinkedObjectProbability;

  [Header("Office - Other Objects")]
  public GameObject[] officeTableLikeObjects;
  public string[] officeTableLikeNames;
  [Range(0f, 1f)]
  public float[] officeTableLikeShowProbability;

  public GameObject[] officeExistingFurniture;
  public string[] officeExistingFurnitureNames;
  [Range(0f, 1f)]
  public float[] officeExistingFurnitureShowProbability;

  public GameObject[] officeFurnitureObjects;
  public string[] officeFurnitureNames;
  [Range(0f, 1f)]
  public float[] officeFurnitureShowProbability;

  [Header("Office - Spawn Points")]
  public Transform[] officeFloorSpawnPoints;

  [Header("Office - Scene Parent")]
  public GameObject officeSceneParent;

  [Header("HOME SCENE CONFIGURATION")]
  [Header("Home - Main Tables")]
  public GameObject homeTable1Object;
  public string homeTable1Name = "Home Coffee Table";
  [Range(0f, 1f)]
  public float homeTable1ShowProbability = 0.7f;
  public Transform[] homeTable1SpawnPoints;
  public GameObject[] homeTable1LinkedObjects;
  [Range(0f, 1f)]
  public float[] homeTable1LinkedObjectProbability;

  public GameObject homeTable2Object;
  public string homeTable2Name = "Home Dining Table";
  [Range(0f, 1f)]
  public float homeTable2ShowProbability = 0.6f;
  public Transform[] homeTable2SpawnPoints;
  public GameObject[] homeTable2LinkedObjects;
  [Range(0f, 1f)]
  public float[] homeTable2LinkedObjectProbability;

  public GameObject homeTable3Object;
  public string homeTable3Name = "Home Side Table";
  [Range(0f, 1f)]
  public float homeTable3ShowProbability = 0.5f;
  public Transform[] homeTable3SpawnPoints;
  public GameObject[] homeTable3LinkedObjects;
  [Range(0f, 1f)]
  public float[] homeTable3LinkedObjectProbability;

  [Header("Home - Other Objects")]
  public GameObject[] homeTableLikeObjects;
  public string[] homeTableLikeNames;
  [Range(0f, 1f)]
  public float[] homeTableLikeShowProbability;

  public GameObject[] homeExistingFurniture;
  public string[] homeExistingFurnitureNames;
  [Range(0f, 1f)]
  public float[] homeExistingFurnitureShowProbability;

  public GameObject[] homeFurnitureObjects;
  public string[] homeFurnitureNames;
  [Range(0f, 1f)]
  public float[] homeFurnitureShowProbability;

  [Header("Home - Spawn Points")]
  public Transform[] homeFloorSpawnPoints;

  [Header("Home - Scene Parent")]
  public GameObject homeSceneParent;

  [Header("Scene Generation Settings")]
  [Range(1, 6)]
  public int minEvidenceItems = 1;
  [Range(1, 6)]
  public int maxEvidenceItems = 6;

  [Header("Evidence Prefabs")]
  public GameObject[] evidencePrefabs;
  public string[] evidenceNames;
  [Range(0f, 1f)]
  public float[] evidenceSpawnProbability;
  public bool[] evidenceIsEssential;

  [Header("Evidence Spawn Rules")]
  public SpawnLocation[] evidenceSpawnLocations;

  [Header("FALLBACK CONFIGURATION")]
  [Header("Used when enableLocationSelection = false")]
  [Header("Main Tables")]
  public GameObject table1Object;
  public string table1Name = "Coffee Table";
  [Range(0f, 1f)]
  public float table1ShowProbability = 0.7f;
  public Transform[] table1SpawnPoints;
  public GameObject[] table1LinkedObjects;
  [Range(0f, 1f)]
  public float[] table1LinkedObjectProbability;

  public GameObject table2Object;
  public string table2Name = "Dining Table";
  [Range(0f, 1f)]
  public float table2ShowProbability = 0.6f;
  public Transform[] table2SpawnPoints;
  public GameObject[] table2LinkedObjects;
  [Range(0f, 1f)]
  public float[] table2LinkedObjectProbability;

  public GameObject table3Object;
  public string table3Name = "Side Table";
  [Range(0f, 1f)]
  public float table3ShowProbability = 0.5f;
  public Transform[] table3SpawnPoints;
  public GameObject[] table3LinkedObjects;
  [Range(0f, 1f)]
  public float[] table3LinkedObjectProbability;

  [Header("Fallback - Other Objects")]
  public GameObject[] tableLikeObjects;
  public string[] tableLikeNames;
  [Range(0f, 1f)]
  public float[] tableLikeShowProbability;

  public GameObject[] existingFurniture;
  public string[] existingFurnitureNames;
  [Range(0f, 1f)]
  public float[] existingFurnitureShowProbability;

  public GameObject[] furnitureObjects;
  public string[] furnitureNames;
  [Range(0f, 1f)]
  public float[] furnitureShowProbability;

  [Header("Static Spawn Points")]
  public Transform[] floorSpawnPoints;

  [Header("Generation Settings")]
  public bool generateOnStart = true;
  public bool clearPreviousScene = true;
  public float tableHeightOffset = 0.0f;

  [Header("Real-Time Integration")]
  public bool enableRealTimeNotifications = true;
  public float notificationDelay = 0.2f;

  [Header("Auto-Generate (Testing)")]
  public bool autoGenerate = false;
  [Range(0.5f, 10f)]
  public float autoGenerateInterval = 1.0f;
  private Coroutine autoGenerateCoroutine;

  [Header("Integration Settings")]
  public EvidenceChecklist evidenceChecklist;
  public bool autoFindEvidenceChecklist = true;
  public bool notifyChecklistOnGeneration = true;

  public CameraScript cameraScript;
  public bool autoFindCameraScript = true;
  public bool notifyCameraOnGeneration = true;

  // - STATE VARIABLES
  // Scene object tracking
  private List<GameObject> spawnedObjects = new List<GameObject>();
  private List<Transform> spawnedTables = new List<Transform>();
  private System.Random randomizer;

  // Location tracking
  private enum CurrentLocation { Bar, Office, Home, Fallback }
  private CurrentLocation currentLocation = CurrentLocation.Fallback;
  private bool isLocationDecided = false;

  // Real-time integration tracking
  private List<string> currentSceneEvidenceNames = new List<string>();
  private List<GameObject> currentSceneEvidenceObjects = new List<GameObject>();
  private int lastGenerationFrame = -1;

  // - UNITY LIFECYCLE METHODS
  void Start()
  {
    InitializeRandomizer();

    CleanAllArrays();
    ValidateArrayLengths();
    NormalizeProbabilities();
    FindIntegrationComponents();

    if (generateOnStart)
    {
      GenerateScene();
    }

    if (autoGenerate)
    {
      StartAutoGeneration();
    }
  }

  void Update()
  {
    if (autoGenerate && autoGenerateCoroutine == null)
    {
      StartAutoGeneration();
    }
    else if (!autoGenerate && autoGenerateCoroutine != null)
    {
      StopAutoGeneration();
    }
  }

  void OnDisable()
  {
    StopAutoGeneration();
  }

  // - RANDOMIZER INITIALIZATION
  private void InitializeRandomizer()
  {
    if (useCustomSeed)
    {
      randomizer = new System.Random(customSeed);
      Debug.Log($"CrimeSceneGenerator initialized with custom seed: {customSeed}");
    }
    else
    {
      randomizer = new System.Random();
      Debug.Log("CrimeSceneGenerator initialized with random seed");
    }
  }

  // - PUBLIC SEED METHODS
  [ContextMenu("Set New Random Seed")]
  public void SetNewRandomSeed()
  {
    customSeed = System.Environment.TickCount;
    useCustomSeed = true;
    InitializeRandomizer();
    Debug.Log($"New random seed set: {customSeed}");
  }

  public void SetSeed(int seed)
  {
    customSeed = seed;
    useCustomSeed = true;
    InitializeRandomizer();
    Debug.Log($"Custom seed set to: {seed}");
  }

  public int GetCurrentSeed()
  {
    return useCustomSeed ? customSeed : -1;
  }

  public void RegenerateWithNewSeed()
  {
    SetNewRandomSeed();
    GenerateScene();
  }

  // - PROBABILITY NORMALIZATION
  void NormalizeProbabilities()
  {
    float total = barProbability + officeProbability + homeProbability;
    if (total > 0)
    {
      barProbability /= total;
      officeProbability /= total;
      homeProbability /= total;
    }
    else
    {
      barProbability = 0.33f;
      officeProbability = 0.33f;
      homeProbability = 0.34f;
    }
  }

  // - LOCATION SELECTION METHODS
  [ContextMenu("Generate Random Location")]
  public void GenerateRandomLocation()
  {
    GenerateScene();
  }

  [ContextMenu("Generate Bar Scene")]
  public void GenerateBarScene()
  {
    currentLocation = CurrentLocation.Bar;
    isLocationDecided = true;
    GenerateScene();
  }

  [ContextMenu("Generate Office Scene")]
  public void GenerateOfficeScene()
  {
    currentLocation = CurrentLocation.Office;
    isLocationDecided = true;
    GenerateScene();
  }

  [ContextMenu("Generate Home Scene")]
  public void GenerateHomeScene()
  {
    currentLocation = CurrentLocation.Home;
    isLocationDecided = true;
    GenerateScene();
  }

  // - SCENE LOCATION DECISION
  private void DecideSceneLocation()
  {
    if (isLocationDecided)
    {
      return; // Location already decided (from context menu methods)
    }

    float randomValue = (float)randomizer.NextDouble();

    if (randomValue < barProbability)
    {
      currentLocation = CurrentLocation.Bar;
    }
    else if (randomValue < barProbability + officeProbability)
    {
      currentLocation = CurrentLocation.Office;
    }
    else
    {
      currentLocation = CurrentLocation.Home;
    }

    isLocationDecided = true;
  }

  // - LOCATION-SPECIFIC OBJECT HANDLING
  private void HandleLocationSpecificObjects()
  {
    if (!isLocationDecided)
    {
      HideAllLocationSpecificObjects();
      ShowHideFallbackTables();
      ShowHideFallbackFurniture();
      return;
    }

    HideAllLocationSpecificObjects();

    switch (currentLocation)
    {
      case CurrentLocation.Bar:
        if (barSceneParent != null) barSceneParent.SetActive(true);
        ShowHideBarObjects();
        break;
      case CurrentLocation.Office:
        if (officeSceneParent != null) officeSceneParent.SetActive(true);
        ShowHideOfficeObjects();
        break;
      case CurrentLocation.Home:
        if (homeSceneParent != null) homeSceneParent.SetActive(true);
        ShowHideHomeObjects();
        break;
      default:
        ShowHideFallbackTables();
        ShowHideFallbackFurniture();
        break;
    }
  }

  // - HIDE ALL LOCATION OBJECTS
  private void HideAllLocationSpecificObjects()
  {
    if (barSceneParent != null) barSceneParent.SetActive(false);
    if (officeSceneParent != null) officeSceneParent.SetActive(false);
    if (homeSceneParent != null) homeSceneParent.SetActive(false);

    HideLocationObjects(barTable1Object, barTable1LinkedObjects);
    HideLocationObjects(barTable2Object, barTable2LinkedObjects);
    HideLocationObjects(barTable3Object, barTable3LinkedObjects);
    HideSimpleObjects(barTableLikeObjects);
    HideSimpleObjects(barExistingFurniture);
    HideSimpleObjects(barFurnitureObjects);

    HideLocationObjects(officeTable1Object, officeTable1LinkedObjects);
    HideLocationObjects(officeTable2Object, officeTable2LinkedObjects);
    HideLocationObjects(officeTable3Object, officeTable3LinkedObjects);
    HideSimpleObjects(officeTableLikeObjects);
    HideSimpleObjects(officeExistingFurniture);
    HideSimpleObjects(officeFurnitureObjects);

    HideLocationObjects(homeTable1Object, homeTable1LinkedObjects);
    HideLocationObjects(homeTable2Object, homeTable2LinkedObjects);
    HideLocationObjects(homeTable3Object, homeTable3LinkedObjects);
    HideSimpleObjects(homeTableLikeObjects);
    HideSimpleObjects(homeExistingFurniture);
    HideSimpleObjects(homeFurnitureObjects);

    // Always hide fallback objects since we're using location-specific scenes
    HideLocationObjects(table1Object, table1LinkedObjects);
    HideLocationObjects(table2Object, table2LinkedObjects);
    HideLocationObjects(table3Object, table3LinkedObjects);
    HideSimpleObjects(tableLikeObjects);
    HideSimpleObjects(existingFurniture);
    HideSimpleObjects(furnitureObjects);
  }

  // - OBJECT HIDING HELPERS
  private void HideLocationObjects(GameObject mainObject, GameObject[] linkedObjects)
  {
    if (mainObject != null)
      mainObject.SetActive(false);

    HideLinkedObjects(linkedObjects);
  }

  private void HideSimpleObjects(GameObject[] objects)
  {
    if (objects != null)
    {
      foreach (GameObject obj in objects)
      {
        if (obj != null)
          obj.SetActive(false);
      }
    }
  }

  // - BAR SCENE SETUP
  private void ShowHideBarObjects()
  {
    if (spawnedTables == null)
      spawnedTables = new List<Transform>();
    else
      spawnedTables.Clear();

    ProcessLocationTable(barTable1Object, barTable1Name, barTable1ShowProbability,
                        barTable1SpawnPoints, barTable1LinkedObjects, barTable1LinkedObjectProbability);
    ProcessLocationTable(barTable2Object, barTable2Name, barTable2ShowProbability,
                        barTable2SpawnPoints, barTable2LinkedObjects, barTable2LinkedObjectProbability);
    ProcessLocationTable(barTable3Object, barTable3Name, barTable3ShowProbability,
                        barTable3SpawnPoints, barTable3LinkedObjects, barTable3LinkedObjectProbability);

    ProcessSimpleObjects(barTableLikeObjects, barTableLikeNames, barTableLikeShowProbability);
    ProcessSimpleObjects(barExistingFurniture, barExistingFurnitureNames, barExistingFurnitureShowProbability);
    ProcessSimpleObjects(barFurnitureObjects, barFurnitureNames, barFurnitureShowProbability);
  }

  // - OFFICE SCENE SETUP
  private void ShowHideOfficeObjects()
  {
    if (spawnedTables == null)
      spawnedTables = new List<Transform>();
    else
      spawnedTables.Clear();

    ProcessLocationTable(officeTable1Object, officeTable1Name, officeTable1ShowProbability,
                        officeTable1SpawnPoints, officeTable1LinkedObjects, officeTable1LinkedObjectProbability);
    ProcessLocationTable(officeTable2Object, officeTable2Name, officeTable2ShowProbability,
                        officeTable2SpawnPoints, officeTable2LinkedObjects, officeTable2LinkedObjectProbability);
    ProcessLocationTable(officeTable3Object, officeTable3Name, officeTable3ShowProbability,
                        officeTable3SpawnPoints, officeTable3LinkedObjects, officeTable3LinkedObjectProbability);

    ProcessSimpleObjects(officeTableLikeObjects, officeTableLikeNames, officeTableLikeShowProbability);
    ProcessSimpleObjects(officeExistingFurniture, officeExistingFurnitureNames, officeExistingFurnitureShowProbability);
    ProcessSimpleObjects(officeFurnitureObjects, officeFurnitureNames, officeFurnitureShowProbability);
  }

  // - HOME SCENE SETUP
  private void ShowHideHomeObjects()
  {
    if (spawnedTables == null)
      spawnedTables = new List<Transform>();
    else
      spawnedTables.Clear();

    ProcessLocationTable(homeTable1Object, homeTable1Name, homeTable1ShowProbability,
                        homeTable1SpawnPoints, homeTable1LinkedObjects, homeTable1LinkedObjectProbability);
    ProcessLocationTable(homeTable2Object, homeTable2Name, homeTable2ShowProbability,
                        homeTable2SpawnPoints, homeTable2LinkedObjects, homeTable2LinkedObjectProbability);
    ProcessLocationTable(homeTable3Object, homeTable3Name, homeTable3ShowProbability,
                        homeTable3SpawnPoints, homeTable3LinkedObjects, homeTable3LinkedObjectProbability);

    ProcessSimpleObjects(homeTableLikeObjects, homeTableLikeNames, homeTableLikeShowProbability);
    ProcessSimpleObjects(homeExistingFurniture, homeExistingFurnitureNames, homeExistingFurnitureShowProbability);
    ProcessSimpleObjects(homeFurnitureObjects, homeFurnitureNames, homeFurnitureShowProbability);
  }

  // - TABLE PROCESSING
  private void ProcessLocationTable(GameObject tableObject, string tableName, float showProbability,
                                   Transform[] spawnPoints, GameObject[] linkedObjects, float[] linkedObjectProbability)
  {
    if (tableObject == null) return;

    bool shouldShow = randomizer.NextDouble() < showProbability;
    tableObject.SetActive(shouldShow);

    if (shouldShow)
    {
      if (linkedObjects != null)
      {
        for (int i = 0; i < linkedObjects.Length; i++)
        {
          if (linkedObjects[i] != null)
          {
            float probability = (linkedObjectProbability != null && i < linkedObjectProbability.Length) ? linkedObjectProbability[i] : 0.7f;
            bool shouldShowLinked = randomizer.NextDouble() < probability;
            linkedObjects[i].SetActive(shouldShowLinked);
          }
        }
      }

      if (spawnPoints != null)
      {
        spawnedTables.AddRange(spawnPoints);
      }
    }
    else
    {
      HideLinkedObjects(linkedObjects);
    }
  }

  // - SIMPLE OBJECT PROCESSING
  private void ProcessSimpleObjects(GameObject[] objects, string[] names, float[] showProbability)
  {
    if (objects == null) return;

    for (int i = 0; i < objects.Length; i++)
    {
      if (objects[i] == null) continue;

      bool shouldShow = randomizer.NextDouble() < (showProbability != null && i < showProbability.Length ? showProbability[i] : 0.7f);
      objects[i].SetActive(shouldShow);
    }
  }

  // - LINKED OBJECT HIDING
  private void HideLinkedObjects(GameObject[] linkedObjects)
  {
    if (linkedObjects != null)
    {
      foreach (GameObject linkedObj in linkedObjects)
      {
        if (linkedObj != null)
          linkedObj.SetActive(false);
      }
    }
  }

  // - SPAWN POINT RETRIEVAL
  Transform[] GetLocationSpecificSpawnPoints(SpawnLocation location)
  {
    List<Transform> validPoints = new List<Transform>();

    if (isLocationDecided && currentLocation != CurrentLocation.Fallback)
    {
      Transform[] floorPoints = null;

      if (currentLocation == CurrentLocation.Bar)
      {
        floorPoints = barFloorSpawnPoints;
      }
      else if (currentLocation == CurrentLocation.Office)
      {
        floorPoints = officeFloorSpawnPoints;
      }
      else if (currentLocation == CurrentLocation.Home)
      {
        floorPoints = homeFloorSpawnPoints;
      }

      switch (location)
      {
        case SpawnLocation.Floor:
          if (floorPoints != null && floorPoints.Length > 0)
            validPoints.AddRange(floorPoints);
          break;
        case SpawnLocation.Table:
          if (spawnedTables != null && spawnedTables.Count > 0)
            validPoints.AddRange(spawnedTables);
          break;
        case SpawnLocation.TableOrFloor:
          if (spawnedTables != null && spawnedTables.Count > 0)
            validPoints.AddRange(spawnedTables);
          if (floorPoints != null && floorPoints.Length > 0)
            validPoints.AddRange(floorPoints);
          break;
        case SpawnLocation.Any:
          if (spawnedTables != null && spawnedTables.Count > 0)
            validPoints.AddRange(spawnedTables);
          if (floorPoints != null && floorPoints.Length > 0)
            validPoints.AddRange(floorPoints);
          break;
      }
    }

    // Filter out inactive spawn points
    List<Transform> activeValidPoints = new List<Transform>();
    foreach (Transform point in validPoints)
    {
      if (point != null && point.gameObject.activeInHierarchy)
      {
        activeValidPoints.Add(point);
      }
    }

    return activeValidPoints.ToArray();
  }

  // - PUBLIC API METHODS
  public string GetCurrentLocationName()
  {
    switch (currentLocation)
    {
      case CurrentLocation.Bar: return "Bar";
      case CurrentLocation.Office: return "Office";
      case CurrentLocation.Home: return "Home";
      default: return "Fallback";
    }
  }

  public int GetCurrentLocationIndex()
  {
    switch (currentLocation)
    {
      case CurrentLocation.Bar: return 0;
      case CurrentLocation.Office: return 1;
      case CurrentLocation.Home: return 2;
      default: return -1;
    }
  }

  // - PROBABILITY SETTERS
  public void SetBarProbability(float probability)
  {
    barProbability = Mathf.Clamp01(probability);
    float remaining = 1f - barProbability;
    officeProbability = remaining * 0.5f;
    homeProbability = remaining * 0.5f;
  }

  public void SetOfficeProbability(float probability)
  {
    officeProbability = Mathf.Clamp01(probability);
    float remaining = 1f - officeProbability;
    barProbability = remaining * 0.5f;
    homeProbability = remaining * 0.5f;
  }

  public void SetHomeProbability(float probability)
  {
    homeProbability = Mathf.Clamp01(probability);
    float remaining = 1f - homeProbability;
    barProbability = remaining * 0.5f;
    officeProbability = remaining * 0.5f;
  }

  public void SetLocationProbabilities(float bar, float office, float home)
  {
    barProbability = bar;
    officeProbability = office;
    homeProbability = home;
    NormalizeProbabilities();
  }

  // - EVIDENCE PREFAB VISIBILITY MANAGEMENT
  private void HideAllEvidencePrefabs()
  {
    if (evidencePrefabs == null) return;

    foreach (GameObject evidencePrefab in evidencePrefabs)
    {
      if (evidencePrefab != null)
      {
        evidencePrefab.SetActive(false);
      }
    }
  }

  private void ShowAllEvidencePrefabs()
  {
    if (evidencePrefabs == null) return;

    foreach (GameObject evidencePrefab in evidencePrefabs)
    {
      if (evidencePrefab != null)
      {
        evidencePrefab.SetActive(true);
      }
    }
  }

  // - SCENE GENERATION
  [ContextMenu("GENERATE AGAIN")]
  public void GenerateAgain()
  {
    if (randomizer == null)
      InitializeRandomizer();
    GenerateScene();
  }

  [ContextMenu("Generate New Scene")]
  public void GenerateScene()
  {
    if (randomizer == null)
      InitializeRandomizer();

    // Reset fingerprints from previous scene
    DustBrush dustBrush = FindObjectOfType<DustBrush>();
    if (dustBrush != null)
    {
      dustBrush.ResetAllFingerprints();
    }

    lastGenerationFrame = Time.frameCount;

    // Reset location decision for fresh random selection
    isLocationDecided = false;

    // Determine scene location
    DecideSceneLocation();

    // Clear previous scene objects
    if (clearPreviousScene)
    {
      ClearScene();
    }

    // Hide all evidence prefabs to prevent duplicates
    HideAllEvidencePrefabs();

    // Reset evidence tracking
    currentSceneEvidenceNames.Clear();
    currentSceneEvidenceObjects.Clear();

    // Show/hide furniture based on location
    HandleLocationSpecificObjects();

    // Generate evidence randomly
    GenerateRandomScene();

    // Notify other systems of scene generation
    if (enableRealTimeNotifications)
    {
      StartCoroutine(NotifySystemsAfterGeneration());
    }
  }

  // - FALLBACK METHODS
  void ShowHideFallbackTables()
  {
    if (spawnedTables == null)
      spawnedTables = new List<Transform>();
    else
      spawnedTables.Clear();

    ProcessFallbackTable(table1Object, table1Name, table1ShowProbability, table1SpawnPoints, table1LinkedObjects, table1LinkedObjectProbability);
    ProcessFallbackTable(table2Object, table2Name, table2ShowProbability, table2SpawnPoints, table2LinkedObjects, table2LinkedObjectProbability);
    ProcessFallbackTable(table3Object, table3Name, table3ShowProbability, table3SpawnPoints, table3LinkedObjects, table3LinkedObjectProbability);
  }

  void ShowHideFallbackFurniture()
  {
    ProcessSimpleObjects(tableLikeObjects, tableLikeNames, tableLikeShowProbability);
    ProcessSimpleObjects(existingFurniture, existingFurnitureNames, existingFurnitureShowProbability);
    ProcessSimpleObjects(furnitureObjects, furnitureNames, furnitureShowProbability);
  }

  void ProcessFallbackTable(GameObject tableObject, string tableName, float showProbability, Transform[] spawnPoints, GameObject[] linkedObjects, float[] linkedObjectProbability)
  {
    if (tableObject == null) return;

    bool shouldShow = randomizer.NextDouble() < showProbability;
    tableObject.SetActive(shouldShow);

    if (shouldShow)
    {
      if (linkedObjects != null)
      {
        for (int i = 0; i < linkedObjects.Length; i++)
        {
          if (linkedObjects[i] != null)
          {
            float probability = (linkedObjectProbability != null && i < linkedObjectProbability.Length) ? linkedObjectProbability[i] : 0.7f;
            bool shouldShowLinked = randomizer.NextDouble() < probability;
            linkedObjects[i].SetActive(shouldShowLinked);
          }
        }
      }

      if (spawnPoints != null)
      {
        spawnedTables.AddRange(spawnPoints);
      }
    }
    else
    {
      if (linkedObjects != null)
      {
        foreach (GameObject linkedObj in linkedObjects)
        {
          if (linkedObj != null)
            linkedObj.SetActive(false);
        }
      }
    }
  }

  // - COMPONENT INTEGRATION
  void FindIntegrationComponents()
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

  // - AUTO-GENERATION SYSTEM
  void StartAutoGeneration()
  {
    if (autoGenerateCoroutine == null && Application.isPlaying)
    {
      autoGenerateCoroutine = StartCoroutine(AutoGenerateCoroutine());
    }
  }

  void StopAutoGeneration()
  {
    if (autoGenerateCoroutine != null)
    {
      StopCoroutine(autoGenerateCoroutine);
      autoGenerateCoroutine = null;
    }
  }

  IEnumerator AutoGenerateCoroutine()
  {
    while (autoGenerate)
    {
      yield return new WaitForSeconds(autoGenerateInterval);

      if (autoGenerate)
      {
        GenerateScene();
      }
    }
    autoGenerateCoroutine = null;
  }

  // - ARRAY VALIDATION AND CLEANUP
  void ValidateArrayLengths()
  {
    int evidenceCount = evidencePrefabs.Length;

    if (evidenceNames.Length != evidenceCount)
      System.Array.Resize(ref evidenceNames, evidenceCount);
    if (evidenceSpawnProbability.Length != evidenceCount)
      System.Array.Resize(ref evidenceSpawnProbability, evidenceCount);
    if (evidenceIsEssential.Length != evidenceCount)
      System.Array.Resize(ref evidenceIsEssential, evidenceCount);
    if (evidenceSpawnLocations.Length != evidenceCount)
      System.Array.Resize(ref evidenceSpawnLocations, evidenceCount);

    CleanupEvidenceArrays();

    for (int i = 0; i < evidenceCount; i++)
    {
      if (string.IsNullOrEmpty(evidenceNames[i]))
        evidenceNames[i] = evidencePrefabs[i] != null ? evidencePrefabs[i].name : $"Evidence_{i}";
      if (evidenceSpawnProbability[i] == 0)
        evidenceSpawnProbability[i] = 0.5f;
    }

    ValidateSpawnPointsConfiguration();
  }

  private void CleanupEvidenceArrays()
  {
    var evidenceList = new List<GameObject>();
    var namesList = new List<string>();
    var probabilityList = new List<float>();
    var essentialList = new List<bool>();
    var locationsList = new List<SpawnLocation>();

    for (int i = 0; i < evidencePrefabs.Length; i++)
    {
      if (evidencePrefabs[i] != null)
      {
        evidenceList.Add(evidencePrefabs[i]);
        namesList.Add(i < evidenceNames.Length ? evidenceNames[i] : evidencePrefabs[i].name);
        probabilityList.Add(i < evidenceSpawnProbability.Length ? evidenceSpawnProbability[i] : 0.5f);
        essentialList.Add(i < evidenceIsEssential.Length ? evidenceIsEssential[i] : false);
        locationsList.Add(i < evidenceSpawnLocations.Length ? evidenceSpawnLocations[i] : SpawnLocation.Floor);
      }
    }

    evidencePrefabs = evidenceList.ToArray();
    evidenceNames = namesList.ToArray();
    evidenceSpawnProbability = probabilityList.ToArray();
    evidenceIsEssential = essentialList.ToArray();
    evidenceSpawnLocations = locationsList.ToArray();
  }

  [ContextMenu("Clean All Arrays")]
  private void CleanAllArrays()
  {
    CleanupEvidenceArrays();
    CleanupSpawnPointArrays();
    CleanupFurnitureArrays();
  }

  private void CleanupSpawnPointArrays()
  {
    barFloorSpawnPoints = CleanTransformArray(barFloorSpawnPoints, "Bar Floor Spawn Points");
    officeFloorSpawnPoints = CleanTransformArray(officeFloorSpawnPoints, "Office Floor Spawn Points");
    homeFloorSpawnPoints = CleanTransformArray(homeFloorSpawnPoints, "Home Floor Spawn Points");
    floorSpawnPoints = CleanTransformArray(floorSpawnPoints, "Fallback Floor Spawn Points");
  }

  private void CleanupFurnitureArrays()
  {
    barTable1LinkedObjects = CleanGameObjectArray(barTable1LinkedObjects, "Bar Table 1 Linked Objects");
    barTable2LinkedObjects = CleanGameObjectArray(barTable2LinkedObjects, "Bar Table 2 Linked Objects");
    barTable3LinkedObjects = CleanGameObjectArray(barTable3LinkedObjects, "Bar Table 3 Linked Objects");
    barTableLikeObjects = CleanGameObjectArray(barTableLikeObjects, "Bar Table-Like Objects");
    barExistingFurniture = CleanGameObjectArray(barExistingFurniture, "Bar Existing Furniture");
    barFurnitureObjects = CleanGameObjectArray(barFurnitureObjects, "Bar Furniture Objects");

    officeTable1LinkedObjects = CleanGameObjectArray(officeTable1LinkedObjects, "Office Table 1 Linked Objects");
    officeTable2LinkedObjects = CleanGameObjectArray(officeTable2LinkedObjects, "Office Table 2 Linked Objects");
    officeTable3LinkedObjects = CleanGameObjectArray(officeTable3LinkedObjects, "Office Table 3 Linked Objects");
    officeTableLikeObjects = CleanGameObjectArray(officeTableLikeObjects, "Office Table-Like Objects");
    officeExistingFurniture = CleanGameObjectArray(officeExistingFurniture, "Office Existing Furniture");
    officeFurnitureObjects = CleanGameObjectArray(officeFurnitureObjects, "Office Furniture Objects");

    homeTable1LinkedObjects = CleanGameObjectArray(homeTable1LinkedObjects, "Home Table 1 Linked Objects");
    homeTable2LinkedObjects = CleanGameObjectArray(homeTable2LinkedObjects, "Home Table 2 Linked Objects");
    homeTable3LinkedObjects = CleanGameObjectArray(homeTable3LinkedObjects, "Home Table 3 Linked Objects");
    homeTableLikeObjects = CleanGameObjectArray(homeTableLikeObjects, "Home Table-Like Objects");
    homeExistingFurniture = CleanGameObjectArray(homeExistingFurniture, "Home Existing Furniture");
    homeFurnitureObjects = CleanGameObjectArray(homeFurnitureObjects, "Home Furniture Objects");

    table1LinkedObjects = CleanGameObjectArray(table1LinkedObjects, "Fallback Table 1 Linked Objects");
    table2LinkedObjects = CleanGameObjectArray(table2LinkedObjects, "Fallback Table 2 Linked Objects");
    table3LinkedObjects = CleanGameObjectArray(table3LinkedObjects, "Fallback Table 3 Linked Objects");
    tableLikeObjects = CleanGameObjectArray(tableLikeObjects, "Fallback Table-Like Objects");
    existingFurniture = CleanGameObjectArray(existingFurniture, "Fallback Existing Furniture");
    furnitureObjects = CleanGameObjectArray(furnitureObjects, "Fallback Furniture Objects");
  }

  private Transform[] CleanTransformArray(Transform[] array, string arrayName)
  {
    if (array == null) return new Transform[0];

    var cleanList = new List<Transform>();

    foreach (Transform t in array)
    {
      if (t != null)
      {
        cleanList.Add(t);
      }
    }

    return cleanList.ToArray();
  }

  private GameObject[] CleanGameObjectArray(GameObject[] array, string arrayName)
  {
    if (array == null) return new GameObject[0];

    var cleanList = new List<GameObject>();

    foreach (GameObject obj in array)
    {
      if (obj != null)
      {
        cleanList.Add(obj);
      }
    }

    return cleanList.ToArray();
  }

  private void ValidateSpawnPointsConfiguration()
  {
    bool hasIssues = false;

    if (currentLocation == CurrentLocation.Bar || !isLocationDecided)
    {
      if (barFloorSpawnPoints == null || barFloorSpawnPoints.Length == 0)
      {
        hasIssues = true;
      }
    }

    if (currentLocation == CurrentLocation.Office || !isLocationDecided)
    {
      if (officeFloorSpawnPoints == null || officeFloorSpawnPoints.Length == 0)
      {
        hasIssues = true;
      }
    }

    if (currentLocation == CurrentLocation.Home || !isLocationDecided)
    {
      if (homeFloorSpawnPoints == null || homeFloorSpawnPoints.Length == 0)
      {
        hasIssues = true;
      }
    }

    if (floorSpawnPoints == null || floorSpawnPoints.Length == 0)
    {
      hasIssues = true;
    }
  }

  // - SYSTEM NOTIFICATION
  private IEnumerator NotifySystemsAfterGeneration()
  {
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(notificationDelay);

    if (notifyCameraOnGeneration && cameraScript != null)
    {
      string[] evidenceNamesArray = currentSceneEvidenceNames.ToArray();
      cameraScript.UpdateEvidenceList(evidenceNamesArray);
      cameraScript.OnSceneGenerated();
    }

    if (notifyChecklistOnGeneration && evidenceChecklist != null)
    {
      evidenceChecklist.OnSceneGenerated();
    }
  }

  // - RANDOM GENERATION
  void GenerateRandomScene()
  {
    int evidenceCount = randomizer.Next(minEvidenceItems, maxEvidenceItems + 1);

    List<int> availableIndices = new List<int>();
    for (int i = 0; i < evidencePrefabs.Length; i++)
    {
      if (evidencePrefabs[i] != null)
        availableIndices.Add(i);
    }

    // Shuffle available indices
    for (int i = 0; i < availableIndices.Count; i++)
    {
      int temp = availableIndices[i];
      int randomIndex = randomizer.Next(i, availableIndices.Count);
      availableIndices[i] = availableIndices[randomIndex];
      availableIndices[randomIndex] = temp;
    }

    int spawnedCount = 0;
    foreach (int evidenceIndex in availableIndices)
    {
      if (spawnedCount >= evidenceCount) break;

      bool shouldSpawn = evidenceIsEssential[evidenceIndex] ||
                        randomizer.NextDouble() < evidenceSpawnProbability[evidenceIndex];

      if (shouldSpawn)
      {
        SpawnEvidence(evidenceIndex);
        spawnedCount++;
      }
    }
  }

  // - EVIDENCE SPAWNING
  void SpawnEvidence(int evidenceIndex)
  {
    if (evidenceIndex >= evidencePrefabs.Length || evidencePrefabs[evidenceIndex] == null)
      return;

    Transform[] spawnPoints = GetAppropriateSpawnPoints(evidenceIndex);

    Vector3 targetPosition;
    Transform selectedSpawnPoint = null;

    if (spawnPoints.Length == 0)
    {
      return;
    }

    // Select a spawn point
    selectedSpawnPoint = spawnPoints[randomizer.Next(spawnPoints.Length)];

    if (selectedSpawnPoint == null)
    {
      return;
    }

    // Use exact spawn point position
    targetPosition = selectedSpawnPoint.position;

    // Instantiate evidence prefab
    GameObject spawnedEvidence = Instantiate(evidencePrefabs[evidenceIndex], Vector3.zero, Quaternion.identity);

    // Ensure the spawned evidence is active
    spawnedEvidence.SetActive(true);

    // Position the evidence exactly at the spawn point
    PositionEvidenceUsingOrigin(spawnedEvidence, targetPosition);

    // Rotate evidence randomly on Y-axis
    spawnedEvidence.transform.Rotate(0, randomizer.Next(0, 360), 0);

    spawnedObjects.Add(spawnedEvidence);

    if (!spawnedEvidence.CompareTag("Evidence"))
    {
      spawnedEvidence.tag = "Evidence";
    }

    string evidenceName = evidenceNames[evidenceIndex];
    currentSceneEvidenceNames.Add(evidenceName);
    currentSceneEvidenceObjects.Add(spawnedEvidence);
  }

  // - EVIDENCE POSITIONING
  void PositionEvidenceUsingOrigin(GameObject evidenceObject, Vector3 targetPosition)
  {
    if (evidenceObject == null) return;

    evidenceObject.transform.position = targetPosition;
  }

  // - SPAWN POINT SELECTION
  Transform[] GetAppropriateSpawnPoints(int evidenceIndex)
  {
    List<Transform> validPoints = new List<Transform>();
    SpawnLocation location = evidenceSpawnLocations[evidenceIndex];

    if (isLocationDecided && currentLocation != CurrentLocation.Fallback)
    {
      Transform[] locationSpecificPoints = GetLocationSpecificSpawnPoints(location);
      if (locationSpecificPoints.Length > 0)
      {
        validPoints.AddRange(locationSpecificPoints);
      }
      else
      {
        switch (location)
        {
          case SpawnLocation.Floor:
            if (floorSpawnPoints != null && floorSpawnPoints.Length > 0)
              validPoints.AddRange(floorSpawnPoints);
            break;
          case SpawnLocation.Table:
            if (spawnedTables != null && spawnedTables.Count > 0)
              validPoints.AddRange(spawnedTables);
            break;
          case SpawnLocation.TableOrFloor:
            if (spawnedTables != null && spawnedTables.Count > 0)
              validPoints.AddRange(spawnedTables);
            if (floorSpawnPoints != null && floorSpawnPoints.Length > 0)
              validPoints.AddRange(floorSpawnPoints);
            break;
          case SpawnLocation.Any:
            if (spawnedTables != null && spawnedTables.Count > 0)
              validPoints.AddRange(spawnedTables);
            if (floorSpawnPoints != null && floorSpawnPoints.Length > 0)
              validPoints.AddRange(floorSpawnPoints);
            break;
        }
      }
    }
    else
    {
      switch (location)
      {
        case SpawnLocation.Floor:
          if (floorSpawnPoints != null) validPoints.AddRange(floorSpawnPoints);
          break;
        case SpawnLocation.Table:
          validPoints.AddRange(spawnedTables);
          break;
        case SpawnLocation.TableOrFloor:
          validPoints.AddRange(spawnedTables);
          if (floorSpawnPoints != null) validPoints.AddRange(floorSpawnPoints);
          break;
        case SpawnLocation.Any:
          validPoints.AddRange(spawnedTables);
          if (floorSpawnPoints != null) validPoints.AddRange(floorSpawnPoints);
          break;
      }
    }

    // Filter out inactive spawn points
    List<Transform> activeValidPoints = new List<Transform>();
    foreach (Transform point in validPoints)
    {
      if (point != null && point.gameObject.activeInHierarchy)
      {
        activeValidPoints.Add(point);
      }
    }

    return activeValidPoints.ToArray();
  }

  // - SCENE CLEARING
  [ContextMenu("Clear Scene")]
  public void ClearScene()
  {
    // Reset evidence tracking
    currentSceneEvidenceNames.Clear();
    currentSceneEvidenceObjects.Clear();

    // Destroy all spawned objects
    if (spawnedObjects != null)
    {
      foreach (GameObject obj in spawnedObjects)
      {
        if (obj != null)
        {
          if (Application.isPlaying)
            Destroy(obj);
          else
            DestroyImmediate(obj);
        }
      }
      spawnedObjects.Clear();
    }

    // Clear table tracking
    if (spawnedTables != null)
      spawnedTables.Clear();

    // Show all evidence prefabs again when clearing
    ShowAllEvidencePrefabs();

    // Notify integrated systems
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

  // - PUBLIC API GETTERS
  public string[] GetEvidenceNames()
  {
    if (evidenceNames == null) return new string[0];

    List<string> activeEvidenceNames = new List<string>();
    for (int i = 0; i < evidenceNames.Length; i++)
    {
      if (!string.IsNullOrEmpty(evidenceNames[i]))
      {
        activeEvidenceNames.Add(evidenceNames[i]);
      }
    }
    return activeEvidenceNames.ToArray();
  }

  public string[] GetCurrentSceneEvidenceNames()
  {
    return currentSceneEvidenceNames.ToArray();
  }

  public GameObject[] GetCurrentSceneEvidenceObjects()
  {
    return currentSceneEvidenceObjects.ToArray();
  }

  public int GetSpawnedObjectCount() => spawnedObjects.Count;
  public int GetCurrentEvidenceCount() => currentSceneEvidenceNames.Count;

  public GameObject[] GetAllFingerprintsInScene()
  {
    List<GameObject> allFingerprints = new List<GameObject>();

    // Check spawned evidence objects for fingerprints
    foreach (GameObject spawnedObj in spawnedObjects)
    {
      if (spawnedObj != null)
      {
        // Check if the object itself is a fingerprint
        if (spawnedObj.name.Contains("Fingerprint") || spawnedObj.tag == "Fingerprint")
        {
          allFingerprints.Add(spawnedObj);
        }

        // Check all children for fingerprints
        Transform[] children = spawnedObj.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
          if (child != spawnedObj.transform &&
              (child.name.Contains("Fingerprint") || child.tag == "Fingerprint"))
          {
            allFingerprints.Add(child.gameObject);
          }
        }
      }
    }

    // Also scan scene for any other fingerprints
    GameObject[] allSceneObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allSceneObjects)
    {
      if (obj.activeInHierarchy &&
          (obj.name.Contains("Fingerprint") || obj.tag == "Fingerprint") &&
          !allFingerprints.Contains(obj))
      {
        allFingerprints.Add(obj);
      }
    }

    return allFingerprints.ToArray();
  }

  public int GetFingerprintCount()
  {
    return GetAllFingerprintsInScene().Length;
  }

  // - CONTEXT MENU TESTING METHODS
  [ContextMenu("Hide All Evidence Prefabs")]
  public void HideAllEvidencePrefabsMenu()
  {
    HideAllEvidencePrefabs();
  }

  [ContextMenu("Show All Evidence Prefabs")]
  public void ShowAllEvidencePrefabsMenu()
  {
    ShowAllEvidencePrefabs();
  }
}