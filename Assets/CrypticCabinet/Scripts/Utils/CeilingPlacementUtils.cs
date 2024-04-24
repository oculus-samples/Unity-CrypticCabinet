// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.SceneManagement;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Creates Static Utility functions to handle ceiling object placement
    /// </summary>
    public static class CeilingPlacementUtils
    {
        internal const float MAX_CEILING_PLACEMENT_HEIGHT = 3.0f;

        public static void SetYPositionToCeiling(Transform transform, CeilingHeightDetector ceilingHeightDetector)
        {
            if (ceilingHeightDetector != null)
            {
                var position = transform.position;
                var ceilingHeight = ceilingHeightDetector.GetCeilingHeight();
                position.y = Mathf.Min(MAX_CEILING_PLACEMENT_HEIGHT, ceilingHeight);
                transform.position = position;
            }
        }
    }
}
