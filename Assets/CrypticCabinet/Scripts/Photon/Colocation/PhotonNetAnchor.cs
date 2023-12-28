// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using ColocationPackage;
using Fusion;

namespace CrypticCabinet.Photon.Colocation
{
    /// <summary>
    ///     Represents a replicated spatial anchor via Photon network.
    /// </summary>
    public struct PhotonNetAnchor : INetworkStruct, IEquatable<PhotonNetAnchor>
    {
        /// <summary>
        ///     True if this anchor can be used to re-align players.
        /// </summary>
        public NetworkBool IsAlignmentAnchor;

        public NetworkString<_64> Uuid;

        /// <summary>
        ///     The anchor is owned by a single user, which is the one that spawned it in first place.
        /// </summary>
        public ulong OwnerOculusId;

        /// <summary>
        ///     A group ID that can be used to have different colocation setups.
        /// </summary>
        public uint ColocationGroupId;

        public Anchor Anchor => new(IsAlignmentAnchor, Uuid.ToString(), OwnerOculusId, ColocationGroupId);

        public PhotonNetAnchor(Anchor anchor)
        {
            IsAlignmentAnchor = anchor.isAlignmentAnchor;
            Uuid = anchor.uuid.ToString();
            OwnerOculusId = anchor.ownerOculusId;
            ColocationGroupId = anchor.colocationGroupId;
        }

        public PhotonNetAnchor(NetworkBool isAlignmentAnchor, NetworkString<_64> uuid, ulong ownerOculusId,
            uint colocationGroupId)
        {
            IsAlignmentAnchor = isAlignmentAnchor;
            Uuid = uuid;
            OwnerOculusId = ownerOculusId;
            ColocationGroupId = colocationGroupId;
        }

        public bool Equals(PhotonNetAnchor other) =>
            IsAlignmentAnchor == other.IsAlignmentAnchor
            && Uuid == other.Uuid
            && OwnerOculusId == other.OwnerOculusId
            && ColocationGroupId == other.ColocationGroupId;
    }
}