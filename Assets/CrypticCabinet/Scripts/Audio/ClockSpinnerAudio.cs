// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    ///     Controls the audio of the clock and its volume (within m_minVol and m_maxVol).
    /// </summary>
    public class ClockSpinnerAudio : MonoBehaviour
    {
        /// <summary>
        ///     The audio source of the clock spinner.
        /// </summary>
        [SerializeField] private AudioSource m_audioSource;

        /// <summary>
        ///     The minimum volume allowed for the the audio clip.     
        /// </summary>
        [SerializeField] private float m_minVol;

        /// <summary>
        ///     The maximum volume allowed for the audio clip.
        /// </summary>
        [SerializeField] private float m_maxVol;

        /// <summary>
        ///     Plays the audio source, setting its volume randomly within a range [m_minVol, m_maxVol].
        /// </summary>
        public void PlayAudio()
        {
            m_audioSource.volume = Random.Range(m_minVol, m_maxVol);
            m_audioSource.Play();
        }
    }
}