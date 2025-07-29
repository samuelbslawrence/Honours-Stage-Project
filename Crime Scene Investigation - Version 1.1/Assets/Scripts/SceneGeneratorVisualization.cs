using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SceneGeneratorVisualization
{
  [Header("Visualization Toggle")]
  public bool enableVisualization = true;

  [Header("Visualization Settings")]
  public bool visualizeSpawnRanges = true;
  public bool visualizeBoundingBoxes = true;
  public bool visualizeSpawnLevels = true;
  public bool showObjectTypeLabels = true;
  public bool onlyShowEnabledCategories = true;

  [Header("Ring Visualization")]
  public bool visualizeRings = true;
  public bool showRingConnections = true;
  public Color tableRingColor = new Color(0f, 1f, 0f, 0.8f);
  public Color chairRingColor = new Color(1f, 1f, 0f, 0.8f);
  public Color cupRingColor = new Color(0f, 1f, 1f, 0.8f);
  public Color connectionColor = new Color(1f, 1f, 1f, 0.8f);

  [Header("Visual Details")]
  [Range(1f, 5f)]
  public float labelOffset = 2f;
  [Range(4, 32)]
  public int visualizationDensity = 8;

  [Header("Category Colors")]
  public Color[] categoryColors = new Color[]
  {
        new Color(0.2f, 0.8f, 0.2f, 0.3f),  // Tables
        new Color(0.8f, 0.4f, 0.1f, 0.3f),  // BarObjects
        new Color(0.2f, 0.2f, 0.8f, 0.3f),  // Chairs
        new Color(0.8f, 0.1f, 0.8f, 0.3f),  // Bottles
        new Color(0.1f, 0.8f, 0.8f, 0.3f),  // Glasses
        new Color(0.6f, 0.3f, 0.8f, 0.3f),  // Furniture
        new Color(0.8f, 0.8f, 0.2f, 0.3f),  // Decorations
        new Color(0.5f, 0.5f, 0.5f, 0.3f)   // Custom
  };

  [Header("Debug Colors")]
  public Color spawnRangeColor = Color.white;
  public Color boundingBoxColor = Color.yellow;
  public Color hierarchyConnectionColor = Color.green;

  [System.Serializable]
  public class ScenePreset
  {
    public string sceneName = "Default Scene";
    public ObjectCategory[] enabledCategories = new ObjectCategory[0];
    public Color sceneColor = Color.white;
    public string description = "Default scene";
  }

  private CrimeSceneGenerator generator;
  private SceneGeneratorSpawning spawningSystem;
  private SceneGeneratorHierarchy hierarchySystem;
  private ScenePreset currentScenePreset;

  public SceneGeneratorVisualization(CrimeSceneGenerator generator)
  {
    this.generator = generator;
    currentScenePreset = new ScenePreset();
  }

  public void SetGenerator(CrimeSceneGenerator generator)
  {
    this.generator = generator;
  }

  public void SetDependencies(SceneGeneratorSpawning spawning, SceneGeneratorHierarchy hierarchy)
  {
    this.spawningSystem = spawning;
    this.hierarchySystem = hierarchy;
  }

  public void HandleUpdate()
  {
    // Minimal update handling
  }

  public void HandleValidate()
  {
#if UNITY_EDITOR
    UnityEditor.SceneView.RepaintAll();
#endif
  }

  public void HandleDrawGizmos()
  {
    if (!enableVisualization || generator == null) return;

    if (generator.visualizeEnvironmentBounds)
    {
      DrawEnvironmentBounds();
    }

    if (visualizeSpawnRanges || visualizeBoundingBoxes || visualizeSpawnLevels)
    {
      DrawObjectTypeConfigurations();
    }

    if (visualizeRings || showRingConnections)
    {
      DrawRingVisualizations();
    }

    if (Application.isPlaying && generator.enableDebugVisualization)
    {
      DrawSpawnedObjectBounds();
    }
  }

  public void HandleDrawGizmosSelected()
  {
    if (!enableVisualization || generator == null) return;

    Vector3 centralPosition = generator.GetCentralPosition();
    Gizmos.color = Color.white;
    Gizmos.DrawWireSphere(centralPosition, 0.3f);
    Gizmos.DrawLine(centralPosition, centralPosition + Vector3.up * 2f);
  }

  public void HandleDestroy()
  {
    // Cleanup if needed
  }

  public void HandleDisable()
  {
    // Cleanup if needed
  }

  private void DrawEnvironmentBounds()
  {
    Vector3 boundsMin = generator.GetEnvironmentBoundsMin();
    Vector3 boundsMax = generator.GetEnvironmentBoundsMax();

    Gizmos.color = generator.environmentBoundsColor;
    Vector3 center = (boundsMin + boundsMax) * 0.5f;
    Vector3 size = boundsMax - boundsMin;
    Gizmos.DrawWireCube(center, size);

    // Draw corner markers with vibrant colors
    Gizmos.color = Color.yellow;
    float markerSize = 0.2f;
    Vector3[] corners = new Vector3[]
    {
            new Vector3(boundsMin.x, boundsMin.y, boundsMin.z),
            new Vector3(boundsMax.x, boundsMin.y, boundsMin.z),
            new Vector3(boundsMin.x, boundsMin.y, boundsMax.z),
            new Vector3(boundsMax.x, boundsMin.y, boundsMax.z),
            new Vector3(boundsMin.x, boundsMax.y, boundsMin.z),
            new Vector3(boundsMax.x, boundsMax.y, boundsMin.z),
            new Vector3(boundsMin.x, boundsMax.y, boundsMax.z),
            new Vector3(boundsMax.x, boundsMax.y, boundsMax.z)
    };

    foreach (Vector3 corner in corners)
    {
      Gizmos.DrawWireCube(corner, Vector3.one * markerSize);
    }
  }

  private void DrawObjectTypeConfigurations()
  {
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();
    if (objectTypes == null) return;

    Vector3 centralPosition = generator.GetCentralPosition();

    for (int i = 0; i < objectTypes.Length; i++)
    {
      ObjectTypeConfig config = objectTypes[i];
      if (onlyShowEnabledCategories && !config.enableThisType) continue;

      Color categoryColor = GetCategoryColor(config.category);
      DrawObjectTypeVisualization(config, categoryColor, i, centralPosition);
    }
  }

  private void DrawObjectTypeVisualization(ObjectTypeConfig config, Color color, int index, Vector3 centralPosition)
  {
    if (visualizeSpawnRanges)
    {
      // Draw min radius with original color
      Gizmos.color = color;
      DrawWireCircleXZ(centralPosition, config.minSpawnRadius);

      // Draw max radius with reduced alpha (like original)
      color.a *= 0.5f;
      Gizmos.color = color;
      DrawWireCircleXZ(centralPosition, config.maxSpawnRadius);
    }

    if (visualizeSpawnLevels)
    {
      color.a = 0.3f;
      Gizmos.color = color;

      Vector3 spawnLevelCenter = centralPosition + Vector3.up * config.spawnLevel;
      float avgRadius = (config.minSpawnRadius + config.maxSpawnRadius) * 0.5f;
      DrawSpawnLevelPlane(spawnLevelCenter, avgRadius, color);
    }

    if (visualizeBoundingBoxes)
    {
      color.a = 0.4f;
      Gizmos.color = color;

      float sampleRadius = (config.minSpawnRadius + config.maxSpawnRadius) * 0.5f;

      // Use original 4 sample approach
      for (int j = 0; j < 4; j++)
      {
        float angle = j * 90f * Mathf.Deg2Rad;
        Vector3 samplePos = centralPosition + new Vector3(
            Mathf.Cos(angle) * sampleRadius,
            config.spawnLevel,
            Mathf.Sin(angle) * sampleRadius
        );

        Gizmos.DrawWireCube(samplePos, config.boundingBoxSize);
      }
    }

    if (showObjectTypeLabels)
    {
      Vector3 labelPos = centralPosition + Vector3.up * (labelOffset + config.spawnLevel) + Vector3.right * index * 1.5f;

#if UNITY_EDITOR
      UnityEditor.Handles.color = Color.white;
      UnityEditor.Handles.Label(labelPos, $"{config.typeName}\nRadius: {config.minSpawnRadius:F1}-{config.maxSpawnRadius:F1}\nSpawn Level: {config.spawnLevel:F1}");
#endif
    }
  }

  private void DrawSpawnLevelPlane(Vector3 center, float radius, Color color)
  {
    int gridLines = 8;
    float gridSize = radius * 2f;
    float cellSize = gridSize / gridLines;

    Gizmos.color = color;

    for (int i = 0; i <= gridLines; i++)
    {
      float x = center.x - radius + (i * cellSize);
      Vector3 start = new Vector3(x, center.y, center.z - radius);
      Vector3 end = new Vector3(x, center.y, center.z + radius);
      Gizmos.DrawLine(start, end);
    }

    for (int i = 0; i <= gridLines; i++)
    {
      float z = center.z - radius + (i * cellSize);
      Vector3 start = new Vector3(center.x - radius, center.y, z);
      Vector3 end = new Vector3(center.x + radius, center.y, z);
      Gizmos.DrawLine(start, end);
    }

    DrawWireCircleAtHeight(center, radius, center.y);
  }

  private void DrawRingVisualizations()
  {
    if (spawningSystem == null) return;

    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();
    ObjectTypeConfig tableConfig = FindConfigForCategory(ObjectCategory.Tables, objectTypes);

    if (tableConfig != null && tableConfig.useRingSpawning && visualizeRings)
    {
      DrawMainTableRing(tableConfig);
    }

    if (Application.isPlaying)
    {
      DrawSpawnedTableRings();
    }
  }

  private void DrawMainTableRing(ObjectTypeConfig tableConfig)
  {
    Vector3 centerPosition = generator.GetCentralPosition();

    // Draw table ring with enhanced visibility
    Gizmos.color = tableRingColor;
    DrawWireCircleAtHeight(centerPosition, tableConfig.tableRingRadius, centerPosition.y + tableConfig.spawnLevel);

    // Add inner ring for depth
    Color innerRing = tableRingColor;
    innerRing.a *= 0.5f;
    Gizmos.color = innerRing;
    DrawWireCircleAtHeight(centerPosition, tableConfig.tableRingRadius * 0.9f, centerPosition.y + tableConfig.spawnLevel);

    // Draw table positions with vibrant markers
    int tableCount = tableConfig.tablePositionsOnRing;
    for (int i = 0; i < tableCount; i++)
    {
      float angle = (360f / tableCount) * i * Mathf.Deg2Rad;
      Vector3 tablePos = centerPosition + new Vector3(
          Mathf.Cos(angle) * tableConfig.tableRingRadius,
          tableConfig.spawnLevel,
          Mathf.Sin(angle) * tableConfig.tableRingRadius
      );

      Gizmos.color = tableRingColor;
      Gizmos.DrawWireSphere(tablePos, 0.2f);
      Gizmos.DrawSphere(tablePos, 0.1f);

      // Connection to center
      Color connectionColor = tableRingColor;
      connectionColor.a *= 0.6f;
      Gizmos.color = connectionColor;
      Gizmos.DrawLine(centerPosition + Vector3.up * tableConfig.spawnLevel, tablePos);
    }

    if (showObjectTypeLabels)
    {
      Vector3 labelPos = centerPosition + Vector3.up * (labelOffset + tableConfig.spawnLevel + 1f);

#if UNITY_EDITOR
      UnityEditor.Handles.color = Color.white;
      string ringInfo = $"Table Ring\nRadius: {tableConfig.tableRingRadius:F1}\nPositions: {tableConfig.tablePositionsOnRing}";
      UnityEditor.Handles.Label(labelPos, ringInfo);
#endif
    }
  }

  private void DrawSpawnedTableRings()
  {
    var spawnedTables = spawningSystem.GetSpawnedTables();
    var tableChairs = spawningSystem.GetTableChairs();
    var tableCups = spawningSystem.GetTableCups();

    foreach (var tableData in spawnedTables)
    {
      if (tableData.gameObject == null) continue;

      if (visualizeRings)
      {
        DrawTableRings(tableData);
      }

      if (showRingConnections)
      {
        DrawRingConnections(tableData, tableChairs, tableCups);
      }
    }
  }

  private void DrawTableRings(SpawnedObjectData tableData)
  {
    Vector3 tablePosition = tableData.position;
    ObjectTypeConfig tableConfig = tableData.typeConfig;

    // Chair ring with vibrant colors
    if (tableConfig.chairsPerTable > 0)
    {
      Gizmos.color = chairRingColor;
      DrawWireCircleAtHeight(tablePosition, tableConfig.chairRingRadius, tablePosition.y);

      // Draw chair position markers
      for (int i = 0; i < tableConfig.chairsPerTable; i++)
      {
        float angle = (360f / tableConfig.chairsPerTable) * i * Mathf.Deg2Rad;
        Vector3 chairPos = tablePosition + new Vector3(
            Mathf.Cos(angle) * tableConfig.chairRingRadius,
            0f,
            Mathf.Sin(angle) * tableConfig.chairRingRadius
        );

        Gizmos.color = chairRingColor;
        Gizmos.DrawWireCube(chairPos, Vector3.one * 0.3f);

        // Add solid marker
        Color solidChair = chairRingColor;
        solidChair.a = 0.8f;
        Gizmos.color = solidChair;
        Gizmos.DrawCube(chairPos, Vector3.one * 0.1f);
      }
    }

    // Cup ring with vibrant colors
    if (tableConfig.cupsPerTable > 0)
    {
      Gizmos.color = cupRingColor;
      DrawWireCircleAtHeight(tablePosition, tableConfig.cupRingRadius, tablePosition.y);

      // Draw cup position markers
      for (int i = 0; i < tableConfig.cupsPerTable; i++)
      {
        float angle = (360f / tableConfig.cupsPerTable) * i * Mathf.Deg2Rad;
        Vector3 cupPos = tablePosition + new Vector3(
            Mathf.Cos(angle) * tableConfig.cupRingRadius,
            0f,
            Mathf.Sin(angle) * tableConfig.cupRingRadius
        );

        Gizmos.color = cupRingColor;
        Gizmos.DrawWireSphere(cupPos, 0.1f);
        Gizmos.DrawSphere(cupPos, 0.05f);
      }
    }

    // Table center marker
    Gizmos.color = tableRingColor;
    Gizmos.DrawWireSphere(tablePosition, 0.15f);
    Gizmos.DrawSphere(tablePosition, 0.08f);

    if (showObjectTypeLabels)
    {
      Vector3 labelPos = tablePosition + Vector3.up * (labelOffset + 0.5f);

#if UNITY_EDITOR
      UnityEditor.Handles.color = Color.white;
      string tableInfo = $"Table\nChairs: {tableConfig.chairsPerTable} (R:{tableConfig.chairRingRadius:F1})\nCups: {tableConfig.cupsPerTable} (R:{tableConfig.cupRingRadius:F1})";
      UnityEditor.Handles.Label(labelPos, tableInfo);
#endif
    }
  }

  private void DrawRingConnections(SpawnedObjectData tableData, Dictionary<GameObject, List<GameObject>> tableChairs, Dictionary<GameObject, List<GameObject>> tableCups)
  {
    Vector3 tableCenter = tableData.position;

    // Chair connections with vibrant colors
    if (tableChairs.ContainsKey(tableData.gameObject))
    {
      foreach (GameObject chair in tableChairs[tableData.gameObject])
      {
        if (chair != null)
        {
          Gizmos.color = connectionColor;
          Gizmos.DrawLine(tableCenter, chair.transform.position);

          Gizmos.color = chairRingColor;
          Gizmos.DrawWireCube(chair.transform.position, Vector3.one * 0.2f);
          Gizmos.DrawCube(chair.transform.position, Vector3.one * 0.1f);
        }
      }
    }

    // Cup connections with vibrant colors
    if (tableCups.ContainsKey(tableData.gameObject))
    {
      foreach (GameObject cup in tableCups[tableData.gameObject])
      {
        if (cup != null)
        {
          Color cupConnection = connectionColor;
          cupConnection.a *= 0.7f;
          Gizmos.color = cupConnection;
          Gizmos.DrawLine(tableCenter, cup.transform.position);

          Gizmos.color = cupRingColor;
          Gizmos.DrawWireSphere(cup.transform.position, 0.08f);
          Gizmos.DrawSphere(cup.transform.position, 0.04f);
        }
      }
    }
  }

  private void DrawSpawnedObjectBounds()
  {
    var spawnedObjects = generator.GetAllSpawnedObjects();
    Vector3 centralPosition = generator.GetCentralPosition();

    foreach (var spawnData in spawnedObjects)
    {
      if (spawnData.gameObject == null) continue;

      // Use debug color for type index (like original)
      Gizmos.color = GetDebugColorForType(spawnData.typeIndex);
      Gizmos.DrawWireCube(spawnData.boundingBox.center, spawnData.boundingBox.size);

      // Connection to center (white like original)
      Gizmos.color = Color.white;
      Gizmos.DrawLine(centralPosition, spawnData.position);

      // Object position marker with category color
      Gizmos.color = GetCategoryColor(spawnData.typeConfig.category);
      Gizmos.DrawWireSphere(spawnData.position, 0.1f);

      // Hierarchy connections (green like original)
      if (hierarchySystem != null)
      {
        var parentChildRelations = hierarchySystem.GetParentChildRelations();
        if (parentChildRelations.ContainsKey(spawnData.gameObject))
        {
          Gizmos.color = Color.green;
          foreach (GameObject child in parentChildRelations[spawnData.gameObject])
          {
            if (child != null)
            {
              Gizmos.DrawLine(spawnData.position, child.transform.position);
              Gizmos.DrawWireSphere(child.transform.position, 0.05f);
            }
          }
        }
      }
    }

    // Draw spawn range circles for each enabled object type (like original)
    ObjectTypeConfig[] objectTypes = generator.GetObjectTypes();
    for (int i = 0; i < objectTypes.Length; i++)
    {
      ObjectTypeConfig config = objectTypes[i];
      if (config.enableThisType)
      {
        Gizmos.color = GetCategoryColor(config.category);
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
        DrawWireCircleAtHeight(centralPosition, config.maxSpawnRadius, centralPosition.y + config.spawnLevel);
      }
    }
  }

  private void DrawWireCircleXZ(Vector3 center, float radius)
  {
    int segments = visualizationDensity * 4;
    float angleStep = 360f / segments;
    Vector3 prevPoint = center + new Vector3(radius, 0, 0);

    for (int i = 1; i <= segments; i++)
    {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector3 newPoint = center + new Vector3(
          Mathf.Cos(angle) * radius,
          0,
          Mathf.Sin(angle) * radius
      );

      Gizmos.DrawLine(prevPoint, newPoint);
      prevPoint = newPoint;
    }
  }

  private void DrawWireCircleAtHeight(Vector3 center, float radius, float height)
  {
    int segments = visualizationDensity * 4;
    float angleStep = 360f / segments;
    Vector3 prevPoint = new Vector3(center.x + radius, height, center.z);

    for (int i = 1; i <= segments; i++)
    {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector3 newPoint = new Vector3(
          center.x + Mathf.Cos(angle) * radius,
          height,
          center.z + Mathf.Sin(angle) * radius
      );

      Gizmos.DrawLine(prevPoint, newPoint);
      prevPoint = newPoint;
    }
  }

  private ObjectTypeConfig FindConfigForCategory(ObjectCategory category, ObjectTypeConfig[] objectTypes)
  {
    foreach (ObjectTypeConfig config in objectTypes)
    {
      if (config.category == category)
        return config;
    }
    return null;
  }

  private Color GetCategoryColor(ObjectCategory category)
  {
    int index = (int)category;
    return (index >= 0 && index < categoryColors.Length) ? categoryColors[index] : Color.gray;
  }

  private Color GetDebugColorForType(int typeIndex)
  {
    if (typeIndex < 0) return Color.red;

    Color[] debugColors = new Color[]
    {
            Color.green,
            Color.blue,
            Color.red,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            Color.white,
            Color.gray
    };

    return debugColors[typeIndex % debugColors.Length];
  }

  // Color utility methods
  public void SetCategoryColor(ObjectCategory category, Color color)
  {
    int index = (int)category;
    if (index >= 0 && index < categoryColors.Length)
    {
      categoryColors[index] = color;
    }
  }

  public Color GetCurrentCategoryColor(ObjectCategory category)
  {
    return GetCategoryColor(category);
  }

  public void SetRingColors(Color tableColor, Color chairColor, Color cupColor)
  {
    tableRingColor = tableColor;
    chairRingColor = chairColor;
    cupRingColor = cupColor;
  }

  public void ResetToDefaultColors()
  {
    categoryColors = new Color[]
    {
            new Color(0.2f, 0.8f, 0.2f, 0.3f),  // Tables
            new Color(0.8f, 0.4f, 0.1f, 0.3f),  // BarObjects
            new Color(0.2f, 0.2f, 0.8f, 0.3f),  // Chairs
            new Color(0.8f, 0.1f, 0.8f, 0.3f),  // Bottles
            new Color(0.1f, 0.8f, 0.8f, 0.3f),  // Glasses
            new Color(0.6f, 0.3f, 0.8f, 0.3f),  // Furniture
            new Color(0.8f, 0.8f, 0.2f, 0.3f),  // Decorations
            new Color(0.5f, 0.5f, 0.5f, 0.3f)   // Custom
    };

    tableRingColor = new Color(0f, 1f, 0f, 0.8f);
    chairRingColor = new Color(1f, 1f, 0f, 0.8f);
    cupRingColor = new Color(0f, 1f, 1f, 0.8f);
    connectionColor = new Color(1f, 1f, 1f, 0.8f);
  }

  // Essential public API methods for other systems
  public void SetVisualizationEnabled(bool enabled)
  {
    enableVisualization = enabled;
  }

  public bool IsVisualizationEnabled()
  {
    return enableVisualization;
  }

  public void SetSelectedCategory(ObjectCategory category)
  {
    // Simplified - no complex selection system
  }

  public void CycleSelectedCategory()
  {
    // Simplified - no complex selection system
  }

  public void SetVisualizationMode(bool singleType, bool singleScene)
  {
    // Simplified - no complex mode switching
  }

  public void SetPreviewMode(bool showPreviews, int count)
  {
    // Simplified - no preview objects
  }

  public void SetVisualizationDensity(int density)
  {
    visualizationDensity = Mathf.Clamp(density, 4, 32);
  }

  public ObjectCategory GetSelectedCategory()
  {
    return ObjectCategory.Tables; // Default
  }

  public int GetPreviewObjectCount()
  {
    return 0; // No preview objects
  }

  public void SetCurrentScene(int sceneIndex)
  {
    // Simplified - no scene presets
  }

  public void NextScene()
  {
    // Simplified - no scene presets
  }

  public void PreviousScene()
  {
    // Simplified - no scene presets
  }

  public string GetCurrentSceneName()
  {
    return "Default Scene";
  }

  public int GetSceneCount()
  {
    return 1;
  }

  public int GetCurrentSceneIndex()
  {
    return 0;
  }

  public ScenePreset GetCurrentScenePreset()
  {
    return currentScenePreset;
  }

  public void SetSceneChangeInterval(float interval)
  {
    // Simplified - no scene cycling
  }

  public void SetManualSceneSelection(bool manual)
  {
    // Simplified - no scene selection
  }
}