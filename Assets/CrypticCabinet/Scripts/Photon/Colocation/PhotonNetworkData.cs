// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using com.meta.xr.colocation;
using CrypticCabinet.Utils;
using Fusion;

namespace CrypticCabinet.Photon.Colocation
{
    /// <summary>
    ///     Holds the count for all colocation groups, the anchors list, and players list.
    /// </summary>
    public class PhotonNetworkData : NetworkSingleton<PhotonNetworkData>, INetworkData
    {
        [Networked] private uint ColocationGroupCount { get; set; }
        [Networked, Capacity(10)] private NetworkLinkedList<PhotonNetAnchor> AnchorList { get; }

        [Networked, Capacity(10)] private NetworkLinkedList<PhotonNetPlayer> PlayerList { get; }

        public void AddPlayer(com.meta.xr.colocation.Player player)
        {
            AddFusionPlayer(new PhotonNetPlayer(player));
        }

        public void RemovePlayer(com.meta.xr.colocation.Player player)
        {
            RemoveFusionPlayer(new PhotonNetPlayer(player));
        }

        public com.meta.xr.colocation.Player? GetPlayerWithPlayerId(ulong playerId)
        {
            foreach (var fusionPlayer in PlayerList)
            {
                if (fusionPlayer.GetPlayer().playerId == playerId)
                {
                    return fusionPlayer.GetPlayer();
                }
            }

            return null;
        }

        public com.meta.xr.colocation.Player? GetPlayerWithOculusId(ulong oculusId)
        {
            foreach (var fusionPlayer in PlayerList)
            {
                if (fusionPlayer.GetPlayer().oculusId == oculusId)
                {
                    return fusionPlayer.GetPlayer();
                }
            }

            return null;
        }

        public List<com.meta.xr.colocation.Player> GetAllPlayers()
        {
            var allPlayers = new List<com.meta.xr.colocation.Player>();
            foreach (var fusionPlayer in PlayerList)
            {
                allPlayers.Add(fusionPlayer.GetPlayer());
            }

            return allPlayers;
        }

        public void AddAnchor(Anchor anchor)
        {
            AnchorList.Add(new PhotonNetAnchor(anchor));
        }

        public void RemoveAnchor(Anchor anchor)
        {
            _ = AnchorList.Remove(new PhotonNetAnchor(anchor));
        }

        public Anchor? GetAnchor(ulong ownerOculusId)
        {
            foreach (var fusionAnchor in AnchorList)
            {
                if (fusionAnchor.GetAnchor().ownerOculusId == ownerOculusId)
                {
                    return fusionAnchor.GetAnchor();
                }
            }

            return null;
        }

        public List<Anchor> GetAllAnchors()
        {
            var anchors = new List<Anchor>();
            foreach (var fusionAnchor in AnchorList)
            {
                anchors.Add(fusionAnchor.GetAnchor());
            }

            return anchors;
        }

        public uint GetColocationGroupCount()
        {
            return ColocationGroupCount;
        }

        public void IncrementColocationGroupCount()
        {
            if (HasStateAuthority)
            {
                ColocationGroupCount++;
            }
            else
            {
                IncrementColocationGroupCountRpc();
            }
        }

        private void AddFusionPlayer(PhotonNetPlayer player)
        {
            if (HasStateAuthority)
            {
                PlayerList.Add(player);
            }
            else
            {
                AddPlayerRpc(player);
            }
        }

        private void RemoveFusionPlayer(PhotonNetPlayer player)
        {
            if (HasStateAuthority)
            {
                _ = PlayerList.Remove(player);
            }
            else
            {
                RemovePlayerRpc(player);
            }
        }

        private void AddFusionAnchor(PhotonNetAnchor anchor)
        {
            if (HasStateAuthority)
            {
                AnchorList.Add(anchor);
            }
            else
            {
                AddAnchorRpc(anchor);
            }
        }

        private void RemoveFusionAnchor(PhotonNetAnchor anchor)
        {
            if (HasStateAuthority)
            {
                _ = AnchorList.Remove(anchor);
            }
            else
            {
                RemoveAnchorRpc(anchor);
            }
        }

        #region Rpcs

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void AddPlayerRpc(PhotonNetPlayer player)
        {
            AddFusionPlayer(player);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RemovePlayerRpc(PhotonNetPlayer player)
        {
            RemoveFusionPlayer(player);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void AddAnchorRpc(PhotonNetAnchor anchor)
        {
            AddFusionAnchor(anchor);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RemoveAnchorRpc(PhotonNetAnchor anchor)
        {
            RemoveFusionAnchor(anchor);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void IncrementColocationGroupCountRpc()
        {
            IncrementColocationGroupCount();
        }

        #endregion
    }
}