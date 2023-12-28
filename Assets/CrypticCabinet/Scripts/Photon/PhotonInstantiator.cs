// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Linq;
using Fusion;
using Meta.Utilities;
using UnityEngine;

namespace CrypticCabinet.Photon
{
    /// <summary>
    ///     Instantiates the specified network object across all clients.
    ///     This is done on Start (when spawned for the first time).
    ///     The spawned network object will automatically be child of the parent of this script.
    ///     Note: This is a Multiton, which keeps track of all the instantiated instances.
    /// </summary>
    public class PhotonInstantiator : Multiton<PhotonInstantiator>
    {
        [SerializeField] private NetworkObject m_networkObject;

        private bool m_instantiated;
        private NetworkRunner m_instantiationRunner;

        private void Start() => TryInstantiate();

        public void TryInstantiate()
        {
            var thisTransform = transform;
            var runner = NetworkRunner.Instances?.FirstOrDefault();
            if (runner != null && runner.State == NetworkRunner.States.Running)
            {
                if (!m_instantiated || runner != m_instantiationRunner)
                {
                    var obj = runner.Spawn(
                        m_networkObject, thisTransform.position,
                        thisTransform.rotation,
                        onBeforeSpawned: OnBeforeSpawned);
                    Debug.Log($"[PhotonInstantiator] Spawned {obj} at {obj.transform.position}", obj);
                    m_instantiationRunner = runner;
                    m_instantiated = true;
                }
            }
            else
            {
                Debug.Log(
                    $"[PhotonInstantiator] Photon disabled; not spawning {m_networkObject} at {thisTransform.position}",
                    this);
            }
        }

        private void OnBeforeSpawned(NetworkRunner runner, NetworkObject obj)
        {
            Transform newTrans;
            (newTrans = obj.transform).SetParent(transform.parent);
            newTrans.localScale = transform.localScale;
        }
    }
}