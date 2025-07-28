using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPPoint : MonoBehaviour
{
  // - SERIALIZED FIELD DECLARATIONS
  [Header("Visual Settings")]
  [SerializeField] private float alphaThresholdDefault = 1.0f;
  [SerializeField] private float alphaThresholdRevealed = 0.0f;
  [SerializeField] private float alphaThresholdHighlighted = 0.0f;
  [SerializeField] private Color defaultColor = Color.blue;
  [SerializeField] private Color highlightedColor = Color.green;
  [SerializeField] private string alphaThresholdProperty = "_Cutoff";

  // - PRIVATE STATE VARIABLES
  // Component references
  private Renderer[] renderers;
  private List<Material> materials = new List<Material>();

  // Animation state
  private Coroutine currentAnimation = null;
  private bool initialized = false;

  // - INITIALIZATION SYSTEM
  // Initialize teleport point with material properties
  public void Initialize(string thresholdPropertyName)
  {
    // Set shader property name if provided
    if (!string.IsNullOrEmpty(thresholdPropertyName))
    {
      alphaThresholdProperty = thresholdPropertyName;
    }

    // Get all child renderers
    renderers = GetComponentsInChildren<Renderer>(true);

    // Collect and setup materials
    SetupMaterials();

    // Set initial hidden state
    if (materials.Count > 0)
    {
      SetAlphaThreshold(alphaThresholdDefault);
    }

    initialized = true;
  }

  // Setup materials with alpha threshold properties
  private void SetupMaterials()
  {
    materials.Clear();

    foreach (Renderer renderer in renderers)
    {
      // Create instance materials to avoid affecting shared materials
      Material[] instanceMaterials = renderer.materials;

      for (int i = 0; i < instanceMaterials.Length; i++)
      {
        Material mat = instanceMaterials[i];

        // Search for alpha threshold property
        string foundProperty = FindAlphaThresholdProperty(mat);

        if (!string.IsNullOrEmpty(foundProperty))
        {
          materials.Add(mat);
          alphaThresholdProperty = foundProperty;

          // Set initial alpha threshold value
          mat.SetFloat(foundProperty, alphaThresholdDefault);
        }
        else
        {
          Debug.LogWarning("No alpha threshold property found on " + renderer.name);
        }
      }

      // Apply instance materials back to renderer
      renderer.materials = instanceMaterials;
    }

    // Validate material setup
    if (materials.Count == 0)
    {
      Debug.LogWarning("No materials with alpha threshold property found on " + name);
    }
  }

  // Find alpha threshold property in material
  private string FindAlphaThresholdProperty(Material material)
  {
    string[] possibleProperties = new string[] { "_Cutoff", "_AlphaClip", "_Cutout", "_AlphaTest", "_AlphaCutoff" };

    foreach (string prop in possibleProperties)
    {
      if (material.HasProperty(prop))
      {
        return prop;
      }
    }

    return "";
  }

  // - VISIBILITY CONTROL SYSTEM
  // Reveal teleport point with animation
  public void RevealPoint(float duration)
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    StopCurrentAnimation();

    // Start reveal animation
    currentAnimation = StartCoroutine(AnimateAlphaThreshold(alphaThresholdRevealed, duration));
  }

  // Hide teleport point with animation
  public void HidePoint(float duration)
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    StopCurrentAnimation();

    // Start hide animation
    currentAnimation = StartCoroutine(AnimateAlphaThreshold(alphaThresholdDefault, duration));
  }

  // Immediately hide point without animation
  public void ForceHidePoint()
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    StopCurrentAnimation();

    // Set to hidden state immediately
    SetAlphaThreshold(alphaThresholdDefault);
  }

  // Set point to highlighted state
  public void HighlightPoint()
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    StopCurrentAnimation();

    // Set to highlighted state
    SetAlphaThreshold(alphaThresholdHighlighted);

    // Apply highlight color if different from default
    if (highlightedColor != defaultColor)
    {
      SetColor(highlightedColor);
    }
  }

  // - ANIMATION SYSTEM
  // Stop current animation if running
  private void StopCurrentAnimation()
  {
    if (currentAnimation != null)
    {
      StopCoroutine(currentAnimation);
      currentAnimation = null;
    }
  }

  // Animate alpha threshold over time
  IEnumerator AnimateAlphaThreshold(float targetThreshold, float duration)
  {
    // Skip animation if no materials available
    if (materials.Count == 0)
    {
      currentAnimation = null;
      yield break;
    }

    // Get starting threshold from first material
    float startThreshold = materials[0].GetFloat(alphaThresholdProperty);
    float time = 0;

    // Animate over duration
    while (time < duration)
    {
      // Calculate animation progress
      float t = time / duration;
      float smoothT = Mathf.SmoothStep(0, 1, t);

      // Interpolate and apply new threshold
      float newThreshold = Mathf.Lerp(startThreshold, targetThreshold, smoothT);
      SetAlphaThreshold(newThreshold);

      yield return null;
      time += Time.deltaTime;
    }

    // Ensure final value is set
    SetAlphaThreshold(targetThreshold);
    currentAnimation = null;
  }

  // - MATERIAL PROPERTY CONTROL
  // Set alpha threshold on all materials
  void SetAlphaThreshold(float threshold)
  {
    foreach (Material material in materials)
    {
      material.SetFloat(alphaThresholdProperty, threshold);
    }
  }

  // Set color on all materials
  void SetColor(Color color)
  {
    foreach (Material material in materials)
    {
      // Try standard color property first
      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", color);
      }
      // Try URP base color property
      else if (material.HasProperty("_BaseColor"))
      {
        material.SetColor("_BaseColor", color);
      }
    }
  }

  // - TESTING METHODS
  // Test reveal functionality
  public void TestReveal()
  {
    RevealPoint(1.0f);
  }

  // Test hide functionality
  public void TestHide()
  {
    HidePoint(1.0f);
  }

  // Test highlight functionality
  public void TestHighlight()
  {
    HighlightPoint();
  }
}