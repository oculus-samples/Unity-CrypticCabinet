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
            return transform.position.y;
        }
    }
}
