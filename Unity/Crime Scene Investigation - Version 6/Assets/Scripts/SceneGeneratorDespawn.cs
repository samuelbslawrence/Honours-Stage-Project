using UnityEngine;
using System.Collections.Generic;

// - SCENE GENERATOR DESPAWN MANAGEMENT SYSTEM
public class SceneGeneratorDespawn
{
  // - DESPAWN SETTINGS
  public bool despawnUnselectedObjects = true;
  public bool hideInsteadOfDestroy = true;
  public string[] objectTagsToManage = { "Table", "Chair", "Bottle", "Glass", "Furniture", "BarObject", "Decoration" };
  public string[] objectNamesToManage = { "Table", "Chair", "Bottle", "Glass", "Counter", "Bar", "Shelf", "Cabinet" };
  public bool manageByLayer = false;
  public LayerMask managedLayers = -1;

  // - PRIVATE STATE VARIABLES
  private CrimeSceneGenerator generator;
  private List<GameObject> managedObjects = new List<GameObject>();
  private Dictionary<GameObject, bool> originalActiveStates = new Dictionary<GameObject, bool>();

  // - CONSTRUCTOR
  public SceneGeneratorDespawn(CrimeSceneGenerator generator)
  {
    this.generator = generator;
  }

  // - INITIALIZATION SYSTEM
  // Scan scene and register objects for despawn management
  public void ScanAndRegisterExistingObjects()
  {
    managedObjects.Clear();
    originalActiveStates.Clear();

    GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);

    foreach (GameObject obj in allObjects)
    {
      if (ShouldManageObject(obj))
      {
        managedObjects.Add(obj);
        originalActiveStates[obj] = obj.activeInHierarchy;

        if (generator.logSpawnDetails)
        {
          Debug.Log($"Registered object for management: {obj.name}");
        }
      }
    }

    if (generator.logSpawnDetails)
    {
      Debug.Log($"Registered {managedObjects.Count} objects for despawn management");
    }
  }

  // Check if object should be managed by despawn system
  private bool ShouldManageObject(GameObject obj)
  {
    if (obj == null) return false;

    // Exclude generator and its children
    Transform centralObject = generator.transform;
    if (centralObject != null && (obj.transform == centralObject || obj.transform.IsChildOf(centralObject)))
    {
      return false;
    }

    // Exclude essential scene objects
    if (obj == generator.gameObject || obj.name.Contains("Generator") || obj.name.Contains("Manager") ||
        obj.name.Contains("Camera") || obj.name.Contains("Light") || obj.name.Contains("Player"))
    {
      return false;
    }

    // Check qualification criteria
    return MatchesManagedCriteria(obj);
  }

  // Check if object meets any management criteria
  private bool MatchesManagedCriteria(GameObject obj)
  {
    // Check by tags
    foreach (string tag in objectTagsToManage)
    {
      if (obj.CompareTag(tag))
      {
        return true;
      }
    }

    // Check by name patterns
    foreach (string namePattern in objectNamesToManage)
    {
      if (obj.name.ToLower().Contains(namePattern.ToLower()))
      {
        return true;
      }
    }

    // Check by layer if enabled
    if (manageByLayer && managedLayers != 0)
    {
      if ((managedLayers.value & (1 << obj.layer)) != 0)
      {
        return true;
      }
    }

    return false;
  }

  // - OBJECT MANAGEMENT SYSTEM
  // Manage existing objects based on category settings
  public void ManageExistingObjects()
  {
    if (!despawnUnselectedObjects) return;

    int despawnedCount = 0;
    int keptCount = 0;

    foreach (GameObject obj in managedObjects)
    {
      if (obj == null) continue;

      ObjectCategory objCategory = GetObjectCategory(obj);
      bool shouldBeActive = IsCategoryEnabled(objCategory);

      if (shouldBeActive != obj.activeInHierarchy)
      {
        if (hideInsteadOfDestroy)
        {
          obj.SetActive(shouldBeActive);
        }
        else if (!shouldBeActive && obj.activeInHierarchy)
        {
          Object.Destroy(obj);
        }

        if (shouldBeActive)
        {
          keptCount++;
        }
        else
        {
          despawnedCount++;
        }
      }
      else if (obj.activeInHierarchy)
      {
        keptCount++;
      }
    }

    if (generator.logSpawnDetails)
    {
      Debug.Log($"Existing objects managed - Kept active: {keptCount}, Despawned: {despawnedCount}");
    }
  }

  // - OBJECT CATEGORIZATION SYSTEM
  // Determine object category based on name and tag
  private ObjectCategory GetObjectCategory(GameObject obj)
  {
    string objName = obj.name.ToLower();
    string objTag = obj.tag.ToLower();

    // Check object against each category in priority order
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

  // Check if object category should be active
  private bool IsCategoryEnabled(ObjectCategory category)
  {
    // Placeholder for category system integration
    return true;
  }

  // - PUBLIC API METHODS
  // Restore all objects to their original active states
  public void RestoreAllObjects()
  {
    foreach (GameObject obj in managedObjects)
    {
      if (obj != null && originalActiveStates.ContainsKey(obj))
      {
        obj.SetActive(originalActiveStates[obj]);
      }
    }
    Debug.Log("Restored all managed objects to original states");
  }

  // Force hide all managed objects
  public void HideAllManagedObjects()
  {
    foreach (GameObject obj in managedObjects)
    {
      if (obj != null)
      {
        obj.SetActive(false);
      }
    }
    Debug.Log("Hidden all managed objects");
  }

  // Force show all managed objects
  public void ShowAllManagedObjects()
  {
    foreach (GameObject obj in managedObjects)
    {
      if (obj != null)
      {
        obj.SetActive(true);
      }
    }
    Debug.Log("Shown all managed objects");
  }

  // Get copy of managed objects list
  public List<GameObject> GetManagedObjects()
  {
    return new List<GameObject>(managedObjects);
  }
}