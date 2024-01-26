// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using CrypticCabinet.GameManagement;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Enables or disables the desired game objects only if we are in the specified game phase.
    /// </summary>
    public class EnableForGamePhase : MonoBehaviour
    {
        /// <summary>
        ///     List of game objects that will be enabled if we are in the specified game phase, disabled otherwise.
        /// </summary>
        [SerializeField] private List<GameObject> m_gameObjectsToEnable;

        /// <summary>
        ///     List of game objects that will be disabled if we are in the specified game phase, enabled otherwise.
        /// </summary>
        [SerializeField] private List<GameObject> m_gameObjectsToDisable;

        /// <summary>
        ///     The game phase that establishes if game objects need to be enabled or disabled.
        /// </summary>
        [SerializeField] private GamePhase m_gamePhase;

        private bool m_hasValidGameObject;

        private void Start()
        {
            m_hasValidGameObject = m_gameObjectsToEnable.Count > 0 || m_gameObjectsToDisable.Count > 0;
            if (m_hasValidGameObject)
            {
                GameManager.Instance.OnGamePhaseChanged += OnGamePhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (m_hasValidGameObject && GameManager.Instance)
            {
                GameManager.Instance.OnGamePhaseChanged -= OnGamePhaseChanged;
            }
        }

        private void OnGamePhaseChanged()
        {
            foreach (var toEnable in m_gameObjectsToEnable)
            {
                toEnable.SetActive(GameManager.Instance.GetCurrentGamePhase() == m_gamePhase);
            }
            foreach (var toDisable in m_gameObjectsToDisable)
            {
                toDisable.SetActive(!(GameManager.Instance.GetCurrentGamePhase() == m_gamePhase));
            }
        }
    }
}