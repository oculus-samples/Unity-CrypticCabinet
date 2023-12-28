// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using CrypticCabinet.Interactions;
using CrypticCabinet.Utils;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Clock
{
    /// <summary>
    ///     Defines the interactable for the key inside the clock puzzle.
    /// </summary>
    public class KeyInteractable : MonoBehaviour
    {
        [SerializeField] private float m_unlockAngle = 90;
        [SerializeField] private OneGrabToggleRotateTransformer m_keyTransformer;
        [SerializeField] private SnapInteractor m_snapInteractor;
        [SerializeField] private GameObject m_colliders;
        [SerializeField] private SnapInteractable m_clockKeySnapZone;
        [SerializeField] private LayerSnapManager m_layerSnapManager;
        [SerializeField] private Rigidbody m_rigidbody;

        public UnityEvent UnlockComplete;

        private Vector3 m_referenceRightVector;

        private IEnumerator Start()
        {
            yield return SnapKey();
            gameObject.SetActive(false);
        }

        public void KeySnappedInPlace()
        {
            m_colliders.SetActive(false);
        }

        public void LockKeyInPlace()
        {
            m_colliders.SetActive(true);
            m_keyTransformer.LockPosition = true;
            m_keyTransformer.CanUnlockPosition = false;
            m_snapInteractor.gameObject.SetActive(false);

            m_rigidbody.isKinematic = true;
            m_rigidbody.constraints = RigidbodyConstraints.FreezePosition;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            m_rigidbody.interpolation = RigidbodyInterpolation.None;

            _ = StartCoroutine(nameof(TrackRotation));
        }

        private IEnumerator TrackRotation()
        {
            var currentTransform = transform;
            var right = currentTransform.right;
            m_referenceRightVector = right;
            var rotation = Vector3.Angle(m_referenceRightVector, right);

            while (rotation <= m_unlockAngle)
            {
                rotation = Vector3.Angle(m_referenceRightVector, transform.right);
                yield return new WaitForSeconds(0.5f);
                yield return null;
            }


            yield return null;
            m_colliders.SetActive(false);
            UnlockComplete?.Invoke();
        }

        public void EnableKeyInClock()
        {
            gameObject.SetActive(true);
            _ = StartCoroutine(nameof(SnapKey));
        }

        private IEnumerator SnapKey()
        {
            var clockSnapZoneTransform = m_clockKeySnapZone.transform;
            transform.SetPositionAndRotation(clockSnapZoneTransform.position, clockSnapZoneTransform.rotation);
            yield return null;

            m_snapInteractor.SetComputeShouldSelectOverride(() => true);
            m_clockKeySnapZone.AddSelectingInteractor(m_snapInteractor);
            m_snapInteractor.SetComputeCandidateOverride(() => m_clockKeySnapZone);
            m_snapInteractor.ProcessCandidate();
            m_snapInteractor.Select();

            m_layerSnapManager.ResetOverrideCheck();

            yield return null;
        }
    }
}