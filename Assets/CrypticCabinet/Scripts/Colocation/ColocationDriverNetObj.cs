// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using com.meta.xr.colocation;
using CrypticCabinet.GameManagement;
using CrypticCabinet.Photon;
using CrypticCabinet.Photon.Colocation;
using CrypticCabinet.UI;
using CrypticCabinet.Utils;
using Cysharp.Threading.Tasks;
using Fusion;
using Meta.XR.Samples;
using Oculus.Platform.Models;
using UnityEngine;

namespace CrypticCabinet.Colocation
{
    /// <summary>
    ///     Manages the complete workflow to ensure that all existing and new users will be colocated correctly
    ///     into the room.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ColocationDriverNetObj : NetworkBehaviour
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

        [SerializeField] private PhotonNetworkData m_networkData;
        [SerializeField] private PhotonNetworkMessenger m_networkMessenger;
        [SerializeField] private GameObject m_anchorPrefab;

        private SharedAnchorManager m_sharedAnchorManager;
        private AutomaticColocationLauncher m_colocationLauncher;
        private ulong m_playerDeviceUid;
        private User m_oculusUser;

        private Transform m_ovrCameraRigTransform;
        public static ColocationDriverNetObj Instance { get; private set; }

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
        }

        public override void Spawned()
        {
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
#else
            await UniTask.Yield();
#endif

            OnColocationCompletedCallback += delegate (bool b)
            {
                m_colocationSuccessful = b;
            };

            m_playerDeviceUid = OculusPlatformUtils.GetUserDeviceGeneratedUid();

            SetupForColocation();
        }

        private void SetupForColocation()
        {
            Debug.Log("SetupForColocation: Initializing network messenger");
            m_networkMessenger.RegisterLocalPlayer(m_playerDeviceUid);

            // Instantiates the manager for the Oculus shared anchors, specifying the desired anchor prefab.
            Debug.Log("SetupForColocation: Instantiating shared anchor manager");
            m_sharedAnchorManager = new SharedAnchorManager { AnchorPrefab = m_anchorPrefab };

            NetworkAdapter.SetConfig(m_networkData, m_networkMessenger);

            Debug.Log("SetupForColocation: Initializing Colocation for the player");

            // Starts the colocation alignment process
            m_colocationLauncher = new AutomaticColocationLauncher();
            m_colocationLauncher.Init(
                NetworkAdapter.NetworkData,
                NetworkAdapter.NetworkMessenger,
                m_sharedAnchorManager,
                m_ovrCameraRigTransform.gameObject,
                m_playerDeviceUid,
                m_oculusUser?.ID ?? default
            );

            // Hooks the event to react to the colocation ready state
            m_colocationLauncher.ColocationReady += OnColocationReady;
            m_colocationLauncher.ColocationFailed += OnColocationFailed;

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
                    m_colocationLauncher.ColocateAutomatically();
                }
            }
        }

        private static void OnColocationReady()
        {
            Debug.Log("Colocation is Ready!");

            // The AlignCameraToAnchor scripts updates on every frame which messes up Physics and create frame spikes.
            // We need to disable it and add our own align manager that is applied only on recenter
            var alignCamBehaviour = FindObjectOfType<AlignCameraToAnchor>();
            alignCamBehaviour.enabled = false;
            var alignmentGameObject = alignCamBehaviour.gameObject;
            var alignManager = alignmentGameObject.AddComponent<AlignCameraToAnchorManager>();
            alignManager.CameraAlignmentBehaviour = alignCamBehaviour;
            alignManager.RealignToAnchor();

            OnColocationCompletedCallback?.Invoke(true);
        }

        private static void OnColocationFailed(ColocationFailedReason reason)
        {
            Debug.Log($"Colocation failed! {reason}");
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
    }
}