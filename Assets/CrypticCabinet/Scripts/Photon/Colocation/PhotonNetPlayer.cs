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
        public ulong OculusId;
        public uint ColocationGroupId;


        public ColocationPackage.Player Player => new(OculusId, ColocationGroupId);

        public PhotonNetPlayer(ColocationPackage.Player player)
        {
            OculusId = player.oculusId;
            ColocationGroupId = player.colocationGroupId;
        }

        public PhotonNetPlayer(ulong oculusId, uint colocationGroupId)
        {
            OculusId = oculusId;
            ColocationGroupId = colocationGroupId;
        }

        public bool Equals(PhotonNetPlayer other) =>
            OculusId == other.OculusId && ColocationGroupId == other.ColocationGroupId;
    }
}