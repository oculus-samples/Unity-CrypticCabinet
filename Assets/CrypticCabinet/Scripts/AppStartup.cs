// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Utils;
using Meta.XR.Samples;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet
{
    /// <summary>
    ///     This script takes care of initializing the required configurations for
    ///     starting the application successfully.
    ///     This should be placed into the main scene.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class AppStartup : MonoBehaviour
    {
        /// <summary>
        ///     Flags if the app is ready to start.
        /// </summary>
        private static bool s_appReady;

        [SerializeField] private UnityEvent m_onAppReady;
        [SerializeField] private UnityEvent m_onOculusPlatformSDKInitialized;
        [SerializeField] private UnityEvent m_onOculusCoreInitializationFailed;
        [SerializeField] private UnityEvent m_onEntitlementCheckFailed;
        [SerializeField] private UnityEvent<User> m_onOculusUserLoginSuccess;

        public async void Awake()
        {
            // Forward events to this class for convenience.
            if (m_onOculusPlatformSDKInitialized != null)
            {
                OculusPlatformUtils.OnOculusPlatformSDKInitialized.AddListener(
                    m_onOculusPlatformSDKInitialized.Invoke);
            }

            if (m_onOculusCoreInitializationFailed != null)
            {
                OculusPlatformUtils.OnOculusCoreInitializationFailed.AddListener(
                    m_onOculusCoreInitializationFailed.Invoke);
            }

            if (m_onEntitlementCheckFailed != null)
            {
                OculusPlatformUtils.OnEntitlementCheckFailed.AddListener(
                    m_onEntitlementCheckFailed.Invoke);
            }

            if (m_onOculusUserLoginSuccess != null)
            {
                OculusPlatformUtils.OnOculusUserLoginSuccess.AddListener(
                    m_onOculusUserLoginSuccess.Invoke);
            }

            if (!s_appReady)
            {
                s_appReady = await OculusPlatformUtils.InitializeOculusPlatformSDK();
            }

            m_onAppReady.Invoke();
            SetOculusPerformance();
        }

        private static void SetOculusPerformance()
        {
            OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
            OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
            OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.HighTop;
            OVRManager.useDynamicFoveatedRendering = true;
        }

        public void OnDestroy()
        {
            // Remove events forwarding.
            if (m_onOculusPlatformSDKInitialized != null)
            {
                OculusPlatformUtils.OnOculusPlatformSDKInitialized.RemoveListener(
                    m_onOculusPlatformSDKInitialized.Invoke);
            }

            if (m_onOculusCoreInitializationFailed != null)
            {
                OculusPlatformUtils.OnOculusCoreInitializationFailed.RemoveListener(
                    m_onOculusCoreInitializationFailed.Invoke);
            }

            if (m_onEntitlementCheckFailed != null)
            {
                OculusPlatformUtils.OnEntitlementCheckFailed.RemoveListener(
                    m_onEntitlementCheckFailed.Invoke);
            }

            if (m_onOculusUserLoginSuccess != null)
            {
                OculusPlatformUtils.OnOculusUserLoginSuccess.RemoveListener(
                    m_onOculusUserLoginSuccess.Invoke);
            }
        }
    }
}