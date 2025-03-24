// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Clock
{
    /// <summary>
    ///     Defines the interactions the user can perform on the key of the clock puzzle.
    ///     It also controls the trigger for the drawer that unlocks and open for the Tesla puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(SnapInteractable))]
    public class KeyInteraction : MonoBehaviour
    {
        [SerializeField] private Transform m_drawRoot;
        [SerializeField] private Transform m_drawTargetPos;
        [SerializeField] private AnimationCurve m_drawOpenCurve;
        [SerializeField] private float m_drawOpenTime = 2;
        [SerializeField] private float m_keyPositioningWaitTime = 2;

        private SnapInteractable m_snapInteractable;
        private KeyInteractable m_key;
        private Vector3 m_keyPos = Vector3.zero;
        private Vector3 m_keyForward = Vector3.zero;

        public UnityEvent OnDrawOpening;

        private void Start()
        {
            m_snapInteractable = GetComponent<SnapInteractable>();
            m_snapInteractable.WhenSelectingInteractorViewAdded += KeySnapped;
        }

        private void KeySnapped(IInteractorView obj)
        {
            var keySnap = obj.Data as SnapInteractor;
            if (keySnap != null)
            {
                var key = keySnap.GetComponentInParent<KeyInteractable>();
                m_key = key;
                var keyTransform = key.transform;
                m_keyPos = keyTransform.position;
                m_keyForward = keyTransform.forward;
                m_key.KeySnappedInPlace();
                _ = StartCoroutine(nameof(WaitForStopped));
            }
        }

        private IEnumerator WaitForStopped()
        {
            var moveDist = float.MaxValue;
            var moveForward = float.MaxValue;

            while (moveDist > 0.0001f && moveForward > 0.0001f)
            {
                // Give some extra frames before stop moving
                yield return null;
                yield return null;
                yield return null;
                var forward = m_key.transform.forward;
                moveForward = Vector3.Dot(m_keyForward, forward);
                var position = m_key.transform.position;
                moveDist = Vector3.SqrMagnitude(m_keyPos - position);
                m_keyPos = position;
                m_keyForward = forward;
            }

            yield return new WaitForSeconds(m_keyPositioningWaitTime);
            m_key.transform.SetParent(m_drawRoot.transform, true);
            m_key.LockKeyInPlace();
            m_key.UnlockComplete.AddListener(KeyUnlockComplete);
            m_snapInteractable.enabled = false;
        }

        private void KeyUnlockComplete()
        {
            m_key.UnlockComplete.RemoveListener(KeyUnlockComplete);
            OnDrawOpening?.Invoke();
            _ = StartCoroutine(nameof(DrawOpenAnimation));
        }

        private IEnumerator DrawOpenAnimation()
        {
            float currentTime = 0;

            var startPos = m_drawRoot.position;
            while (currentTime < m_drawOpenTime)
            {
                yield return null;
                currentTime += Time.deltaTime;
                var movePercentage = m_drawOpenCurve.Evaluate(currentTime / m_drawOpenTime);
                var currentPos = Vector3.Lerp(startPos, m_drawTargetPos.position, movePercentage);
                m_drawRoot.position = currentPos;
            }
        }
    }
}