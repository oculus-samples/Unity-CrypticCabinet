// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using CrypticCabinet.GameManagement;
using CrypticCabinet.Passthrough;
using CrypticCabinet.Photon;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     An in-game menu that can be used to exit the game play.
    /// </summary>
    public class InGameMenu : MonoBehaviour
    {
        [SerializeField] private List<GamePhase> m_allowedGamePhases;

        private bool m_isMenuOpen;

        [ContextMenu("Quit to Main Menu")]
        public async void QuitToMainMenu()
        {
            if (PassthroughChanger.Instance != null)
            {
                PassthroughChanger.Instance.SetPassthroughDefaultLut();
            }

            m_isMenuOpen = false;
            UISystem.Instance.HideAll();

            await PhotonConnector.Instance.DisconnectFromRoom();
        }

        public void ToggleInGameMenu()
        {
            var currentGamePhase = GameManager.Instance.GetCurrentGamePhase();
            // Do not allow to show quit game menu if gameplay did not start.
            if (currentGamePhase == null || (m_allowedGamePhases != null && !m_allowedGamePhases.Contains(currentGamePhase)))
            {
                Debug.Log("In game menu not allowed in current game phase.");
                UISystem.Instance.HideInGameMenu();
                m_isMenuOpen = false;
                return;
            }

            if (m_isMenuOpen)
            {
                UISystem.Instance.HideInGameMenu();
            }
            else
            {
                UISystem.Instance.ShowInGameMenu();
            }
            m_isMenuOpen = !m_isMenuOpen;
        }
    }
}