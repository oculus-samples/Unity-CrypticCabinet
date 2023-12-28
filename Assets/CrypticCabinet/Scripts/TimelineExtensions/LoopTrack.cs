// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine.Timeline;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom implementation for a track asset.
    /// </summary>
    [TrackColor(1f, 0.2794118f, 0.7117646f)]
    [TrackClipType(typeof(LoopClip))]
    public class LoopTrack : TrackAsset
    {
    }
}