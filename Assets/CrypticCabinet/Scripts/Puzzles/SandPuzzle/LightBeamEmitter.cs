// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Represents the source of a light beam, from which the light beam is generated.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightBeamEmitter : MonoBehaviour
    {
        [SerializeField] private Vector3 m_normalisedLightDirection = Vector3.forward;
        [SerializeField] private float m_raycastMaxDistance = 5f;
        [SerializeField] private float m_updateBounceLightSecs = 0.1f;
        [Range(1, 6), SerializeField] private int m_maxBounces = 6;
        [SerializeField] private LayerMask m_layerToHit;
        [SerializeField] private GameObject m_glarePrefab;

        private LineRenderer m_lineRenderer;
        private float m_lastBounceSecs;
        private float m_currentBeamWidth;
        private bool m_isBouncingLight;

        private List<Vector3> m_linePoints;
        private OVRCameraRig m_cameraRig;
        private LightBeamDestination m_lastBeamDestinationHit;
        private readonly Queue<LightBeamGlare> m_glaresPool = new();
        private readonly List<LightBeamGlare> m_usedGlaresObject = new();

        private void Start()
        {
            m_linePoints = Enumerable.Repeat(Vector3.zero, m_maxBounces + 1).ToList();
            m_isBouncingLight = true;
            m_lineRenderer = GetComponent<LineRenderer>();
            m_lastBeamDestinationHit = null;

            m_cameraRig = FindObjectOfType<OVRCameraRig>();
            if (m_cameraRig == null)
            {
                Debug.LogError("Couldn't find OVRCameraRig");
            }

            // set initial light beam to be 0 length at source
            var initialPosition = gameObject.transform.position;
            m_linePoints[0] = initialPosition;
            m_linePoints[1] = initialPosition;
            m_lineRenderer.SetPositions(m_linePoints.GetRange(0, 2).ToArray());
        }

        private void Update()
        {
            if (m_isBouncingLight && Time.time >= m_lastBounceSecs)
            {
                DrawLightBeamPoints();
                m_lastBounceSecs = Time.time + m_updateBounceLightSecs;
            }
        }

        private void OnDisable()
        {
            CleanUpAllGlareObjects();
        }

        private void OnDestroy()
        {
            m_isBouncingLight = false;
        }

        /// <summary>
        ///     Either hitting the first blocker, or bounce of all objects.
        /// </summary>
        private void DrawLightBeamPoints()
        {
            var keepBouncing = true;
            var initialPosition = gameObject.transform.position;
            var m = Matrix4x4.Rotate(transform.rotation);
            var ray = new Ray(initialPosition, m.MultiplyVector(m_normalisedLightDirection));
            var pointsCounter = 0;
            m_linePoints[pointsCounter++] = initialPosition;
            for (var index = 0; index < m_usedGlaresObject.Count; index++)
            {
                ReturnGlareObject(m_usedGlaresObject[index]);
            }

            m_usedGlaresObject.Clear();

            while (keepBouncing && pointsCounter < m_maxBounces + 1)
            {
                if (Physics.Raycast(ray, out var hitInfo, m_raycastMaxDistance, m_layerToHit))
                {
                    var lightBouncer = hitInfo.collider.gameObject.GetComponent<LightBeamBouncer>();
                    m_linePoints[pointsCounter] = hitInfo.point;

                    if (lightBouncer == null)
                    {
                        keepBouncing = false;

                        // This is the last bouncing we can have for this light beam.
                        // Check if the collided GameObject is a light beam destination
                        var lastLightBeamDestination = hitInfo.collider.gameObject.GetComponent<LightBeamDestination>();
                        if (lastLightBeamDestination != null)
                        {
                            m_lastBeamDestinationHit = lastLightBeamDestination;
                            lastLightBeamDestination.LightBeamArrived();
                        }
                        else if (m_lastBeamDestinationHit)
                        {
                            // If we already triggered a light beam destination before, untrigger it
                            m_lastBeamDestinationHit.LightBeamLeft();
                        }
                    }
                    else
                    {
                        PlaceGlare(m_linePoints[pointsCounter], m_cameraRig.centerEyeAnchor.position);
                    }

                    ray.origin = m_linePoints[pointsCounter];
                    ray.direction = Vector3.Reflect(ray.direction, hitInfo.normal);
                    pointsCounter++;
                }
                else
                {
                    keepBouncing = false;
                    m_linePoints[pointsCounter] = ray.origin + ray.direction * m_raycastMaxDistance;
                    pointsCounter++;

                    if (m_lastBeamDestinationHit)
                    {
                        // If we already triggered a light beam destination before, untrigger it
                        m_lastBeamDestinationHit.LightBeamLeft();
                    }
                }
            }

            m_lineRenderer.positionCount = pointsCounter;
            m_lineRenderer.SetPositions(m_linePoints.GetRange(0, pointsCounter).ToArray());
        }

        private LightBeamGlare GetGlareObject()
        {
            if (m_glaresPool.Count > 0)
            {
                var glareRenderer = m_glaresPool.Dequeue();
                glareRenderer.Renderer.enabled = true;
                return glareRenderer;
            }

            return Instantiate(m_glarePrefab).GetComponent<LightBeamGlare>();
        }

        private void ReturnGlareObject(LightBeamGlare glareObject)
        {
            glareObject.Renderer.enabled = false;
            m_glaresPool.Enqueue(glareObject);
        }

        private void PlaceGlare(Vector3 bouncePosition, Vector3 headsetPosition)
        {
            var glareObject = GetGlareObject();
            m_usedGlaresObject.Add(glareObject);

            var lookRotation = Quaternion.LookRotation(bouncePosition - headsetPosition, m_cameraRig.centerEyeAnchor.up);
            glareObject.transform.SetPositionAndRotation(bouncePosition, lookRotation);
        }

        private void CleanUpAllGlareObjects()
        {
            for (var index = 0; index < m_usedGlaresObject.Count; index++)
            {
                ReturnGlareObject(m_usedGlaresObject[index]);
            }
        }
    }
}
