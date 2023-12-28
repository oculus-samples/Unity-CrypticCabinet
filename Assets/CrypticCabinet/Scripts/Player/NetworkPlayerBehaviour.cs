// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Fusion;

namespace CrypticCabinet.Player
{
    /// <summary>
    ///     Represents a player with replicated values through Photon Fusion.
    /// </summary>
    /// <typeparam name="T">The class of the player.</typeparam>
    public class NetworkPlayerBehaviour<T> : NetworkBehaviour
        where T : NetworkPlayerBehaviour<T>
    {
        private static readonly Dictionary<PlayerRef, T> s_instancesByPlayer = new();

        private PlayerRef m_currentPlayerRef;

        public override void Spawned()
        {
            base.Spawned();

            m_currentPlayerRef = Object.StateAuthority;

            var added = s_instancesByPlayer.TryAdd(m_currentPlayerRef, (T)this);
            Assert.Check(added, $"Player {m_currentPlayerRef} already spawned a {typeof(T).Name}!");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Assert.Check(
                s_instancesByPlayer[m_currentPlayerRef] == this,
                $"Player {m_currentPlayerRef} already despawned a {typeof(T).Name}!");
            _ = s_instancesByPlayer.Remove(m_currentPlayerRef);

            base.Despawned(runner, hasState);
        }

        public static T Get(PlayerRef player) =>
            s_instancesByPlayer.TryGetValue(player, out var behaviour) ? behaviour : null;
    }
}