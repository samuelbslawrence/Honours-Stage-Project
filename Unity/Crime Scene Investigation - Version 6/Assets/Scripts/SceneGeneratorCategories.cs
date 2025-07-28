using UnityEngine;

// - SCENE GENERATOR CATEGORY MANAGEMENT SYSTEM
public class SceneGeneratorCategories : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("Category Settings")]
  [Tooltip("Enable table objects in scene generation")]
  public bool enableTables = true;
  [Tooltip("Enable bar counter and related objects")]
  public bool enableBarObjects = true;
  [Tooltip("Enable chair objects for seating")]
  public bool enableChairs = true;
  [Tooltip("Enable bottle objects for drinks")]
  public bool enableBottles = true;
  [Tooltip("Enable glass objects for beverages")]
  public bool enableGlasses = true;
  [Tooltip("Enable furniture objects like shelves and cabinets")]
  public bool enableFurniture = true;
  [Tooltip("Enable decorative objects for ambiance")]
  public bool enableDecorations = false;
  [Tooltip("Enable custom object categories")]
  public bool enableCustomObjects = false;

  // - PRIVATE STATE VARIABLES
  // Component references
  private CrimeSceneGenerator generator;

  // - INITIALIZATION
  public SceneGeneratorCategories(CrimeSceneGenerator generator)
  {
    this.generator = generator;
  }

  // - UNITY LIFECYCLE METHODS
  void Start()
  {
    // Find the generator if not set during construction
    if (generator == null)
    {
      generator = FindObjectOfType<CrimeSceneGenerator>();
    }
  }

  // - CATEGORY CONTROL METHODS
  public void EnableAllCategories()
  {
    enableTables = true;
    enableBarObjects = true;
    enableChairs = true;
    enableBottles = true;
    enableGlasses = true;
    enableFurniture = true;
    enableDecorations = true;
    enableCustomObjects = true;

    Debug.Log("SceneGeneratorCategories: All object categories enabled");
  }

  public void DisableAllCategories()
  {
    enableTables = false;
    enableBarObjects = false;
    enableChairs = false;
    enableBottles = false;
    enableGlasses = false;
    enableFurniture = false;
    enableDecorations = false;
    enableCustomObjects = false;

    Debug.Log("SceneGeneratorCategories: All object categories disabled");
  }

  // - SINGLE CATEGORY METHODS
  public void EnableOnlyTables()
  {
    DisableAllCategories();
    enableTables = true;
    Debug.Log("SceneGeneratorCategories: Only tables enabled");
  }

  public void EnableOnlyBarObjects()
  {
    DisableAllCategories();
    enableBarObjects = true;
    Debug.Log("SceneGeneratorCategories: Only bar objects enabled");
  }

  public void EnableOnlyChairs()
  {
    DisableAllCategories();
    enableChairs = true;
    Debug.Log("SceneGeneratorCategories: Only chairs enabled");
  }

  public void EnableOnlyBottles()
  {
    DisableAllCategories();
    enableBottles = true;
    Debug.Log("SceneGeneratorCategories: Only bottles enabled");
  }

  public void EnableOnlyGlasses()
  {
    DisableAllCategories();
    enableGlasses = true;
    Debug.Log("SceneGeneratorCategories: Only glasses enabled");
  }

  public void EnableOnlyFurniture()
  {
    DisableAllCategories();
    enableFurniture = true;
    Debug.Log("SceneGeneratorCategories: Only furniture enabled");
  }

  // - SCENE PRESET METHODS
  public void EnableBarScene()
  {
    DisableAllCategories();
    enableTables = true;
    enableBarObjects = true;
    enableChairs = true;
    enableBottles = true;
    enableGlasses = true;

    Debug.Log("SceneGeneratorCategories: Bar scene configuration enabled");
  }

  public void EnableOfficeScene()
  {
    DisableAllCategories();
    enableTables = true;
    enableChairs = true;
    enableFurniture = true;

    Debug.Log("SceneGeneratorCategories: Office scene configuration enabled");
  }

  [ContextMenu("Enable Restaurant Scene")]
  public void EnableRestaurantScene()
  {
    DisableAllCategories();
    enableTables = true;
    enableChairs = true;
    enableGlasses = true;
    enableBottles = true;
    enableDecorations = true;

    Debug.Log("SceneGeneratorCategories: Restaurant scene configuration enabled");
  }

  [ContextMenu("Enable Living Room Scene")]
  public void EnableLivingRoomScene()
  {
    DisableAllCategories();
    enableFurniture = true;
    enableDecorations = true;
    enableTables = true;

    Debug.Log("SceneGeneratorCategories: Living room scene configuration enabled");
  }

  [ContextMenu("Enable Minimal Scene")]
  public void EnableMinimalScene()
  {
    DisableAllCategories();
    enableTables = true;

    Debug.Log("SceneGeneratorCategories: Minimal scene configuration enabled");
  }

  // - CATEGORY STATE MANAGEMENT
  public void SetCategoryEnabled(ObjectCategory category, bool enabled)
  {
    switch (category)
    {
      case ObjectCategory.Tables:
        enableTables = enabled;
        break;
      case ObjectCategory.BarObjects:
        enableBarObjects = enabled;
        break;
      case ObjectCategory.Chairs:
        enableChairs = enabled;
        break;
      case ObjectCategory.Bottles:
        enableBottles = enabled;
        break;
      case ObjectCategory.Glasses:
        enableGlasses = enabled;
        break;
      case ObjectCategory.Furniture:
        enableFurniture = enabled;
        break;
      case ObjectCategory.Decorations:
        enableDecorations = enabled;
        break;
      case ObjectCategory.Custom:
        enableCustomObjects = enabled;
        break;
    }
  }

  public bool IsCategoryEnabled(ObjectCategory category)
  {
    switch (category)
    {
      case ObjectCategory.Tables:
        return enableTables;
      case ObjectCategory.BarObjects:
        return enableBarObjects;
      case ObjectCategory.Chairs:
        return enableChairs;
      case ObjectCategory.Bottles:
        return enableBottles;
      case ObjectCategory.Glasses:
        return enableGlasses;
      case ObjectCategory.Furniture:
        return enableFurniture;
      case ObjectCategory.Decorations:
        return enableDecorations;
      case ObjectCategory.Custom:
        return enableCustomObjects;
      case ObjectCategory.Evidence:
        return true; // Evidence is always enabled
      default:
        return true;
    }
  }

  // - SCENE GENERATION WITH CATEGORIES
  public void GenerateSceneWithCategories(params ObjectCategory[] categories)
  {
    // Disable all categories first
    DisableAllCategories();

    // Enable specified categories
    foreach (ObjectCategory category in categories)
    {
      SetCategoryEnabled(category, true);
    }

    // Generate scene with current settings
    if (generator != null)
    {
      generator.GenerateScene();
    }
    else
    {
      Debug.LogWarning("SceneGeneratorCategories: No generator reference found for scene generation");
    }
  }

  // - CATEGORY STATUS METHODS
  public int GetEnabledCategoryCount()
  {
    int count = 0;
    if (enableTables) count++;
    if (enableBarObjects) count++;
    if (enableChairs) count++;
    if (enableBottles) count++;
    if (enableGlasses) count++;
    if (enableFurniture) count++;
    if (enableDecorations) count++;
    if (enableCustomObjects) count++;
    return count;
  }

  public ObjectCategory[] GetEnabledCategories()
  {
    System.Collections.Generic.List<ObjectCategory> enabledCategories = new System.Collections.Generic.List<ObjectCategory>();

    if (enableTables) enabledCategories.Add(ObjectCategory.Tables);
    if (enableBarObjects) enabledCategories.Add(ObjectCategory.BarObjects);
    if (enableChairs) enabledCategories.Add(ObjectCategory.Chairs);
    if (enableBottles) enabledCategories.Add(ObjectCategory.Bottles);
    if (enableGlasses) enabledCategories.Add(ObjectCategory.Glasses);
    if (enableFurniture) enabledCategories.Add(ObjectCategory.Furniture);
    if (enableDecorations) enabledCategories.Add(ObjectCategory.Decorations);
    if (enableCustomObjects) enabledCategories.Add(ObjectCategory.Custom);

    return enabledCategories.ToArray();
  }

  public ObjectCategory[] GetDisabledCategories()
  {
    System.Collections.Generic.List<ObjectCategory> disabledCategories = new System.Collections.Generic.List<ObjectCategory>();

    if (!enableTables) disabledCategories.Add(ObjectCategory.Tables);
    if (!enableBarObjects) disabledCategories.Add(ObjectCategory.BarObjects);
    if (!enableChairs) disabledCategories.Add(ObjectCategory.Chairs);
    if (!enableBottles) disabledCategories.Add(ObjectCategory.Bottles);
    if (!enableGlasses) disabledCategories.Add(ObjectCategory.Glasses);
    if (!enableFurniture) disabledCategories.Add(ObjectCategory.Furniture);
    if (!enableDecorations) disabledCategories.Add(ObjectCategory.Decorations);
    if (!enableCustomObjects) disabledCategories.Add(ObjectCategory.Custom);

    return disabledCategories.ToArray();
  }

  public string GetCategoryStatusSummary()
  {
    var enabled = GetEnabledCategories();
    if (enabled.Length == 0)
    {
      return "No categories enabled";
    }

    return $"Enabled categories ({enabled.Length}): {string.Join(", ", enabled)}";
  }

  public string GetDetailedCategoryStatus()
  {
    System.Text.StringBuilder status = new System.Text.StringBuilder();
    status.AppendLine("Category Status:");
    status.AppendLine($"Tables: {(enableTables ? "Enabled" : "Disabled")}");
    status.AppendLine($"Bar Objects: {(enableBarObjects ? "Enabled" : "Disabled")}");
    status.AppendLine($"Chairs: {(enableChairs ? "Enabled" : "Disabled")}");
    status.AppendLine($"Bottles: {(enableBottles ? "Enabled" : "Disabled")}");
    status.AppendLine($"Glasses: {(enableGlasses ? "Enabled" : "Disabled")}");
    status.AppendLine($"Furniture: {(enableFurniture ? "Enabled" : "Disabled")}");
    status.AppendLine($"Decorations: {(enableDecorations ? "Enabled" : "Disabled")}");
    status.AppendLine($"Custom Objects: {(enableCustomObjects ? "Enabled" : "Disabled")}");
    return status.ToString();
  }

  // - TOGGLE METHODS
  public void ToggleCategory(ObjectCategory category)
  {
    SetCategoryEnabled(category, !IsCategoryEnabled(category));
  }

  public void ToggleTables()
  {
    ToggleCategory(ObjectCategory.Tables);
    Debug.Log($"SceneGeneratorCategories: Tables {(enableTables ? "enabled" : "disabled")}");
  }

  public void ToggleBarObjects()
  {
    ToggleCategory(ObjectCategory.BarObjects);
    Debug.Log($"SceneGeneratorCategories: Bar objects {(enableBarObjects ? "enabled" : "disabled")}");
  }

  public void ToggleChairs()
  {
    ToggleCategory(ObjectCategory.Chairs);
    Debug.Log($"SceneGeneratorCategories: Chairs {(enableChairs ? "enabled" : "disabled")}");
  }

  public void ToggleBottles()
  {
    ToggleCategory(ObjectCategory.Bottles);
    Debug.Log($"SceneGeneratorCategories: Bottles {(enableBottles ? "enabled" : "disabled")}");
  }

  public void ToggleGlasses()
  {
    ToggleCategory(ObjectCategory.Glasses);
    Debug.Log($"SceneGeneratorCategories: Glasses {(enableGlasses ? "enabled" : "disabled")}");
  }

  public void ToggleFurniture()
  {
    ToggleCategory(ObjectCategory.Furniture);
    Debug.Log($"SceneGeneratorCategories: Furniture {(enableFurniture ? "enabled" : "disabled")}");
  }

  public void ToggleDecorations()
  {
    ToggleCategory(ObjectCategory.Decorations);
    Debug.Log($"SceneGeneratorCategories: Decorations {(enableDecorations ? "enabled" : "disabled")}");
  }

  public void ToggleCustomObjects()
  {
    ToggleCategory(ObjectCategory.Custom);
    Debug.Log($"SceneGeneratorCategories: Custom objects {(enableCustomObjects ? "enabled" : "disabled")}");
  }

  // - BATCH OPERATIONS
  public void SetMultipleCategoriesEnabled(bool enabled, params ObjectCategory[] categories)
  {
    foreach (ObjectCategory category in categories)
    {
      SetCategoryEnabled(category, enabled);
    }
  }

  public void EnableDrinkingCategories()
  {
    SetMultipleCategoriesEnabled(true, ObjectCategory.Bottles, ObjectCategory.Glasses, ObjectCategory.BarObjects);
    Debug.Log("SceneGeneratorCategories: Drinking-related categories enabled");
  }

  public void EnableSeatingCategories()
  {
    SetMultipleCategoriesEnabled(true, ObjectCategory.Tables, ObjectCategory.Chairs);
    Debug.Log("SceneGeneratorCategories: Seating-related categories enabled");
  }

  public void EnableAmbientCategories()
  {
    SetMultipleCategoriesEnabled(true, ObjectCategory.Furniture, ObjectCategory.Decorations);
    Debug.Log("SceneGeneratorCategories: Ambient categories enabled");
  }

  // - VALIDATION METHODS
  public bool HasAnyEnabledCategories()
  {
    return GetEnabledCategoryCount() > 0;
  }

  public bool AreAllCategoriesEnabled()
  {
    return GetEnabledCategoryCount() == 8; // Total number of categories
  }

  public bool AreAllCategoriesDisabled()
  {
    return GetEnabledCategoryCount() == 0;
  }

  // - CONTEXT MENU METHODS
  [ContextMenu("Log Category Status")]
  public void LogCategoryStatus()
  {
    Debug.Log(GetDetailedCategoryStatus());
  }

  [ContextMenu("Enable All Categories")]
  public void EnableAllCategoriesMenu()
  {
    EnableAllCategories();
  }

  [ContextMenu("Disable All Categories")]
  public void DisableAllCategoriesMenu()
  {
    DisableAllCategories();
  }

  [ContextMenu("Generate Scene With Current Categories")]
  public void GenerateSceneWithCurrentCategories()
  {
    if (generator != null)
    {
      generator.GenerateScene();
    }
    else
    {
      Debug.LogWarning("SceneGeneratorCategories: No generator reference found");
    }
  }

  // - PUBLIC API METHODS
  // Generator reference management
  public void SetGenerator(CrimeSceneGenerator newGenerator)
  {
    generator = newGenerator;
  }

  public CrimeSceneGenerator GetGenerator()
  {
    return generator;
  }

  // Category state queries
  public bool IsEmpty()
  {
    return AreAllCategoriesDisabled();
  }

  public bool IsFull()
  {
    return AreAllCategoriesEnabled();
  }

  // - PRESET VALIDATION METHODS
  public bool IsBarSceneConfiguration()
  {
    return HasExactlyTheseCategories(
      ObjectCategory.Tables,
      ObjectCategory.BarObjects,
      ObjectCategory.Chairs,
      ObjectCategory.Bottles,
      ObjectCategory.Glasses
    );
  }

  public bool IsOfficeSceneConfiguration()
  {
    return HasExactlyTheseCategories(
      ObjectCategory.Tables,
      ObjectCategory.Chairs,
      ObjectCategory.Furniture
    );
  }

  public bool IsRestaurantSceneConfiguration()
  {
    return HasExactlyTheseCategories(
      ObjectCategory.Tables,
      ObjectCategory.Chairs,
      ObjectCategory.Glasses,
      ObjectCategory.Bottles,
      ObjectCategory.Decorations
    );
  }

  public bool IsLivingRoomSceneConfiguration()
  {
    return HasExactlyTheseCategories(
      ObjectCategory.Furniture,
      ObjectCategory.Decorations,
      ObjectCategory.Tables
    );
  }

  public bool IsMinimalSceneConfiguration()
  {
    return HasExactlyTheseCategories(
      ObjectCategory.Tables
    );
  }

  // - HELPER METHODS
  private bool HasExactlyTheseCategories(params ObjectCategory[] requiredCategories)
  {
    var enabledCategories = GetEnabledCategories();

    // Check counts match exactly
    if (enabledCategories.Length != requiredCategories.Length)
      return false;

    // Check all required categories are present
    foreach (ObjectCategory required in requiredCategories)
    {
      if (!System.Array.Exists(enabledCategories, cat => cat == required))
        return false;
    }

    return true;
  }
}