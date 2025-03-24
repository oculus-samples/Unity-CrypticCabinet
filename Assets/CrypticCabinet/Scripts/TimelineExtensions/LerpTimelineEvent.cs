// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Defines a lerp timeline update event.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class LerpTimelineEvent : MonoBehaviour
    {
        public UnityEvent<double> UpdateEvent;
    }
}