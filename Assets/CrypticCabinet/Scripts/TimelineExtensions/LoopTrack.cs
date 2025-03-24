// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine.Timeline;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom implementation for a track asset.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [TrackColor(1f, 0.2794118f, 0.7117646f)]
    [TrackClipType(typeof(LoopClip))]
    public class LoopTrack : TrackAsset
    {
    }
}