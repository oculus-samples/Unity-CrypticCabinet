// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    [MetaCodeSample("CrypticCabinet")]
    public class DistanceChecker : MonoBehaviour
    {
        [SerializeField] private Transform m_ropeRoot;
        [SerializeField] private float m_distance = 2.2f;

        private List<Grabbable> m_grabbables = new();
        private PointerEvent? m_currentGrab;
        private Transform m_parentTransform;
        private bool m_isGrabbed = false;

        private void Start()
        {
            m_parentTransform = transform.parent;
        }

        private void FixedUpdate()
        {
            if (!m_isGrabbed)
                return;

            var dist = Vector3.Distance(transform.position, m_ropeRoot.position);
            if (dist > m_distance)
            {
                ForceUnselect();
            }
        }

        public void Grabbed(PointerEvent evt)
        {
            m_isGrabbed = true;
            m_currentGrab = evt;
            m_grabbables = m_parentTransform.GetComponentsInChildren<Grabbable>().ToList();
        }

        public void Released(PointerEvent evt)
        {
            m_isGrabbed = false;
            m_currentGrab = null;
            m_grabbables.Clear();
        }

        private void ForceUnselect()
        {
            if (m_currentGrab == null)
                return;

            if (m_grabbables == null)
                return;

            if (m_grabbables.Count <= 0)
                return;

            foreach (var t in m_grabbables.ToList())
            {
                t.ProcessPointerEvent(new PointerEvent(m_currentGrab.Value.Identifier, PointerEventType.Unselect, m_currentGrab.Value.Pose));
                t.ProcessPointerEvent(new PointerEvent(m_currentGrab.Value.Identifier, PointerEventType.Unhover, m_currentGrab.Value.Pose));
            }
        }
    }
}
