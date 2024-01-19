// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using CrypticCabinet.UI;
using CrypticCabinet.Utils;
using UnityEngine;

namespace CrypticCabinet.GameManagement.RoomSetup
{
    /// <summary>
    ///     Represent the game phase that configures the placement of the virtual objects into the real room.
    /// </summary>
    [CreateAssetMenu(fileName = "New Object Spawn Phase", menuName = "CrypticCabinet/Object Spawn Game Phase")]
    public class ObjectSpawningLogicPhase : GamePhase
    {
        /// <summary>
        ///     Prefab that asks the user to confirm the placement of the objects in the room.
        /// </summary>
        [SerializeField] private ObjectPlacementManager m_placementPrefab;

        private ObjectPlacementManager m_spawnedPlacementManager;

        protected override void InitializeInternal()
        {
            if (PhotonConnector.Instance != null && PhotonConnector.Instance.IsMasterClient())
            {
                // Spawn the prefab and hooks the callback for when the user accepts the desired objects' placement.
                m_spawnedPlacementManager = Instantiate(m_placementPrefab);
                m_spawnedPlacementManager.ConfirmObjectPlacementsCallback += ConfirmObjectPlacementsCallback;
            }
            else
            {
                UISystem.Instance.HideAll();
                GameManager.Instance.NextGameplayPhase();
            }
        }

        /// <summary>
        ///     When the user confirms the placement, this callback should be called to move to the next game phase.
        /// </summary>
        private static void ConfirmObjectPlacementsCallback()
        {
            GameManager.Instance.NextGameplayPhase();
        }

        protected override void DeinitializeInternal()
        {
            if (m_spawnedPlacementManager != null)
            {
                m_spawnedPlacementManager.CleanUp();
                m_spawnedPlacementManager.ConfirmObjectPlacementsCallback -= ConfirmObjectPlacementsCallback;
            }
        }
    }
}