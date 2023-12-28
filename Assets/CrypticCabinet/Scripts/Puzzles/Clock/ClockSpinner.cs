// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using CrypticCabinet.Audio;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Clock
{
    /// <summary>
    ///     Represents the clock spinner the user can interact with to update the clock hands' positions.
    /// </summary>
    public class ClockSpinner : MonoBehaviour
    {
        [SerializeField] private Transform m_handleRoot;
        [SerializeField] private Transform m_handleTarget;
        [SerializeField] private float m_spinsPerFullRotation = 3;
        [SerializeField] private bool m_reverseHandle;
        [SerializeField] private ClockHandMover m_clockHandMover;
        [SerializeField] private ClockSpinnerAudio m_clockSpinnerAudioScript;
        [SerializeField] private float m_audioSourceCooldownInterval;

        private bool m_audioIsPlaying;
        private Vector3 m_lastRight;

        private void Start()
        {
            m_lastRight = m_handleRoot.worldToLocalMatrix.MultiplyVector(m_handleTarget.right);
        }

        private void Update()
        {
            var currentRight = m_handleRoot.worldToLocalMatrix.MultiplyVector(m_handleTarget.right);
            var handleMovementAmount = Vector3.SignedAngle(currentRight, m_lastRight, Vector3.up);
            var absHandMovement = Mathf.Abs(handleMovementAmount);
            if (absHandMovement > 0.01f)
            {
                var remappedMovementAngle = Mathf.InverseLerp(0, 360 * m_spinsPerFullRotation, absHandMovement);
                var movementAngle = handleMovementAmount < 0 ? -remappedMovementAngle : remappedMovementAngle;
                movementAngle = m_reverseHandle ? -movementAngle : movementAngle;
                m_clockHandMover.UpdateHands(movementAngle);

                if (absHandMovement > 1.2f && !m_audioIsPlaying)
                {
                    m_audioIsPlaying = true;
                    m_clockSpinnerAudioScript.PlayAudio();
                    _ = StartCoroutine(nameof(AudioCooldownTimer));
                }
            }

            m_lastRight = currentRight;
        }

        private IEnumerator AudioCooldownTimer()
        {
            yield return new WaitForSeconds(m_audioSourceCooldownInterval);
            m_audioIsPlaying = false;
        }
    }
}