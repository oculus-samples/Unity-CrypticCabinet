// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Used to trigger actions when an object is attached or removed from the hook of the sand puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class Hook : MonoBehaviour
    {
        /// <summary>
        /// Event called by the hookable object. It will pass down the hookable GameObject.
        /// </summary>
        public UnityEvent<GameObject> OnGameObjectAttached;

        /// <summary>
        /// Event called by the hookable object. When the object is removed fom the hook.
        /// </summary>
        public UnityEvent<GameObject> OnGameObjectRemoved;
    }
}
