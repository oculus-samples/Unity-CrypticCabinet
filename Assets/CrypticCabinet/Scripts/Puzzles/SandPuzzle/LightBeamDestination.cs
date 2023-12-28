// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Passthrough;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Represents the intended destination of the light beam.
    ///     Attach this to the intended object that needs to be hit by the light beam last to trigger an event.
    ///     Requiring Rigidbody to ensure it can receive a raycast.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LightBeamDestination : MonoBehaviour
    {
        [SerializeField] private UnityEvent m_onLightBeamReceived;
        [SerializeField] private UnityEvent m_onLightBeamLeft;

        private bool m_isLitByLightBeam;

        private void Start()
        {
            m_isLitByLightBeam = false;
            m_onLightBeamLeft.AddListener(PassthroughChanger.Instance.SetPassthroughDarkerRoomLut);
            m_onLightBeamReceived.AddListener(PassthroughChanger.Instance.SetPassthroughDarkerRoomLut);
        }

        private void OnDestroy()
        {
            m_onLightBeamLeft.RemoveListener(PassthroughChanger.Instance.SetPassthroughDarkerRoomLut);
            m_onLightBeamReceived.RemoveListener(PassthroughChanger.Instance.SetPassthroughDarkerRoomLut);
            PassthroughChanger.Instance.SetPassthroughDefaultLut();
        }

        /// <summary>
        ///     Triggers the action that will be performed once this object received the light beam.
        /// </summary>
        [ContextMenu("Light Beam Arrived")]
        public void LightBeamArrived()
        {
            if (!enabled)
            {
                return;
            }

            if (m_isLitByLightBeam)
            {
                return;
            }

            Debug.Log("Light beam arrived to " + gameObject.name + ", triggering.");
            m_onLightBeamReceived?.Invoke();
            m_isLitByLightBeam = true;
        }

        /// <summary>
        ///     Un-triggers the action that was performed when the object received the light beam.
        /// </summary>
        [ContextMenu("Light Beam Left")]
        public void LightBeamLeft()
        {
            if (!enabled)
            {
                return;
            }

            if (!m_isLitByLightBeam)
            {
                return;
            }

            Debug.Log("Light beam left from " + gameObject.name + ", un-triggering.");
            m_onLightBeamLeft?.Invoke();
            m_isLitByLightBeam = false;
        }
    }
}
