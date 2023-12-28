// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Used to follow a rigidbody. We use this for the rope and the hook to stay connected.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyFollower : MonoBehaviour
    {
        [SerializeField] private Transform m_target;
        private Rigidbody m_thisBody;
        private float m_lerpTime = -1;

        private void Start()
        {
            m_thisBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (m_lerpTime > 0)
            {
                var pos = Vector3.Lerp(m_target.position, m_thisBody.position, m_lerpTime);
                var rot = Quaternion.Slerp(m_target.rotation, m_thisBody.rotation, m_lerpTime);
                m_thisBody.Move(pos, rot);
                m_lerpTime -= Time.fixedDeltaTime * 0.5f;
            }
            else
            {
                m_thisBody.Move(m_target.position, m_target.rotation);
            }
        }

        public void JumpToLocation(Vector3 position, Quaternion rotation)
        {
            m_lerpTime = 1.0f;
            m_thisBody.Move(position, rotation);
            m_thisBody.transform.SetPositionAndRotation(position, rotation);
        }
    }
}