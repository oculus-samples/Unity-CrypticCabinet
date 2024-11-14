// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.MRUtilityKit;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    /// Generates a mesh that represents a plane's boundary.
    /// </summary>
    /// <remarks>
    /// When added to a GameObject that represents a scene entity, such as a floor, ceiling, or desk, this component
    /// generates a mesh from its boundary vertices.
    /// </remarks>
    [RequireComponent(typeof(MeshFilter))]
    public class MRUKPlaneMeshFilter : MonoBehaviour
    {
        private MeshFilter m_meshFilter;

        private Mesh m_mesh;

        private JobHandle? m_jobHandle;

        private bool m_meshRequested;

        private NativeArray<Vector2> m_boundary;

        private NativeArray<int> m_triangles;


        private void Start()
        {
            m_mesh = new Mesh();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshFilter.sharedMesh = m_mesh;

            var sceneAnchor = GetComponent<MRUKAnchor>();
            m_mesh.name = sceneAnchor ?
                $"{nameof(MRUKPlaneMeshFilter)} {sceneAnchor.Anchor.Uuid}" :
                $"{nameof(MRUKPlaneMeshFilter)} (anonymous)";

            RequestMeshGeneration();
        }

        internal void ScheduleMeshGeneration()
        {
            if (m_jobHandle != null) return;
            if (!TryGetComponent<MRUKAnchor>(out var plane) || plane.PlaneBoundary2D.Count < 3) return;

            var vertexCount = plane.PlaneBoundary2D.Count;
            Debug.Assert(
                m_boundary.IsCreated == false,
                "Boundary buffer should not be allocated.");

            m_boundary = new NativeArray<Vector2>(
                vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < plane.PlaneBoundary2D.Count; i++)
            {
                //feed in the boundary verts in reverse because MRUK uses counter-clockwise polys
                m_boundary[i] = plane.PlaneBoundary2D[plane.PlaneBoundary2D.Count - 1 - i];
            }

            m_triangles = new NativeArray<int>((vertexCount - 2) * 3, Allocator.TempJob);
            m_jobHandle = new TriangulateBoundaryJob { Boundary = m_boundary, Triangles = m_triangles, }.Schedule();
        }

        private void Update()
        {
            if (m_jobHandle?.IsCompleted == true)
            {
                // Even though the job is complete, we have to call Complete() in order
                // to mark the shared arrays as safe to read from.
                m_jobHandle.Value.Complete();
                m_jobHandle = null;
            }
            else
            {
                // Otherwise, there's a job running
                return;
            }

            if (m_boundary.IsCreated && m_triangles.IsCreated)
            {
                try
                {
                    if (m_triangles[0] == 0 &&
                        m_triangles[1] == 0 &&
                        m_triangles[2] == 0)
                    {
                        return;
                    }

                    var vertices = new NativeArray<Vector3>(
                        m_boundary.Length, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var normals = new NativeArray<Vector3>(
                        m_boundary.Length, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var uvs = new NativeArray<Vector2>(
                        m_boundary.Length, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);

                    for (var i = 0; i < m_boundary.Length; i++)
                    {
                        var point = m_boundary[i];
                        vertices[i] = new Vector3(point.x, point.y, 0);
                        normals[i] = new Vector3(0, 0, 1);
                        uvs[i] = new Vector2(point.x, point.y);
                    }

                    using (vertices)
                    using (normals)
                    using (uvs)
                    {
                        m_mesh.Clear();
                        m_mesh.SetVertices(vertices);
                        m_mesh.SetIndices(m_triangles, MeshTopology.Triangles, 0, calculateBounds: true);
                        m_mesh.SetNormals(normals);
                        m_mesh.SetUVs(0, uvs);
                    }
                }
                finally
                {
                    m_boundary.Dispose();
                    m_triangles.Dispose();
                }
            }
            else if (m_meshRequested)
            {
                ScheduleMeshGeneration();
            }
        }

        internal void RequestMeshGeneration()
        {
            m_meshRequested = true;
            if (enabled)
            {
                ScheduleMeshGeneration();
            }
        }

        private void OnDisable()
        {
            // Job completed but we may not yet have consumed the data
            if (m_triangles.IsCreated)
            {
                _ = m_triangles.Dispose(m_jobHandle ?? default);
            }

            m_triangles = default;
            m_jobHandle = null;
        }

        private struct TriangulateBoundaryJob : IJob
        {
            [ReadOnly]
            public NativeArray<Vector2> Boundary;

            [WriteOnly]
            public NativeArray<int> Triangles;

            private struct NList : IDisposable
            {
                public int Count { get; private set; }

                private NativeArray<int> m_data;

                public NList(int capacity, Allocator allocator)
                {
                    Count = capacity;
                    m_data = new NativeArray<int>(capacity, allocator);
                    for (var i = 0; i < capacity; i++)
                    {
                        m_data[i] = i;
                    }
                }

                public void RemoveAt(int index)
                {
                    --Count;
                    for (var i = index; i < Count; i++)
                    {
                        m_data[i] = m_data[i + 1];
                    }
                }

                public int GetAt(int index)
                {
                    return index >= Count ? m_data[index % Count] : index < 0 ? m_data[index % Count + Count] : m_data[index];
                }

                public int this[int index] => m_data[index];

                public void Dispose() => m_data.Dispose();
            }

            public void Execute()
            {
                if (Boundary.Length == 0 || float.IsNaN(Boundary[0].x)) return;

                var indexList = new NList(Boundary.Length, Allocator.Temp);
                using var disposer = indexList;

                var indexListChanged = true;

                // Find a valid triangle.
                // Checks:
                // 1. Connected edges do not form a co-linear or reflex angle.
                // 2. There's no vertices inside the selected triangle area.
                var triangleCount = 0;
                while (indexList.Count > 3)
                {
                    if (!indexListChanged)
                    {
                        Debug.LogError($"[{nameof(MRUKPlaneMeshFilter)}] Plane boundary triangulation failed.");

                        Triangles[0] = 0;
                        Triangles[1] = 0;
                        Triangles[2] = 0;
                        return;
                    }

                    indexListChanged = false;

                    for (var i = 0; i < indexList.Count; i++)
                    {
                        var a = indexList[i];
                        var b = indexList.GetAt(i - 1);
                        var c = indexList.GetAt(i + 1);

                        var va = Boundary[a];
                        var vb = Boundary[b];
                        var vc = Boundary[c];

                        var atob = vb - va;
                        var atoc = vc - va;

                        // reflex angle check
                        if (Cross(atob, atoc) < 0) continue;

                        var validTriangle = true;
                        for (var j = 0; j < Boundary.Length; j++)
                        {
                            if (j == a || j == b || j == c) continue;

                            if (PointInTriangle(Boundary[j], va, vb, vc))
                            {
                                validTriangle = false;
                                break;
                            }
                        }

                        // add indices to triangle list
                        if (!validTriangle) continue;

                        Triangles[triangleCount++] = c;
                        Triangles[triangleCount++] = a;
                        Triangles[triangleCount++] = b;

                        indexList.RemoveAt(i);
                        indexListChanged = true;
                        break;
                    }
                }

                Triangles[triangleCount++] = indexList[2];
                Triangles[triangleCount++] = indexList[1];
                Triangles[triangleCount] = indexList[0];
            }

            private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

            private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) =>
                Cross(b - a, p - a) >= 0 &&
                Cross(c - b, p - b) >= 0 &&
                Cross(a - c, p - c) >= 0;
        }
    }
}