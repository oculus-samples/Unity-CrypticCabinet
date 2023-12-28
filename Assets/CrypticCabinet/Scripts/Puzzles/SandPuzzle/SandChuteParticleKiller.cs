// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using CrypticCabinet.Utils;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Describes how the particles for the sand chute behave.
    /// </summary>
    public class SandChuteParticleKiller : MonoBehaviour
    {
        [SerializeField] private List<VFXCone> m_volumeConeDescriptors = new();

        /// <summary>
        ///     Hashmap between GO id and volume index.
        /// </summary>
        private readonly Dictionary<int, int> m_instanceIDtoVolumeIndex = new();
        private readonly Queue<int> m_availableVolumes = new();

        private void Start()
        {
            for (var i = 0; i < m_volumeConeDescriptors.Count; i++)
            {
                ResetVolume(i);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_availableVolumes.Count <= 0)
            {
                Debug.LogWarning("Ignoring collision as we have no volumes remaining");
                return;
            }

            //We ignore trigger colliders.
            if (other.isTrigger)
            {
                return;
            }

            var volumeKiller = other.GetComponent<SandChuteVolumeKiller>();
            if (volumeKiller != null)
            {
                //The object could have multiple colliders, if so we do the setup only once.
                if (!m_instanceIDtoVolumeIndex.TryGetValue(volumeKiller.gameObject.GetInstanceID(), out var volumeIndex))
                {
                    volumeIndex = m_availableVolumes.Dequeue();
                    m_instanceIDtoVolumeIndex.Add(volumeKiller.gameObject.GetInstanceID(), volumeIndex);
                    volumeKiller.ToVfxCone(m_volumeConeDescriptors[volumeIndex]);
                }
            }
            else
            {
                Debug.LogWarning("Potential particle blocker without " + nameof(VFXCone) + " component");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            //We ignore trigger colliders.
            if (other.isTrigger)
            {
                return;
            }

            if (m_instanceIDtoVolumeIndex.TryGetValue(other.gameObject.GetInstanceID(), out var descriptorIndex))
            {
                _ = m_instanceIDtoVolumeIndex.Remove(other.gameObject.GetInstanceID());
                ResetVolume(descriptorIndex);
            }
        }

        private void ResetVolume(int index)
        {
            m_volumeConeDescriptors[index].ZeroScale();
            m_availableVolumes.Enqueue(index);
        }
    }
}
