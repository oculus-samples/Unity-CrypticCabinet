// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Represents the glare for a light beam.
    /// </summary>
    public class LightBeamGlare : MonoBehaviour
    {
        [field: SerializeField] public Renderer Renderer { get; private set; }
    }
}
