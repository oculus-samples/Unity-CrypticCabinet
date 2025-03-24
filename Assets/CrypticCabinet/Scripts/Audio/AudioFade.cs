// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    /// Allows a signal emitter to trigger an audio fade via a timeline
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(AudioSource))]
    public class AudioFade : MonoBehaviour
    {
        private AudioSource m_audioSource;
        private float m_initialVolume;
        private void Start()
        {
            m_audioSource = GetComponent<AudioSource>();
        }

        public void FadeAudio()
        {
            m_initialVolume = m_audioSource.volume;
            _ = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            var elapsedTime = 0.0f;

            while (elapsedTime < 2f)
            {
                var newVolume = Mathf.Lerp(m_initialVolume, 0.0f, elapsedTime / 2f);

                m_audioSource.volume = newVolume;

                yield return null;

                elapsedTime += Time.deltaTime;
            }

            m_audioSource.volume = 0.0f;
        }
    }
}
