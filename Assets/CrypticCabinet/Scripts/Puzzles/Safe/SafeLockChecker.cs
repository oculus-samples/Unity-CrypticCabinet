// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Utils;
using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Safe
{
    /// <summary>
    ///     Checks the current values for the <see cref="SafeLockDialer"/>, and triggers the relative events on correct or
    ///     wrong combination.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SafeLockChecker : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField] private SafeLockDialer m_safeLockDialer1;
        [SerializeField] private SafeLockDialer m_safeLockDialer2;
        [SerializeField] private SafeLockDialer m_safeLockDialer3;

        [Header("Events")]
        [Space(10)]
        [Tooltip("Fired when the correct combination has been set")]
        [SerializeField] private UnityEvent m_onCorrectCombination;

        [Tooltip("Fired when the wrong combination has been set")]
        [SerializeField] private UnityEvent m_onWrongCombination;

        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Start()
        {
            this.AssertField(m_safeLockDialer1, nameof(m_safeLockDialer1));
            this.AssertField(m_safeLockDialer2, nameof(m_safeLockDialer2));
            this.AssertField(m_safeLockDialer2, nameof(m_safeLockDialer2));
        }

        /// <summary>
        ///     Callback for when the user pushes the button to attempt the combination to open the safe.
        ///     NNote: this can also be called in editor using the context menu.
        /// </summary>
        [ContextMenu("Submit combination")]
        public void SubmitCombination()
        {
            // Note: we need to make sure that when we submit the combination, we are the state authority.
            // To do this, we try to execute the expected operations: if we are state authority we execute them
            // straight away, otherwise we enqueue them as pending until we acquire the state authority.
            // This is to make sure that these actions are performed for all users by the state authority player,
            // which is the only one able to change values for the safe dials and the networked objects.
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                if (m_safeLockDialer1.CombinationGuessed
                    && m_safeLockDialer2.CombinationGuessed
                    && m_safeLockDialer3.CombinationGuessed)
                {
                    Debug.Log("Correct safe combination submitted");
                    m_onCorrectCombination?.Invoke();
                }
                else
                {
                    Debug.Log("Wrong safe combination submitted");
                    m_onWrongCombination?.Invoke();
                }
            }, HasStateAuthority);
        }

        /// <summary>
        ///     Ensures that when the state authority has changed (acquired in our case) the list of
        ///     pending actions to perform is executed by this user.
        /// </summary>
        public void StateAuthorityChanged()
        {
            m_pendingTasksHandler.ExecuteAllOrClear(HasStateAuthority);
        }
    }
}
