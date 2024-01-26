// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CrypticCabinet.Utils;
using UnityEngine;
using static CrypticCabinet.Utils.MathsUtils;
using Random = UnityEngine.Random;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     System for finding the space on a floor including functionality to place relative to the wall.
    /// </summary>
    [RequireComponent(typeof(OVRScenePlaneMeshFilter))]
    public class FloorSpaceFinder : MonoBehaviour, ISpaceFinder
    {
        /// <summary>
        /// Plane that describes the floor. 
        /// </summary>
        private OVRScenePlane m_floorPlane;

        /// <summary>
        /// Size of the cells on the floor.
        /// </summary>
        [SerializeField] private float m_cellSize = 0.09f;

        /// <summary>
        /// 2D List of the cells that will describe the floor.
        /// </summary>
        private readonly List<List<Cell>> m_cells = new();

        /// <summary>
        /// The floor mesh collider, used for placing the cells.
        /// </summary>
        private MeshCollider m_meshCollider;

        /// <summary>
        /// The largest distance to an edge, used for visualisations.
        /// </summary>
        private float m_largestDistToEdge = float.MinValue;

        /// <summary>
        /// Flag to indicate if the cells are set up.
        /// </summary>
        private bool m_isSetUp;

        /// <summary>
        /// Cache of objects that were passed in before the cells were set up. 
        /// </summary>
        private readonly List<(Matrix4x4 transform, Vector3 size)> m_volumeCache = new();

        /// <summary>
        /// The root transform that has all child cells game objects
        /// </summary>
        private Transform m_cellsRootTransform;

        public Vector3 FloorCenterPosition { get; private set; }

        private bool m_debugViewEnabled;

        public Material DebugMaterial;

        public bool IsReadyToGenerate { get; private set; }

        /// <summary>
        /// Structure that describes the cells.
        /// </summary>
        private struct Cell
        {
            public Transform CellDebugRoot;
            public float DistanceToWall;
            public float DistanceToAnyObject;
            private float m_originalDistanceToAnyObject;
            public Vector2 LocalPosition;
            public bool Blocked;
            private bool m_originallyBlocked;
            public Renderer DebugRenderer;

            public void Reset()
            {
                DistanceToAnyObject = m_originalDistanceToAnyObject;
                Blocked = m_originallyBlocked;
            }

            public void RememberDistance()
            {
                m_originalDistanceToAnyObject = DistanceToAnyObject;
                m_originallyBlocked = Blocked;
            }
        }

        private struct CellIndex
        {
            public int X;
            public int Y;
        }

        public bool HasSetUpCompleted() => m_isSetUp;
        public void CleanUp()
        {
            foreach (var cell in m_cells.SelectMany(column => column))
            {
                Destroy(cell.CellDebugRoot.gameObject);
            }

            if (m_cellsRootTransform != null)
            {
                Destroy(m_cellsRootTransform.gameObject);
            }
        }

        public void GetFinderTransform(out Matrix4x4 localToWorldMatrix) => localToWorldMatrix = transform.localToWorldMatrix;

        public void GetFinderSize(out Vector3 worldSize) => worldSize = m_floorPlane.Dimensions;

        private Collider[] m_nonAllocColliders = new Collider[1000];

        public bool CheckPhysicsResultIsClear(Collider[] hitColliders, int hitCount)
        {
            for (var j = 0; j < hitCount; j++)
            {
                foreach (var t in m_cells)
                {
                    for (var y = 0; y < t.Count; y++)
                    {
                        if (t[y].CellDebugRoot != hitColliders[j].transform)
                        {
                            continue;
                        }

                        if (t[y].Blocked)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void BlockPhysicsResult(Collider[] hitColliders, int hitCount)
        {
            var hitCells = new List<CellIndex>();
            for (var j = 0; j < hitCount; j++)
            {
                for (var x = 0; x < m_cells.Count; x++)
                {
                    var t = m_cells[x];
                    for (var y = 0; y < t.Count; y++)
                    {
                        var cell = t[y];
                        if (cell.CellDebugRoot != hitColliders[j].transform)
                        {
                            continue;
                        }

                        hitCells.Add(new CellIndex { X = x, Y = y });
                        cell.Blocked = true;
                        t[y] = cell;
                    }
                }
            }

            UpdateDistanceField(hitCells);
        }

        /// <summary>
        /// Requests a random floor location that is both a set distance from a floor and and not intersecting with
        /// any objects. 
        /// </summary>
        /// <param name="locationToFace">The location of the user that the spawned object should face.</param>
        /// <param name="objectRadius">Radius of the object being placed</param>
        /// <param name="foundPosition">Returns the chosen random location.</param>
        /// <param name="markAsBlocked">When true, the cell is marked as blocked, otherwise it is unchanged.</param>
        /// <param name="edgeDistance">The desired distance from the wall, otherwise -1 to ignore.</param>
        /// <returns>Float to if a location was successfully found.</returns>
        public bool RequestRandomLocation(Vector3 locationToFace, float objectRadius, out Vector3 foundPosition, out Quaternion foundRotation, bool markAsBlocked = false, float edgeDistance = -1)
        {
            return RequestRandomLocation(locationToFace, Vector3.one * (objectRadius * 2), out foundPosition, out foundRotation, markAsBlocked);
        }


        public bool RequestRandomLocation(Vector3 locationToFace, Vector3 objectDimensions,
            out Vector3 foundPosition, out Quaternion foundRotation, bool markAsBlocked = false)
        {
            var allCells = new List<Cell>();
            foreach (var column in m_cells)
            {
                allCells.AddRange(column);
            }
            allCells.Shuffle();

            var objectHalfExtends = objectDimensions * 0.5f;
            var centerOffset = new Vector3(0, objectHalfExtends.y, 0);
            var minDist = Mathf.Max(objectHalfExtends.x, objectHalfExtends.z);

            foreach (var cell in allCells)
            {
                if (cell.DistanceToAnyObject < minDist)
                {
                    continue;
                }

                var angleOffset = Vector3.SignedAngle(cell.CellDebugRoot.right, Vector3.right, Vector3.up) % 90;

                var testPos = cell.CellDebugRoot.position + centerOffset;
                var testRotation = GetTestRotation(locationToFace, testPos) * Quaternion.Euler(0, -angleOffset, 0);
                var hitCount = Physics.OverlapBoxNonAlloc(
                    testPos, objectHalfExtends, m_nonAllocColliders, testRotation);
                if (CheckPhysicsResultIsClear(m_nonAllocColliders, hitCount))
                {
                    foundPosition = cell.CellDebugRoot.position;
                    foundRotation = testRotation;
                    if (markAsBlocked)
                    {
                        BlockPhysicsResult(m_nonAllocColliders, hitCount);
                    }

                    return true;
                }
            }

            foundPosition = Vector3.zero;
            foundRotation = Quaternion.identity;
            return false;
        }

        private Quaternion GetTestRotation(Vector3 targetPos, Vector3 objectPosition)
        {
            objectPosition.y = 0;
            targetPos.y = 0;
            var lookDir = targetPos - objectPosition;
            var rot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            var roundedRotation = Mathf.Round(rot.eulerAngles.y / 90) * 90;
            return Quaternion.Euler(0, roundedRotation, 0);
        }


        /// <summary>
        /// Due to the slightly odd nature of the the Scene understanding system, the floor boundary points
        /// are not immediately available so I've added a short delay before generating the cells. 
        /// </summary>
        private IEnumerator Start()
        {
            IsReadyToGenerate = false;
            m_floorPlane = GetComponent<OVRScenePlane>();
            var meshFilter = GetComponent<MeshFilter>();
            m_meshCollider = gameObject.AddComponent<MeshCollider>();
            var colliderMesh = meshFilter.sharedMesh;
            // The mesh for the collider is not actually loaded at this point so we have to wait until we get some vertices 
            while (colliderMesh.vertices.Length <= 0 || colliderMesh.GetIndices(0).Length <= 0)
            {
                yield return null;
            }
#if UNITY_EDITOR
            // When in editor and using XR Simulator the floor mesh is flipped, we need to rotate it to fit the floors
            // forward direction.
            if (Vector3.Dot(colliderMesh.normals[0], transform.forward) < 0)
            {
                var normals = new List<Vector3>();
                colliderMesh.GetNormals(normals);
                var forward = transform.forward;
                for (var i = 0; i < normals.Count; ++i)
                {
                    var normal = normals[i];
                    Vector3.RotateTowards(normal, forward, Mathf.PI, Mathf.PI);
                    normals[i] = normal;
                }
                colliderMesh.SetNormals(normals);
            }
#endif

            m_meshCollider.sharedMesh = colliderMesh;
            yield return null;
            IsReadyToGenerate = true;
        }

        /// <summary>
        /// Generates the cells over the floor plane. This is done by generating an array of points that are in a
        /// grid using the <see cref="OVRScenePlane.Dimensions"/> field, and then raycast against the floor plane mesh
        /// collider to find valid locations.
        /// </summary>
        public void GenerateCells()
        {
            if (m_cellsRootTransform == null)
            {
                m_cellsRootTransform = new GameObject("FloorCells").transform;
                m_cellsRootTransform.SetParent(transform, false);
            }
            var floorPlaneDimensions = m_floorPlane.Dimensions;

            // this is the same spacing system that is used for the walls and desk cell grids.
            var xCellCount = Mathf.FloorToInt(floorPlaneDimensions.x / m_cellSize);
            var xCellSize = floorPlaneDimensions.x / xCellCount;

            var yCellCount = Mathf.FloorToInt(floorPlaneDimensions.y / m_cellSize);
            var yCellSize = floorPlaneDimensions.y / yCellCount;

            var startX = -(floorPlaneDimensions.x / 2) + xCellSize / 2;
            var startY = -(floorPlaneDimensions.y / 2) + yCellSize / 2;

            m_largestDistToEdge = float.MinValue;
            var testRay = new Ray();

            for (var x = 0; x < xCellCount; x++)
            {
                m_cells.Add(new List<Cell>());
                for (var y = 0; y < yCellCount; y++)
                {
                    // Calculates the world space locations of the ray cast from the calculated cell position.
                    var position = new Vector3(startX + x * xCellSize, startY + yCellSize * y, 0.5f);
                    var thisTransform = transform;
                    testRay.origin = thisTransform.localToWorldMatrix.MultiplyPoint(position);
                    testRay.direction = thisTransform.forward * -1.0f;
                    position.z = 0;

                    // raycasts with the floor for points that hit the floor a cell structure is created
                    if (m_meshCollider.Raycast(testRay, out _, 1.0f))
                    {
                        var debugVisualiser = AddDebugVisualiser(position);
                        var distance = DistanceToNearestEdge(position);
                        m_largestDistToEdge = Mathf.Max(distance, m_largestDistToEdge);
                        var cellRenderer = debugVisualiser.GetComponent<Renderer>();
                        cellRenderer.enabled = false;
                        m_cells[x].Add(new Cell
                        {
                            CellDebugRoot = debugVisualiser,
                            Blocked = false,
                            LocalPosition = position,
                            DistanceToWall = distance,
                            DistanceToAnyObject = distance,
                            DebugRenderer = cellRenderer,
                        });
                        cellRenderer.material = DebugMaterial;
                        cellRenderer.material.color = Color.green;
#if UNITY_EDITOR
                        debugVisualiser.gameObject.name = distance.ToString(CultureInfo.InvariantCulture);
#endif
                    }
                }
            }

            // debugView off by default
            m_debugViewEnabled = false;

            RememberDistanceField();

            m_isSetUp = true;

            // Set up any volumes that were passed in before the cells were initialised. 
            foreach (var volumeObject in m_volumeCache)
            {
                CalculateBlockedArea(volumeObject.transform, volumeObject.size);
            }
        }

        public void RememberDistanceField()
        {
            var centerCellDist = float.MinValue;

            foreach (var column in m_cells)
            {
                for (var y = 0; y < column.Count; y++)
                {
                    var cell = column[y];
                    cell.RememberDistance();
                    column[y] = cell;

                    if (column[y].DistanceToAnyObject > centerCellDist)
                    {
                        centerCellDist = column[y].DistanceToAnyObject;
                        FloorCenterPosition = column[y].CellDebugRoot.position;
                    }
                }
            }
        }

        public void ResetDistanceField()
        {
            foreach (var column in m_cells)
            {
                for (var y = 0; y < column.Count; y++)
                {
                    var cell = column[y];
                    cell.Reset();
                    column[y] = cell;
                }
            }

            UpdateCellVisualisations();
        }

        public void SetDebugViewEnabled(bool isVisible)
        {
            m_debugViewEnabled = isVisible;
            UpdateDebugVisible();
        }

        public void ToggleDebugView()
        {
            m_debugViewEnabled = !m_debugViewEnabled;
            UpdateDebugVisible();
        }

        public void SetSearchCollidersActive(bool collidersActive)
        {
            foreach (var cell in m_cells.SelectMany(column => column))
            {
                cell.CellDebugRoot.gameObject.SetActive(collidersActive);
            }
        }

        private void UpdateDebugVisible()
        {
            foreach (var cell in from column in m_cells from cell in column where cell.DebugRenderer != null select cell)
            {
                cell.DebugRenderer.enabled = m_debugViewEnabled;
            }
        }

        /// <summary>
        /// Creates a cube used for visualising the distance field.
        /// </summary>
        /// <param name="localPosition">The floor space location of the cube</param>
        /// <returns>Transform of the cube.</returns>
        private Transform AddDebugVisualiser(Vector3 localPosition)
        {
            var visualiser = GameObject.CreatePrimitive(PrimitiveType.Cube);

            visualiser.layer = LayerMask.NameToLayer(StringConstants.SCENE_UNDERSTANDING_LAYER);
            var visualiserTransform = visualiser.transform;

            var scaleFreeTransform = new Matrix4x4();
            var localToWorldMatrix = transform.localToWorldMatrix;
            scaleFreeTransform.SetTRS(localToWorldMatrix.GetPosition(), localToWorldMatrix.rotation, Vector3.one);

            visualiserTransform.localScale = Vector3.one * m_cellSize;
            visualiserTransform.position = scaleFreeTransform.MultiplyPoint(localPosition);
            var thisTransform = transform;
            visualiserTransform.localRotation = thisTransform.rotation;

            visualiserTransform.SetParent(m_cellsRootTransform, true);

            return visualiserTransform;
        }

        /// <summary>
        /// Iterates over all the edges that make up the floor shape using <see cref="OVRScenePlane.Boundary"/>
        /// to find the shortest distance to an edge.
        /// </summary>
        /// <param name="point"></param>
        /// <returns>The nearest distance to an edge.</returns>
        private float DistanceToNearestEdge(Vector2 point)
        {
            if (m_floorPlane.Boundary.Count < 2)
            {
                return -1;
            }

            // iterates over the pairs of points tracking the shortest distance.
            var currentDist = float.MaxValue;
            for (var i = 1; i < m_floorPlane.Boundary.Count; i++)
            {
                var dist = Mathf.Sqrt(DistanceToLineSegmentSquared(point, m_floorPlane.Boundary[i - 1], m_floorPlane.Boundary[i]));
                currentDist = Mathf.Min(dist, currentDist);
            }

            // Checks the first and last point that closes the loop
            var lastDist = Mathf.Sqrt(DistanceToLineSegmentSquared(point, m_floorPlane.Boundary[0], m_floorPlane.Boundary[^1]));
            currentDist = Mathf.Min(lastDist, currentDist);

            return currentDist;
        }

        /// <summary>
        /// Takes a 2D plane and calculates the blocked area for it.
        /// </summary>
        /// <param name="objectTransform">Matrix describing the objects position.</param>
        /// <param name="objectSize">Dimensions of the surface.</param>
        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector2 objectSize)
        {
            if (!m_isSetUp)
            {
                m_volumeCache.Add(new ValueTuple<Matrix4x4, Vector3>(objectTransform, objectSize));
                return;
            }

            CalculateBlockedArea(objectTransform, (Vector3)objectSize);
        }

        /// <summary>
        /// Uses the passed in details to workout the areas on the floor that are blocked by the object,
        /// updating the the distance field accordingly.
        /// </summary>
        /// <param name="objectTransform">Transform of the potentially blocking object.</param>
        /// <param name="objectSize">Dimensions of the object.</param>
        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector3 objectSize)
        {
            // if the cells are not yet set up, caches this object until set up is complete.
            if (!m_isSetUp)
            {
                m_volumeCache.Add(new ValueTuple<Matrix4x4, Vector3>(objectTransform, objectSize));
                return;
            }

            // Flags cells that are blocked and caches the index of cells that are now blocked.
            var hitPoints = new List<CellIndex>();
            for (var x = 0; x < m_cells.Count; x++)
            {
                for (var y = 0; y < m_cells[x].Count; y++)
                {
                    var cell = m_cells[x][y];

                    var cellPos = cell.CellDebugRoot.position;
                    var isIn = false;

                    // check the 4 corners of a cell for being blocked.
                    var cellOffset = new Vector3(1, 0, 1) * (m_cellSize * 0.5f);
                    isIn = IsPointWithinColumnSpace(objectTransform, objectSize, cellPos + cellOffset) ||
                           IsPointWithinColumnSpace(objectTransform, objectSize, cellPos - cellOffset);

                    if (!isIn)
                    {
                        cellOffset.x = 1;
                        cellOffset.z = -1;
                        cellOffset *= m_cellSize * 0.5f;
                        isIn = IsPointWithinColumnSpace(objectTransform, objectSize, cellPos + cellOffset) ||
                               IsPointWithinColumnSpace(objectTransform, objectSize, cellPos - cellOffset);
                    }

                    if (isIn)
                    {
                        cell.Blocked = true;
                        cell.DistanceToAnyObject = 0;

                        hitPoints.Add(new CellIndex { X = x, Y = y });
                        m_cells[x][y] = cell;
                    }
                }
            }

            // uses the now blocked cells to re-calculate the distance fields accounting for the newly blocked cells.
            foreach (var hitCellPosition in hitPoints.Select(hit => m_cells[hit.X][hit.Y].LocalPosition))
            {
                foreach (var t in m_cells)
                {
                    for (var y = 0; y < t.Count; y++)
                    {
                        var cell = t[y];
                        if (!cell.Blocked)
                        {
                            var hitDist = Vector2.Distance(cell.LocalPosition, hitCellPosition);
                            if (hitDist < cell.DistanceToAnyObject)
                            {
                                cell.DistanceToAnyObject = hitDist;
                                t[y] = cell;
                            }
                        }
                    }
                }
            }

            RememberDistanceField();
            UpdateCellVisualisations();
        }

        private void UpdateCellVisualisations()
        {
            // updates the debug view with the new distance field values.
            foreach (var column in m_cells)
            {
                foreach (var cell in column)
                {
                    var ren = cell.DebugRenderer;
                    if (ren != null)
                    {
                        ren.material.color = cell.Blocked ? Color.red : Color.green;
#if UNITY_EDITOR
                        cell.CellDebugRoot.gameObject.name = cell.DistanceToAnyObject.ToString(CultureInfo.InvariantCulture);
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a point is within a vertical column. The check is against a vertical column rather than
        /// if inside a cube because the cube is likely above the floor and not extending through it. As a result
        /// all objects on a floor would not strictly block cells.
        /// </summary>
        /// <param name="boxPosition">World transform of the blocking box</param>
        /// <param name="size">Size of the blocking box.</param>
        /// <param name="point">The point to check against.</param>
        /// <returns>True if the point is inside the checked volume.</returns>
        private static bool IsPointWithinColumnSpace(Matrix4x4 boxPosition, Vector3 size, Vector3 point)
        {
            size *= 0.5f; // convert from full width of the box to half extends of the box.
            var boxPos = boxPosition.GetPosition();
            boxPos.y -= size.z;
            // Converts the local space position of the box edges into world space and checks if the test point is inside
            // or outside the box.
            return PlaneSideCheck(boxPos, boxPosition.MultiplyPoint(new Vector3(size.x, 0, -size.z)), point) &&
                   PlaneSideCheck(boxPos, boxPosition.MultiplyPoint(new Vector3(-size.x, 0, -size.z)), point) &&
                   PlaneSideCheck(boxPos, boxPosition.MultiplyPoint(new Vector3(0, size.y, -size.z)), point) &&
                   PlaneSideCheck(boxPos, boxPosition.MultiplyPoint(new Vector3(0, -size.y, -size.z)), point);
        }

        /// <summary>
        /// Checks if the target point is on the same side of the plane as the center of the box or not.
        /// Simply a Dot product on the vectors from the edge to the center and the edge to the point.
        /// </summary>
        /// <param name="center">Center of the cube in world space.</param>
        /// <param name="edgePoint">center of the face of the cube in world space.</param>
        /// <param name="point">The point of interest in world space.</param>
        /// <returns>Returns true of the point is on the center side of the cube face.</returns>
        private static bool PlaneSideCheck(Vector3 center, Vector3 edgePoint, Vector3 point)
        {
            return Vector3.Dot((edgePoint - center).normalized, edgePoint - point) >= 0;
        }

        private void CalculateBlockedAreaFromRadius(Vector2 localPosition, float localRadius)
        {
            var hitPoints = new List<CellIndex>();
            for (var x = 0; x < m_cells.Count; x++)
            {
                for (var y = 0; y < m_cells[x].Count; y++)
                {
                    var cell = m_cells[x][y];
                    var isIn = Vector2.Distance(localPosition, cell.LocalPosition) <= localRadius;

                    if (isIn)
                    {
                        cell.Blocked = true;
                        cell.DistanceToAnyObject = 0;

                        hitPoints.Add(new CellIndex { X = x, Y = y });
                        m_cells[x][y] = cell;
                    }
                }
            }

            UpdateDistanceField(hitPoints);
        }

        private void UpdateDistanceField(List<CellIndex> hitPoints)
        {
            // Iterates over the found covered and recalculates the distances to include the newly blocked cells.
            foreach (var hitCellPosition in hitPoints.Select(cellIndex => m_cells[cellIndex.X][cellIndex.Y].LocalPosition))
            {
                foreach (var t in m_cells)
                {
                    for (var y = 0; y < t.Count; y++)
                    {
                        var cell = t[y];
                        if (!cell.Blocked)
                        {
                            var hitDist = Vector2.Distance(cell.LocalPosition, hitCellPosition);
                            if (hitDist < cell.DistanceToAnyObject)
                            {
                                cell.DistanceToAnyObject = hitDist;
                                t[y] = cell;
                            }
                        }
                    }
                }
            }

            UpdateCellVisualisations();
        }

        public void RequestTrulyRandomLocation(out Vector3 position)
        {
            if (m_cells.Count > 0)
            {
                var randomRow = Random.Range(0, m_cells.Count);
                var randomColumn = Random.Range(0, m_cells[randomRow].Count);
                var cell = m_cells[randomRow][randomColumn];
                position = cell.CellDebugRoot.position;
            }
            else
            {
                position = Vector3.zero;
            }
        }
    }
}