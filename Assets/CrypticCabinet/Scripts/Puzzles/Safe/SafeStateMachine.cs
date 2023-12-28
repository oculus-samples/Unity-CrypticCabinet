// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Utils;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Safe
{
    /// <summary>
    ///     Represents the state machine for the safe puzzle.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SafeStateMachine : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField] private NetworkMecanimAnimator m_mecanimAnimator;

        [Header("Events")]
        [Space(10)]
        [Tooltip("Fired when the safe door opened")]
        [SerializeField] private UnityEvent m_onDoorOpened;
        [Tooltip("Fired when the safe drawer opened")]
        [SerializeField] private UnityEvent m_onDrawerOpened;
        [Tooltip("Fired when the safe drawer closed")]
        [SerializeField] private UnityEvent m_onDrawerClosed;

        private static readonly int s_openDoor = Animator.StringToHash("OpenDoor");
        private static readonly int s_openDrawer = Animator.StringToHash("OpenDrawer");
        private static readonly int s_closeDrawer = Animator.StringToHash("CloseDrawer");

        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Start()
        {
            if (m_mecanimAnimator == null)
            {
                m_mecanimAnimator = GetComponent<NetworkMecanimAnimator>();
            }
            this.AssertField(m_mecanimAnimator, nameof(m_mecanimAnimator));

            if (m_mecanimAnimator.Animator == null)
            {
                m_mecanimAnimator.Animator = GetComponent<Animator>();
            }
            this.AssertField(m_mecanimAnimator.Animator, nameof(m_mecanimAnimator.Animator));

            if (m_mecanimAnimator == null)
            {
                Debug.LogError("Mecanim animator not set!");
            }
            else
            {
                if (m_mecanimAnimator.Animator == null)
                {
                    Debug.LogError("Mecanim animator has no animator set!");
                }
            }
        }

        /// <summary>
        ///     Opens the safe door, once this user is the state authority.
        /// </summary>
        [ContextMenu("Open Door")]
        public void OpenDoor()
        {
            m_pendingTasksHandler.TryExecuteAction(RPC_OpenDoor, m_mecanimAnimator.Object.HasStateAuthority);
        }

        /// <summary>
        ///     Opens the safe drawer, once this user is the state authority.
        /// </summary>
        [ContextMenu("Open Drawer")]
        public void OpenDrawer()
        {
            m_pendingTasksHandler.TryExecuteAction(RPC_OpenDrawer, m_mecanimAnimator.Object.HasStateAuthority);
        }

        /// <summary>
        ///     Closes the safe drawer, once this user is the state authority.
        /// </summary>
        [ContextMenu("Close Drawer")]
        public void CloseDrawer()
        {
            m_pendingTasksHandler.TryExecuteAction(RPC_CloseDrawer, m_mecanimAnimator.Object.HasStateAuthority);
        }

        /// <summary>
        ///     Opens the door for all connected players via RPC, if the state machine allows that.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_OpenDoor()
        {
            m_mecanimAnimator.Animator.SetBool(s_openDoor, true);
            m_onDoorOpened?.Invoke();
        }

        /// <summary>
        ///     Opens the drawer for all connected players via RPC, if the state machine allows that.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_OpenDrawer()
        {
            m_mecanimAnimator.Animator.SetBool(s_openDrawer, true);
            m_mecanimAnimator.Animator.SetBool(s_closeDrawer, false);
            m_onDrawerOpened?.Invoke();
        }

        /// <summary>
        ///     Closes the drawer for all connected players via RPC, if the state machine allows that.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_CloseDrawer()
        {
            m_mecanimAnimator.Animator.SetBool(s_openDrawer, false);
            m_mecanimAnimator.Animator.SetBool(s_closeDrawer, true);
            m_onDrawerClosed?.Invoke();
        }

        public void StateAuthorityChanged()
        {
            m_pendingTasksHandler.ExecuteAllOrClear(m_mecanimAnimator.Object.HasStateAuthority);
        }
    }
}
