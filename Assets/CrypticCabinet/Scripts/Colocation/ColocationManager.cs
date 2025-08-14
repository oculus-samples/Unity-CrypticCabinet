// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using Assert = UnityEngine.Assertions.Assert;

namespace CrypticCabinet.Colocation
{
    /// <summary>
    ///     Handles the colocation process, and offers functions to emulate colocation completed or failed in Editor.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ColocationManager : MonoBehaviour
    {
        [SerializeField] private ColocationDriverNetObj m_colocationPrefab;
        [SerializeField] private UnityEvent<bool> m_onColocationCompletedCallback;
        [SerializeField] private UnityEvent m_onColocationSkippedCallback;

        /// <summary>
        ///     Spawn the prefab that is responsible for the colocation process.
        /// </summary>
        /// <param name="runner">The Network Runner that will spawn this prefab.</param>
        public void SpawnColocationPrefab(NetworkRunner runner)
        {
            // Make sure the Photon Connector exists and has a working Runner to use for the spawn.
            Assert.IsNotNull(PhotonConnector.Instance);
            Assert.IsNotNull(runner);

            if (!PhotonConnector.Instance.IsMultiplayerSession)
            {
                Debug.Log("Single player session, skipping colocation...", this);
                HandleColocationSkipped();
                return;
            }
            // Register for event (use WhenInstantiated so it registers before the object initializes)
            ColocationDriverNetObj.WhenInstantiated(colo =>
            {
                colo.OnColocationCompletedCallback += HandleColocationCompleted;
            });

            Debug.Log("Spawn Colocation Prefab");
            _ = runner.Spawn(m_colocationPrefab);
            Debug.Log("Colocation Prefab Spawned");
        }

        /// <summary>
        /// Executed when OnColocationCompletedCallback is fired by the ColocationDriverNetObj.
        /// </summary>
        /// <param name="isSuccess">True if colocation succeeded and is ready.</param>
        private void HandleColocationCompleted(bool isSuccess)
        {
            if (isSuccess)
            {
                m_onColocationCompletedCallback?.Invoke(true);
            }
            else
            {
                _ = ColocationDriverNetObj.Instance.StartCoroutine(ColocationDriverNetObj.Instance.RetryColocation());
            }
        }

        /// <summary>
        /// Executed when OnColocationSkippedCallback is fired by the ColocationDriverNetObj.
        /// </summary>
        private void HandleColocationSkipped()
        {
            m_onColocationSkippedCallback?.Invoke();
        }

        /// <summary>
        ///     Emulates the successful completion of the colocation process.
        ///     This can be called in editor using the context menu if needed for debug.
        /// </summary>
        [ContextMenu("Emulate Colocation Completed")]
        public void EmulateColocationCompleted()
        {
            HandleColocationCompleted(true);
        }

        /// <summary>
        ///     Emulates the failure of the colocation process.
        ///     This can be called in editor using the context menu if needed for debug.
        /// </summary>
        [ContextMenu("Emulate Colocation Failed")]
        public void EmulateColocationFailed()
        {
            HandleColocationCompleted(false);
        }
    }
}