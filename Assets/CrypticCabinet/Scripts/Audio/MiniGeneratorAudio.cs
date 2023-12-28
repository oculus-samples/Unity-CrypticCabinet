// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    ///     Controls the audios for the mini generators.
    /// </summary>
    public class MiniGeneratorAudio : MonoBehaviour
    {
        /// <summary>
        ///     The audio source of the mini generator.
        /// </summary>
        [SerializeField] private AudioSource m_audioSource;

        /// <summary>
        ///     The audio clip to play when the generator is on but not connected by electric arc.
        /// </summary>
        [SerializeField] private AudioClip m_arcNotConnected;

        /// <summary>
        ///     The audio clip to play when the generator is on and connected by electric arc.
        /// </summary>
        [SerializeField] private AudioClip m_arcConnected;

        private bool m_currentClipIsForConnectedVFX;

        /// <summary>
        ///     Update the audio clip to play depending on whether the generator has a connected electric arc or not.
        /// </summary>
        /// <param name="connected">If true, there is an electric arc connected to the generator. False otherwise.</param>
        public void ChangeClip(bool connected)
        {
            if (connected)
            {
                m_audioSource.clip = m_arcConnected;

                if (!m_currentClipIsForConnectedVFX)
                {
                    m_audioSource.Play();
                    m_currentClipIsForConnectedVFX = true;
                }
            }
            else
            {
                m_audioSource.clip = m_arcNotConnected;

                if (m_currentClipIsForConnectedVFX)
                {
                    m_audioSource.Play();
                    m_currentClipIsForConnectedVFX = false;
                }
            }
        }

        private void OnEnable()
        {
            m_audioSource.Play();
        }
    }
}