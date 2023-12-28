// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils.Transformers
{
    /// <summary>
    ///     This two hands grab has the following rules:
    ///     - Follow position of the first grab
    ///     - Calculate rotation defined by the angle made between the first grab and
    ///       the second grab positions.
    /// </summary>
    public class TwoHandGrabTransformer : MonoBehaviour, ITransformer
    {
        private IGrabbable m_grabbable;
        private GameObject m_grabAGameObject;
        private GameObject m_grabBGameObject;
        private Pose m_grabDeltaInLocalSpace;

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
        }

        public void BeginTransform()
        {
            m_grabAGameObject = new GameObject("GrabA_Anchor");
            m_grabBGameObject = new GameObject("GrabB_Anchor");

            var grabA = m_grabbable.GrabPoints[0];
            var grabB = m_grabbable.GrabPoints[1];
            var grabbableTransform = m_grabbable.Transform;

            m_grabAGameObject.transform.position = grabA.position;
            m_grabAGameObject.transform.rotation = grabA.rotation;

            m_grabBGameObject.transform.position = grabB.position;
            m_grabBGameObject.transform.rotation = grabB.rotation;

            // Make the first grab look at the second one, to establish the resulting rotation
            m_grabAGameObject.transform.LookAt(m_grabBGameObject.transform);

            m_grabDeltaInLocalSpace = new Pose(grabbableTransform.InverseTransformVector(
                    m_grabAGameObject.transform.position - grabbableTransform.position),
                Quaternion.Inverse(m_grabAGameObject.transform.rotation) * grabbableTransform.rotation);
        }

        public void UpdateTransform()
        {
            var grabA = m_grabbable.GrabPoints[0];
            var grabB = m_grabbable.GrabPoints[1];

            // Update the anchors positions
            m_grabAGameObject.transform.position = grabA.position;
            m_grabBGameObject.transform.position = grabB.position;

            // Make the first grab look at the second one, to establish the resulting rotation
            m_grabAGameObject.transform.LookAt(m_grabBGameObject.transform);

            var targetTransform = m_grabbable.Transform;
            targetTransform.rotation = m_grabAGameObject.transform.rotation * m_grabDeltaInLocalSpace.rotation;
            targetTransform.position = m_grabAGameObject.transform.position - targetTransform.TransformVector(m_grabDeltaInLocalSpace.position);
        }

        public void EndTransform()
        {
            // Destroy auxiliary anchors
            Destroy(m_grabAGameObject);
            Destroy(m_grabBGameObject);
        }
    }
}