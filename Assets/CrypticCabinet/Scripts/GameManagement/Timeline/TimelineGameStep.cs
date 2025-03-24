// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace CrypticCabinet.GameManagement.Timeline
{
    /// <summary>
    ///     Used to trigger an action when the timeline animation completes / stops.
    ///     We use this to proceed to the next game phase when the timeline game phase completes.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class TimelineGameStep : NetworkBehaviour
    {
        public UnityEvent TimelineCompleteCallback;

        private void Start()
        {
            var playableDirector = GetComponent<PlayableDirector>();
            playableDirector.stopped += _ =>
            {
                // Note: in multiplayer the Host is the only one that can trigger the callback once
                // the timeline animation is completed. This is to avoid conflicts among multiple users.
                if (Runner == null || Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
                {
                    TimelineCompleteCallback?.Invoke();
                }
            };
        }
    }
}