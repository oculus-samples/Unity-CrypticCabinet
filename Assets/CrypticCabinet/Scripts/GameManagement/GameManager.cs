// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using CrypticCabinet.Passthrough;
using CrypticCabinet.Photon;
using CrypticCabinet.SceneManagement;
using CrypticCabinet.UI;
using Fusion;
using Meta.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrypticCabinet.GameManagement
{
    /// <summary>
    ///     This class manages the game phases dynamically.
    ///     This is a singleton for convenience, so that from any part of the game anything can request
    ///     to proceed to the next phase of the gameplay if desired.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>
        ///     Represents the current game phase of the gameplay.
        /// </summary>
        [SerializeField] private GamePhase m_currentGamePhase;

        /// <summary>
        ///     Represents the full list of game phases of the gameplay.
        /// </summary>
        [SerializeField] private List<GamePhase> m_listOfGamePhases;
        [SerializeField] private GameObject m_sceneUnderstanding;
        [SerializeField] private SceneUnderstandingLocationPlacer m_objectPlacer;
        [SerializeField] private PassthroughConfigurator m_passthroughConfiguratorPrefab;

        private int m_currentGamePhaseIndex;
        private bool m_gameplayStarted;
        private Coroutine m_waitForSceneUnderstandingCoroutine;

        public bool GameWasRestarted;
        public Action OnGamePhaseChanged;

        /// <summary>
        /// Performed on Awake to ensure that the initial configuration is valid.
        /// </summary>
        protected override void InternalAwake()
        {
            // Ensure we have a definition for the gameplay phases
            Debug.Assert(m_listOfGamePhases != null, nameof(m_listOfGamePhases) + " != null");
            Debug.Assert(m_listOfGamePhases.Count > 0, nameof(m_listOfGamePhases) + " count > 0");

            // No game phase is initially performed. To start the gameplay we call StartGameplay().
            m_currentGamePhaseIndex = -1;
        }

        /// <summary>
        ///     Starts the gameplay by initializing the current game phase and running it.
        /// </summary>
        public void StartGameplay()
        {
            if (m_currentGamePhaseIndex != -1 && m_currentGamePhase != null)
            {
                // The gameplay already started, nothing to do.
                return;
            }

            UISystem.Instance.ShowMessage("Analyzing the room.\nPlease wait...", null, -1);
            m_sceneUnderstanding.SetActive(true);
            m_objectPlacer.gameObject.SetActive(true);
            if (m_waitForSceneUnderstandingCoroutine != null)
            {
                StopCoroutine(m_waitForSceneUnderstandingCoroutine);
            }
            m_waitForSceneUnderstandingCoroutine = StartCoroutine(nameof(WaitForSceneUnderstanding));

            if (PassthroughChanger.Instance == null && m_passthroughConfiguratorPrefab)
            {
                _ = PhotonConnector.Instance.Runner.Spawn(m_passthroughConfiguratorPrefab);
            }
        }

        /// <summary>
        ///     Restart the flow for the entire gameplay.
        ///     Call this function if you need to re-initialize the game management to the initial state.
        /// </summary>
        public void RestartGameplay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        ///     Jumps to the next gameplay phase.
        ///     If no more phases are available, the end of gameplay is reached and nothing happens.
        /// </summary>
        public void NextGameplayPhase()
        {
            if (!IsLastGameplayPhase())
            {
                GoToGameplayPhase(m_currentGamePhaseIndex + 1);
            }
            else
            {
                m_currentGamePhase = null;
                Log.Debug("End of gameplay reached");
            }
        }

        /// <summary>
        ///     Returns the current game phase of the gameplay.
        /// </summary>
        /// <returns>The current game phase, or null if the gameplay was not started yet.</returns>
        public GamePhase GetCurrentGamePhase()
        {
            return m_currentGamePhase;
        }

        /// <summary>
        /// Wait for all the scene understanding to complete before starting the first game phase.
        /// </summary>
        /// <returns>Waits for scene understanding to complete.</returns>
        private IEnumerator WaitForSceneUnderstanding()
        {
            yield return new WaitUntil(() => m_objectPlacer.HasLoadingCompleted());
            yield return null;

            if (m_gameplayStarted)
            {
                Log.Error("StartGameplay called, but we already started the game.");
                yield break;
            }

            NextGameplayPhase();
        }

        /// <summary>
        ///     True if the current game phase is the last on the gameplay.
        /// </summary>
        /// <returns>True if we are in the last gameplay phase.</returns>
        private bool IsLastGameplayPhase() =>
            m_currentGamePhase != null && m_listOfGamePhases.Count > 0 &&
            m_currentGamePhase == m_listOfGamePhases[^1];

        /// <summary>
        ///     Jumps to the desired gameplay phase by index.
        ///     If the index is invalid, nothing happens.
        /// </summary>
        private void GoToGameplayPhase(int gamePhaseIndex)
        {
            if (gamePhaseIndex > m_listOfGamePhases.Count)
            {
                Log.Error("Index of requested game phase does not exist!");
                return;
            }

            // Deinitialize current phase before jumping to the next one
            if (m_currentGamePhase != null)
            {
                m_currentGamePhase.Deinitialize();
            }

            m_currentGamePhaseIndex = gamePhaseIndex;
            m_currentGamePhase = m_listOfGamePhases[m_currentGamePhaseIndex];
            OnGamePhaseChanged?.Invoke();
            InitializeCurrentGameplayPhase();
        }

        /// <summary>
        ///     Initializes the current gameplay phase, if not initialized yet.
        /// </summary>
        private void InitializeCurrentGameplayPhase()
        {
            if (m_currentGamePhase != null)
            {
                m_currentGamePhase.Initialize();
            }
            else
            {
                Debug.LogError("Current game phase was not set, initialize failed!");
            }
        }
    }
}