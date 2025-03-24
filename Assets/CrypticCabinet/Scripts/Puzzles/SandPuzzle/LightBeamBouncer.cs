// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Component giving the object the ability to bounce the light beam
    ///     Requiring Rigid body to ensure it can receive a raycast
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Rigidbody))]
    public class LightBeamBouncer : MonoBehaviour
    {
        private const float UPDATE_POSITION_TIMER = 0.1f;

        private Grabbable m_grabbable;
        private float m_nextUpdateTime;
        private Vector3 m_lastPosition;
        private Quaternion m_lastRotation;
        private bool m_isSelected = false;

        private void Start()
        {
            m_isSelected = false;
            m_grabbable = GetComponent<Grabbable>();
            m_grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
        }

        private void Update()
        {
            if (m_isSelected && Time.time >= m_nextUpdateTime)
            {
                UpdateLastPositionAndRotation();
                m_nextUpdateTime = Time.time + UPDATE_POSITION_TIMER;
            }
        }

        private void UpdateLastPositionAndRotation()
        {
            var tempTransform = transform;
            m_lastPosition = tempTransform.position;
            m_lastRotation = tempTransform.rotation;
        }

        private void SetToLastPositionAndRotation()
        {
            var tempTransform = transform;
            tempTransform.position = m_lastPosition;
            tempTransform.rotation = m_lastRotation;
        }

        private void GrabbableOnWhenPointerEventRaised(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Hover:
                    m_isSelected = true;
                    break;
                case PointerEventType.Unhover:
                    m_isSelected = false;
                    break;
                case PointerEventType.Select:
                    m_isSelected = true;
                    break;
                case PointerEventType.Unselect:
                    m_isSelected = false;
                    SetToLastPositionAndRotation();
                    break;
                case PointerEventType.Move:
                    m_isSelected = true;
                    break;
                case PointerEventType.Cancel:
                    m_isSelected = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}