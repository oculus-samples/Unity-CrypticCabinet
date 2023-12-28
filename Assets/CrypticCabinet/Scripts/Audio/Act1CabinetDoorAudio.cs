// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Audio
{
    /// <summary>
    ///     Controls the audio for the intro animation timeline (Act1).
    /// </summary>
    public class Act1CabinetAudio : MonoBehaviour
    {
        [SerializeField] private Transform m_cabinetDoorTransform;
        [SerializeField] private AudioSource m_cabinetAudioSource;
        [SerializeField] private float m_rotationToTriggerCabinetAudio;

        private bool m_audioHasBeenPlayed;

        private void Update()
        {
            if (m_cabinetDoorTransform.localRotation.eulerAngles.y >= m_rotationToTriggerCabinetAudio
                && !m_audioHasBeenPlayed)
            {
                m_cabinetAudioSource.Play();
                m_audioHasBeenPlayed = true;
            }
        }
    }
}