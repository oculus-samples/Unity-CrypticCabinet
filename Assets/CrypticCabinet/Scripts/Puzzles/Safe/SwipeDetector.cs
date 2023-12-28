// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Safe
{
    /// <summary>
    ///     Detects if the user hands (or the hands controllers) perform a swipe over a specific area.
    ///     This is used for the dial interactions. The mechanism uses three colliders, to detect swipe up or
    ///     swipe down actions with the user finger.
    /// </summary>
    public class SwipeDetector : MonoBehaviour
    {
        [SerializeField] private CollisionDetector m_topCollisionDetector;
        [SerializeField] private CollisionDetector m_midCollisionDetector;
        [SerializeField] private CollisionDetector m_botCollisionDetector;

        [Space(10)]
        [Header("Events")]
        [Space(10)]
        [Tooltip("Triggered when the end position of a swipe up action is reached")]
        [SerializeField] private UnityEvent m_onSwipeUp;
        [Tooltip("Triggered when the end position of a swipe down action is reached")]
        [SerializeField] private UnityEvent m_onSwipeDown;
        [Tooltip("Triggered when the start position of a swipe is reached")]
        [SerializeField] private UnityEvent m_onSwipeReady;
        [Tooltip("Triggered when the start position of a swipe is reached")]
        [SerializeField] private UnityEvent m_onSwipeNotReady;

        private bool m_midColliderEntered;

        private void Start()
        {
            this.AssertField(m_topCollisionDetector, nameof(m_topCollisionDetector));
            this.AssertField(m_midCollisionDetector, nameof(m_midCollisionDetector));
            this.AssertField(m_botCollisionDetector, nameof(m_botCollisionDetector));

            if (m_topCollisionDetector != null)
            {
                m_topCollisionDetector.OnTriggerEntered += TopColliderEntered;
            }
            if (m_midCollisionDetector != null)
            {
                m_midCollisionDetector.OnTriggerEntered += MidColliderEntered;
                m_midCollisionDetector.OnTriggerExited += MidColliderExited;
            }
            if (m_botCollisionDetector != null)
            {
                m_botCollisionDetector.OnTriggerEntered += BotColliderEntered;
            }
        }

        private void OnDestroy()
        {
            if (m_topCollisionDetector != null)
            {
                m_topCollisionDetector.OnTriggerEntered -= TopColliderEntered;
            }
            if (m_midCollisionDetector != null)
            {
                m_midCollisionDetector.OnTriggerEntered -= MidColliderEntered;
                m_midCollisionDetector.OnTriggerExited -= MidColliderExited;
            }
            if (m_botCollisionDetector != null)
            {
                m_botCollisionDetector.OnTriggerEntered -= BotColliderEntered;
            }
        }

        private void MidColliderEntered(Collider otherCollider)
        {
            Debug.Log("Dial middle collider trigger entered by: " + otherCollider.gameObject.name);
            m_midColliderEntered = true;
            ReadyForSwipe();
        }

        private void MidColliderExited(Collider otherCollider)
        {
            Debug.Log("Dial middle collider trigger exited by: " + otherCollider.gameObject.name);
            m_midColliderEntered = false;
            NotReadyForSwipe();
        }

        private void TopColliderEntered(Collider otherCollider)
        {
            Debug.Log("Dial top collider trigger entered by: " + otherCollider.gameObject.name);
            if (!m_midColliderEntered)
            {
                return;
            }

            SwipeUpAction();
            m_midColliderEntered = false;
        }

        private void BotColliderEntered(Collider otherCollider)
        {
            Debug.Log("Dial bottom collider trigger entered by: " + otherCollider.gameObject.name);
            if (!m_midColliderEntered)
            {
                return;
            }

            SwipeDownAction();
            m_midColliderEntered = false;
        }

        private void SwipeUpAction()
        {
            Debug.Log("Dial Swipe Up Action Triggered");
            m_onSwipeUp?.Invoke();
        }

        private void SwipeDownAction()
        {
            Debug.Log("Dial Swipe Down Action Triggered");
            m_onSwipeDown?.Invoke();
        }

        private void ReadyForSwipe()
        {
            Debug.Log("Dial is ready for a swipe");
            m_onSwipeReady?.Invoke();
        }

        private void NotReadyForSwipe()
        {
            Debug.Log("Dial is not ready for a swipe");
            m_onSwipeNotReady?.Invoke();
        }
    }
}
