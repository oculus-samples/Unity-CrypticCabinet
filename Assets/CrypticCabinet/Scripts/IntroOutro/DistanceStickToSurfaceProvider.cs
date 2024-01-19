// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Provides a visual DistanceStickToSurface object configured for the specified ObjectPlacementType.
    /// </summary>
    public class DistanceStickToSurfaceProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField] private LayerMask m_layerMaskWall;
        [SerializeField] private LayerMask m_layerMaskFloor;
        [SerializeField] private ObjectPlacementType m_placementType;
        [SerializeField] private bool m_useMaxWallHeight;
        [SerializeField] private float m_maxWallHeight = 1.0f;
        private BoxCollider m_queryCollider;

        public enum ObjectPlacementType
        {
            WALL_OBJECT,
            AGAINST_WALL,
            HORIZONTAL
        }

        private void Start()
        {
            m_queryCollider = GetComponentInParent<BoxCollider>();
        }

        public IMovement CreateMovement()
        {
            var thisTransform = transform;
            var currentPose = new Pose(thisTransform.position, thisTransform.rotation);
            return new DistanceStickToSurface(currentPose, m_layerMaskWall, m_layerMaskFloor, m_placementType,
                m_useMaxWallHeight, m_maxWallHeight, m_queryCollider);
        }
    }

    /// <summary>
    ///     A ray that sticks to the specified surface depending on the layer mask and ObjectPlacementType.
    /// </summary>
    public class DistanceStickToSurface : IMovement
    {
        public Pose Pose => m_current;
        public bool Stopped => true;

        private Pose m_current;
        private Pose m_target;
        private Ray m_ray;
        private LayerMask m_layerMaskWall;
        private LayerMask m_layerMaskFloor;
        private readonly RaycastHit[] m_raycastHits;
        private readonly DistanceStickToSurfaceProvider.ObjectPlacementType m_objectPlacementType;
        private readonly bool m_useMaxWallHeight;
        private readonly float m_maxWallHeight;
        private BoxCollider m_queryCollider;

        public DistanceStickToSurface(Pose currentPose, LayerMask layerMaskWall, LayerMask layerMaskFloor,
            DistanceStickToSurfaceProvider.ObjectPlacementType objectPlacementType, bool useMaxWallHeight,
                float maxWallHeight, BoxCollider queryCollider)
        {
            m_target = m_current = currentPose;
            m_ray = new Ray();
            m_layerMaskWall = layerMaskWall;
            m_layerMaskFloor = layerMaskFloor;
            m_raycastHits = new RaycastHit[5];
            m_objectPlacementType = objectPlacementType;
            m_useMaxWallHeight = useMaxWallHeight;
            m_maxWallHeight = maxWallHeight;
            m_queryCollider = queryCollider;
        }

        public void MoveTo(Pose target)
        {
        }

        /// <summary>
        ///     Updates the position and direction of the ray depending on the ObjectPlacementType.
        /// </summary>
        /// <param name="target">The target pose for the ray.</param>
        /// <exception cref="ArgumentOutOfRangeException">Exception thrown if unexpected ObjectPlacementType.</exception>
        public void UpdateTarget(Pose target)
        {
            m_ray.direction = target.forward;
            m_ray.origin = target.position;

            switch (m_objectPlacementType)
            {
                case DistanceStickToSurfaceProvider.ObjectPlacementType.WALL_OBJECT:
                    UpdateForWall();
                    break;
                case DistanceStickToSurfaceProvider.ObjectPlacementType.AGAINST_WALL:
                    UpdateForAgainstWall();
                    break;
                case DistanceStickToSurfaceProvider.ObjectPlacementType.HORIZONTAL:
                    UpdateForHorizontal();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Updates the position of objects that move freely on wall as they are manipulated keeping them on the wall.
        /// </summary>
        private void UpdateForWall()
        {
            var size = Physics.RaycastNonAlloc(m_ray, m_raycastHits, 10, m_layerMaskWall.value);
            if (size > 0)
            {
                var hitAngleOffset = Vector3.Dot(Vector3.up, m_raycastHits[0].normal);
                if (hitAngleOffset < 0.25)
                {
                    var newPosition = m_raycastHits[0].point;
                    if (m_useMaxWallHeight)
                    {
                        newPosition.y = Mathf.Min(newPosition.y, m_maxWallHeight);
                    }
                    m_target.position = newPosition;
                    m_target.rotation = Quaternion.LookRotation(m_raycastHits[0].normal, Vector3.up);
                }
            }
        }

        /// <summary>
        ///     Raycast over an horizontal surface.
        /// </summary>
        private void UpdateForHorizontal()
        {
            var size = Physics.RaycastNonAlloc(m_ray, m_raycastHits, 10, m_layerMaskFloor.value);
            if (size > 0)
            {
                var hitAngleOffset = Vector3.Dot(Vector3.up, m_raycastHits[0].normal);
                if (hitAngleOffset > 0.75)
                {
                    var newPosition = m_raycastHits[0].point;
                    var castOrigin = m_ray.origin;
                    castOrigin.y = newPosition.y;
                    newPosition = ClampedPosition(castOrigin, newPosition);
                    m_target.position = newPosition;
                    var rayOrigin = m_ray.origin;
                    rayOrigin.y = newPosition.y;
                    rayOrigin -= newPosition;
                    m_target.rotation = Quaternion.LookRotation(rayOrigin.normalized, Vector3.up);
                }
            }
        }

        /// <summary>
        ///     Updates the position of manipulated objects that must remain against a wall but at ground level.
        /// </summary>
        private void UpdateForAgainstWall()
        {
            var size = Physics.RaycastNonAlloc(m_ray, m_raycastHits, 10, m_layerMaskWall.value);
            if (size > 0)
            {
                var hitAngleOffset = Vector3.Dot(Vector3.up, m_raycastHits[0].normal);
                if (hitAngleOffset < 0.25)
                {
                    var foundRotation = Quaternion.LookRotation(m_raycastHits[0].normal, Vector3.up);
                    var newPosition = m_raycastHits[0].point;

                    var center = newPosition + m_raycastHits[0].normal * 0.05f;
                    var direction = Vector3.down;
                    var extent = Vector3.one * 0.05f;
                    // use boxcast to cover any potential gaps between wall and floor
                    size = Physics.BoxCastNonAlloc(center, extent, direction, m_raycastHits, foundRotation, m_layerMaskFloor.value);
                    if (size > 0)
                    {
                        // anchor to floor
                        newPosition.y = m_raycastHits[0].point.y;
                    }
                    else
                    {
                        Debug.LogWarning("No floor found at wall, default position to y=0");
                        newPosition.y = 0;
                    }
                    m_target.position = newPosition;
                    m_target.rotation = foundRotation;
                }
            }
        }

        public void StopAndSetPose(Pose source)
        {
        }

        /// <summary>
        ///     Called by the Oculus input system linearly interpolates the object smoothly to the new location. 
        /// </summary>
        public void Tick()
        {
            m_current.Lerp(m_target, 0.25f);
        }

        /// <summary>
        ///     Calculates where a floor object should be able to move to to prevent placing objects half outside of
        ///     the play space.
        /// </summary>
        /// <param name="start">The last location of the object.</param>
        /// <param name="targetEnd">The target new location</param>
        /// <returns>Adjusted target location inside the play space</returns>
        private Vector3 ClampedPosition(Vector3 start, Vector3 targetEnd)
        {
            if (m_queryCollider == null)
            {
                return targetEnd;
            }

            var extends = m_queryCollider.size * 0.5f;
            var center = m_queryCollider.center;
            var colliderStart = start + center;
            var colliderEnd = targetEnd + center;

            var castDirection = (colliderEnd - colliderStart).normalized;
            if (Physics.BoxCast(colliderStart, extends, castDirection, out var hitInfo,
                    m_queryCollider.transform.rotation, (colliderEnd - colliderStart).magnitude, m_layerMaskWall))
            {
                var pos = start + castDirection * hitInfo.distance;
                return pos;
            }

            return targetEnd;
        }
    }
}