// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.GameManagement
{
    /// <summary>
    ///     Triggers the start of the outro (Act3) when the directional coil fires the ray towards the cabinet.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ActThreeStarter : MonoBehaviour
    {
        /// <summary>
        ///     The audio source to play when the next phase of the game is being triggered.
        /// </summary>
        [SerializeField] private AudioSource m_audioSource;

        private bool m_hasFired;

        public void StartActThree()
        {
            if (!m_hasFired)
            {
                StartNextGamePhase();
                m_hasFired = true;
            }
        }

        private void StartNextGamePhase()
        {
            if (m_audioSource != null)
            {
                m_audioSource.Play();
            }

            if (Passthrough.PassthroughChanger.Instance != null)
            {
                Passthrough.PassthroughChanger.Instance.SetPassthroughDefaultLut();
            }
            GameManager.Instance.NextGameplayPhase();
        }
    }
}