// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Fusion;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Interactions
{
    /// <summary>
    ///     This is a blend of the <see cref="OneGrabRotateTransformer"/> and <see cref="OneGrabFreeTransformer"/>
    ///     There's added functionality to allow toggling between a free movement and rotation transformation.
    ///     Furthermore, there's a "break snap" feature to pull from rotation to free movement.  
    /// </summary>
    public class OneGrabToggleRotateTransformer : NetworkBehaviour, ITransformer
    {
        public enum Axis
        {
            RIGHT = 0,
            UP = 1,
            FORWARD = 2
        }

        public enum RotationDirection
        {
            BOTH,
            POSITIVE,
            NEGATIVE
        }

        [Networked] public bool LockPosition { get; set; }
        [Networked] public bool CanUnlockPosition { get; set; }
        [Networked] public float ConstrainedRelativeAngle { get; set; }
        [Networked] private float RelativeAngle { get; set; }

        [SerializeField] private float m_unlockBreakDistance = 0.3f;
        [SerializeField, Optional] private Transform m_pivotTransform;
        [SerializeField] private Axis m_rotationAxis = Axis.UP;

        [SerializeField]
        private OneGrabRotateConstraints m_constraints = new()
        {
            MinAngle = new FloatConstraint(),
            MaxAngle = new FloatConstraint()
        };

        public Axis RotationAxis => m_rotationAxis;
        public RotationDirection RotationDirectionLimit;
        public OneGrabRotateConstraints Constraints => m_constraints;

        public Transform Pivot => m_pivotTransform != null ? m_pivotTransform : transform;

        [Serializable]
        public class OneGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        private IGrabbable m_grabbable;
        private Vector3 m_grabPositionInPivotSpace;
        private Pose m_transformPoseInPivotSpace;
        private Pose m_worldPivotPose;
        private Vector3 m_previousVectorInPivotSpace;
        private Quaternion m_localRotation;
        private float m_startAngle;
        private Pose m_grabDeltaInLocalSpace;

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

        public void BeginTransform()
        {
            var grabPoint = m_grabbable.GrabPoints[0];
            var targetTransform = m_grabbable.Transform;

            //BeginTransform functionality from OneGrabFreeTransformer 
            m_grabDeltaInLocalSpace = new Pose(targetTransform.InverseTransformVector(grabPoint.position - targetTransform.position),
                Quaternion.Inverse(grabPoint.rotation) * targetTransform.rotation);

            if (m_pivotTransform == null)
            {
                m_localRotation = targetTransform.localRotation;
            }

            //BeginTransform functionality from OneGrabRotateTransformer
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

            m_startAngle = ConstrainedRelativeAngle;
            RelativeAngle = m_startAngle;

            var parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            m_transformPoseInPivotSpace.position /= parentScale;
        }

        public void UpdateTransform()
        {
            var grabPoint = m_grabbable.GrabPoints[0];
            var targetTransform = m_grabbable.Transform;

            // Check for if the transform should switch from locked to rotation to free movement again. 
            if (LockPosition &&
                CanUnlockPosition &&
                Vector3.Distance(grabPoint.position, targetTransform.position) > m_unlockBreakDistance)
            {
                LockPosition = false;
            }

            if (!LockPosition)
            {
                //UpdateTransform functionality from OneGrabFreeTransformer 
                targetTransform.rotation = grabPoint.rotation * m_grabDeltaInLocalSpace.rotation;
                targetTransform.position = grabPoint.position - targetTransform.TransformVector(m_grabDeltaInLocalSpace.position);
                return;
            }

            //UpdateTransform functionality from OneGrabRotateTransformer
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

            if (RotationDirectionLimit == RotationDirection.NEGATIVE)
            {
                signedAngle = Mathf.Min(signedAngle, 0);
            }
            else if (RotationDirectionLimit == RotationDirection.POSITIVE)
            {
                signedAngle = Mathf.Max(signedAngle, 0);
            }

            RelativeAngle += signedAngle;

            ConstrainedRelativeAngle = RelativeAngle;
            if (Constraints.MinAngle.Constrain)
            {
                ConstrainedRelativeAngle = Mathf.Max(ConstrainedRelativeAngle, Constraints.MinAngle.Value);
            }
            if (Constraints.MaxAngle.Constrain)
            {
                ConstrainedRelativeAngle = Mathf.Min(ConstrainedRelativeAngle, Constraints.MaxAngle.Value);
            }

            var deltaRotation = Quaternion.AngleAxis(ConstrainedRelativeAngle - m_startAngle, rotationAxis);

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