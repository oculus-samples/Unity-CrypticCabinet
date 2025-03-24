// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.TimelineExtensions;
using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Script for the Intro timeline animation control (Act1).
    ///     Triggers the next timeline phase for all users in a session when the note has been grabbed.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ActOneMultiuserManager : NetworkBehaviour
    {
        [SerializeField] private WaitTimeline m_waitTimeline;

        public void NoteGrabbed()
        {
            if (Runner == null || Runner.IsSinglePlayer)
            {
                InteractableTriggerBroadcaster.ForceGlobalUpdateTriggers();
                m_waitTimeline.Trigger = true;
            }
            else
            {
                NoteGrabbedRpc();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void NoteGrabbedRpc(RpcInfo info = default)
        {
            InteractableTriggerBroadcaster.ForceGlobalUpdateTriggers();
            m_waitTimeline.Trigger = true;
        }
    }
}