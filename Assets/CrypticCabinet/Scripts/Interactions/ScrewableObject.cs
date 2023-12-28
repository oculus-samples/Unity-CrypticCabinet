// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Interactions
{
    /// <summary>
    ///     Simulates the motion of a screwable object onto a screw snap zone.
    /// </summary>
    [RequireComponent(typeof(OneGrabToggleRotateTransformer))]
    public class ScrewableObject : NetworkBehaviour
    {
        [SerializeField] private OneGrabToggleRotateTransformer m_transformer;
        [SerializeField] private ScrewSnapZone m_startingSnapZone;
        [SerializeField] private float m_screwCompleteDeadZone = 5.0f;
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private Grabbable m_grabbable;

        /// <summary>
        ///     Fired when the screwable object is completely screwed into the screw snap zone.
        /// </summary>
        [SerializeField] private UnityEvent m_onScrewComplete;

        private const int CURRENT_SCREW_ANGLE_THRESHOLD = 10;

        private float CurrentScrewAngle { get; set; }

        private bool m_isIntersectingScrewSnapZone;
        private ScrewSnapZone m_currentScrewSnapZone;
        private bool m_screwCompleteCallbackFired;
        [Networked] private RigidbodyConstraints OriginalRigidbodyConstraints { get; set; }
        private float m_turnPercentage;
        private const RigidbodyConstraints FROZEN_CONSTRAINTS = RigidbodyConstraints.FreezePosition
                                                                | RigidbodyConstraints.FreezeRotationX
                                                                | RigidbodyConstraints.FreezeRotationZ;

        private void Start()
        {
            if (m_rigidbody == null)
            {
                Debug.LogError("No Rigidbody found for this screwable object.", gameObject);
            }
            else
            {
                OriginalRigidbodyConstraints = m_rigidbody.constraints;
            }


            if (m_grabbable == null)
            {
                Debug.LogError("No grabbable found for this screwable object.", gameObject);
            }
            else
            {
                m_grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
            }

            if (m_startingSnapZone == null)
            {
                return;
            }

            if (m_rigidbody != null)
            {
                m_rigidbody.constraints = FROZEN_CONSTRAINTS;
            }

            m_isIntersectingScrewSnapZone = true;
            m_currentScrewSnapZone = m_startingSnapZone;
            m_startingSnapZone.CurrentObject = this;

            m_transformer.InjectOptionalPivotTransform(m_currentScrewSnapZone.transform);
            m_transformer.LockPosition = true;
            m_transformer.CanUnlockPosition = false;

            var thisTransform = transform;
            thisTransform.position = m_currentScrewSnapZone.GuideTopPosition;
            var currentScrewSnapZoneTransform = m_currentScrewSnapZone.transform;
            thisTransform.rotation = currentScrewSnapZoneTransform.rotation * Quaternion.AngleAxis(
                m_transformer.Constraints.MaxAngle.Value, currentScrewSnapZoneTransform.up);

            CurrentScrewAngle = m_transformer.Constraints.MaxAngle.Value;
            m_transformer.ConstrainedRelativeAngle = CurrentScrewAngle;
        }

        private void OnDestroy()
        {
            if (m_grabbable != null)
            {
                m_grabbable.WhenPointerEventRaised -= GrabbableOnWhenPointerEventRaised;
            }
        }

        private void GrabbableOnWhenPointerEventRaised(PointerEvent obj)
        {
            if (obj.Type == PointerEventType.Select)
            {
                m_turnPercentage = Mathf.InverseLerp(
                    m_transformer.Constraints.MinAngle.Value,
                    m_transformer.Constraints.MaxAngle.Value,
                    CurrentScrewAngle);

                if (m_turnPercentage < 0.1f)
                {
                    m_transformer.RotationDirectionLimit = OneGrabToggleRotateTransformer.RotationDirection.POSITIVE;
                }
                else if (m_turnPercentage > 0.9f)
                {
                    m_transformer.RotationDirectionLimit = OneGrabToggleRotateTransformer.RotationDirection.NEGATIVE;
                }
            }
        }

        private void LateUpdate()
        {
            if (m_isIntersectingScrewSnapZone && m_transformer.LockPosition)
            {
                UpdateScrewPosition();
            }
        }

        private void UpdateScrewPosition()
        {
            CurrentScrewAngle = m_transformer.ConstrainedRelativeAngle;
            m_transformer.CanUnlockPosition = CurrentScrewAngle < CURRENT_SCREW_ANGLE_THRESHOLD;
            m_turnPercentage = Mathf.InverseLerp(
                m_transformer.Constraints.MinAngle.Value,
                m_transformer.Constraints.MaxAngle.Value,
                CurrentScrewAngle);

            var pos = Vector3.Lerp(
                m_currentScrewSnapZone.GuideTopPosition,
                m_currentScrewSnapZone.GuideBottomPosition,
                m_turnPercentage);

            transform.position = pos;

            if (CurrentScrewAngle >= (m_transformer.Constraints.MaxAngle.Value - m_screwCompleteDeadZone))
            {
                if (!m_screwCompleteCallbackFired)
                {
                    m_screwCompleteCallbackFired = true;
                    m_onScrewComplete?.Invoke();
                    m_currentScrewSnapZone.OnObjectCompleteScrew?.Invoke(this);
                }
            }
            else
            {
                if (m_screwCompleteCallbackFired)
                {
                    m_currentScrewSnapZone.OnObjectStartUnscrew?.Invoke(this);
                }

                m_screwCompleteCallbackFired = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerEnterStay(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerEnterStay(other);
        }

        private void TriggerEnterStay(Collider other)
        {
            if (m_isIntersectingScrewSnapZone)
            {
                return;
            }

            var screwSnapZone = other.GetComponent<ScrewSnapZone>();
            if (screwSnapZone != null && !screwSnapZone.HasObject)
            {
                if (m_rigidbody != null)
                {
                    m_rigidbody.constraints = FROZEN_CONSTRAINTS;
                }

                m_isIntersectingScrewSnapZone = true;
                m_currentScrewSnapZone = screwSnapZone;
                screwSnapZone.CurrentObject = this;

                m_transformer.InjectOptionalPivotTransform(m_currentScrewSnapZone.transform);
                m_transformer.LockPosition = true;
                m_transformer.CanUnlockPosition = false;

                var thisTransform = transform;
                thisTransform.position = m_currentScrewSnapZone.GuideTopPosition;
                thisTransform.rotation = m_currentScrewSnapZone.transform.rotation;
                CurrentScrewAngle = 0;

                m_screwCompleteCallbackFired = false;

                screwSnapZone.OnObjectSnap?.Invoke(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var screwSnapZone = other.GetComponent<ScrewSnapZone>();
            if (screwSnapZone == null || !screwSnapZone.HasObject)
            {
                return;
            }

            if (screwSnapZone.CurrentObject == this)
            {
                if (m_rigidbody != null)
                {
                    m_rigidbody.constraints = OriginalRigidbodyConstraints;
                }

                m_isIntersectingScrewSnapZone = false;
                m_currentScrewSnapZone = null;
                screwSnapZone.CurrentObject = null;
                screwSnapZone.OnObjectRemoved?.Invoke(this);
            }
        }
    }
}