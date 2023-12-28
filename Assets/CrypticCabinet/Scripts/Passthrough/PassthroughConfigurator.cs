// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.Passthrough
{
    [Serializable]
    public enum ELutId
    {
        DEFAULT = 0,
        DARKER_ROOM = 1,
    }

    /// <summary>
    ///     Allows to change settings of a specified passthrough layer on demand.
    ///     It can propagate the change to all players using RPC.
    /// </summary>
    public class PassthroughConfigurator : NetworkBehaviour
    {
        [SerializeField] private OVRPassthroughLayer m_passthroughLayer;
        [SerializeField] private float m_currentOpacity = 1f;
        [SerializeField] private float m_currentContrast;
        [SerializeField] private float m_currentBrightness;
        [SerializeField] private float m_currentSaturation;
        [SerializeField] private ELutId m_currentLutId;
        [SerializeField] private float m_currentLutBlend;

        [SerializeField] private float m_opacityTransitionDuration = 1f;
        [SerializeField] private float m_contrastTransitionDuration = 1f;
        [SerializeField] private float m_brightnessTransitionDuration = 1f;
        [SerializeField] private float m_saturationTransitionDuration = 1f;
        [SerializeField] private float m_lutTransitionDuration = 1f;

        [SerializeField] private Texture2D m_defaultLut;
        [SerializeField] private Texture2D m_darkerRoomLut;

        private OVRPassthroughColorLut m_ovrCurrentLut;
        private OVRPassthroughColorLut m_ovrNextLut;

        private OVRPassthroughColorLut m_defaultRoomLut;
        private OVRPassthroughColorLut m_ovrDarkerRoomLut;

        private float m_opacityElapsedTime;
        private float m_contrastElapsedTime;
        private float m_brightnessElapsedTime;
        private float m_saturationElapsedTime;

        private Coroutine m_opacityCoroutine;
        private Coroutine m_contrastCoroutine;
        private Coroutine m_brightnessCoroutine;
        private Coroutine m_saturationCoroutine;
        private Coroutine m_lutCoroutine;

        public static PassthroughConfigurator Instance { get; private set; }

        public void Awake()
        {
            // Enforce singleton across all Runners.
            if (Instance)
            {
                Destroy(this);
            }
            Instance = this;

            if (m_passthroughLayer == null)
            {
                m_passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
            }
        }

        private void Start()
        {
            // Initialize passthrough with desired settings.
            UpdatePassthroughSetup();

            // Ensure the texture is supported for the LUT
            if (!OVRPassthroughColorLut.IsTextureSupported(m_defaultLut, out var errorMsg))
            {
                Debug.LogError($"LUT texture not supported, reason: {errorMsg}");
            }
            else
            {
                m_defaultRoomLut = new OVRPassthroughColorLut(m_defaultLut);
            }

            if (!OVRPassthroughColorLut.IsTextureSupported(m_darkerRoomLut, out errorMsg))
            {
                Debug.LogError($"LUT texture not supported, reason: {errorMsg}");
            }
            else
            {
                m_ovrDarkerRoomLut = new OVRPassthroughColorLut(m_darkerRoomLut);
            }
        }

        private void OnDisable()
        {
            SetLut(ELutId.DEFAULT, 1, true);
        }

        #region Smoothing changes

        private void UpdatePassthroughLut(OVRPassthroughColorLut previousLut, OVRPassthroughColorLut newLut)
        {
            m_passthroughLayer.SetColorLut(newLut);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdatePassthroughSetup()
        {
            m_passthroughLayer.SetBrightnessContrastSaturation(m_currentBrightness, m_currentContrast, m_currentSaturation);
            m_passthroughLayer.textureOpacity = m_currentOpacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerator InterpolateValue(float startValue, float targetValue, float duration, Action<float> setValueAction)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / duration);
                var value = Mathf.Lerp(startValue, targetValue, t);
                // Invoke action to forward the new value to the caller and its delegate.
                setValueAction(value);
                yield return null;
            }

            // Set the destination value to ensure it is exactly the expected one.
            setValueAction(targetValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartInterpolation(ref Coroutine coroutine, float startValue, float targetValue, float duration, Action<float> setValueAction)
        {
            if (!m_passthroughLayer)
            {
                Debug.LogError("No passthrough layer specifier for PassthroughConfigurator." +
                               "Unable to update passthrough setup!");
                return;
            }

            // If there's already an interpolation coroutine for the variable, stop it.
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            // Start the new interpolation coroutine.
            coroutine = StartCoroutine(InterpolateValue(startValue, targetValue, duration, setValueAction));
        }
        #endregion

        #region Passthrough configuration
        public void ResetPassthrough()
        {
            if (!m_passthroughLayer)
            {
                Debug.LogError("No passthrough layer specifier for PassthroughConfigurator." +
                               "Unable to reset passthrough!");
                return;
            }
            m_passthroughLayer.DisableColorMap();
        }

        public void SetOpacity(float opacity, bool propagateToAllClients)
        {
            if (!m_passthroughLayer)
            {
                Debug.LogError("No passthrough layer specifier for PassthroughConfigurator." +
                               "Unable to change opacity!");
                return;
            }

            // Smooth change of opacity
            StartInterpolation(ref m_opacityCoroutine, m_currentOpacity, opacity, m_opacityTransitionDuration,
            value =>
            {
                m_currentOpacity = value;
                UpdatePassthroughSetup();
            });

            if (propagateToAllClients)
            {
                RPC_SetOpacity(opacity);
            }
        }

        public void SetContrast(float contrast, bool propagateToAllClients)
        {
            // Smooth change of contrast
            StartInterpolation(ref m_contrastCoroutine, m_currentContrast, contrast, m_contrastTransitionDuration,
            value =>
            {
                m_currentContrast = value;
                UpdatePassthroughSetup();
            });

            if (propagateToAllClients)
            {
                RPC_SetContrast(contrast);
            }
        }

        public void SetBrightness(float brightness, bool propagateToAllClients)
        {
            // Smooth change of brightness
            StartInterpolation(ref m_brightnessCoroutine, m_currentBrightness, brightness, m_brightnessTransitionDuration,
            value =>
            {
                m_currentBrightness = value;
                UpdatePassthroughSetup();
            });

            if (propagateToAllClients)
            {
                RPC_SetBrightness(brightness);
            }
        }

        public void SetSaturation(float saturation, bool propagateToAllClients)
        {
            // Smooth change of saturation
            StartInterpolation(ref m_saturationCoroutine, m_currentSaturation, saturation, m_saturationTransitionDuration,
            value =>
            {
                m_currentSaturation = value;
                UpdatePassthroughSetup();
            });

            if (propagateToAllClients)
            {
                RPC_SetSaturation(saturation);
            }
        }

        public void SetLut(ELutId lutID, float targetBlend, bool propagateToAllClients)
        {
            var previousLut = m_ovrCurrentLut;
            var nextLut = lutID switch
            {
                ELutId.DEFAULT => m_defaultRoomLut,
                ELutId.DARKER_ROOM => m_ovrDarkerRoomLut,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (nextLut != null)
            {
                m_currentLutId = lutID;
                m_ovrCurrentLut = nextLut;
                m_currentLutBlend = 0;

                // Smooth change of Lut, going from current blend to target blend from previous to next LUT
                StartInterpolation(
                    ref m_lutCoroutine, m_currentLutBlend, targetBlend, m_lutTransitionDuration,
                    value =>
                    {
                        m_currentLutBlend = value;
                        UpdatePassthroughLut(previousLut, nextLut);
                    });
            }
            else
            {
                Debug.LogWarning("SetLut failed: new lut ID not found!");
            }

            if (propagateToAllClients)
            {
                RPC_SetLut(lutID, targetBlend);
            }
        }
        #endregion

        #region RPC
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SetOpacity(float opacity, RpcInfo info = default)
        {
            // Only send this to other players
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            Debug.Log("RPC: received passthrough change opacity request");
            if (opacity.Equals(m_currentOpacity))
            {
                Debug.Log("RPC: passthrough opacity already equal to new value");
                return;
            }

            Debug.Log("RPC: changing passthrough opacity with new value");
            SetOpacity(opacity, false);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SetContrast(float contrast, RpcInfo info = default)
        {
            // Only send this to other players
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            Debug.Log("RPC: received passthrough change contrast request");
            if (contrast.Equals(m_currentContrast))
            {
                Debug.Log("RPC: passthrough contrast already equal to new value");
                return;
            }

            Debug.Log("RPC: changing passthrough contrast with new value");
            SetContrast(contrast, false);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SetBrightness(float brightness, RpcInfo info = default)
        {
            // Only send this to other players
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            Debug.Log("RPC: received passthrough change brightness request");
            if (brightness.Equals(m_currentBrightness))
            {
                Debug.Log("RPC: passthrough brightness already equal to new value");
                return;
            }

            Debug.Log("RPC: changing passthrough brightness with new value");
            SetBrightness(brightness, false);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SetSaturation(float saturation, RpcInfo info = default)
        {
            // Only send this to other players
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            Debug.Log("RPC: received passthrough change saturation request");
            if (saturation.Equals(m_currentSaturation))
            {
                Debug.Log("RPC: passthrough saturation already equal to new value");
                return;
            }

            Debug.Log("RPC: changing passthrough saturation with new value");
            SetSaturation(saturation, false);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SetLut(ELutId lutID, float blend, RpcInfo info = default)
        {
            // Only send this to other players
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            Debug.Log("RPC: received passthrough change lut request");
            if (lutID == m_currentLutId && Mathf.Approximately(blend, m_currentLutBlend))
            {
                Debug.Log("RPC: passthrough lut already equal to new value");
                return;
            }

            Debug.Log("RPC: changing passthrough lut with new value");
            SetLut(lutID, blend, false);
        }
        #endregion
    }
}
