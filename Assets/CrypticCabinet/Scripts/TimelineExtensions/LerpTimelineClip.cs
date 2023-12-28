// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom implementation for a lerp timeline clip.
    /// </summary>
    public class LerpTimelineClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LerpTimelineBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.UpdateEvent = owner.GetComponent<LerpTimelineEvent>();
            return playable;
        }
    }
}