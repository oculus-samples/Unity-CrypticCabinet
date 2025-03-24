// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Custom implementation of the playable behaviour for a lerp timeline.
    ///     Specifically, this is a bridge between the LerpTimelineClip set up in a timeline and the LerpTimelineEvent
    ///     to report the percentage through the LerpTimelineClip.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class LerpTimelineBehaviour : PlayableBehaviour
    {
        public LerpTimelineEvent UpdateEvent;

        public override void OnPlayableCreate(Playable playable)
        {
            base.OnPlayableCreate(playable);
            Debug.Log("OnPlayableCreate");
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            if (UpdateEvent == null)
            {
                return;
            }

            var timePercent = playable.GetTime() / playable.GetDuration();
            UpdateEvent.UpdateEvent?.Invoke(timePercent);
        }
    }
}