using UnityEngine;

public class FingerprintReveal : MonoBehaviour
{
  [Header("Fingerprint Materials")]
  public Material beforeMaterial;  // Hidden/dusty fingerprint material
  public Material afterMaterial;   // Revealed fingerprint material

  [Header("Reveal Settings")]
  public float revealThreshold = 1.0f;  // How much brushing needed to fully reveal

  private float currentRevealAmount = 0f;
  private Renderer fingerprintRenderer;
  private bool isFullyRevealed = false;

  void Start()
  {
    fingerprintRenderer = GetComponent<Renderer>();

    // Start with the "before" material (hidden fingerprint)
    if (beforeMaterial != null && fingerprintRenderer != null)
    {
      fingerprintRenderer.material = beforeMaterial;
    }
  }

  public void BrushAtPosition(Vector3 brushPosition, float brushRadius, float brushForce)
  {
    if (isFullyRevealed) return;

    // Check if brush is close enough to this fingerprint
    float distance = Vector3.Distance(transform.position, brushPosition);
    if (distance <= brushRadius)
    {
      // Add to reveal progress based on brush force
      currentRevealAmount += brushForce;

      // Check if we've reached the reveal threshold
      if (currentRevealAmount >= revealThreshold)
      {
        RevealFingerprint();
      }
    }
  }

  void RevealFingerprint()
  {
    if (isFullyRevealed) return;

    isFullyRevealed = true;

    // Switch to the "after" material (revealed fingerprint)
    if (afterMaterial != null && fingerprintRenderer != null)
    {
      fingerprintRenderer.material = afterMaterial;
    }

    Debug.Log($"Fingerprint revealed: {gameObject.name}");

    // Optional: Notify evidence checklist if available
    EvidenceChecklist checklist = FindObjectOfType<EvidenceChecklist>();
    if (checklist != null)
    {
      // You can add a method to the checklist to mark this evidence as found
      // checklist.MarkEvidenceFound("fingerprint");
    }
  }

  // Public method to check if fingerprint is revealed (useful for game logic)
  public bool IsRevealed()
  {
    return isFullyRevealed;
  }

  // Method to reset fingerprint (useful for testing or restarting scenes)
  public void ResetFingerprint()
  {
    isFullyRevealed = false;
    currentRevealAmount = 0f;

    if (beforeMaterial != null && fingerprintRenderer != null)
    {
      fingerprintRenderer.material = beforeMaterial;
    }
  }
}