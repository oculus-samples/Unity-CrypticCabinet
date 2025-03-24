// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrypticCabinet.Colocation;
using CrypticCabinet.GameManagement;
using CrypticCabinet.Photon.Utils;
using CrypticCabinet.UI;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CrypticCabinet.Photon
{
    /// <summary>
    ///     Represents different game session states relevant to the Photon workflow.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public enum GameSessionStatus
    {
        /// <summary>
        ///     The Host needs to finish room setup before guests are allowed to join the game session.
        /// </summary>
        WAITING_HOST_SETUP_TO_BE_COMPLETED,

        /// <summary>
        ///     The host finished room setup and did not start the game yet. All guest users can join the game session.
        /// </summary>
        ALLOW_GUESTS_TO_JOIN,

        /// <summary>
        ///     The host started the game, it is too late for new users to join the game session: join is forbidden.
        /// </summary>
        FORBID_NEW_GUESTS_FROM_JOINING,

        /// <summary>
        ///     The host is destroying the current game session, and all guest users in the room need to leave.
        /// </summary>
        DISCONNECT_ALL_GUESTS
    }

    /// <summary>
    ///     Manages network connection between the App and the Photon Fusion cloud services.
    ///     When in a multiplayer session, it is possible to connect as the main player responsible for the scene
    ///     configuration (Host), or as guest with colocation (Join).
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class PhotonConnector : Singleton<PhotonConnector>, INetworkRunnerCallbacks
    {
        private const string GAME_SESSION_STATUS = "game_session_status";

        [SerializeField] private NetworkSceneManagerDefault m_sceneManager;
        [SerializeField] private NetworkRunner m_networkRunnerPrefab;
        [SerializeField] private NetworkObject m_playerPrefab;
        [SerializeField] private string m_chosenRoomName;
        [SerializeField] private string m_selectedRegionCode;

        /// <summary>
        ///     Occurs when a Host has changed and a new Runner needs to replace the old one.
        /// </summary>
        public UnityEvent OnHostMigrationOccured;

        /// <summary>
        ///     Occurs when the Host should start loading its scene while the connection is being finalized.
        /// </summary>
        [SerializeField] private UnityEvent m_onRequestHostInitialization;

        /// <summary>
        ///     Occurs when a successful connection to the room was established.
        /// </summary>
        [SerializeField] private UnityEvent m_onConnectedToRoom;

        /// <summary>
        ///     Occurs when the attempt to connect to Photon Fusion failed.
        /// </summary>
        [SerializeField] private UnityEvent m_onConnectionFailed;

        /// <summary>
        ///     Occurs when the master client connects to the server.
        ///     This is the one responsible for spawning the initial objects for the
        ///     game and to initiate the colocation for all players.
        /// </summary>
        [SerializeField] private UnityEvent<NetworkRunner> m_onMasterClientSessionAlive;

        public bool JoinedActiveGameSession { get; private set; }

        private NetworkObject m_playerObject;
        private bool m_guestIsJoiningRoom;
        private bool m_guestIsInLobby;
        private Coroutine m_lobbyTimeoutCoroutine;

        /// <summary>
        ///     The Runner that handles all communication between this App and Photon Fusion servers.
        /// </summary>
        public NetworkRunner Runner { get; private set; }

        /// <summary>
        ///     True if the current session is multiplayer.
        ///     False if the current session is single player.
        /// </summary>
        public bool IsMultiplayerSession { get; private set; }

        /// <summary>
        ///     Handles single player flow: the player is the only one in the game session, and none else can join.
        ///     The room name is randomly generated.
        /// </summary>
        public void StartSinglePlayerSession()
        {
            Debug.Log("Initiating single player... Creating random name room invisible to other users.");
            IsMultiplayerSession = false;
            m_chosenRoomName = RoomNameGenerator.GenerateRoom();
            BeginHosting(m_chosenRoomName);
        }

        /// <summary>
        ///     Handles multiplayer flow: the UI to choose Host or Guest is shown.
        ///     Host shows a randomly generated 6 digit room name, which can be communicated to guests.
        ///     Guests will be able to specify the desired room name and join.
        /// </summary>
        public void StartMultiplayerSession()
        {
            Debug.Log("Initiating multiplayer... Showing user network selection UI.");
            IsMultiplayerSession = true;
            ShowNetworkSelectionMenu();
        }

        /// <summary>
        ///     Shutdown the previous runner and destroy it.
        /// </summary>
        public async Task Shutdown()
        {
            if (Runner != null)
            {
                // Shutdown the previous runner and destroy it.
                Runner.RemoveCallbacks(this);
                await Runner.Shutdown();
                Destroy(Runner);
                Runner = null;
            }
        }

        /// <summary>
        ///     Initializes the Network Runner, ensuring it is bound to this class.
        /// </summary>
        private async Task SetupForNetworkRunner()
        {
            await Shutdown();

            // Spawn network runner
            Runner = Instantiate(m_networkRunnerPrefab);
            Runner.name = "NetworkRunner";
            // Hook the callbacks to this class instance
            Runner.AddCallbacks(this);
            // Enable input processing across the Photon network
            Runner.ProvideInput = true;
        }

        /// <summary>
        ///     Emulates the user selecting a single player session gameplay.
        /// </summary>
        [ContextMenu("Single Player Host")]
        private void DebugSinglePlayerHost()
        {
            UISystem.Instance.HideAll();
            IsMultiplayerSession = false;
            BeginHosting(m_chosenRoomName);
        }

        /// <summary>
        ///     Emulates the user selecting a multiplayer session gameplay as Host.
        /// </summary>
        [ContextMenu("Multiplayer Host")]
        private void DebugMultiplayerHost()
        {
            UISystem.Instance.HideAll();
            IsMultiplayerSession = true;
            BeginHosting(m_chosenRoomName);
        }

        /// <summary>
        ///     Emulates the user selecting a multiplayer session gameplay as Guest.
        /// </summary>
        [ContextMenu("Join")]
        private void DebugJoin()
        {
            UISystem.Instance.HideAll();
            BeginJoining(m_chosenRoomName);
        }

        private void BeginHosting(string roomName)
        {
            UISystem.Instance.HideAll();
            m_chosenRoomName = roomName;
            StartHost();
        }

        private void BeginJoining(string roomName)
        {
            UISystem.Instance.HideAll();
            m_chosenRoomName = roomName;
            StartClient();
        }


        private void StartHost() => StartConnection(true);

        private void StartClient()
        {
            if (string.IsNullOrWhiteSpace(m_chosenRoomName))
            {
                UISystem.Instance.ShowMessage("Enter room code to join", ShowNetworkSelectionMenu);
            }
            else
            {
                StartConnection(false);
            }
        }

        private void StartConnection(bool isHost) => StartConnectionAsync(isHost);

        private async void StartConnectionAsync(bool isHost)
        {
            if (isHost)
            {
                // Notify that the content of the host should now be loaded
                m_onRequestHostInitialization?.Invoke();
            }

            Debug.Log("StartConnection");
            await SetupForNetworkRunner();

            UISystem.Instance.ShowMessage(
                IsMultiplayerSession ?
                    "Connecting to Photon, please wait..." :
                    "Preparing single player session, please wait...");

            ColocationDriverNetObj.OnColocationCompletedCallback += OnColocationReady;
            ColocationDriverNetObj.OnColocationSkippedCallback += OnColocationSkipped;
            // We colocate even if in single player, to keep aligned with the room.
            ColocationDriverNetObj.SkipColocation = false;
            await Task.Delay(1000); // delay before connecting to show the message
            await Connect(isHost);
        }

        private async Task Connect(bool isHost)
        {
            var sessionName = string.IsNullOrWhiteSpace(m_chosenRoomName) ? null : m_chosenRoomName;
            if (isHost)
            {
                // If we are Single Player or Host and no room name was given we create a random 6 character room name
                if (!IsMultiplayerSession || string.IsNullOrWhiteSpace(sessionName) || GameManager.Instance.GameWasRestarted)
                {
                    sessionName = RoomNameGenerator.GenerateRoom();
                    // Given the scope we don't check for collision with existing room name, but checking if the room exists
                    // in the lobby would be a great validator to make sure we don't join someone else session.
                }

                // The host creates a Lobby and a Game session inside it.
                // The game session stores properties visible to all clients in the lobby, that establish if it is
                // still possible to join a game session or if the game already started and no more guests are allowed.
                // Note: if IsMultiplayerSession is false, no other user can see or join the created room.
                _ = await HostCreateRoomFromLobby(sessionName);
            }
            else
            {
                // Guest users connect to the lobby, and check if any game session with the desired room name
                // exists and is accessible.
                await GuestJoinLobby(sessionName);
            }
        }

        /// <summary>
        ///     Disconnects the current player from the active room.
        ///     Note: this can also be triggered via editor using the context menu.
        /// </summary>
        [ContextMenu("Disconnect from room")]
        public async Task DisconnectFromRoom()
        {
            // The host has now left an active game session.
            JoinedActiveGameSession = false;

            if (Runner == null)
            {
                Debug.LogError("Runner does not exist, cannot call DisconnectFromRoom()");
                return;
            }

            if (Runner.IsSharedModeMasterClient)
            {
                // If this is the Host, all players need to be kicked out from the current game session.                
                Instance.HostDisconnectAllFromRoom();
            }

            await RestartFromMainMenu("You Left the Room");
        }

        #region Colocation Callback

        private void OnColocationReady(bool success)
        {
            if (IsMultiplayerSession)
            {
                UISystem.Instance.ShowMessage(success ? "Colocation Ready, please wait..." : "Colocation Failed!");
            }
        }

        private static void OnColocationSkipped()
        {
            Debug.Log("Colocation Skipped (OnColocationSkipped called)");
        }

        #endregion


        #region UI Controller

        public void ShowNetworkSelectionMenu() =>
            UISystem.Instance.ShowNetworkSelectionMenu(
                BeginHosting,
                BeginJoining,
                m_chosenRoomName
            );

        #endregion


        #region Master client (Host multiplayer / Master single player)

        /// <summary>
        ///     Checks if the local player is Host (multiplayer) or the main player of a single player session.
        /// </summary>
        /// <returns>True if the local player is Host in multiplayer, or in single player mode.</returns>
        public bool IsMasterClient()
        {
            return (Runner != null && Runner.IsSharedModeMasterClient) || Runner.GameMode == GameMode.Single;
        }

        /// <summary>
        ///     Starts the game setup managed by the Master Client.
        ///     This implies the room setup, the spawning of all relevant objects, and the colocation setup (if required).
        /// </summary>
        private void InitiateMasterClientSetup(NetworkRunner runner)
        {
            if (!IsMasterClient())
            {
                return;
            }

            Debug.Log("Instantiate Room Scene objects");
            foreach (var instantiator in PhotonInstantiator.Instances)
            {
                instantiator.TryInstantiate();
            }

            if (IsMultiplayerSession)
            {
                // Show a popup with OK button with the generated room name.
                // This will be used by the Host to invite guest users to join the game.
                UISystem.Instance.ShowMessageWithOneButton(
                    $"Game session created, use the following room code to invite other users: {runner.SessionInfo.Name}",
                    "Confirm", () =>
                    {
                        // The subscribers of this event should take care for spawning the
                        // other relevant network objects, including the ones for colocation.
                        m_onMasterClientSessionAlive?.Invoke(runner);
                    }
                );
            }
            else
            {
                m_onMasterClientSessionAlive?.Invoke(runner);
            }
        }

        /// <summary>
        ///     Shuts down any ongoing game and clears the state to the initial one, showing the Main Menu.
        /// </summary>
        private async Task RestartFromMainMenu(string message)
        {
            await Runner.Shutdown();
            Runner = null;

            UISystem.Instance.ShowMessage(message, () =>
            {
                // Restart gameplay, or we get stuck on the previously active game phase.
                GameManager.Instance.RestartGameplay();
                UISystem.Instance.ShowMainMenu();
            });
        }

        #endregion


        #region Host actions

        /// <summary>
        ///     Disconnects all the guests of the current room, destroying the current game session.
        ///     This should be called when the Host disconnects from a game, so that all remaining guest
        ///     users will not be stuck inside it.
        /// </summary>
        public void HostDisconnectAllFromRoom()
        {
            _ = HostDisconnectAllGuestsFromRoom();
        }

        /// <summary>
        ///     Allows new Guest users to join the current game session (if any).
        /// </summary>
        public void HostAllowGuests()
        {
            _ = HostAllowGuestsToJoinRoom();
        }

        /// <summary>
        ///     Forbids new Guest users from joining the current game session (if any).
        /// </summary>
        public void HostForbidGuests()
        {
            _ = HostForbidNewGuestsFromJoiningRoom();
        }

        private async Task<bool> HostCreateRoomFromLobby(string roomName)
        {
            var lobbyName = GetLobbyName(roomName);

            var customProps = new Dictionary<string, SessionProperty>
            {
                [GAME_SESSION_STATUS] = IsMultiplayerSession ? (int)GameSessionStatus.WAITING_HOST_SETUP_TO_BE_COMPLETED
                                                             : (int)GameSessionStatus.FORBID_NEW_GUESTS_FROM_JOINING
            };

            var args = new StartGameArgs
            {
                GameMode = IsMultiplayerSession ? GameMode.Shared : GameMode.Single,
                SessionName = roomName,
                CustomLobbyName = lobbyName,
                Scene = SceneManager.GetActiveScene().buildIndex,
                SceneManager = m_sceneManager,
                DisableClientSessionCreation = false,
                IsVisible = IsMultiplayerSession,
                SessionProperties = customProps
            };

            if (!string.IsNullOrEmpty(m_selectedRegionCode))
            {
                args.CustomPhotonAppSettings = CreateAppSettingsForRegion(m_selectedRegionCode);
            }

            var joined = await Runner.StartGame(args);
            var success = joined.Ok;
            if (success)
            {
                if (!IsMultiplayerSession)
                {
                    UISystem.Instance.ShowMessage("Preparing room setup, please wait...");
                }
                m_onConnectedToRoom?.Invoke();
            }
            else
            {
                var errorMsg = $"Connection failed, please make sure to have access to Internet.\nFailure reason: {joined.ShutdownReason}";
                Debug.LogError(errorMsg);
                UISystem.Instance.ShowMessage(errorMsg, ShowNetworkSelectionMenu);
                m_onConnectionFailed?.Invoke();
            }

            // The host has now joined an active game session, if success.
            JoinedActiveGameSession = success;

            return success;
        }

        [ContextMenu("Host create room from lobby")]
        private async Task<bool> HostCreateRoomFromLobby()
        {
            return await HostCreateRoomFromLobby(m_chosenRoomName);
        }

        [ContextMenu("Host allow guests to join room")]
        private bool HostAllowGuestsToJoinRoom()
        {
            if (Runner.SessionInfo == null)
            {
                Debug.LogError("Call to HostAllowGuestsToJoinRoom without a valid session is not allowed!");
                return false;
            }

            var customProps = new Dictionary<string, SessionProperty>
            {
                [GAME_SESSION_STATUS] = (int)GameSessionStatus.ALLOW_GUESTS_TO_JOIN
            };
            var success = Runner.SessionInfo.UpdateCustomProperties(customProps);

            if (!success)
            {
                Debug.LogError("Host was unable to change room prop to allow guest users to join!");
            }

            return success;
        }

        [ContextMenu("Host forbid guests from joining room")]
        private bool HostForbidNewGuestsFromJoiningRoom()
        {
            if (Runner.SessionInfo == null)
            {
                Debug.LogError("Call to HostForbidNewGuestsFromJoiningRoom without a valid session is not allowed!");
                return false;
            }

            var customProps = new Dictionary<string, SessionProperty>
            {
                [GAME_SESSION_STATUS] = (int)GameSessionStatus.FORBID_NEW_GUESTS_FROM_JOINING
            };
            var success = Runner.SessionInfo.UpdateCustomProperties(customProps);

            if (!success)
            {
                Debug.LogError("Host was unable to change room prop to forbid new guest users to join!");
            }

            return success;
        }

        [ContextMenu("Host disconnect all guests from room")]
        private bool HostDisconnectAllGuestsFromRoom()
        {
            if (Runner.SessionInfo == null)
            {
                Debug.LogError("Call to HostDisconnectAllGuestsFromRoom without a valid session is not allowed!");
                return false;
            }

            var customProps = new Dictionary<string, SessionProperty>
            {
                [GAME_SESSION_STATUS] = (int)GameSessionStatus.DISCONNECT_ALL_GUESTS
            };
            var success = Runner.SessionInfo.UpdateCustomProperties(customProps);

            if (!success)
            {
                Debug.LogError("Host was unable to change room prop to disconnect all users of the room!");
            }

            return success;
        }

        #endregion


        #region Guest actions

        /// <summary>
        ///     Call this function if the Guest wants to leave the lobby.
        /// </summary>
        public void GuestLeaveLobby()
        {
            // The guest has now left an active game session / lobby.
            JoinedActiveGameSession = false;
            m_guestIsInLobby = false;
            Runner.Disconnect(Runner.LocalPlayer);
        }

        private async Task GuestJoinLobby(string roomToSearch)
        {
            var lobbyName = GetLobbyName(roomToSearch);
            var result = await Runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
            var success = result.Ok;

            if (success)
            {
                // Guest user is in the lobby, and starts listening to OnSessionListUpdated callbacks to
                // check if the desired game session is available and accessible.
                // If the user clicks on the button "Back", the guest leaves the lobby.
                UISystem.Instance.ShowMessageWithOneButton(
                    $"Joined lobby for game '{roomToSearch}', waiting for game session to start...",
                    "Back",
                    () =>
                    {
                        GuestLeaveLobby();
                        ShowNetworkSelectionMenu();
                    });
                m_guestIsInLobby = true;
            }
            else
            {
                var errorMsg = $"Connection failed, please make sure to have access to Internet.\nFailure reason: {result.ShutdownReason}";
                Debug.LogError(errorMsg);
                UISystem.Instance.ShowMessage(errorMsg, ShowNetworkSelectionMenu);
                m_onConnectionFailed?.Invoke();
                m_guestIsInLobby = false;
            }
        }

        private async void GuestTryJoinRoom(NetworkRunner runner, IReadOnlyCollection<SessionInfo> sessionList)
        {
            if (!IsMultiplayerSession)
            {
                Debug.LogError("GuestTryJoinRoom should only be called within a multiplayer session!");
                return;
            }

            // The guest needs to look for the desired room and check if it can be accessed.
            Debug.Log($"Session List Updated with {sessionList.Count} session(s), looking for desired room");

            var session = sessionList.FirstOrDefault(sessionItem => sessionItem.Name == m_chosenRoomName);

            if (session != null && session.Properties.TryGetValue(GAME_SESSION_STATUS, out var gameSessionStatusProp) && gameSessionStatusProp.IsInt)
            {
                var gameSessionStatus = (GameSessionStatus)gameSessionStatusProp.PropertyValue;
                switch (gameSessionStatus)
                {
                    case GameSessionStatus.WAITING_HOST_SETUP_TO_BE_COMPLETED:
                        {
                            break;
                        }
                    case GameSessionStatus.ALLOW_GUESTS_TO_JOIN:
                        {
                            if (m_guestIsJoiningRoom)
                            {
                                // Do not try to join the room if already joining.
                                break;
                            }
                            m_guestIsJoiningRoom = true;

                            UISystem.Instance.HideAll();
                            UISystem.Instance.ShowMessage(
                                $"Joining game session of room '{runner.SessionInfo.Name}', please wait...");

                            var args = new StartGameArgs
                            {
                                GameMode = GameMode.Shared,
                                SessionName = m_chosenRoomName,
                                Scene = SceneManager.GetActiveScene().buildIndex,
                                SceneManager = m_sceneManager,
                                DisableClientSessionCreation = true,
                                IsVisible = true
                            };

                            if (!string.IsNullOrEmpty(m_selectedRegionCode))
                            {
                                args.CustomPhotonAppSettings = CreateAppSettingsForRegion(m_selectedRegionCode);
                            }

                            var joined = await Runner.StartGame(args);
                            if (!joined.Ok)
                            {
                                var errorMsg = $"Unable to join game session of room, reason: {joined.ShutdownReason}";
                                Debug.LogError(errorMsg);
                                UISystem.Instance.HideAll();
                                UISystem.Instance.ShowMessage(errorMsg, ShowNetworkSelectionMenu);
                                m_onConnectionFailed?.Invoke();
                            }
                            else
                            {
                                m_onConnectedToRoom?.Invoke();
                                // The guest has now joined an active game session.
                                JoinedActiveGameSession = true;
                            }

                            // Allow new join attempts for the guest user.
                            m_guestIsJoiningRoom = false;
                            break;
                        }
                    case GameSessionStatus.FORBID_NEW_GUESTS_FROM_JOINING:
                        {
                            UISystem.Instance.HideAll();
                            GuestLeaveLobby();
                            UISystem.Instance.ShowMessage("The game already started, joining after is forbidden. " +
                                                          "Leaving lobby.", ShowNetworkSelectionMenu);
                            break;
                        }
                    case GameSessionStatus.DISCONNECT_ALL_GUESTS:
                        {
                            GuestLeaveLobby();
                            UISystem.Instance.ShowMessage(
                                "The game just finished, joining after is forbidden. Leaving lobby.", ShowNetworkSelectionMenu);
                            break;
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                }
            }
            else
            {
                // Game session not found
                UISystem.Instance.HideAll();
                UISystem.Instance.ShowMessage(
                    $"The requested room was not found or no longer available: {m_chosenRoomName}",
                    () =>
                    {
                        GuestLeaveLobby();
                        ShowNetworkSelectionMenu();
                    });
            }
        }

        #endregion


        #region Utils

        private static string GetLobbyName(string roomName)
        {
            return roomName + "_lobby";
        }

        #endregion


        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) =>
            Debug.Log($"OnPlayerJoined playerRef: {player}");

        public async void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"OnPlayerLeft playerRef: {player}");

            // Check if the player that left was the Host
            var session = runner.SessionInfo;
            if (session != null && session.Properties.TryGetValue(GAME_SESSION_STATUS, out var gameSessionStatusProp) &&
                gameSessionStatusProp.IsInt)
            {
                var gameSessionStatus = (GameSessionStatus)gameSessionStatusProp.PropertyValue;
                if (gameSessionStatus == GameSessionStatus.DISCONNECT_ALL_GUESTS)
                {
                    // The Host disconnected, as a guest we need to leave the room and disconnect.
                    await RestartFromMainMenu("The host disconnected, leaving the game.");
                }
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Network runner has shut down, reason: {shutdownReason}");
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            UISystem.Instance.ShowMessage(
                $"Connected To Photon Session '{runner.SessionInfo.Name}', please wait...");

            if (IsMasterClient())
            {
                InitiateMasterClientSetup(runner);
            }

            // Spawn the remote player placeholder for the other players
            m_playerObject = runner.Spawn(m_playerPrefab);
            runner.SetPlayerObject(runner.LocalPlayer, m_playerObject);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner)
        {
            // If this is the Host, all players need to be kicked out from the current game session.
            if (runner.IsSharedModeMasterClient)
            {
                Instance.HostDisconnectAllFromRoom();
            }

            Debug.Log("Network runner disconnected from server");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"Network runner connection failed to remote address {remoteAddress} for reason {reason}");
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (IsMasterClient())
            {
                // The host does not need to do anything on game session update.
                return;
            }

            // If the guest is not already joining the room, try to join.
            if (m_guestIsInLobby && !m_guestIsJoiningRoom)
            {
                GuestTryJoinRoom(runner, sessionList);
            }
            else
            {
                // If the guest is in the room, make sure the current game is still in a valid state.
                var session = sessionList.FirstOrDefault(sessionItem => sessionItem.Name == m_chosenRoomName);

                if (session != null &&
                    session.Properties.TryGetValue(GAME_SESSION_STATUS, out var gameSessionStatusProp) &&
                    gameSessionStatusProp.IsInt)
                {
                    var gameSessionStatus = (GameSessionStatus)gameSessionStatusProp.PropertyValue;
                    if (gameSessionStatus == GameSessionStatus.DISCONNECT_ALL_GUESTS)
                    {
                        // The Host disconnected, as a guest we need to leave the room and disconnect.
                        GuestLeaveLobby();
                        UISystem.Instance.ShowMessage("The host disconnected, leaving the game.", ShowNetworkSelectionMenu);
                    }
                }
                else
                {
                    GuestLeaveLobby();
                    UISystem.Instance.ShowMessage("The game session was terminated, leaving the game.", ShowNetworkSelectionMenu);
                }
            }
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) =>
            OnHostMigrationOccured?.Invoke();

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (!IsMultiplayerSession)
            {
                // Trigger the master client setup for single player.
                // Note: if multiplayer, we wait instead for the OnConnectedToServer callback.
                InitiateMasterClientSetup(Runner);

                // Spawn the remote player placeholder for the other players
                m_playerObject = runner.Spawn(m_playerPrefab);
                runner.SetPlayerObject(runner.LocalPlayer, m_playerObject);
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        #endregion // INetworkRunnerCallbacks

        #region Connection Region Configuration

        private static AppSettings CreateAppSettingsForRegion(string region)
        {
            var appSettings = PhotonAppSettings.Instance.AppSettings.GetCopy();

            if (!string.IsNullOrEmpty(region))
            {
                appSettings.FixedRegion = region.ToLower();
            }

            return appSettings;
        }

        #endregion
    }
}