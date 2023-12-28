// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Fusion;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Describes a networked snappable object for all users in a session.
    /// </summary>
    public class NetworkedSnappedObject : NetworkBehaviour
    {
        [Networked] public Guid SnapId { get; set; }

        [SerializeField] private SnapInteractor m_snapInteractor;

        public bool RemoteAdd { get; private set; }
        public bool RemoteRemove { get; private set; }
        public bool RemoteSelectAdd { get; private set; }
        public bool RemoteSelectRemove { get; private set; }

        public override void Spawned()
        {
            base.Spawned();
            if (HasStateAuthority)
            {
                SnapId = Guid.NewGuid();
            }
        }

        private void Awake()
        {
            ClearSelectionOverrides();
        }

        public void Hover(SnapInteractable zone)
        {
            RemoteAdd = true;

            zone.AddInteractor(m_snapInteractor);
            m_snapInteractor.SetComputeCandidateOverride(() => zone);
            m_snapInteractor.ProcessCandidate();
            m_snapInteractor.Hover();
            RemoteAdd = false;
        }

        public void Unhover(SnapInteractable zone)
        {
            RemoteRemove = true;
            zone.RemoveInteractor(m_snapInteractor);
            m_snapInteractor.Unhover();

            ClearSelectionOverrides();

            RemoteRemove = false;
        }

        public void Snap(SnapInteractable zone)
        {
            RemoteSelectAdd = true;

            zone.AddSelectingInteractor(m_snapInteractor);
            m_snapInteractor.SetComputeCandidateOverride(() => zone);
            m_snapInteractor.ProcessCandidate();
            m_snapInteractor.Select();
            RemoteSelectAdd = false;
        }

        public void Unsnap(SnapInteractable zone)
        {
            RemoteSelectRemove = true;
            zone.RemoveSelectingInteractor(m_snapInteractor);
            m_snapInteractor.Unselect();

            ClearSelectionOverrides();

            RemoteSelectRemove = false;
        }

        public void ClearSelectionOverrides()
        {
            if (Photon.PhotonConnector.Instance != null && Photon.PhotonConnector.Instance.IsMultiplayerSession)
            {
                m_snapInteractor.ClearComputeCandidateOverride();
                m_snapInteractor.ClearComputeShouldSelectOverride();
                m_snapInteractor.ClearComputeShouldUnselectOverride();
            }
        }
    }
}