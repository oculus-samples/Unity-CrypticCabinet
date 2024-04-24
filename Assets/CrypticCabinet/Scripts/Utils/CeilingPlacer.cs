// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.SceneManagement;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Moves an object to ceiling height if the <see cref="CeilingHeightDetector"/> is spawned.
    /// </summary>
    public class CeilingPlacer : MonoBehaviour
    {
        [SerializeField] private bool m_placeOnStart;

        private void Start()
        {
            if (m_placeOnStart)
            {
                PlaceOnCeiling();
            }
        }

        public void PlaceOnCeiling()
        {
            var ceilingHeightDetector = FindObjectOfType<CeilingHeightDetector>();
            CeilingPlacementUtils.SetYPositionToCeiling(transform, ceilingHeightDetector);
        }
    }
}
