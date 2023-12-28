// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Updates the transform of the object it is attached to, following the one of the specified primary camera.
    ///     Position, rotation, and local scale are used from the followed user camera.
    /// </summary>
    public class FollowUserCamera : MonoBehaviour
    {
        [SerializeField] private Camera m_activeCamera;

        private void Start() => m_activeCamera = Camera.main;

        private void Update()
        {
            if (m_activeCamera == null)
            {
                return;
            }

            if (transform.position != m_activeCamera.transform.position)
            {
                transform.position = m_activeCamera.transform.position;
            }

            if (transform.rotation != m_activeCamera.transform.rotation)
            {
                transform.rotation = m_activeCamera.transform.rotation;
            }

            if (transform.localScale != m_activeCamera.transform.localScale)
            {
                transform.localScale = m_activeCamera.transform.localScale;
            }
        }
    }
}