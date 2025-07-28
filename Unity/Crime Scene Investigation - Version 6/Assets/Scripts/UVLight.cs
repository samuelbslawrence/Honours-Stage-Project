using UnityEngine;
using System.Collections.Generic;

public class UVLight : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("UV Light Settings")]
  public Light spotLight;
  public Color glowColor = Color.cyan;

  [Header("Opacity Settings")]
  [Range(0.1f, 1.0f)]
  public float uvRevealedOpacity = 0.8f;
  public float opacityTransitionSpeed = 3.0f;

  // - PRIVATE STATE VARIABLES
  // Material tracking for fingerprints
  private Dictionary<Renderer, MaterialData> originalMaterials = new Dictionary<Renderer, MaterialData>();
  private Dictionary<Renderer, float> targetOpacity = new Dictionary<Renderer, float>();

  // - MATERIAL DATA STRUCTURE
  [System.Serializable]
  public class MaterialData
  {
    public Color originalColor;
    public Texture originalTexture;
    public Color originalEmission;
    public bool hadEmission;
    public float originalAlpha;
  }

  // - UPDATE LOOP
  void Update()
  {
    if (spotLight != null && spotLight.enabled)
    {
      CheckForFingerprints();
      UpdateOpacityTransitions();
    }
  }

  // - FINGERPRINT DETECTION SYSTEM
  // Check for fingerprints within UV light cone
  void CheckForFingerprints()
  {
    // Find all fingerprint objects in scene
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj.name == "Fingerprint")
      {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
          // Store original material data if not already stored
          StoreOriginalMaterialData(renderer);

          // Apply UV effect if in light cone
          if (IsInLightCone(obj.transform.position))
          {
            ApplyUVEffect(renderer);
          }
          else
          {
            RestoreOriginalMaterial(renderer);
          }
        }
      }
    }
  }

  // Store original material properties for restoration
  void StoreOriginalMaterialData(Renderer renderer)
  {
    if (!originalMaterials.ContainsKey(renderer))
    {
      MaterialData data = new MaterialData();
      data.originalColor = renderer.material.color;
      data.originalTexture = renderer.material.mainTexture;
      data.originalAlpha = renderer.material.color.a;

      // Store emission properties
      if (renderer.material.HasProperty("_EmissionColor"))
      {
        data.originalEmission = renderer.material.GetColor("_EmissionColor");
        data.hadEmission = renderer.material.IsKeywordEnabled("_EMISSION");
      }

      // Handle URP base color
      if (renderer.material.HasProperty("_BaseColor"))
      {
        data.originalColor = renderer.material.GetColor("_BaseColor");
        data.originalAlpha = data.originalColor.a;
      }

      originalMaterials[renderer] = data;
    }
  }

  // Apply UV glow effect to fingerprint
  void ApplyUVEffect(Renderer renderer)
  {
    // Set target opacity to revealed level
    targetOpacity[renderer] = uvRevealedOpacity;

    // Apply glow emission
    if (renderer.material.HasProperty("_EmissionColor"))
    {
      renderer.material.SetColor("_EmissionColor", glowColor);
      renderer.material.EnableKeyword("_EMISSION");
    }

    // Configure transparency for URP
    ConfigureTransparency(renderer);
  }

  // Restore original material properties
  void RestoreOriginalMaterial(Renderer renderer)
  {
    MaterialData originalData = originalMaterials[renderer];
    targetOpacity[renderer] = originalData.originalAlpha;

    // Restore original material properties
    renderer.material.color = originalData.originalColor;
    renderer.material.mainTexture = originalData.originalTexture;

    // Restore emission properties
    if (renderer.material.HasProperty("_EmissionColor"))
    {
      renderer.material.SetColor("_EmissionColor", originalData.originalEmission);
      if (originalData.hadEmission)
        renderer.material.EnableKeyword("_EMISSION");
      else
        renderer.material.DisableKeyword("_EMISSION");
    }
  }

  // Configure material transparency settings for URP
  void ConfigureTransparency(Renderer renderer)
  {
    // Enable transparency for URP materials
    if (renderer.material.HasProperty("_Surface"))
    {
      renderer.material.SetFloat("_Surface", 1); // Transparent
    }
    if (renderer.material.HasProperty("_Blend"))
    {
      renderer.material.SetFloat("_Blend", 0); // Alpha blend
    }
  }

  // - OPACITY TRANSITION SYSTEM
  // Smoothly transition opacity values over time
  void UpdateOpacityTransitions()
  {
    List<Renderer> renderersToRemove = new List<Renderer>();

    foreach (var kvp in targetOpacity)
    {
      Renderer renderer = kvp.Key;
      float target = kvp.Value;

      if (renderer == null)
      {
        renderersToRemove.Add(renderer);
        continue;
      }

      // Update opacity with smooth transition
      UpdateRendererOpacity(renderer, target, renderersToRemove);
    }

    // Clean up completed transitions
    foreach (Renderer renderer in renderersToRemove)
    {
      targetOpacity.Remove(renderer);
    }
  }

  // Update individual renderer opacity
  void UpdateRendererOpacity(Renderer renderer, float target, List<Renderer> renderersToRemove)
  {
    // Get current color
    Color currentColor = renderer.material.color;
    if (renderer.material.HasProperty("_BaseColor"))
    {
      currentColor = renderer.material.GetColor("_BaseColor");
    }

    // Smoothly transition to target opacity
    float newAlpha = Mathf.MoveTowards(currentColor.a, target, opacityTransitionSpeed * Time.deltaTime);

    // Apply new alpha value
    Color newColor = currentColor;
    newColor.a = newAlpha;

    renderer.material.color = newColor;

    // Update URP base color if available
    if (renderer.material.HasProperty("_BaseColor"))
    {
      renderer.material.SetColor("_BaseColor", newColor);
    }

    // Mark for removal if target reached
    if (Mathf.Approximately(newAlpha, target))
    {
      renderersToRemove.Add(renderer);
    }
  }

  // - LIGHT CONE DETECTION
  // Check if position is within spotlight cone
  bool IsInLightCone(Vector3 targetPosition)
  {
    Vector3 lightPosition = spotLight.transform.position;
    Vector3 lightDirection = spotLight.transform.forward;

    // Check distance from light
    float distance = Vector3.Distance(lightPosition, targetPosition);
    if (distance > spotLight.range)
      return false;

    // Check angle within spotlight cone
    Vector3 directionToTarget = (targetPosition - lightPosition).normalized;
    float angle = Vector3.Angle(lightDirection, directionToTarget);
    return angle <= spotLight.spotAngle / 2f;
  }

  // - PUBLIC API METHODS
  // Reset all fingerprints to original state
  public void ResetAllFingerprints()
  {
    foreach (var kvp in originalMaterials)
    {
      Renderer renderer = kvp.Key;
      MaterialData originalData = kvp.Value;

      if (renderer != null)
      {
        // Restore original material properties
        renderer.material.color = originalData.originalColor;
        renderer.material.mainTexture = originalData.originalTexture;

        // Restore emission properties
        if (renderer.material.HasProperty("_EmissionColor"))
        {
          renderer.material.SetColor("_EmissionColor", originalData.originalEmission);
          if (originalData.hadEmission)
            renderer.material.EnableKeyword("_EMISSION");
          else
            renderer.material.DisableKeyword("_EMISSION");
        }

        // Restore URP base color
        if (renderer.material.HasProperty("_BaseColor"))
        {
          renderer.material.SetColor("_BaseColor", originalData.originalColor);
        }
      }
    }

    targetOpacity.Clear();
  }

  // - CLEANUP
  void OnDestroy()
  {
    // Clean up when destroyed
    ResetAllFingerprints();
  }
}