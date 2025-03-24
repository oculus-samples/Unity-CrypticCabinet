// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    /// Toggles the <see cref="TouchHandGrabInteractorVisual"/> enable state so it can only
    /// override the hand pose state when touches are active.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(TouchHandGrabInteractor),
        typeof(TouchHandGrabInteractorVisual))]
    public class EnableTouchVisualOnHover : MonoBehaviour
    {
        private void Start()
        {
            var touchHandGrabInteractor = GetComponent<TouchHandGrabInteractor>();
            var touchHandGrabInteractorVisual = GetComponent<TouchHandGrabInteractorVisual>();
            touchHandGrabInteractor.WhenStateChanged += args =>
            {
                touchHandGrabInteractorVisual.enabled = args.NewState switch
                {
                    InteractorState.Normal => false,
                    InteractorState.Hover => true,
                    InteractorState.Select => true,
                    InteractorState.Disabled => false,
                    _ => throw new ArgumentOutOfRangeException()
                };
            };
        }
    }
}
