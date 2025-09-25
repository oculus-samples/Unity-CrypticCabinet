// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Convenient class to reposition objects when they are too far away from their original position.
    ///     This is used for example as a backup if any object would fall under the floor.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Rigidbody))]
    public class RepositionWhenFarAway : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField, Tooltip("Event being called by the script used by the spawner to position this object")]
        private UnityEvent m_onSpawnComplete;

        [SerializeField, Tooltip("Event being called by this script when the repositioning occur (Same frame)")]
        private UnityEvent m_onRepositionComplete;

        [SerializeField, Tooltip("Distance at which point this object will be forcefully moved back")]
        private float m_originDistanceBeforeReposition = 60f;

        [SerializeField, Tooltip("Frequency (Secs) of the check")]
        private float m_checkFrequencySecs = 1f;

        //We only reposition if the rigid body is not set to kinematic. We also need the rigid body to kill
        //any momentum the object has acquired.
        private Rigidbody m_rigidbody;

        private WaitForSeconds m_waitForSeconds;

        private Quaternion m_originalRotation;
        private Vector3 m_originalPosition;

        private void Awake()
        {
            m_onSpawnComplete.AddListener(InitializeOriginalTransform);
        }

        private void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            if (m_rigidbody.isKinematic)
            {
                Debug.LogWarning("RepositionWhenOutside on kinematic object, maybe not needed?");
            }

            if (m_originalPosition == Vector3.zero && m_originalRotation != Quaternion.identity)
            {
                //Let's sample the current position if not yet set, it will be overridden later;
                InitializeOriginalTransform();
            }
            m_waitForSeconds = new WaitForSeconds(m_checkFrequencySecs);
        }

        private void InitializeOriginalTransform()
        {
            var transformCache = transform;
            m_originalRotation = transformCache.rotation;
            m_originalPosition = transformCache.position;
            Debug.Log("Reposition: Setting pos: " + m_originalPosition + " rotation: " + m_originalRotation);
        }

        private void OnDestroy()
        {
            m_onSpawnComplete.RemoveListener(InitializeOriginalTransform);
        }

        private IEnumerator CheckRepositioning()
        {
            var maxDistancePow = m_originDistanceBeforeReposition * m_originDistanceBeforeReposition;

            // Note: only run this coroutine while we are state authority.
            while (HasStateAuthority)
            {
                yield return m_waitForSeconds;

                if (m_rigidbody.isKinematic || !(Vector3.SqrMagnitude(transform.position) > maxDistancePow))
                {
                    continue;
                }

                var transformLoc = transform;
                m_rigidbody.angularVelocity = Vector3.zero;
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.position = transformLoc.position = m_originalPosition;
                m_rigidbody.rotation = transformLoc.rotation = m_originalRotation;
                m_rigidbody.Sleep();
                m_onRepositionComplete?.Invoke();
                Debug.Log("Reposition: Repositioning pos: " + m_originalPosition + " rotation: " + m_originalRotation);
            }
        }

        public void StateAuthorityChanged()
        {
            StopCoroutine(nameof(CheckRepositioning));
            if (HasStateAuthority)
            {
                _ = StartCoroutine(nameof(CheckRepositioning));
            }
        }
    }
}
