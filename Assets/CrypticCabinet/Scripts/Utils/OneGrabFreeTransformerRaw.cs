// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     A Transformer that moves the target in a 1-1 fashion with the GrabPoint.
    ///     Simplified to place exactly where the grab point is calculated to be.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class OneGrabFreeTransformerRaw : MonoBehaviour, ITransformer
    {
        private IGrabbable m_grabbable;

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
        }

        public void BeginTransform()
        {
        }

        public void UpdateTransform()
        {
            var grabPoint = m_grabbable.GrabPoints[0];
            var targetTransform = m_grabbable.Transform;
            targetTransform.rotation = grabPoint.rotation;
            targetTransform.position = grabPoint.position;
        }

        public void EndTransform() { }
    }
}
