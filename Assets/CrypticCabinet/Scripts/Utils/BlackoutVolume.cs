// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     A volume that triggers a blackout effect for the player.
    ///     Used to hide the inside of the main puzzle objects during the gameplay.
    /// </summary>
    public class BlackoutVolume : MonoBehaviour
    {
        private Camera m_mainCamera;
        private UniversalAdditionalCameraData m_universalAdditionalCameraData;

        private void Start()
        {
            m_mainCamera = Camera.main;

            if (m_mainCamera != null)
            {
                m_universalAdditionalCameraData = m_mainCamera.GetComponent<UniversalAdditionalCameraData>();
            }

            if (m_universalAdditionalCameraData == null)
            {
                Debug.LogError(
                    "No m_universalAdditionalCameraData found on the assigned camera. BlackoutVolume cannot work.");
            }
            else
            {
                m_universalAdditionalCameraData.allowXRRendering = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_universalAdditionalCameraData != null && other.gameObject == m_mainCamera.gameObject)
            {
                m_universalAdditionalCameraData.renderPostProcessing = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_universalAdditionalCameraData != null && other.gameObject == m_mainCamera.gameObject)
            {
                m_universalAdditionalCameraData.renderPostProcessing = false;
            }
        }
    }
}