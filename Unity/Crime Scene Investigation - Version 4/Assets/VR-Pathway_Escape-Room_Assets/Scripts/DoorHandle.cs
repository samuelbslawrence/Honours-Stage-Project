using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Serialization;

namespace FinalExample
{

    public class DoorHandle : XRBaseInteractable
    {
        [Header("Door Handle Data")]
        public Transform draggedTransform;

        public Vector3 localDragDirection;
        public float dragDistance;

        //arbitrary unit, not matching any "physic" reality, just a factor of how "heavy" the door is to pull
        public int doorWeight = 20;


        // ================== EXTENSION FOR THE VISUAL LINE ==========================
        [Header("Visual References")]
        public LineRenderer handleToHandLine;
        public LineRenderer dragVectorLine;
        //============================================================================

        private Vector3 m_StartPosition;
        private Vector3 m_EndPosition;

        private Vector3 m_WorldDragDirection;

        private void Start()
        {
            //we cache that, meaning we don't expect the draggable to rotate during its lifetime. If we wanted to support that,
            //we would need to transform from local to world in the ProcessInteractable every frame.
            m_WorldDragDirection = transform.TransformDirection(localDragDirection).normalized;


            //we store the start and end position of the drag, as the object will move as it is dragged
            m_StartPosition = draggedTransform.position;
            m_EndPosition = m_StartPosition + m_WorldDragDirection * dragDistance;

            // ================== EXTENSION FOR THE VISUAL LINE ==========================
            handleToHandLine.gameObject.SetActive(false);
            dragVectorLine.gameObject.SetActive(false);
            //============================================================================
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed && isSelected)
            {
                var interactorTransform = firstInteractorSelecting.GetAttachTransform(this);

                //we get the vector that goes from this to the interactor
                Vector3 selfToInteractor = interactorTransform.position - transform.position;

                //project onto the movement vector
                float forceInDirectionOfDrag = Vector3.Dot(selfToInteractor, m_WorldDragDirection);

                //we then need to check in which direction are we dragging : toward the end (positive direction) or toward
                //the start (megative direction)
                bool dragToEnd = forceInDirectionOfDrag > 0.0f;

                //we take the absolute of that value now, as we need a speed, not a direction anymore
                float absoluteForce = Mathf.Abs(forceInDirectionOfDrag);

                //we transform our force into a speed (by dividing it by delta Time). Then we "scale" that speed by the door
                //weight. The "heavier" the door, the lower the speed will be.
                float speed = absoluteForce / Time.deltaTime / doorWeight;

                //finally we move the target either toward end or start based on the speed.
                draggedTransform.position = Vector3.MoveTowards(draggedTransform.position,
                    //the target depend on the direction of drag we recovered earlier
                    dragToEnd ? m_EndPosition : m_StartPosition,
                    speed * Time.deltaTime);

                // ================== EXTENSION FOR THE VISUAL LINE ==========================

                handleToHandLine.SetPosition(0, transform.position);
                handleToHandLine.SetPosition(1, interactorTransform.position);

                //to be sure to see it we offset it a bit back on x so it is not IN the door 
                dragVectorLine.SetPosition(0, transform.position);
                dragVectorLine.SetPosition(1, transform.position + forceInDirectionOfDrag * m_WorldDragDirection);

                // ===========================================================================
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 worldDirection = transform.TransformDirection(localDragDirection);
            //make sure this is a unit vector, so when we multiply by the drag distance we get the right distance.
            worldDirection.Normalize();

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + worldDirection * dragDistance);
        }


        // ================== EXTENSION FOR THE VISUAL LINE ==========================
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            handleToHandLine.gameObject.SetActive(true);
            dragVectorLine.gameObject.SetActive(true);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            handleToHandLine.gameObject.SetActive(false);
            dragVectorLine.gameObject.SetActive(false);
        }
        //============================================================================
    }
}