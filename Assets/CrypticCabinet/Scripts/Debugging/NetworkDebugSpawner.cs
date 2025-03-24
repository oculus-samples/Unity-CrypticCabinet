// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Debugging
{
    /// <summary>
    ///     Programmatically spawns a specific network object every second when enabled.
    ///     We can use this to assess multiplayer sessions.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class NetworkDebugSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject m_objectToSpawn;
        [SerializeField] private float m_interval = 1f; // Interval in seconds
        private bool m_isSpawning;

        private void OnEnable()
        {
            if (m_isSpawning)
            {
                return;
            }

            InvokeRepeating(nameof(Spawn), 0f, m_interval);
            m_isSpawning = true;
        }

        private void OnDisable()
        {
            if (!m_isSpawning)
            {
                return;
            }

            CancelInvoke(nameof(Spawn));
            m_isSpawning = false;
        }

        private void Spawn()
        {
            if (PhotonConnector.Instance != null && PhotonConnector.Instance.Runner != null)
            {
                _ = PhotonConnector.Instance.Runner.Spawn(m_objectToSpawn);
            }
            else
            {
                Debug.LogWarning("Debug network spawning failed!");
            }
        }
    }
}