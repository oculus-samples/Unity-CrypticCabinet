// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using CrypticCabinet.Photon;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.GameManagement.Puzzles
{
    [MetaCodeSample("CrypticCabinet")]
    [CreateAssetMenu(fileName = "New CrypticCabinet Game Phase", menuName = "CrypticCabinet/Sand Puzzle GamePhase")]
    public class SandPuzzleGamePhase : GamePhase
    {
        [SerializeField] private GameObject[] m_prefabSandPuzzlePrefabs;

        protected override void InitializeInternal()
        {
            if (m_prefabSandPuzzlePrefabs == null || m_prefabSandPuzzlePrefabs.Length <= 0)
            {
                Debug.LogError("No Prefabs specified!");
                return;
            }

            if (PhotonConnector.Instance != null && PhotonConnector.Instance.Runner != null)
            {
                _ = GameManager.Instance.StartCoroutine(HandleSpawn());
            }
            else
            {
                Debug.LogWarning("Couldn't instantiate sand puzzle prefab!");
            }
        }

        private IEnumerator HandleSpawn()
        {
            return m_prefabSandPuzzlePrefabs.Select(puzzlePrefab => new WaitUntil(() => Spawn(puzzlePrefab))).GetEnumerator();
        }

        private bool Spawn(GameObject prefab)
        {
            var spawned = false;
            _ = PhotonConnector.Instance.Runner.Spawn(prefab, onBeforeSpawned: delegate (NetworkRunner runner,
                NetworkObject o)
            {
                spawned = true;
            });
            return spawned;
        }

    }
}