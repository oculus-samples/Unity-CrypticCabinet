// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Controls the enable state of a list of objects to enable or disable.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class EnabledStateTarget : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_objectToEnable;
        [SerializeField] private GameObject[] m_objectToDisable;

        /// <summary>
        ///     Enables all objects to be enabled, disable all objects to be disabled.
        /// </summary>
        public void ApplyNewStates()
        {
            SetStatesForArray(m_objectToEnable, true);
            SetStatesForArray(m_objectToDisable, false);
        }

        /// <summary>
        ///     Disable all objects that were enabled, enable all objects that were enabled.
        /// </summary>
        public void RevertStatesStates()
        {
            SetStatesForArray(m_objectToEnable, false);
            SetStatesForArray(m_objectToDisable, true);
        }

        private static void SetStatesForArray(GameObject[] targets, bool state)
        {
            if (targets == null)
            {
                return;
            }

            foreach (var o in targets)
            {
                if (o != null)
                {
                    o.SetActive(state);
                }
            }
        }
    }
}