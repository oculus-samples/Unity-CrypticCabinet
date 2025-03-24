// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Represents a cone volume for VFX.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class VFXCone : MonoBehaviour
    {
        public float BaseRadius;
        public float TopRadius;
        public float Height;
        public Transform Transform;

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;

            var position = Transform.position;
            Gizmos.DrawSphere(position, BaseRadius);
            position += Transform.up * Height;
            Gizmos.DrawSphere(position, TopRadius);
        }

        public void ZeroScale()
        {
            BaseRadius = 0f;
            TopRadius = 0f;
            Height = 0f;
            Transform = transform;
        }
    }
}
