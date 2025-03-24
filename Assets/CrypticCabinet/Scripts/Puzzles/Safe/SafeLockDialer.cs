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
    ///     Defines the behavior for the lock dialer of the safe.
    ///     It controls the way it spins, the values it can assume, and the mesh rotation for the currently chosen number.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public sealed class SafeLockDialer : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField] private int m_initialNumber = 1;
        [SerializeField] private float m_rotationDuration = 0.1f;
        [SerializeField] private int m_correctNumber;
        [SerializeField] private GameObject m_visualDialMesh;

        [Space(10)]
        [Header("Events")]
        [Space(10)]
        [Tooltip("Triggered when the dial starts rotating")]
        [SerializeField] private UnityEvent m_onStartRotation;

        public bool CombinationGuessed => m_correctNumber == m_currentNumber;

        /// <summary>
        ///     Holds the networked value this dial should have on all clients.
        ///     Whenever it gets updated, all clients respond to the change re-adapting the dial rotation.
        /// </summary>
        [Networked]
        private int NetworkedCurrentNumber { get; set; }
        /// <summary>
        ///     The initial rotation when the asset has been spawned by the Network Runner.
        ///     This is to ensure all players use this rotation as reference when calculating the
        ///     target rotation whenever the dial has been rotated.
        /// </summary>
        [Networked]
        private Quaternion InitialRotation { get; set; }

        /// <summary>
        ///     If true, players can rotate this dial.
        /// </summary>
        [Networked]
        private bool CanRotate { get; set; }

        /// <summary>
        ///     The number currently showing on the dial.
        /// </summary>
        private int m_currentNumber;

        private const int ROTATION_DEGREES_PER_UNIT = 36;

        private float m_elapsedTime;
        private Quaternion m_startRotation;
        private Quaternion m_targetRotation;

        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Start()
        {
            m_elapsedTime = 0;
            UpdateTargetRotation(NetworkedCurrentNumber);
        }

        public override void Spawned()
        {
            base.Spawned();

            // Set initial values for all clients
            NetworkedCurrentNumber = m_currentNumber = m_initialNumber;
            CanRotate = true;

            // Note: only the host establishes the initial rotation on spawn.
            this.AssertField(m_visualDialMesh, nameof(m_visualDialMesh));
            if (m_visualDialMesh != null && HasStateAuthority)
            {
                InitialRotation = m_visualDialMesh.transform.localRotation;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (m_currentNumber != NetworkedCurrentNumber)
            {
                Debug.Log($"Updating dial rotation within FixedUpdateNetwork, current is {m_currentNumber} and networked is {NetworkedCurrentNumber}");
                UpdateTargetRotation(NetworkedCurrentNumber);
            }

            if (m_startRotation.Equals(m_targetRotation))
            {
                return;
            }

            if (m_elapsedTime < m_rotationDuration)
            {
                var t = m_elapsedTime / m_rotationDuration;
                m_visualDialMesh.transform.localRotation = Quaternion.Slerp(m_startRotation, m_targetRotation, t);
                m_elapsedTime += Time.deltaTime;
            }
            else
            {
                m_startRotation = m_targetRotation;
                m_visualDialMesh.transform.localRotation = m_startRotation;
                m_elapsedTime = 0;
            }
        }

        private void UpdateTargetRotation(int newNumber)
        {
            Debug.Log($"Updating target rotation for number {newNumber}");

            var meshRotation = m_visualDialMesh.transform.localRotation;
            m_startRotation = meshRotation;
            // Note: 0 = 36 degrees, 1 = 0 degrees, 2 = -36 degrees, etc.
            var targetDeltaAngle = ROTATION_DEGREES_PER_UNIT - newNumber * ROTATION_DEGREES_PER_UNIT;
            var deltaRotation = Quaternion.Euler(targetDeltaAngle, 0, 0);
            m_targetRotation = InitialRotation * deltaRotation;

            m_currentNumber = newNumber;
            // Start again the rotation timing for the new rotation.
            // This ensures that if we are already rotating, we have the same rotation duration
            // for this new incoming rotation change.
            m_elapsedTime = 0;

            // Fire event to trigger what is needed when the rotation starts (e.g. audio)
            m_onStartRotation?.Invoke();
        }

        [ContextMenu("Next Number")]
        public void NextNumber()
        {
            if (!CanRotate)
            {
                Debug.Log("NextNumber failed: CanRotate is set to false");
                return;
            }

            EnsureHaveStateAuthority();
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                NetworkedCurrentNumber = (NetworkedCurrentNumber + 1) % 10;
                Debug.Log($"Next number, new dialer value is {NetworkedCurrentNumber}");
            }, HasStateAuthority);
        }

        [ContextMenu("Previous Number")]
        public void PreviousNumber()
        {
            if (!CanRotate)
            {
                Debug.Log("PreviousNumber failed: CanRotate is set to false");
                return;
            }

            EnsureHaveStateAuthority();
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                // Adding 9 ensures that negative values wrap around
                NetworkedCurrentNumber = (NetworkedCurrentNumber + 9) % 10;
                Debug.Log($"Previous number, new dialer value is {NetworkedCurrentNumber}");
            }, HasStateAuthority);
        }

        /// <summary>
        ///     Allows the dial to spin.
        ///     Note: this can also be called by the editor using the context menu.
        /// </summary>
        [ContextMenu("Enable Rotation")]
        public void EnableRotation()
        {
            EnsureHaveStateAuthority();
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                CanRotate = true;
                Debug.Log("EnableRotation executed");
            }, HasStateAuthority);
        }

        /// <summary>
        ///     Disable the dial rotation.
        ///     Note: this can also be called by the editor using the context menu.
        /// </summary>
        [ContextMenu("Disable Rotation")]
        public void DisableRotation()
        {
            EnsureHaveStateAuthority();
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                CanRotate = false;
                Debug.Log("DisableRotation executed");
            }, HasStateAuthority);
        }

        private void EnsureHaveStateAuthority()
        {
            if (!HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }
        }

        /// <summary>
        ///     Execute the pending actions that we eventually enqueued while waiting to become state authority.
        /// </summary>
        public void StateAuthorityChanged()
        {
            m_pendingTasksHandler.ExecuteAllOrClear(HasStateAuthority);
        }
    }
}
