// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     A copy of OneGrabRotateTransformer but with a function exposed to allow the resetting of the
    ///     _constrainedRelativeAngle value after force resetting the switch.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class OneGrabRotateTransformerExtended : MonoBehaviour, ITransformer
    {
        public enum Axis
        {
            RIGHT = 0,
            UP = 1,
            FORWARD = 2
        }

        [SerializeField, Optional] private Transform m_pivotTransform;

        public Transform Pivot => m_pivotTransform != null ? m_pivotTransform : transform;

        [SerializeField] private Axis m_rotationAxis = Axis.UP;

        public Axis RotationAxis => m_rotationAxis;

        [Serializable]
        public class OneGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        [SerializeField]
        private OneGrabRotateConstraints m_constraints = new()
        {
            MinAngle = new FloatConstraint(),
            MaxAngle = new FloatConstraint()
        };

        private OneGrabRotateConstraints Constraints => m_constraints;

        private float m_relativeAngle;
        private float m_constrainedRelativeAngle;

        private IGrabbable m_grabbable;
        private Vector3 m_grabPositionInPivotSpace;
        private Pose m_transformPoseInPivotSpace;

        private Pose m_worldPivotPose;
        private Vector3 m_previousVectorInPivotSpace;

        private Quaternion m_localRotation;
        private float m_startAngle;

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
        }

        private Pose ComputeWorldPivotPose()
        {
            if (m_pivotTransform != null)
            {
                return m_pivotTransform.GetPose();
            }

            var targetTransform = m_grabbable.Transform;

            var worldPosition = targetTransform.position;
            var worldRotation = targetTransform.parent != null
                ? targetTransform.parent.rotation * m_localRotation
                : m_localRotation;

            return new Pose(worldPosition, worldRotation);
        }

        public void UpdateObjectConstrainedValue(float newAngle)
        {
            m_constrainedRelativeAngle = newAngle;
        }

        public void BeginTransform()
        {
            var grabPoint = m_grabbable.GrabPoints[0];
            var targetTransform = m_grabbable.Transform;

            if (m_pivotTransform == null)
            {
                m_localRotation = targetTransform.localRotation;
            }

            var localAxis = Vector3.zero;
            localAxis[(int)m_rotationAxis] = 1f;

            m_worldPivotPose = ComputeWorldPivotPose();
            var rotationAxis = m_worldPivotPose.rotation * localAxis;

            var inverseRotation = Quaternion.Inverse(m_worldPivotPose.rotation);

            var grabDelta = grabPoint.position - m_worldPivotPose.position;

            // The initial delta must be non-zero between the pivot and grab location for rotation
            if (Mathf.Abs(grabDelta.magnitude) < 0.001f)
            {
                var localAxisNext = Vector3.zero;
                localAxisNext[((int)m_rotationAxis + 1) % 3] = 0.001f;
                grabDelta = m_worldPivotPose.rotation * localAxisNext;
            }

            m_grabPositionInPivotSpace =
                inverseRotation * grabDelta;

            var worldPositionDelta =
                inverseRotation * (targetTransform.position - m_worldPivotPose.position);

            var worldRotationDelta = inverseRotation * targetTransform.rotation;
            m_transformPoseInPivotSpace = new Pose(worldPositionDelta, worldRotationDelta);

            var initialOffset = m_worldPivotPose.rotation * m_grabPositionInPivotSpace;
            var initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);
            m_previousVectorInPivotSpace = Quaternion.Inverse(m_worldPivotPose.rotation) * initialVector;

            m_startAngle = m_constrainedRelativeAngle;
            m_relativeAngle = m_startAngle;

            var parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            m_transformPoseInPivotSpace.position /= parentScale;
        }

        public void UpdateTransform()
        {
            var grabPoint = m_grabbable.GrabPoints[0];
            var targetTransform = m_grabbable.Transform;

            var localAxis = Vector3.zero;
            localAxis[(int)m_rotationAxis] = 1f;
            m_worldPivotPose = ComputeWorldPivotPose();
            var rotationAxis = m_worldPivotPose.rotation * localAxis;

            // Project our positional offsets onto a plane with normal equal to the rotation axis
            var targetOffset = grabPoint.position - m_worldPivotPose.position;
            var targetVector = Vector3.ProjectOnPlane(targetOffset, rotationAxis);

            var previousVectorInWorldSpace =
                m_worldPivotPose.rotation * m_previousVectorInPivotSpace;

            // update previous
            m_previousVectorInPivotSpace = Quaternion.Inverse(m_worldPivotPose.rotation) * targetVector;

            var signedAngle =
                Vector3.SignedAngle(previousVectorInWorldSpace, targetVector, rotationAxis);

            m_relativeAngle += signedAngle;

            m_constrainedRelativeAngle = m_relativeAngle;
            if (Constraints.MinAngle.Constrain)
            {
                m_constrainedRelativeAngle = Mathf.Max(m_constrainedRelativeAngle, Constraints.MinAngle.Value);
            }
            if (Constraints.MaxAngle.Constrain)
            {
                m_constrainedRelativeAngle = Mathf.Min(m_constrainedRelativeAngle, Constraints.MaxAngle.Value);
            }

            var deltaRotation = Quaternion.AngleAxis(m_constrainedRelativeAngle - m_startAngle, rotationAxis);

            var parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            var transformDeltaInWorldSpace =
                new Pose(
                    m_worldPivotPose.rotation * (parentScale * m_transformPoseInPivotSpace.position),
                    m_worldPivotPose.rotation * m_transformPoseInPivotSpace.rotation);

            var transformDeltaRotated = new Pose(
                deltaRotation * transformDeltaInWorldSpace.position,
                deltaRotation * transformDeltaInWorldSpace.rotation);

            targetTransform.position = m_worldPivotPose.position + transformDeltaRotated.position;
            targetTransform.rotation = transformDeltaRotated.rotation;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalPivotTransform(Transform pivotTransform)
        {
            m_pivotTransform = pivotTransform;
        }

        public void InjectOptionalRotationAxis(Axis rotationAxis)
        {
            m_rotationAxis = rotationAxis;
        }

        public void InjectOptionalConstraints(OneGrabRotateConstraints constraints)
        {
            m_constraints = constraints;
        }

        #endregion
    }
}
