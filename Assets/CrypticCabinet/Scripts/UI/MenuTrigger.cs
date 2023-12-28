// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using CrypticCabinet.UI.Modal;
using UnityEngine;

namespace CrypticCabinet.UI
{
    /// <summary>
    ///     Controls the trigger to open the in-game menu.
    /// </summary>
    public class MenuTrigger : MonoBehaviour
    {
        [SerializeField] private InGameMenu m_inGameMenu;
        private bool m_initialized;

        private void Start()
        {
            m_initialized = m_inGameMenu != null;
        }

        private void Update()
        {
            // Checks if the start menu trigger has been pressed with hands or with controller
            var startButtonTrigger = OVRInput.GetUp(OVRInput.Button.Start);

            if (!startButtonTrigger || !PhotonConnector.Instance.JoinedActiveGameSession)
            {
                return;
            }

            if (m_initialized)
            {
                m_inGameMenu.ToggleInGameMenu();
            }
            else
            {
                Debug.LogError("Cannot open in game menu: not initialized!");
            }
        }
    }
}