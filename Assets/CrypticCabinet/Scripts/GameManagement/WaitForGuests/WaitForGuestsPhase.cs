// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using CrypticCabinet.UI;
using UnityEngine;

namespace CrypticCabinet.GameManagement.WaitForGuests
{
    /// <summary>
    ///     Simple game phase where a message is shown to the Host asking for pressing a "start game" button
    ///     that will force all new future guests to be forbidden from joining the game.
    ///     When the "start game" button is pressed, the game officially starts for all joined players.
    /// </summary>
    [CreateAssetMenu(fileName = "New Wait For Guests Game Phase", menuName = "CrypticCabinet/Wait For Guests Game Phase")]
    public class WaitForGuestsPhase : GamePhase
    {
        /// <summary>
        ///     Prefab of the UI that will be shown the Host while waiting for other guests to join.
        ///     This UI will also show a start button that the Host user can click to start the game for all users in
        ///     the multiplayer session.
        /// </summary>
        [SerializeField] private GameObject m_startGameUIPrefab;

        private GameObject m_startGameUI;

        protected override void InitializeInternal()
        {
            // Ensure we are in multiplayer before proceeding, otherwise skip this phase
            if (!PhotonConnector.Instance.IsMultiplayerSession)
            {
                Debug.Log("Skipping WaitForGuestsPhase, this is a single player session.");
                GameManager.Instance.NextGameplayPhase();
                return;
            }

            // Ensure all messages are hidden before showing the UI
            UISystem.Instance.HideAll();

            // From now on, allow guest users to join the session
            PhotonConnector.Instance.HostAllowGuests();
            Debug.Log("Waiting for new guests to join the room ...");

            if (m_startGameUIPrefab == null)
            {
                Debug.LogError("Start Game UI prefab not set!");
                return;
            }

            // Spawn UI to start the game for Host only or single player
            if (PhotonConnector.Instance != null && PhotonConnector.Instance.IsMasterClient())
            {
                m_startGameUI = Instantiate(m_startGameUIPrefab);
                if (m_startGameUI == null)
                {
                    Debug.LogError("Start Game UI spawning failed!");
                    return;
                }

                var trigger = m_startGameUI.GetComponent<StartGameTrigger>();
                if (trigger == null)
                {
                    Debug.LogError("Start Game UI does not have a valid StartGameTrigger script!");
                    return;
                }
                trigger.OnStartGame = StartGameCallback;

                trigger.SetRoomNumber(PhotonConnector.Instance.Runner.SessionInfo.Name);
            }

            // Ensure the start game UI is visible
            m_startGameUI.SetActive(true);
        }

        protected override void DeinitializeInternal()
        {
            if (m_startGameUI == null)
            {
                return;
            }

            // Ensure the start game UI is hidden.
            m_startGameUI.SetActive(false);
            Destroy(m_startGameUI);
            m_startGameUI = null;
        }

        /// <summary>
        ///     Callback executed after the "Start Game" button of the UI is pressed.
        ///     It denies new guest users from joining after the game started, and progresses to the next game phase.
        /// </summary>
        private static void StartGameCallback()
        {
            Debug.Log("Waiting over, starting game (no more guests are allowed to join the room)");
            // Forbid new guest users from joining the current session
            PhotonConnector.Instance.HostForbidGuests();
            // Proceed with the next game phase
            GameManager.Instance.NextGameplayPhase();
        }
    }
}
