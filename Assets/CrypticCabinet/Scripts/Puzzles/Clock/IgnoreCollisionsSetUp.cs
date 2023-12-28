// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Puzzles.Clock
{
    public class IgnoreCollisionsSetUp : MonoBehaviour
    {
        [SerializeField] private GameObject m_body;
        [SerializeField] private GameObject m_otherParts;

        private void Start()
        {
            var bodyColliders = m_body.GetComponentsInChildren<Collider>(true);
            var otherColliders = m_otherParts.GetComponentsInChildren<Collider>(true);

            foreach (var b in bodyColliders)
            {
                foreach (var o in otherColliders)
                {
                    Physics.IgnoreCollision(b, o);
                }
            }
        }
    }
}
