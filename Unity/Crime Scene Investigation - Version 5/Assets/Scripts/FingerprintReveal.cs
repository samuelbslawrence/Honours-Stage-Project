using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerprintReveal : MonoBehaviour
{
  [Header("Reveal Settings")]
  public float revealThreshold = 0.7f;
  public bool isRevealed = false;
  public bool isFullyRevealed = false;

  [Header("Visual Settings")]
  public Renderer fingerprintRenderer;
  public Material hiddenMaterial;
  public Material revealedMaterial;

  [Header("Audio")]
  public AudioClip revealCompleteSound;
  public AudioSource audioSource;

  [Header("Evidence Integration")]
  public string evidenceName = "Fingerprint";
  public bool autoFindEvidenceChecklist = true;
  public EvidenceChecklist evidenceChecklist;

  [Header("Events")]
  public UnityEngine.Events.UnityEvent OnFingerprintRevealed;
  public UnityEngine.Events.UnityEvent OnFingerprintFullyRevealed;

  private Texture2D dynamicMask;
  private float[] maskData;
  private int maskWidth = 256;
  private int maskHeight = 256;
  private float currentRevealAmount = 0f;
  private Material materialInstance;
  private Collider fingerprintCollider;
  private bool hasBeenReported = false;

  void Start()
  {
    SetupFingerprint();
  }

  void SetupFingerprint()
  {
    // Auto-find evidence checklist
    if (autoFindEvidenceChecklist && evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
    }

    fingerprintCollider = GetComponent<Collider>();
    if (!fingerprintCollider)
    {
      fingerprintCollider = gameObject.AddComponent<BoxCollider>();
    }

    if (!fingerprintRenderer)
      fingerprintRenderer = GetComponent<Renderer>();

    if (!audioSource)
      audioSource = GetComponent<AudioSource>();

    // Make sure object has Evidence tag
    if (!gameObject.CompareTag("Evidence"))
    {
      gameObject.tag = "Evidence";
    }

    // Create dynamic reveal mask
    CreateDynamicMask();

    // Setup material
    SetupMaterial();

    // Start hidden
    SetVisibility(false);
  }

  void CreateDynamicMask()
  {
    dynamicMask = new Texture2D(maskWidth, maskHeight, TextureFormat.RGB24, false);
    maskData = new float[maskWidth * maskHeight];

    // Initialize all pixels as black (hidden)
    Color[] pixels = new Color[maskWidth * maskHeight];
    for (int i = 0; i < pixels.Length; i++)
    {
      pixels[i] = Color.black;
      maskData[i] = 0f;
    }

    dynamicMask.SetPixels(pixels);
    dynamicMask.Apply();
  }

  void SetupMaterial()
  {
    if (fingerprintRenderer && revealedMaterial)
    {
      materialInstance = new Material(revealedMaterial);
      materialInstance.SetTexture("_RevealMask", dynamicMask);
      fingerprintRenderer.material = materialInstance;
    }
  }

  public void BrushAtPosition(Vector3 worldPosition, float brushRadius, float brushStrength)
  {
    if (isFullyRevealed) return;

    // Convert world position to UV coordinates
    Vector2 uv = WorldToUV(worldPosition);

    if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
    {
      RevealAtUV(uv, brushRadius, brushStrength);
    }
  }

  Vector2 WorldToUV(Vector3 worldPosition)
  {
    // Convert world position to local position
    Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

    // Convert to UV (assuming the fingerprint is on a quad/plane)
    float u = (localPosition.x + 0.5f);
    float v = (localPosition.z + 0.5f);

    return new Vector2(u, v);
  }

  void RevealAtUV(Vector2 uv, float brushRadius, float brushStrength)
  {
    int centerX = Mathf.RoundToInt(uv.x * maskWidth);
    int centerY = Mathf.RoundToInt(uv.y * maskHeight);

    int radiusPixels = Mathf.RoundToInt(brushRadius * maskWidth * 10f);

    bool maskChanged = false;

    for (int x = centerX - radiusPixels; x <= centerX + radiusPixels; x++)
    {
      for (int y = centerY - radiusPixels; y <= centerY + radiusPixels; y++)
      {
        if (x >= 0 && x < maskWidth && y >= 0 && y < maskHeight)
        {
          float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

          if (distance <= radiusPixels)
          {
            int index = y * maskWidth + x;
            float falloff = 1f - (distance / radiusPixels);

            maskData[index] = Mathf.Min(1f, maskData[index] + brushStrength * falloff);
            maskChanged = true;
          }
        }
      }
    }

    if (maskChanged)
    {
      UpdateMask();
      CheckRevealStatus();
    }
  }

  void UpdateMask()
  {
    Color[] pixels = new Color[maskWidth * maskHeight];

    for (int i = 0; i < maskData.Length; i++)
    {
      float value = maskData[i];
      pixels[i] = new Color(value, value, value, 1f);
    }

    dynamicMask.SetPixels(pixels);
    dynamicMask.Apply();

    if (materialInstance)
    {
      materialInstance.SetTexture("_RevealMask", dynamicMask);
    }
  }

  void CheckRevealStatus()
  {
    // Calculate how much has been revealed
    float totalRevealed = 0f;
    for (int i = 0; i < maskData.Length; i++)
    {
      totalRevealed += maskData[i];
    }

    currentRevealAmount = totalRevealed / maskData.Length;

    // Check if fingerprint should be considered "revealed"
    if (!isRevealed && currentRevealAmount >= revealThreshold)
    {
      isRevealed = true;
      SetVisibility(true);
      OnFingerprintRevealed?.Invoke();

      // Notify your evidence system
      NotifyEvidenceSystem();
    }

    // Check if fully revealed
    if (!isFullyRevealed && currentRevealAmount >= 0.9f)
    {
      isFullyRevealed = true;
      OnFingerprintFullyRevealed?.Invoke();

      if (audioSource && revealCompleteSound)
      {
        audioSource.PlayOneShot(revealCompleteSound);
      }
    }
  }

  void SetVisibility(bool visible)
  {
    if (fingerprintRenderer)
    {
      fingerprintRenderer.enabled = visible;
    }
  }

  void NotifyEvidenceSystem()
  {
    if (hasBeenReported) return;
    hasBeenReported = true;

    // Notify your evidence checklist
    if (evidenceChecklist != null)
    {
      evidenceChecklist.MarkEvidenceAsFound(evidenceName);
      Debug.Log($"Fingerprint evidence revealed and reported to checklist: {evidenceName}");
    }
    else
    {
      Debug.LogWarning("EvidenceChecklist not found! Fingerprint evidence discovered but not tracked.");
    }

    Debug.Log($"Fingerprint evidence discovered: {evidenceName}");
  }

  // Public methods for testing/debugging
  [ContextMenu("Reveal Instantly")]
  public void RevealInstantly()
  {
    for (int i = 0; i < maskData.Length; i++)
    {
      maskData[i] = 1f;
    }
    UpdateMask();
    CheckRevealStatus();
  }

  [ContextMenu("Hide Completely")]
  public void HideCompletely()
  {
    for (int i = 0; i < maskData.Length; i++)
    {
      maskData[i] = 0f;
    }
    isRevealed = false;
    isFullyRevealed = false;
    currentRevealAmount = 0f;
    hasBeenReported = false;
    UpdateMask();
    SetVisibility(false);
  }

  void OnDestroy()
  {
    if (materialInstance)
      DestroyImmediate(materialInstance);
    if (dynamicMask)
      DestroyImmediate(dynamicMask);
  }
}