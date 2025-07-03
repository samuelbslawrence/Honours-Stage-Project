using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPPoint : MonoBehaviour
{
  [Header("Visual Settings")]
  [SerializeField] private float alphaThresholdDefault = 1.0f; // Fully hidden
  [SerializeField] private float alphaThresholdRevealed = 0.0f; // Fully visible
  [SerializeField] private float alphaThresholdHighlighted = 0.0f; // Highlighted state
  [SerializeField] private Color defaultColor = Color.blue;
  [SerializeField] private Color highlightedColor = Color.green;
  [SerializeField] private string alphaThresholdProperty = "_Cutoff"; // Default property name

  // Internal references
  private Renderer[] renderers;
  private List<Material> materials = new List<Material>();
  private Coroutine currentAnimation = null;
  private bool initialized = false;

  public void Initialize(string thresholdPropertyName)
  {
    // Set the shader property name if provided
    if (!string.IsNullOrEmpty(thresholdPropertyName))
    {
      alphaThresholdProperty = thresholdPropertyName;
    }

    // Get all renderers
    renderers = GetComponentsInChildren<Renderer>(true);

    // Collect all materials
    materials.Clear();
    foreach (Renderer renderer in renderers)
    {
      // Create instance materials to avoid affecting shared materials
      Material[] instanceMaterials = renderer.materials;

      for (int i = 0; i < instanceMaterials.Length; i++)
      {
        Material mat = instanceMaterials[i];

        // Try different common alpha threshold property names
        string[] possibleProperties = new string[] { "_Cutoff", "_AlphaClip", "_Cutout", "_AlphaTest", "_AlphaCutoff" };
        string foundProperty = "";

        foreach (string prop in possibleProperties)
        {
          if (mat.HasProperty(prop))
          {
            foundProperty = prop;
            alphaThresholdProperty = prop;
            break;
          }
        }

        if (!string.IsNullOrEmpty(foundProperty))
        {
          materials.Add(mat);
          Debug.Log("Found alpha property " + foundProperty + " on " + renderer.name);

          // Set initial value
          mat.SetFloat(foundProperty, alphaThresholdDefault);
        }
        else
        {
          Debug.LogWarning("No alpha threshold property found on " + renderer.name);
        }
      }

      // Apply the instance materials back to the renderer
      renderer.materials = instanceMaterials;
    }

    // If no materials found with alpha threshold, log a warning
    if (materials.Count == 0)
    {
      Debug.LogWarning("No materials with alpha threshold property found on " + name);
    }
    else
    {
      // Force-hide the teleport point initially
      SetAlphaThreshold(alphaThresholdDefault);
      Debug.Log(name + ": Set initial alpha threshold to " + alphaThresholdDefault);
    }

    initialized = true;
  }

  public void RevealPoint(float duration)
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    if (currentAnimation != null)
    {
      StopCoroutine(currentAnimation);
    }

    // Start animation to reveal
    currentAnimation = StartCoroutine(AnimateAlphaThreshold(alphaThresholdRevealed, duration));
  }

  public void HidePoint(float duration)
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    if (currentAnimation != null)
    {
      StopCoroutine(currentAnimation);
    }

    // Start animation to hide
    currentAnimation = StartCoroutine(AnimateAlphaThreshold(alphaThresholdDefault, duration));
  }

  // Force immediate hiding without animation
  public void ForceHidePoint()
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    if (currentAnimation != null)
    {
      StopCoroutine(currentAnimation);
      currentAnimation = null;
    }

    // Immediately set to hidden
    SetAlphaThreshold(alphaThresholdDefault);
  }

  public void HighlightPoint()
  {
    if (!initialized)
    {
      Initialize(alphaThresholdProperty);
    }

    // Stop any ongoing animations
    if (currentAnimation != null)
    {
      StopCoroutine(currentAnimation);
    }

    // Set to highlighted state
    SetAlphaThreshold(alphaThresholdHighlighted);

    // Apply highlight color if it's different from default
    if (highlightedColor != defaultColor)
    {
      SetColor(highlightedColor);
    }
  }

  void SetAlphaThreshold(float threshold)
  {
    // Apply to all materials
    foreach (Material material in materials)
    {
      material.SetFloat(alphaThresholdProperty, threshold);
    }
  }

  void SetColor(Color color)
  {
    // Apply to all materials
    foreach (Material material in materials)
    {
      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", color);
      }
      else if (material.HasProperty("_BaseColor"))  // For URP materials
      {
        material.SetColor("_BaseColor", color);
      }
    }
  }

  IEnumerator AnimateAlphaThreshold(float targetThreshold, float duration)
  {
    // Skip animation if we have no materials
    if (materials.Count == 0)
    {
      currentAnimation = null;
      yield break;
    }

    // Get the current alpha threshold from the first material
    float startThreshold = materials[0].GetFloat(alphaThresholdProperty);

    float time = 0;

    while (time < duration)
    {
      // Calculate progress (0 to 1)
      float t = time / duration;

      // Smooth easing
      float smoothT = Mathf.SmoothStep(0, 1, t);

      // Calculate and set the new threshold
      float newThreshold = Mathf.Lerp(startThreshold, targetThreshold, smoothT);
      SetAlphaThreshold(newThreshold);

      // Wait for the next frame
      yield return null;

      // Update time
      time += Time.deltaTime;
    }

    // Ensure final value
    SetAlphaThreshold(targetThreshold);
    Debug.Log(name + ": Alpha threshold animation complete. Value = " + targetThreshold);

    // Clear current animation reference
    currentAnimation = null;
  }

  // For debugging from the Inspector
  public void TestReveal()
  {
    RevealPoint(1.0f);
  }

  public void TestHide()
  {
    HidePoint(1.0f);
  }

  public void TestHighlight()
  {
    HighlightPoint();
  }
}