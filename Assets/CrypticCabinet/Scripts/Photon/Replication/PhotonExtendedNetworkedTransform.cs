// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Photon.Replication
{
    /// <summary>
    ///     The NetworkTransform of Photon Fusion by default only replicates position and rotation.
    ///     This class instead replicates the scale vector3 as well.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class PhotonExtendedNetworkedTransform : NetworkTransform
    {
        [Networked]
        public Vector3 ScaleVector { get; set; }

        /// <summary>
        ///     Represents the tick from the network.
        ///     Here we update the scale depending on the updated value.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // Only update the scale if we are not the state authority.
            // This is to avoid re-setting again the scale after changing it.
            if (!HasStateAuthority)
            {
                transform.localScale = ScaleVector;
            }
            else if (transform.localScale != ScaleVector)
            {
                // Update ScaleVector with local scale for all other users
                ScaleVector = transform.localScale;
            }
        }
    }
}