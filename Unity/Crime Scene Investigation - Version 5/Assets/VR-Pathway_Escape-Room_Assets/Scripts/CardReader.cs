using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Serialization;

namespace FinalExample
{

    public class CardReader : XRSocketInteractor
    {
        [Header("CardReader ReaderOptions Data")]
        public float allowedUprightErrorRange = 0.2f;

        [Header("Success References")]
        public GameObject visualLockToHide;
        public MonoBehaviour handleToEnable;

        private Vector3 m_HoverEntry;
        private bool m_SwipIsValid;

        private Transform m_KeycardTransform;

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            //we disable selection, we just want this interactor to detect hovering
            return false;
        }

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            //if out interactable isn't of Keycard type, we ignore it, we are only interested in Keycard.
            return interactable is Keycard;
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);

            m_KeycardTransform = args.interactableObject.transform;
            m_HoverEntry = m_KeycardTransform.position;
            m_SwipIsValid = true;
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);


            Vector3 entryToExit = m_KeycardTransform.position - m_HoverEntry;

            //if our swip is valid (i.e. our card stayed upright, see Update function) and we swiped at least 0.15 unit down,
            //then unlock the door. 0.15 unit down is hardcoded for simplicity but we could make that an exposed variable
            //to setup in the inspector, or even look at the collider size or any other way we want to make it dynamic
            if (m_SwipIsValid && entryToExit.y < -0.15f)
            {
                visualLockToHide.gameObject.SetActive(false);
                handleToEnable.enabled = true;
            }

            //we just moved out, we set back our keycard transform to null so Update know there is no more keycard to check
            m_KeycardTransform = null;
        }

        private void Update()
        {
            //only if we currently have a card hovering us. We stored that value in OnHoverEntered and set it to null in
            //OnHoverExit
            if (m_KeycardTransform != null)
            {
                //the way our model is made (flat on a surface), the blue vector (z, forward) is the "up" of the card, it
                //should be pointing up when the card is in the right orientation
                Vector3 keycardUp = m_KeycardTransform.forward;

                //dot product "project" the left vector onto the right vector, the value being a ratio of the right vector
                //(e.g. a value of 0.5 mean 0.5*length of right vector). So if both our vector are unit vector (which is the
                //case here) the value will go from 1 (the two vector are parallels going in the same direction) to -1
                //(parallel but in opposite direction) with 0 meaning they are perpendicular.
                float dot = Vector3.Dot(keycardUp, Vector3.up);

                //if we are further away from 1 (parallel in the same direction as up) than the allowed error range, our card
                //isn't upright enough for this to be a valid swip
                if (dot < 1 - allowedUprightErrorRange)
                {
                    m_SwipIsValid = false;
                }
            }
        }
    }
}