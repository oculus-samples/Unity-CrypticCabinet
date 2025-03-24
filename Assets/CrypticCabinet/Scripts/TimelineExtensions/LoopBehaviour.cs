// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using UnityEngine.Playables;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom playable behaviour for a loop.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [Serializable]
    public class LoopBehaviour : PlayableBehaviour
    {
        public PlayableDirector Director { get; set; }

        public WaitTimeline WaitTimeline { get; set; }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (WaitTimeline.Trigger)
            {
                WaitTimeline.Trigger = false;
                return;
            }

            Director.time -= playable.GetDuration();
        }
    }
}