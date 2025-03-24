// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    ///     Triggers the audio for the creak and chimes SFX of the Window on the sand puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ShutterAudio : MonoBehaviour
    {
        /// <summary>
        ///     The transform of the window shutter.
        /// </summary>
        [SerializeField] private Transform m_shutterTransform;

        /// <summary>
        ///     The audio source to play when the window shutter opens.
        /// </summary>
        [SerializeField] private AudioSource m_creakAudioSource;

        /// <summary>
        ///     The audio source to play when the window opens for the first time.
        /// </summary>
        [SerializeField] private AudioSource m_chimesAudioSource;

        /// <summary>
        ///     The rotation threshold for the chimes audio trigger.
        /// </summary>
        [SerializeField] private float m_rotationToTriggerChimesAudio;

        /// <summary>
        ///     The rotation threshold for the creak audio trigger.
        /// </summary>
        [SerializeField] private float m_rotationToTriggerCreakAudio;

        private bool m_chimesAudioHasBeenPlayed;
        private bool m_canPlayCreakAudio = true;

        private void Update()
        {
            if (m_shutterTransform.localRotation.eulerAngles.y >= m_rotationToTriggerCreakAudio
                && m_canPlayCreakAudio)
            {
                m_creakAudioSource.Play();
                m_canPlayCreakAudio = false;
            }

            if (m_shutterTransform.localRotation.eulerAngles.y >= m_rotationToTriggerChimesAudio
                && !m_chimesAudioHasBeenPlayed)
            {
                m_chimesAudioSource.Play();
                m_chimesAudioHasBeenPlayed = true;
            }
        }
    }
}
