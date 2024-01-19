// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using CrypticCabinet.Utils;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Describes the rope for the sand puzzle.
    ///     Based on: https://github.com/GaryMcWhorter/Verlet-Chain-Unity
    /// </summary>
    public class Rope : NetworkBehaviour
    {
        [Header("Verlet Parameters")]
        [SerializeField, Tooltip("The distance between each link in the chain")]
        private float m_nodeDistance = 0.35f;

        [SerializeField, Tooltip("The radius of the sphere collider used for each chain link")]
        private float m_nodeColliderRadius = 0.2f;

        [SerializeField, Tooltip("Works best with a lower value")]
        private float m_gravityStrength = 2;

        [SerializeField, Tooltip("The number of chain links. Decreases performance with high values and high iteration")]
        private int m_totalNodes = 100;

        [SerializeField, Range(0, 1), Tooltip("Modifier to dampen velocity so the simulation can stabilize")]
        private float m_velocityDampen = 0.95f;

        [SerializeField, Range(0, 0.99f),
         Tooltip("The stiffness of the simulation. Set to lower values for more elasticity")]
        private float m_stiffness = 0.8f;

        [SerializeField,
         Tooltip("Setting this will test collisions for every n iterations. Possibly more performance but less stable collisions")]
        private int m_iterateCollisionsEvery = 1;

        [SerializeField, Tooltip("Iterations for the simulation. More iterations is more expensive but more stable")]
        private int m_iterations = 100;

        [SerializeField, Tooltip("How many colliders to test against for every node.")]
        private int m_colliderBufferSize = 1;

        [SerializeField] private LayerMask m_layerMask;

        [SerializeField] private Transform m_startHandle;

        private Collider[] m_colliderHitBuffer;
        private Vector3 m_gravity;
        private Vector3 m_startLock;
        private Vector3 m_endLock;

        [Space]
        private Vector3[] m_previousNodePositions;
        private Vector3[] m_currentNodePositions;
        private Quaternion[] m_currentNodeRotations;

        private SphereCollider m_nodeCollider;
        private GameObject m_nodeTester;

        [SerializeField] private Transform[] m_meshNodes = Array.Empty<Transform>();

        [SerializeField] private float m_ropeFlutterMultiplier = 0.01f;
        [SerializeField] private float m_ropeFlutterChangeSpeed = 0.5f;

        [SerializeField] private float m_meshNodeGap = 0.2f;
        private Vector3[] m_evenlySpacedLocations;
        [SerializeField] private int m_evenlySpacedBufferSized = 100;
        private int m_spacedLocationCount;


        [Networked]
        public bool AddWeightToEnd { get; set; }
        [SerializeField] private float m_weightValue = 2;

        [SerializeField] private float m_interpolationSpeed = 10f;

        [Serializable]
        private struct GrabData : INetworkStruct
        {
            public int Index;
            public Vector3 WorldPos;
        }

        private const int MAX_GRABS = 2;

        [Networked, Capacity(2)]
        private NetworkArray<GrabData> Grabs { get; } = MakeInitializer(new GrabData[MAX_GRABS]);


        public override void Spawned()
        {
            base.Spawned();

            for (var i = 0; i < Grabs.Length; i++)
            {
                var grabData = Grabs[i];
                grabData.Index = -1;
                _ = Grabs.Set(i, grabData);
            }

            m_evenlySpacedLocations = new Vector3[m_evenlySpacedBufferSized];

            m_currentNodePositions = new Vector3[m_totalNodes];
            m_previousNodePositions = new Vector3[m_totalNodes];
            m_currentNodeRotations = new Quaternion[m_totalNodes];
            m_colliderHitBuffer = new Collider[m_colliderBufferSize];
            m_gravity = new Vector3(0, -m_gravityStrength, 0);

            // using a single dynamically created GameObject to test collisions on every node
            m_nodeTester = new GameObject { name = "Node Tester", layer = 8 };
            m_nodeCollider = m_nodeTester.AddComponent<SphereCollider>();
            m_nodeCollider.radius = m_nodeColliderRadius;

            m_meshNodeGap = Mathf.Max(m_meshNodeGap, 0.001f);

            Reset();
        }

        public void Reset()
        {
            m_startLock = m_startHandle.position;
            var stiffnessDiff = (1 - m_stiffness) * m_nodeDistance * m_totalNodes;
            var position = m_startLock + Vector3.down * stiffnessDiff;
            for (var i = 0; i < m_totalNodes; i++)
            {
                m_currentNodePositions[i] = position;
                m_currentNodeRotations[i] = Quaternion.identity;
                m_previousNodePositions[i] = position;
                position.y -= m_nodeDistance;
            }

            UpdateMeshVisuals(1);
        }

        private void Update()
        {
            m_startLock = m_startHandle.position;

            m_gravity = new Vector3(0, -m_gravityStrength, 0);

            m_meshNodeGap = Mathf.Max(m_meshNodeGap, 0.001f);

            UpdateMeshVisuals(Time.deltaTime * m_interpolationSpeed);
        }

        private void UpdateMeshVisuals(float interpRatio)
        {
            m_spacedLocationCount = 0;
            m_evenlySpacedLocations[m_spacedLocationCount] = m_startLock;
            m_spacedLocationCount++;

            float rollOver = 0;
            for (var i = -1; i < m_currentNodePositions.Length - 1; i++)
            {
                var start = m_startLock;
                if (i >= 0)
                {
                    start = m_currentNodePositions[i];
                }
                var end = m_currentNodePositions[i + 1];

                var dist = Vector3.Distance(start, end);

                var totalDist = dist + rollOver;
                if (totalDist < m_meshNodeGap)
                {
                    rollOver = totalDist;
                    continue;
                }

                var direction = (end - start).normalized;
                var current = m_meshNodeGap - rollOver;
                m_evenlySpacedLocations[m_spacedLocationCount] = Vector3.Lerp(m_evenlySpacedLocations[m_spacedLocationCount], start + direction * current, interpRatio);
                m_spacedLocationCount++;

                while (current + m_meshNodeGap < dist)
                {
                    current += m_meshNodeGap;
                    m_evenlySpacedLocations[m_spacedLocationCount] = Vector3.Lerp(m_evenlySpacedLocations[m_spacedLocationCount], start + direction * current, interpRatio);
                    m_spacedLocationCount++;
                }

                rollOver = dist - current;
            }

            var loopCount = Mathf.Min(m_meshNodes.Length, m_spacedLocationCount);
            for (var i = 0; i < loopCount; i++)
            {
                m_meshNodes[i].position = Vector3.Lerp(m_meshNodes[i].position, m_evenlySpacedLocations[m_spacedLocationCount - i - 1], interpRatio);
            }

            if (m_meshNodes.Length > m_spacedLocationCount)
            {
                for (var i = m_spacedLocationCount; i < m_meshNodes.Length; i++)
                {
                    m_meshNodes[i].position = Vector3.Lerp(m_meshNodes[i].position, m_evenlySpacedLocations[0], interpRatio);
                }
            }

            for (var i = 0; i < loopCount - 2; i++)
            {
                m_meshNodes[i].LookAt(m_meshNodes[i + 1]);
                m_meshNodes[i].RotateAround(m_meshNodes[i].position, m_meshNodes[i].right, 270);
            }

            m_meshNodes[^1].rotation = m_meshNodes[^2].rotation = m_meshNodes[^3].rotation;
        }

        private void FixedUpdate()
        {
            Simulate();

            for (var i = 0; i < m_iterations; i++)
            {
                ApplyConstraint();

                if (i % m_iterateCollisionsEvery == 0)
                {
                    AdjustCollisions();
                }
            }

            SetAngles();
        }

        public Vector3 NearestPointOnRope(Vector3 queryPoint)
        {
            var currentMax = float.MaxValue;
            var currentPoint = Vector3.zero;

            for (var i = 0; i < m_currentNodePositions.Length - 1; i++)
            {
                var calculatedPoint = MathsUtils.NearestPointOnLineSegment(
                    m_currentNodePositions[i], m_currentNodePositions[i + 1], queryPoint);
                var calculatedDistSquare = (calculatedPoint - queryPoint).sqrMagnitude;
                if (calculatedDistSquare < currentMax)
                {
                    currentMax = calculatedDistSquare;
                    currentPoint = calculatedPoint;
                }
            }

            return currentPoint;
        }


        public int GrabRopeAtPoint(Vector3 queryPoint)
        {
            var currentMax = float.MaxValue;
            var currentIndex = -1;

            for (var i = 0; i < m_currentNodePositions.Length - 1; i++)
            {
                var calculatedPoint = MathsUtils.NearestPointOnLineSegment(
                    m_currentNodePositions[i], m_currentNodePositions[i + 1], queryPoint);
                var calculatedDistSquare = (calculatedPoint - queryPoint).sqrMagnitude;
                if (calculatedDistSquare < currentMax)
                {
                    currentMax = calculatedDistSquare;
                    currentIndex = i;
                }
            }

            for (var i = 0; i < Grabs.Length; i++)
            {
                var grabData = Grabs[i];
                if (grabData.Index == -1)
                {
                    grabData.Index = currentIndex;
                    grabData.WorldPos = queryPoint;
                    _ = Grabs.Set(i, grabData);
                    return currentIndex;
                }
            }

            // failed to save the grab information so should return as failed.
            return -1;
        }

        public void ReleaseRope(int index)
        {
            for (var i = 0; i < Grabs.Length; i++)
            {
                var grabData = Grabs[i];
                if (grabData.Index == index)
                {
                    grabData.Index = -1;
                    _ = Grabs.Set(i, grabData);
                    return;
                }
            }
        }

        public void UpdateGrabbedRope(int index, Vector3 pos)
        {
            for (var i = 0; i < Grabs.Length; i++)
            {
                var grabData = Grabs[i];
                if (grabData.Index == index)
                {
                    grabData.WorldPos = pos;
                    _ = Grabs.Set(i, grabData);
                    return;
                }
            }
        }

        private void Simulate()
        {
            var fixedDt = Time.fixedDeltaTime;
            for (var i = 0; i < m_totalNodes; i++)
            {
                var velocity = m_currentNodePositions[i] - m_previousNodePositions[i];
                velocity *= m_velocityDampen;

                m_previousNodePositions[i] = m_currentNodePositions[i];

                // calculate new position
                var newPos = m_currentNodePositions[i] + velocity;
                newPos += m_gravity * fixedDt;

                newPos.x += (Mathf.PerlinNoise(0, Time.time * m_ropeFlutterChangeSpeed) - 0.5f) *
                            m_ropeFlutterMultiplier;
                newPos.z += (Mathf.PerlinNoise(Time.time * m_ropeFlutterChangeSpeed, 0) - 0.5f) *
                            m_ropeFlutterMultiplier;

                m_currentNodePositions[i] = newPos;
            }

            if (AddWeightToEnd)
            {
                for (var i = 0; i < m_totalNodes - 1; i++)
                {
                    var pos = m_currentNodePositions[i];
                    var direction = m_currentNodePositions[i + 1] - m_previousNodePositions[i];
                    pos += direction.normalized * (m_weightValue * fixedDt);
                    m_currentNodePositions[i] = pos;
                }
            }
        }

        private void AdjustCollisions()
        {
            for (var i = 0; i < m_totalNodes; i++)
            {
                if (i % 2 == 0) continue;

                var result = Physics.OverlapSphereNonAlloc(
                    m_currentNodePositions[i], m_nodeColliderRadius + 0.01f, m_colliderHitBuffer, m_layerMask.value,
                    QueryTriggerInteraction.Ignore);

                for (var n = 0; n < result; n++)
                {
                    var colliderPosition = m_colliderHitBuffer[n].transform.position;
                    var colliderRotation = m_colliderHitBuffer[n].gameObject.transform.rotation;

                    _ = Physics.ComputePenetration(
                        m_nodeCollider, m_currentNodePositions[i], Quaternion.identity, m_colliderHitBuffer[n],
                        colliderPosition, colliderRotation, out var dir, out var distance);

                    m_currentNodePositions[i] += dir * distance;
                }
            }
        }

        private void ApplyConstraint()
        {
            m_currentNodePositions[0] = m_startLock;

            foreach (var grabData in Grabs)
            {
                if (grabData.Index >= 0)
                {
                    m_currentNodePositions[grabData.Index] = grabData.WorldPos;
                }
            }

            for (var i = 0; i < m_totalNodes - 1; i++)
            {
                var node1 = m_currentNodePositions[i];
                var node2 = m_currentNodePositions[i + 1];

                // Get the current distance between rope nodes
                var currentDistance = (node1 - node2).magnitude;
                var difference = Mathf.Abs(currentDistance - m_nodeDistance);
                var direction = Vector3.zero;

                // determine what direction we need to adjust our nodes
                if (currentDistance > m_nodeDistance)
                {
                    direction = (node1 - node2).normalized;
                }
                else if (currentDistance < m_nodeDistance)
                {
                    direction = (node2 - node1).normalized;
                }

                // calculate the movement vector
                var movement = direction * difference;

                // apply correction
                m_currentNodePositions[i] -= movement * m_stiffness;
                m_currentNodePositions[i + 1] += movement * m_stiffness;
            }
        }

        private void SetAngles()
        {
            for (var i = 0; i < m_totalNodes - 1; i++)
            {
                var node1 = m_currentNodePositions[i];
                var node2 = m_currentNodePositions[i + 1];

                var dir = (node2 - node1).normalized;
                if (dir != Vector3.zero)
                {
                    if (i > 0)
                    {
                        var desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                        m_currentNodeRotations[i + 1] = desiredRotation;
                    }
                    else if (i < m_totalNodes - 1)
                    {
                        var desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                        m_currentNodeRotations[i + 1] = desiredRotation;
                    }
                    else
                    {
                        var desiredRotation = Quaternion.LookRotation(dir, Vector3.right);
                        m_currentNodeRotations[i] = desiredRotation;
                    }
                }

                if (i % 2 == 0 && i != 0)
                {
                    m_currentNodeRotations[i + 1] *= Quaternion.Euler(0, 0, 90);
                }
            }
        }
    }
}