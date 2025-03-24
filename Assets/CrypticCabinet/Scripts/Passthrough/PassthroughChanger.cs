// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Passthrough
{
    /// <summary>
    ///     This component interacts with the PassthroughConfigurator (if any in the scene)
    ///     to apply changes to the passthrough dynamically in terms of brightness, contrast,
    ///     opacity and saturation.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class PassthroughChanger : Singleton<PassthroughChanger>
    {

        /// <summary>
        ///     True if this change will need to happen on all headsets.
        /// </summary>
        [SerializeField] private bool m_propagateToAllClients;

        private bool m_darkActive;
        private bool m_uvActive;

        /// <summary>
        ///     Reset the passthrough to default if this is disabled. 
        /// </summary>
        private void OnDisable()
        {
            SetPassthroughDefaultLut();
        }

        [ContextMenu("Set Passthrough Default Lut")]
        public void SetPassthroughDefaultLut()
        {
            m_darkActive = false;
            m_uvActive = false;
            SetPassthroughLut(ELutId.DEFAULT, 1);
        }

        [ContextMenu("Set Passthrough Darker Room Lut")]
        public void SetPassthroughDarkerRoomLut()
        {
            if (m_darkActive)
            {
                m_darkActive = false;
                if (!m_uvActive)
                {
                    SetPassthroughDefaultLut();
                }
            }
            else
            {
                m_darkActive = true;
                SetPassthroughLut(ELutId.DARKER_ROOM, 1);
            }
        }

        [ContextMenu("Set Passthrough Lighter Room Lut")]
        public void SetPassthroughUvLightRoomLut()
        {
            if (m_uvActive)
            {
                m_uvActive = false;
                if (!m_darkActive)
                {
                    SetPassthroughDefaultLut();
                }
            }
            else
            {
                m_uvActive = true;
                SetPassthroughLut(ELutId.DARKER_ROOM, 1);
            }
        }

        /// <summary>
        ///     Sets a new color LUT by ID, and smoothly reaches the specified target blend between
        ///     previous LUT and new LUT.
        /// </summary>
        /// <param name="lutID"></param>
        /// <param name="targetBlend"> Value is clamped between 0 and 1 inclusive </param>
        private void SetPassthroughLut(ELutId lutID, float targetBlend)
        {
            targetBlend = Mathf.Clamp(targetBlend, 0, 1);

            Debug.Assert(PassthroughConfigurator.Instance != null,
                "Instance of passthrough configurator is null!");
            if (m_darkActive && m_uvActive)
            {
                targetBlend = 1.0f;
            }

            PassthroughConfigurator.Instance.SetLut(lutID, targetBlend, m_propagateToAllClients);
        }

        /// <summary>
        ///     Changes the brightness of the passthrough layer designated by the passthrough configurator.
        /// </summary>
        /// <param name="brightness">Value between -1 and +1</param>
        public void SetPassthroughBrightness(float brightness)
        {
            brightness = Mathf.Clamp(brightness, -1, 1);
            Debug.Assert(PassthroughConfigurator.Instance != null,
                "Instance of passthrough configurator is null!");
            PassthroughConfigurator.Instance.SetBrightness(brightness, m_propagateToAllClients);
        }

        /// <summary>
        ///     Changes the contrast of the passthrough layer designated by the passthrough configurator.
        /// </summary>
        /// <param name="contrast">Value between -1 and +1</param>
        public void SetPassthroughContrast(float contrast)
        {
            contrast = Mathf.Clamp(contrast, -1, 1);
            Debug.Assert(PassthroughConfigurator.Instance != null,
                "Instance of passthrough configurator is null!");
            PassthroughConfigurator.Instance.SetContrast(contrast, m_propagateToAllClients);
        }

        /// <summary>
        ///     Changes the opacity of the passthrough layer designated by the passthrough configurator.
        /// </summary>
        /// <param name="opacity">Value between -1 and +1</param>
        public void SetPassthroughOpacity(float opacity)
        {
            opacity = Mathf.Clamp(opacity, -1, 1);
            Debug.Assert(PassthroughConfigurator.Instance != null,
                "Instance of passthrough configurator is null!");
            PassthroughConfigurator.Instance.SetOpacity(opacity, m_propagateToAllClients);
        }

        /// <summary>
        ///     Changes the saturation of the passthrough layer designated by the passthrough configurator.
        /// </summary>
        /// <param name="saturation">Value between -1 and +1</param>
        public void SetPassthroughSaturation(float saturation)
        {
            saturation = Mathf.Clamp(saturation, -1, 1);
            Debug.Assert(PassthroughConfigurator.Instance != null,
                "Instance of passthrough configurator is null!");
            PassthroughConfigurator.Instance.SetSaturation(saturation, m_propagateToAllClients);
        }
    }
}
