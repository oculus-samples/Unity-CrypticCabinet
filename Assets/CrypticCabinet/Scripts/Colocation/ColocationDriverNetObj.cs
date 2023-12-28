// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using ColocationPackage;
using CrypticCabinet.GameManagement;
using CrypticCabinet.Photon;
using CrypticCabinet.Photon.Colocation;
using CrypticCabinet.UI;
#if !UNITY_EDITOR
using CrypticCabinet.Utils;
#endif
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Oculus.Platform.Models;
using UnityEngine;

namespace CrypticCabinet.Colocation
{
    /// <summary>
    ///     Manages the complete workflow to ensure that all existing and new users will be colocated correctly
    ///     into the room.
    /// </summary>
    public class ColocationDriverNetObj : NetworkBehaviour, INetworkRunnerCallbacks
    {
        /// <summary>
        ///     Callback for when the colocation process completes.
        ///     If succeeded, the callback will be passed a true, otherwise a false.
        /// </summary>
        public static Action<bool> OnColocationCompletedCallback;

        /// <summary>
        ///     If set to true, the colocation process will be skipped. False otherwise.
        /// </summary>
        public static bool SkipColocation;

        /// <summary>
        ///     Callback for when the colocation process is skipped.
        /// </summary>
        public static Action OnColocationSkippedCallback;

        [SerializeField] private GameObject m_networkDataPrefab;
        [SerializeField] private GameObject m_networkDictionaryPrefab;
        [SerializeField] private GameObject m_networkMessengerPrefab;
        [SerializeField] private GameObject m_anchorPrefab;
        [SerializeField] private GameObject m_alignmentAnchorManagerPrefab;

        private AlignmentAnchorManager m_alignmentAnchorManager;
        private ColocationLauncher m_colocationLauncher;
        private Guid m_headsetGuid;
        private User m_oculusUser;

        private Transform m_ovrCameraRigTransform;
        public static ColocationDriverNetObj Instance { get; private set; }

        /// <summary>
        ///     Keeps track of the list of players IDs connected to the same game session.
        /// </summary>
        public PhotonPlayerIDDictionary PlayerIDDictionary { get; private set; }

        private const int RETRY_ATTEMPTS_ALLOWED = 3;
        private int m_currentRetryAttempts;
        private bool m_colocationSuccessful;

        private void Awake()
        {
            Debug.Assert(Instance == null, $"{nameof(ColocationDriverNetObj)} instance already exists");
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            m_colocationLauncher?.DestroyAlignementAnchor();

            if (m_alignmentAnchorManager)
            {
                Destroy(m_alignmentAnchorManager.gameObject);
            }
        }

        public void SetPlayerIdDictionary(PhotonPlayerIDDictionary idDictionary) => PlayerIDDictionary = idDictionary;

        public override void Spawned()
        {
            Runner.AddCallbacks(this);
            Init();
        }

        private async void Init()
        {
            // Initialize colocation regardless on single or multiplayer session.
            UISystem.Instance.ShowMessage("Waiting for colocation to be ready, please wait...", null, -1);
            m_ovrCameraRigTransform = FindObjectOfType<OVRCameraRig>().transform;

#if !UNITY_EDITOR
            if (PhotonConnector.Instance.IsMultiplayerSession)
            {
                // Only get the logged in user if we are not in editor, and we are in multiplayer.
                m_oculusUser = await OculusPlatformUtils.GetLoggedInUser();
            }
#endif

            OnColocationCompletedCallback += delegate (bool b)
            {
                m_colocationSuccessful = b;
            };

            m_headsetGuid = Guid.NewGuid();

            await SetupForColocation();
        }

