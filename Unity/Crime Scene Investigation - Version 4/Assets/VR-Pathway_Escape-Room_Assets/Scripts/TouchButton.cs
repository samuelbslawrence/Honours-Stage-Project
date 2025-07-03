using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Serialization;

namespace FinalExample
{

    public class TouchButton : XRBaseInteractable
    {
        [Header("Visuals")]
        public Material normalMaterial;
        public Material touchedMaterial;

        [Header("Button Data")]
        public int buttonNumber;
        public NumberPad linkedNumberpad;

        private int m_NumberOfInteractor = 0;
        private Renderer m_RendererToChange;

        private void Start()
        {
            m_RendererToChange = GetComponent<MeshRenderer>();
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);

            if (m_NumberOfInteractor == 0)
            {
                m_RendererToChange.material = touchedMaterial;

                linkedNumberpad.ButtonPressed(buttonNumber);
            }

            m_NumberOfInteractor += 1;
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);

            m_NumberOfInteractor -= 1;

            if (m_NumberOfInteractor == 0)
                m_RendererToChange.material = normalMaterial;
        }
    }
}