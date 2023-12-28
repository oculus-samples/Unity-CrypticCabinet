// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using CrypticCabinet.Photon;
using CrypticCabinet.Photon.Utils;
using TMPro;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     Handles the logic for a generic Multiplayer Menu UI.
    /// </summary>
    public class MultiplayerMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_roomNameForGuest;

        private Action<string> m_hostAction;
        private Action<string> m_joinAction;

        /// <summary>
        ///     Initialize the callbacks for the click of the buttons, and optionally set the default room name.
        /// </summary>
        /// <param name="hostAction">The callback to run when the Host button is clicked.</param>
        /// <param name="joinAction">The callback to run when the Join button is clicked.</param>
        /// <param name="defaultRoomName">[Optional] the default room name to join.</param>
        public void Initialize(Action<string> hostAction, Action<string> joinAction, string defaultRoomName = null)
        {
            if (!string.IsNullOrWhiteSpace(defaultRoomName))
            {
                m_roomNameForGuest.text = defaultRoomName;
            }
            m_hostAction = hostAction;
            m_joinAction = joinAction;
        }

        /// <summary>
        ///     Execute the callback associated to the Host button click.
        /// </summary>
        public void OnHostClicked()
        {
            // Generate random room name
            var randomRoomName = RoomNameGenerator.GenerateRoom();
            m_hostAction?.Invoke(randomRoomName);
        }

        /// <summary>
        ///     Execute the callback associated to the Join button click.
        /// </summary>
        public void OnJoinClicked()
        {
            // Show text input to choose room name
            m_roomNameForGuest.gameObject.SetActive(true);
            if (m_roomNameForGuest != null)
            {
                if (m_roomNameForGuest.text != string.Empty)
                {
                    m_joinAction?.Invoke(m_roomNameForGuest.text);
                }
                else
                {
                    UISystem.Instance.HideNetworkSelectionMenu();
                    UISystem.Instance.ShowMessage("You need to specify a room code to join!", () =>
                    {
                        PhotonConnector.Instance.ShowNetworkSelectionMenu();
                    });
                }
            }
            else
            {
                Debug.LogError("m_roomNameForGuest not set!");
            }
        }

        public void OnBackClicked()
        {
            UISystem.Instance.HideNetworkSelectionMenu();
            UISystem.Instance.ShowMainMenu();
        }
    }
}