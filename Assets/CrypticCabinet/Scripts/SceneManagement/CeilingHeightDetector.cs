// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Script to detect the height of the ceiling.
    ///     Useful for when we want to show clue textures over the ceiling.
    /// </summary>
    public class CeilingHeightDetector : MonoBehaviour
    {
        public float GetCeilingHeight()
        {
            // Note: the owning GameObject is supposed to be the child of
            // Photon's instantiator, so the real height is provided by the
            // instantiator, which is the parent.
            return transform.parent.transform.position.y;
        }
    }
}
