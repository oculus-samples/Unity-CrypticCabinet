// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils.InteractiveObjects
{
    /// <summary>
    ///     Represents an interactive button switch.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class MeshButtonSwitch : NetworkBehaviour
    {
        [SerializeField] private UnityEvent m_onSwitchedOn;
        [SerializeField] private UnityEvent m_onSwitchedOff;

        [SerializeField] private GameObject m_switchButtonMesh;

        [SerializeField] private float m_switchSpeed = 5f;

        [Networked(OnChanged = nameof(OnSwitchChanged)), SerializeField]
        private bool IsSwitchedOn { get; set; }

        [SerializeField] private Quaternion m_targetLocalRotationOn;
        [SerializeField] private Quaternion m_targetLocalRotationOff;

        private void Start()
        {
            if (m_switchButtonMesh == null)
            {
                Debug.LogError("Switch button mesh not set!");
            }

            // Update initial position of the switch button mesh
            m_switchButtonMesh.transform.localRotation = IsSwitchedOn ? m_targetLocalRotationOn : m_targetLocalRotationOff;

        }

        public void ToggleSwitch()
        {
            IsSwitchedOn = !IsSwitchedOn;
            TriggerCallbacks();
        }

        public void SwitchOn()
        {
            if (IsSwitchedOn)
            {
                return;
            }

            IsSwitchedOn = true;
            TriggerCallbacks();
        }

        public void SwitchOff()
        {
            if (!IsSwitchedOn)
            {
                return;
            }

            IsSwitchedOn = false;
            TriggerCallbacks();
        }

        private static void OnSwitchChanged(Changed<MeshButtonSwitch> changed)
        {
            changed.Behaviour.UpdateButtonVisuals();
        }

        private void TriggerCallbacks()
        {
            if (IsSwitchedOn)
            {
                m_onSwitchedOn?.Invoke();
            }
            else
            {
                m_onSwitchedOff?.Invoke();
            }

        }

        private void UpdateButtonVisuals()
        {
            StopAllCoroutines(); // Stop all coroutines to ensure a smooth transition

            var targetRotation = IsSwitchedOn ? m_targetLocalRotationOn : m_targetLocalRotationOff;
            _ = StartCoroutine(RotateButton(targetRotation));
        }

        private IEnumerator RotateButton(Quaternion targetRotation)
        {
            var initialRotation = m_switchButtonMesh.transform.localRotation;
            var elapsedTime = 0f;

            while (elapsedTime < m_switchSpeed)
            {
                var t = elapsedTime / m_switchSpeed;
                m_switchButtonMesh.transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            m_switchButtonMesh.transform.localRotation = targetRotation;
        }
    }
}