// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom implementation of a playable asset for a loop clip.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [Serializable]
    public class LoopClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LoopBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Director = owner.GetComponent<PlayableDirector>();
            behaviour.WaitTimeline = owner.GetComponent<WaitTimeline>();
            return playable;
        }
    }
}