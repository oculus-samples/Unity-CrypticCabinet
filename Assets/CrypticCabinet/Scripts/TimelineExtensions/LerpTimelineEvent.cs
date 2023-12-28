// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.TimelineExtensions
{
    /// <summary>
    ///     Defines a lerp timeline update event.
    /// </summary>
    public class LerpTimelineEvent : MonoBehaviour
    {
        public UnityEvent<double> UpdateEvent;
    }
}