using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DustBrush : XRGrabInteractable
{
  [Header("Brush Settings")]
  public float brushRadius = 0.05f;
  public float brushForce = 1.0f;
  public LayerMask fingerprintLayer = 1;

  [Header("VR Haptics")]
  public float hapticIntensity = 0.3f;
  public float hapticDuration = 0.1f;

  [Header("Audio")]
  public AudioClip brushingSound;
  public AudioSource audioSource;

  [Header("Visual Effects")]
  public ParticleSystem dustParticles;
  public Transform brushTip;

  [Header("Integration")]
  public bool autoFindEvidenceChecklist = true;
  public EvidenceChecklist evidenceChecklist;

  private bool isBrushing = false;
  private XRBaseController controller;

  protected override void Awake()
  {
    base.Awake();

    if (!audioSource)
      audioSource = GetComponent<AudioSource>();

    if (!brushTip)
      brushTip = transform;

    // Auto-find evidence checklist
    if (autoFindEvidenceChecklist && evidenceChecklist == null)
    {
      evidenceChecklist = FindObjectOfType<EvidenceChecklist>();
    }
  }

  protected override void OnSelectEntered(SelectEnterEventArgs args)
  {
    base.OnSelectEntered(args);
    controller = args.interactorObject.transform.GetComponent<XRBaseController>();
    Debug.Log("Brush grabbed - ready to dust for fingerprints!");
  }

  protected override void OnSelectExited(SelectExitEventArgs args)
  {
    base.OnSelectExited(args);
    controller = null;
    StopBrushing();
  }

  void Update()
  {
    if (isSelected)
    {
      CheckForBrushing();
    }
  }

  void CheckForBrushing()
  {
    // Cast a sphere around the brush tip to detect fingerprints
    Collider[] fingerprints = Physics.OverlapSphere(brushTip.position, brushRadius, fingerprintLayer);

    if (fingerprints.Length > 0)
    {
      if (!isBrushing)
      {
        StartBrushing();
      }

      // Process each fingerprint found
      foreach (Collider fingerprintCollider in fingerprints)
      {
        FingerprintReveal fingerprint = fingerprintCollider.GetComponent<FingerprintReveal>();
        if (fingerprint != null)
        {
          Vector3 brushPosition = brushTip.position;
          fingerprint.BrushAtPosition(brushPosition, brushRadius, brushForce * Time.deltaTime);
        }
      }
    }
    else
    {
      if (isBrushing)
      {
        StopBrushing();
      }
    }
  }

  void StartBrushing()
  {
    isBrushing = true;

    // Start dust particles
    if (dustParticles && !dustParticles.isPlaying)
    {
      dustParticles.Play();
    }

    // Play brushing sound
    if (audioSource && brushingSound && !audioSource.isPlaying)
    {
      audioSource.clip = brushingSound;
      audioSource.loop = true;
      audioSource.Play();
    }

    // Haptic feedback
    TriggerHapticFeedback();
  }

  void StopBrushing()
  {
    isBrushing = false;

    // Stop dust particles
    if (dustParticles && dustParticles.isPlaying)
    {
      dustParticles.Stop();
    }

    // Stop brushing sound
    if (audioSource && audioSource.isPlaying)
    {
      audioSource.Stop();
    }
  }

  void TriggerHapticFeedback()
  {
    if (controller != null)
    {
      controller.SendHapticImpulse(hapticIntensity, hapticDuration);
    }
  }

  void OnDrawGizmosSelected()
  {
    if (brushTip)
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(brushTip.position, brushRadius);
    }
  }
}