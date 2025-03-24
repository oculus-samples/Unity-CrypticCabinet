// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Controls when to enable or disable the physics programmatically in a multiplayer session.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class PhysicsToggleSwitch : NetworkBehaviour
    {
        [SerializeField] private Grabbable m_grabbable;
        [SerializeField] private Transform m_switchArm;
        [SerializeField] private Transform m_switchVisuals;
        [SerializeField] private float m_flickedAngle = 13;

        public UnityEvent<bool> SwitchToggled;

        [Networked(OnChanged = nameof(RemoteChanged))]
        private bool SwitchState { get; set; }

        private Quaternion m_switchVisualsOriginalRotation;
        private OneGrabRotateTransformerExtended m_grabRotateTransformer;

        private void Start()
        {
            m_grabRotateTransformer = GetComponentInChildren<OneGrabRotateTransformerExtended>();
            m_grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
            m_switchVisualsOriginalRotation = m_switchVisuals.localRotation;
            SetSwitchState(SwitchState, true);
        }

        private static void RemoteChanged(Changed<PhysicsToggleSwitch> changed)
        {
            changed.Behaviour.SetSwitchState(changed.Behaviour.SwitchState, false);
        }

        public void SetSwitchState(bool toggled, bool updateState)
        {
            if (updateState)
            {
                SwitchState = toggled;
            }

            var angle = toggled ? m_flickedAngle : -m_flickedAngle;
            m_switchArm.localRotation = Quaternion.Euler(angle, 0, 0);
            if (m_grabRotateTransformer != null)
            {
                m_grabRotateTransformer.UpdateObjectConstrainedValue(angle);
            }
        }

        private void GrabbableOnWhenPointerEventRaised(PointerEvent obj)
        {
            if (obj.Data is GrabInteractor or HandGrabInteractor)
            {
                if (obj.Type == PointerEventType.Unselect)
                {
                    SnapSwitch();
                }
            }
        }

        private void Update()
        {
            m_switchVisuals.localRotation = m_switchVisualsOriginalRotation *
                                            Quaternion.Euler(m_switchArm.localRotation.eulerAngles.x, 0, 0);
        }

        private void SnapSwitch()
        {
            var eulerAnglesX = m_switchArm.localRotation.eulerAngles.x;
            var newSwitchState = eulerAnglesX is > 0 and < 180;
            SetSwitchState(newSwitchState, false);

            if (SwitchState != newSwitchState)
            {
                SwitchState = newSwitchState;
                SwitchToggled?.Invoke(newSwitchState);
            }
        }
    }
}