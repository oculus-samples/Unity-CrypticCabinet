// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Represents a point of the rope that can be grabbed by the user.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class RopeGrabPoint : MonoBehaviour
    {
        [SerializeField] private Rope m_rope;
        [SerializeField] private bool m_isRight;
        private int m_ropeIndex = -1;

        private void Update()
        {
            var handPos = GetInputDevicePos();
            if (m_ropeIndex >= 0)
            {
                m_rope.UpdateGrabbedRope(m_ropeIndex, transform.position);
            }
            else
            {
                var nearestPointOnRope = m_rope.NearestPointOnRope(handPos);
                transform.position = nearestPointOnRope;
            }
        }

        public void Grabbed()
        {
            m_ropeIndex = m_rope.GrabRopeAtPoint(transform.position);
        }

        public void Release()
        {
            m_rope.ReleaseRope(m_ropeIndex);
            m_ropeIndex = -1;
        }

        private Vector3 GetInputDevicePos()
        {
            if (m_isRight)
            {
                var controller = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                var hand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand);
                return Mathf.Abs(controller.sqrMagnitude) > Mathf.Abs(hand.sqrMagnitude) ? controller : hand;
            }
            else
            {
                var controller = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
                var hand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand);
                return Mathf.Abs(controller.sqrMagnitude) > Mathf.Abs(hand.sqrMagnitude) ? controller : hand;
            }
        }
    }
}