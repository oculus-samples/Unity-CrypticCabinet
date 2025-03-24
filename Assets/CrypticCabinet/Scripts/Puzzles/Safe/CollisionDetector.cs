// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Safe
{
    /// <summary>
    ///     Detects collisions with the colliders tagged with m_tagName, and performs callbacks on trigger enter or exit.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class CollisionDetector : MonoBehaviour
    {
        public delegate void TriggerDelegate(Collider otherCollider);

        public event TriggerDelegate OnTriggerEntered;
        public event TriggerDelegate OnTriggerExited;

        [SerializeField] private string m_tagName;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(m_tagName))
            {
                OnTriggerEntered?.Invoke(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(m_tagName))
            {
                OnTriggerExited?.Invoke(other);
            }
        }
    }
}