        private async UniTask SetupForColocation()
        {
            if (HasStateAuthority || !PhotonConnector.Instance.IsMultiplayerSession)
            {
                Debug.Log("SetupForColocation: Setup started by the host");
                _ = Runner.Spawn(m_networkDataPrefab).GetComponent<PhotonNetworkData>();
                _ = Runner.Spawn(m_networkDictionaryPrefab).GetComponent<PhotonPlayerIDDictionary>();
                _ = Runner.Spawn(m_networkMessengerPrefab).GetComponent<PhotonNetworkMessenger>();
            }

            Debug.Log("SetupForColocation: Waiting for network objects to spawn");
            await UniTask.WaitUntil(
                () => NetworkAdapter.NetworkData != null && NetworkAdapter.NetworkMessenger != null &&
                      PlayerIDDictionary != null);

            Debug.Log("SetupForColocation: Adding user to Players dictionary");
            AddToIdDictionary(m_oculusUser?.ID ?? default, Runner.LocalPlayer.PlayerId, m_headsetGuid);

            Debug.Log("SetupForColocation: Initializing network messenger");
            var messenger = (PhotonNetworkMessenger)NetworkAdapter.NetworkMessenger;
            messenger.Init(PlayerIDDictionary);

            // Instantiates the manager for the Oculus shared anchors, specifying the desired anchor prefab.
            Debug.Log("SetupForColocation: Instantiating shared anchor manager");
            var sharedAnchorManager = new SharedAnchorManager { AnchorPrefab = m_anchorPrefab };

            // Initializes the manager that will be responsible for aligning the players through the
            // alignment anchors.
            m_alignmentAnchorManager =
                Instantiate(m_alignmentAnchorManagerPrefab).GetComponent<AlignmentAnchorManager>();

            // Passes the position of the current Oculus player to the alignment anchor manager
            m_alignmentAnchorManager.Init(m_ovrCameraRigTransform);
            Debug.Log("SetupForColocation: Initializing Colocation for the player");

            var eventCode = new Dictionary<CaapEventCode, byte>
            {
                { CaapEventCode.TellOwnerToShareAnchor, 4 },
                { CaapEventCode.TellAnchorRequesterToLocalizeAnchor, 7 },
            };

            // Starts the colocation alignment process
            m_colocationLauncher = new ColocationLauncher();
            m_colocationLauncher.Init(
                m_oculusUser?.ID ?? default,
                m_headsetGuid,
                NetworkAdapter.NetworkData,
                NetworkAdapter.NetworkMessenger,
                sharedAnchorManager,
                m_alignmentAnchorManager,
                eventCode
            );

            // Hooks the event to react to the colocation ready state
            m_colocationLauncher.RegisterOnAfterColocationReady(OnAfterColocationReady);
            if (HasStateAuthority || !PhotonConnector.Instance.IsMultiplayerSession)
            {
                // Being the state authority for the network or a single player, this user will
                // create from scratch a new alignment anchor, which will be used by
                // all other users to colocate in multiplayer, or by the single player.
                m_colocationLauncher.CreateColocatedSpace();
            }
            else
            {
                // Don't try to colocate if we want to skip colocation
                if (SkipColocation)
                {
                    OnColocationSkippedCallback?.Invoke();
                }
                else
                {
                    // An anchor should already exist into the space, and we colocate the player relatively to it.
                    m_colocationLauncher.CreateAnchorIfColocationFailed = false;
                    m_colocationLauncher.OnAutoColocationFailed += OnColocationFailed;
                    m_colocationLauncher.ColocateAutomatically();
                }
            }
        }

        private static void OnAfterColocationReady()
        {
            Debug.Log("Colocation is Ready!");
            OnColocationCompletedCallback?.Invoke(true);
        }

        private static void OnColocationFailed()
        {
            Debug.Log("Colocation failed!");
            OnColocationCompletedCallback?.Invoke(false);
        }

        private static void OnColocationSkipped()
        {
            Debug.Log("Colocation skipped");
            OnColocationSkippedCallback?.Invoke();
        }


        public IEnumerator RetryColocation()
        {
            yield return new WaitForSeconds(5f);

            if (m_colocationSuccessful)
            {
                yield break;
            }

            if (m_currentRetryAttempts >= RETRY_ATTEMPTS_ALLOWED)
            {
                GameManager.Instance.RestartGameplay();
                yield break;
            }

            m_currentRetryAttempts++;

            m_colocationLauncher.ColocateAutomatically();
        }

        private void AddToIdDictionary(ulong oculusId, int playerId, Guid headsetGuid)
        {
            if (HasStateAuthority)
            {
                // Adds itself to the list of connected users.
                // Note: being the state authority, there is no need to inform anyone else,
                // as the current user is the one handling all connected users.
                PlayerIDDictionary.Add(oculusId, playerId, headsetGuid);
            }
            else
            {
                // Being guest users, we tell to the state authority who we are, so that we
                // are included in the list of connected users.
                TellHostToAddToIdDictionaryServerRpc(oculusId, playerId, headsetGuid);
            }
        }

        /// <summary>
        ///     Registers the oculus ID, player ID and headset ID of a remote player to the list of all connected players.
        ///     This is called by all guest users, addressing the change to the state authority, which is the
        ///     main player handling all the connections and info of the connected users.
        /// </summary>
        /// <param name="oculusId">Oculus ID of the user that registers to the list of connected users</param>
        /// <param name="playerId">Player ID of the user that registers to the list of connected users</param>
        /// <param name="headsetGuid">Headset ID of the user that registers to the list of connected users</param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void TellHostToAddToIdDictionaryServerRpc(ulong oculusId, int playerId, Guid headsetGuid)
        {
            PlayerIDDictionary.Add(oculusId, playerId, headsetGuid);
            Debug.Log($"TellHostToAddToIdDictionaryServerRpc: {PlayerIDDictionary}");
        }

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            Debug.Log(
                $"[ColocationDriverNetObj] Player {player} left, removing from dictionary and colocationLauncher");
            var oculusId = PlayerIDDictionary.GetOculusId(player);

            if (oculusId.HasValue)
            {
                m_colocationLauncher.OnPlayerLeft(oculusId.Value);
            }

            PlayerIDDictionary.RemoveUsingNetworkId((int)player);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

        public void OnSceneLoadDone(NetworkRunner runner) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion //INetworkRunnerCallbacks
    }
}