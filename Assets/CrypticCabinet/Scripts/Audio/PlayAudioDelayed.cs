// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    ///     Plays the specified audio source after the desired m_delayAmount.
    /// </summary>
    public class PlayAudioDelayed : MonoBehaviour
    {
        /// <summary>
        ///     The audio source that will be played after m_delayAmount seconds.
        /// </summary>
        [SerializeField] private AudioSource m_audioSource;

        /// <summary>
        ///     The delay in seconds after which the audio source is played.
        /// </summary>
        [SerializeField] private float m_delayAmount;

        private void Start()
        {
            _ = StartCoroutine(nameof(PlayAudio));
        }

        private IEnumerator PlayAudio()
        {
            yield return new WaitForSeconds(m_delayAmount);
            m_audioSource.Play();
        }
    }
}