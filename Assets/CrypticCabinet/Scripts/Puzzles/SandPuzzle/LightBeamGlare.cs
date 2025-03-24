// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Represents the glare for a light beam.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class LightBeamGlare : MonoBehaviour
    {
        [field: SerializeField] public Renderer Renderer { get; private set; }
    }
}
