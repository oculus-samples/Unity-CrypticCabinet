// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Defines a timeline with a trigger to wait for.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class WaitTimeline : MonoBehaviour
    {
        private bool m_trigger;

        public bool Trigger
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false)
                {
                    return true;
                }
#endif
                return m_trigger;
            }
            set => m_trigger = value;
        }
    }
}