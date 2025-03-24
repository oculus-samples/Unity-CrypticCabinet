// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using CrypticCabinet.Photon.Colocation;
using CrypticCabinet.Utils;
using Cysharp.Threading.Tasks;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Player
{
    /// <summary>
    ///     Represents the main player. It controls the way the user can interact with all aspects of the game,
    ///     the interaction with virtual objects and UI, and the player's identification for other players.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class CrypticCabinetPlayer : NetworkPlayerBehaviour<CrypticCabinetPlayer>
    {
        [SerializeField] private UnityEvent<string> m_onPlayerNameChanged = new();
        [SerializeField] private UnityEvent<bool> m_onIsMasterClientChanged = new();
        [SerializeField] private GameObject m_userNameUI;
        [SerializeField] private FollowUserCamera m_followUserCamera;

        private bool m_isSpawned;

        [Networked(OnChanged = nameof(OnColocationGroupIdChanged))]
        private uint ColocationGroupId { get; set; } = uint.MaxValue;

        [Networked(OnChanged = nameof(OnPlayerNameChanged))]
        private string PlayerName { get; set; }

        [Networked]
        private ulong PlayerUid { get; set; }

        [Networked(OnChanged = nameof(OnIsMasterClientChanged))]
        private NetworkBool IsMasterClient { get; set; }

        protected void Awake()
        {
            PhotonConnector.WhenInstantiated(c => c.OnHostMigrationOccured.AddListener(OnHostMigrationOccured));
            m_followUserCamera = GetComponent<FollowUserCamera>();
        }

        protected void OnDestroy()
        {
            if (PhotonConnector.Instance != null)
            {
                PhotonConnector.Instance.OnHostMigrationOccured.RemoveListener(OnHostMigrationOccured);
            }
        }

        private void OnHostMigrationOccured()
        {
            if (HasStateAuthority)
            {
                UpdateIsMasterClient();
            }
        }

        private void UpdateIsMasterClient() => IsMasterClient = PhotonConnector.Instance.IsMasterClient();

        public static void OnIsMasterClientChanged(Changed<CrypticCabinetPlayer> changed)
        {
            var player = changed.Behaviour;
            player.m_onIsMasterClientChanged?.Invoke(player.IsMasterClient);
        }

        public static void OnColocationGroupIdChanged(Changed<CrypticCabinetPlayer> changed) =>
            changed.Behaviour.UpdateVisibility();

        public static void OnRemoteChanged(Changed<CrypticCabinetPlayer> changed) => changed.Behaviour.UpdateVisibility();

        private void UpdateVisibility()
        {
            // Make sure we only make this player follow the active camera if we are the state authority of that player.
            m_followUserCamera.enabled = HasStateAuthority;

            if (!m_isSpawned)
            {
                // Invisible until spawned
                if (m_userNameUI != null)
                {
                    m_userNameUI.SetActive(false);
                }

                return;
            }

            if (m_userNameUI != null)
            {
                m_userNameUI.SetActive(!HasStateAuthority);
            }
        }

        public static void OnPlayerNameChanged(Changed<CrypticCabinetPlayer> changed) =>
            changed.Behaviour.m_onPlayerNameChanged?.Invoke(changed.Behaviour.PlayerName);

        public override async void Spawned()
        {
            base.Spawned();

            m_isSpawned = true;

            if (HasStateAuthority)
            {
#if !UNITY_EDITOR
                if (PhotonConnector.Instance.IsMultiplayerSession)
                {
                    SetUpPlayerInfo();
                }
                else
                {
                    Debug.Log("Skipping Setup Player Info, as this is a single player session...");
                }
#else
                Debug.Log("Skipping setting up player info in editor");
#endif
                UpdateIsMasterClient();

                // Handle Colocation
                await UniTask.WaitUntil(
                    () => PhotonNetworkData.Instance != null &&
                        PlayerUid != 0);
                // Handle Network Data
                await UniTask.WaitUntil(
                    () => PhotonNetworkData.Instance.GetPlayerWithPlayerId(PlayerUid) != null,
                    cancellationToken: PhotonNetworkData.Instance.GetCancellationTokenOnDestroy());
                ColocationGroupId = PhotonNetworkData.Instance.GetPlayerWithPlayerId(PlayerUid)?.colocationGroupId ?? uint.MaxValue;
            }

            UpdateVisibility();
        }

        private async void SetUpPlayerInfo()
        {
            do
            {
                var user = await OculusPlatformUtils.GetLoggedInUser();
                if (user == null)
                {
                    return;
                }
                PlayerName = user.OculusID;
                PlayerUid = OculusPlatformUtils.GetUserDeviceGeneratedUid();
                await UniTask.Yield();
            } while (this != null && string.IsNullOrWhiteSpace(PlayerName));
        }
    }
}