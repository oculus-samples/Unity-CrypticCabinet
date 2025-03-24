// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     This is a convenient script to initiate / re-calibrate the Room Setup when needed.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class RequestRoomSetup : MonoBehaviour
    {

        /// <summary>
        /// Triggers the Room Setup workflow, even if a room setup is already in place.
        /// </summary>
        public void InitiateRoomSetup()
        {
            _ = OVRScene.RequestSpaceSetup();
        }
    }
}