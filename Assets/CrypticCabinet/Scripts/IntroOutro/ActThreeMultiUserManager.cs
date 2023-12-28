// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Passthrough;
using CrypticCabinet.Photon;
using CrypticCabinet.TimelineExtensions;
using CrypticCabinet.UI;
using Fusion;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Script for the Outro timeline animation control (Act3).
    ///     Triggers the next timeline phase for all users in a session when the note has been grabbed.
    ///     On outro timeline completed, it disconnects all guest users, shows credits, and restarts the gameplay.
    /// </summary>
    public class ActThreeMultiUserManager : NetworkBehaviour
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
            PassthroughChanger.Instance.SetPassthroughDefaultLut();
            InteractableTriggerBroadcaster.ForceGlobalUpdateTriggers();
            m_waitTimeline.Trigger = true;
        }

        public void OnActThreeComplete()
        {
            if (Runner != null && Runner.IsSharedModeMasterClient)
            {
                // If this is the Host, all players need to be kicked out from the current game session.                
                PhotonConnector.Instance.HostDisconnectAllFromRoom();
            }
            else
            {
                Debug.Log("Runner was already null, OnActThreeComplete will not shut it down.");
            }

            UISystem.Instance.HideAll();
            UISystem.Instance.ShowCredits();
        }
    }
}