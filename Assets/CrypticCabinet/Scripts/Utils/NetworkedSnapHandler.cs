// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Linq;
using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     A networked handler for snap actions in multiplayer session.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class NetworkedSnapHandler : NetworkBehaviour
    {
        [SerializeField] private SnapInteractable m_snapper;
        [Networked] public Guid SnapZoneId { get; set; }
        private NetworkObject m_networkObject;

        public override void Spawned()
        {
            base.Spawned();
            if (HasStateAuthority)
            {
                SnapZoneId = Guid.NewGuid();
            }
        }

        private void Start()
        {
            m_networkObject = GetComponentInParent<NetworkObject>();
            if (m_networkObject == null)
            {
                Debug.LogError("Missing network object", gameObject);
            }

            m_snapper.WhenInteractorViewAdded += WhenInteractorViewAdded;
            m_snapper.WhenInteractorViewRemoved += OnWhenInteractorViewRemoved;
            m_snapper.WhenSelectingInteractorViewAdded += WhenSelectingInteractorViewAdded;
            m_snapper.WhenSelectingInteractorViewRemoved += OnWhenSelectingInteractorViewRemoved;
        }

        private void OnDestroy()
        {
            m_snapper.WhenInteractorViewAdded -= WhenInteractorViewAdded;
            m_snapper.WhenInteractorViewRemoved -= OnWhenInteractorViewRemoved;
            m_snapper.WhenSelectingInteractorViewAdded -= WhenSelectingInteractorViewAdded;
            m_snapper.WhenSelectingInteractorViewRemoved -= OnWhenSelectingInteractorViewRemoved;
        }

        private void WhenInteractorViewAdded(IInteractorView view)
        {
            var (snapInteractor, networkedSnappedObject) = GetSnapData(view.Identifier);
            if (networkedSnappedObject != null && snapInteractor != null)
            {
                if (!networkedSnappedObject.RemoteAdd)
                {
                    RpcReceiveAddInteractor(SnapZoneId, networkedSnappedObject.SnapId);
                }
            }
        }

        private void OnWhenInteractorViewRemoved(IInteractorView view)
        {
            var (snapInteractor, networkedSnappedObject) = GetSnapData(view.Identifier);
            if (networkedSnappedObject != null && snapInteractor != null)
            {
                if (!networkedSnappedObject.RemoteRemove)
                {
                    networkedSnappedObject.ClearSelectionOverrides();
                    RpcReceiveRemoveInteractor(SnapZoneId, networkedSnappedObject.SnapId);
                }
            }
        }

        private void WhenSelectingInteractorViewAdded(IInteractorView view)
        {
            var (snapInteractor, networkedSnappedObject) = GetSnapData(view.Identifier);
            if (networkedSnappedObject != null && snapInteractor != null)
            {
                if (!networkedSnappedObject.RemoteSelectAdd)
                {
                    RpcReceiveSelectAddInteractor(SnapZoneId, networkedSnappedObject.SnapId);
                }
            }
        }

        private void OnWhenSelectingInteractorViewRemoved(IInteractorView view)
        {
            var (snapInteractor, networkedSnappedObject) = GetSnapData(view.Identifier);
            if (networkedSnappedObject != null && snapInteractor != null)
            {
                if (!networkedSnappedObject.RemoteSelectRemove)
                {
                    networkedSnappedObject.ClearSelectionOverrides();
                    RpcReceiveSelectRemoveInteractor(SnapZoneId, networkedSnappedObject.SnapId);
                }
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RpcReceiveAddInteractor(Guid zoneId, Guid objectId, RpcInfo info = default)
        {
            if (info.Source != PlayerRef.None && info.Source.PlayerId != Runner.LocalPlayer.PlayerId &&
                zoneId == SnapZoneId)
            {
                var objectToSnap = GetNetworkObjectForId(objectId);
                if (objectToSnap != null)
                {
                    objectToSnap.Hover(m_snapper);
                }
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RpcReceiveRemoveInteractor(Guid zoneId, Guid objectId, RpcInfo info = default)
        {
            if (info.Source != PlayerRef.None && info.Source.PlayerId != Runner.LocalPlayer.PlayerId &&
                zoneId == SnapZoneId)
            {
                var objectToSnap = GetNetworkObjectForId(objectId);
                if (objectToSnap != null)
                {
                    objectToSnap.Unhover(m_snapper);
                }
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RpcReceiveSelectAddInteractor(Guid zoneId, Guid objectId, RpcInfo info = default)
        {
            if (info.Source != PlayerRef.None && info.Source.PlayerId != Runner.LocalPlayer.PlayerId &&
                zoneId == SnapZoneId)
            {
                var objectToSnap = GetNetworkObjectForId(objectId);
                if (objectToSnap != null)
                {
                    objectToSnap.Snap(m_snapper);
                }
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RpcReceiveSelectRemoveInteractor(Guid zoneId, Guid objectId, RpcInfo info = default)
        {
            if (info.Source != PlayerRef.None && info.Source.PlayerId != Runner.LocalPlayer.PlayerId &&
                zoneId == SnapZoneId)
            {
                var objectToSnap = GetNetworkObjectForId(objectId);
                if (objectToSnap != null)
                {
                    objectToSnap.Unsnap(m_snapper);
                }
            }
        }

        private static NetworkedSnappedObject GetNetworkObjectForId(Guid objectId)
        {
            var snappedObjects = FindObjectsOfType<NetworkedSnappedObject>();
            return snappedObjects.FirstOrDefault(o => o.SnapId == objectId);
        }

        private static (SnapInteractor, NetworkedSnappedObject) GetSnapData(int id)
        {
            var o = GetInteractorById(id);
            NetworkedSnappedObject networkTransform = null;

            if (o != null)
            {
                networkTransform = o.GetComponentInParent<NetworkedSnappedObject>();
            }

            return (o, networkTransform);
        }

        private static SnapInteractor GetInteractorById(int id)
        {
            var snapInteractors = FindObjectsOfType<SnapInteractor>();
            foreach (var snapInteractor in snapInteractors)
            {
                if (snapInteractor.Identifier == id)
                {
                    return snapInteractor;
                }
            }

            return null;
        }
    }
}