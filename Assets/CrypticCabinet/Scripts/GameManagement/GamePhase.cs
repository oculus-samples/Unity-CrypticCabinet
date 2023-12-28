// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.GameManagement
{
    /// <summary>
    ///     Represents a game phase for the gameplay.
    /// </summary>
    [CreateAssetMenu(fileName = "New CrypticCabinet Game Phase", menuName = "CrypticCabinet/Game Phase")]
    public class GamePhase : ScriptableObject
    {
        /// <summary>
        ///     Describes what this game phase does.
        /// </summary>
        [TextArea, SerializeField] private string m_gamePhaseDescription;

        public void Initialize()
        {
            InitializeInternal();
        }

        public void Deinitialize()
        {
            DeinitializeInternal();
        }

        protected virtual void InitializeInternal()
        {
            // Handle initialization on child class, if needed.
        }

        protected virtual void DeinitializeInternal()
        {
            // Handle de-initialization on child class, if needed.
        }
    }
}