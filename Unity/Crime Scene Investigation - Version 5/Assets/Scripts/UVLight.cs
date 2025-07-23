using UnityEngine;
using System.Collections.Generic;

public class UVLight : MonoBehaviour
{
  [Header("UV Light Settings")]
  public Light spotLight;  // The spotlight component
  public Color glowColor = Color.cyan;  // Glow color for fingerprints

  [Header("Opacity Settings")]
  [Range(0.1f, 1.0f)]
  public float uvRevealedOpacity = 0.8f;  // Opacity when under UV light
  public float opacityTransitionSpeed = 3.0f;  // How fast opacity changes

  private Dictionary<Renderer, MaterialData> originalMaterials = new Dictionary<Renderer, MaterialData>();
  private Dictionary<Renderer, float> targetOpacity = new Dictionary<Renderer, float>();

  [System.Serializable]
  public class MaterialData
  {
    public Color originalColor;
    public Texture originalTexture;
    public Color originalEmission;
    public bool hadEmission;
    public float originalAlpha;
  }

  void Update()
  {
    if (spotLight != null && spotLight.enabled)
    {
      CheckForFingerprints();
      UpdateOpacityTransitions();
    }
  }

  void CheckForFingerprints()
  {
    // Get all objects with "Fingerprint" name
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    foreach (GameObject obj in allObjects)
    {
      if (obj.name == "Fingerprint")
      {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
          // Store original material data if we haven't already
          if (!originalMaterials.ContainsKey(renderer))
          {
            MaterialData data = new MaterialData();
            data.originalColor = renderer.material.color;
            data.originalTexture = renderer.material.mainTexture;
            data.originalAlpha = renderer.material.color.a;

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

          if (IsInLightCone(obj.transform.position))
          {
            // Set target opacity to revealed level
            targetOpacity[renderer] = uvRevealedOpacity;

            // Switch to glow - keep original texture but add emission
            if (renderer.material.HasProperty("_EmissionColor"))
            {
              renderer.material.SetColor("_EmissionColor", glowColor);
              renderer.material.EnableKeyword("_EMISSION");
            }

            // Ensure transparency is enabled for URP
            if (renderer.material.HasProperty("_Surface"))
            {
              renderer.material.SetFloat("_Surface", 1); // Transparent
            }
            if (renderer.material.HasProperty("_Blend"))
            {
              renderer.material.SetFloat("_Blend", 0); // Alpha blend
            }
          }
          else
          {
            // Set target opacity back to original
            MaterialData originalData = originalMaterials[renderer];
            targetOpacity[renderer] = originalData.originalAlpha;

            // Switch to no glow - restore original material properties
            renderer.material.color = originalData.originalColor;
            renderer.material.mainTexture = originalData.originalTexture;

            if (renderer.material.HasProperty("_EmissionColor"))
            {
              renderer.material.SetColor("_EmissionColor", originalData.originalEmission);
              if (originalData.hadEmission)
                renderer.material.EnableKeyword("_EMISSION");
              else
                renderer.material.DisableKeyword("_EMISSION");
            }
          }
        }
      }
    }
  }

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

      // Get current color
      Color currentColor = renderer.material.color;
      if (renderer.material.HasProperty("_BaseColor"))
      {
        currentColor = renderer.material.GetColor("_BaseColor");
      }

      // Smoothly transition to target opacity
      float newAlpha = Mathf.MoveTowards(currentColor.a, target, opacityTransitionSpeed * Time.deltaTime);

      // Apply new alpha
      Color newColor = currentColor;
      newColor.a = newAlpha;

      renderer.material.color = newColor;

      // Also update URP base color if it exists
      if (renderer.material.HasProperty("_BaseColor"))
      {
        renderer.material.SetColor("_BaseColor", newColor);
      }

      // Remove from tracking if we've reached the target
      if (Mathf.Approximately(newAlpha, target))
      {
        renderersToRemove.Add(renderer);
      }
    }

    // Clean up completed transitions
    foreach (Renderer renderer in renderersToRemove)
    {
      targetOpacity.Remove(renderer);
    }
  }

  bool IsInLightCone(Vector3 targetPosition)
  {
    Vector3 lightPosition = spotLight.transform.position;
    Vector3 lightDirection = spotLight.transform.forward;

    // Check distance
    float distance = Vector3.Distance(lightPosition, targetPosition);
    if (distance > spotLight.range)
      return false;

    // Check angle
    Vector3 directionToTarget = (targetPosition - lightPosition).normalized;
    float angle = Vector3.Angle(lightDirection, directionToTarget);
    return angle <= spotLight.spotAngle / 2f;
  }

  // Public method to reset all fingerprints to original state
  public void ResetAllFingerprints()
  {
    foreach (var kvp in originalMaterials)
    {
      Renderer renderer = kvp.Key;
      MaterialData originalData = kvp.Value;

      if (renderer != null)
      {
        renderer.material.color = originalData.originalColor;
        renderer.material.mainTexture = originalData.originalTexture;

        if (renderer.material.HasProperty("_EmissionColor"))
        {
          renderer.material.SetColor("_EmissionColor", originalData.originalEmission);
          if (originalData.hadEmission)
            renderer.material.EnableKeyword("_EMISSION");
          else
            renderer.material.DisableKeyword("_EMISSION");
        }

        if (renderer.material.HasProperty("_BaseColor"))
        {
          renderer.material.SetColor("_BaseColor", originalData.originalColor);
        }
      }
    }

    targetOpacity.Clear();
  }

  void OnDestroy()
  {
    // Clean up when destroyed
    ResetAllFingerprints();
  }
}