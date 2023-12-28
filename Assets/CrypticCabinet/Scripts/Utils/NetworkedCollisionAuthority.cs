// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Support for collision with conditional state authority.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NetworkedCollisionAuthority : NetworkBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            var networkObject = other.gameObject.GetComponentInParent<NetworkObject>();

            if (networkObject != null && HasStateAuthority)
            {
                Debug.Log("Switching State authority " + networkObject.name + " " + HasStateAuthority);
                networkObject.RequestStateAuthority();
            }
        }
    }
}
