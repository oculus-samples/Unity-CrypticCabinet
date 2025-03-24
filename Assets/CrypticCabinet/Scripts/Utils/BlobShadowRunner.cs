// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Runs placing a blob shadow on a surface below the target object. 
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class BlobShadowRunner : MonoBehaviour
    {
        [SerializeField] private Transform m_targetObject;
        [SerializeField] private LayerMask m_floorLayer;
        [SerializeField] private MeshRenderer m_blobRenderer;

        private Ray m_ray;
        private const float SHADOW_LIFT_DISTANCE = 0.05f;
        private const float RAYCAST_DISTANCE = 10.0f;

        private void Start()
        {
            if (m_targetObject == null || m_blobRenderer == null)
            {
                Debug.LogError("Missing required references for blob shadow.");
                gameObject.SetActive(false);
                return;
            }

            m_ray = new Ray(m_targetObject.position, Vector3.down);
        }

        private void Update()
        {
            m_ray.origin = m_targetObject.position;
            if (Physics.Raycast(m_ray, out var hitInfo, RAYCAST_DISTANCE, m_floorLayer))
            {
                if (!m_blobRenderer.enabled)
                {
                    m_blobRenderer.enabled = true;
                }

                transform.position = hitInfo.point + Vector3.up * SHADOW_LIFT_DISTANCE;
            }
            else
            {
                if (m_blobRenderer.enabled)
                {
                    m_blobRenderer.enabled = false;
                }
            }
        }
    }
}