// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Fusion;

namespace CrypticCabinet.Photon.Colocation
{
    /// <summary>
    ///     Represents a connected user, identified by Oculus ID and Colocation group ID.
    /// </summary>
    public struct PhotonNetPlayer : INetworkStruct, IEquatable<PhotonNetPlayer>
    {
        public ulong PlayerId;
        public ulong OculusId;
        public uint ColocationGroupId;

        public PhotonNetPlayer(com.meta.xr.colocation.Player player)
        {
            PlayerId = player.playerId;
            OculusId = player.oculusId;
            ColocationGroupId = player.colocationGroupId;
        }

        public com.meta.xr.colocation.Player GetPlayer()
        {
            return new com.meta.xr.colocation.Player(PlayerId, OculusId, ColocationGroupId);
        }

        public bool Equals(PhotonNetPlayer other)
        {
            return GetPlayer().Equals(other.GetPlayer());
        }
    }
}