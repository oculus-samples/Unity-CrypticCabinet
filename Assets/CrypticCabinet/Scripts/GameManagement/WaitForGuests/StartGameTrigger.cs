// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace CrypticCabinet.GameManagement.WaitForGuests
{
    /// <summary>
    ///     Trigger to start the game during a multiplayer session.
    /// </summary>
    public class StartGameTrigger : MonoBehaviour
    {
        /// <summary>
        ///     Callback to perform once the "Start Game" button from the UI is pressed.
        /// </summary>
        public Action OnStartGame;

        public void StartGame()
        {
            Debug.Log("Start Game trigger received, starting game...");
            if (OnStartGame == null)
            {
                Debug.LogError("[CRITICAL] Unable to start the game: m_onStartGame not set on StartGameTrigger!");
            }
            else
            {
                OnStartGame.Invoke();
            }
        }
    }
}
