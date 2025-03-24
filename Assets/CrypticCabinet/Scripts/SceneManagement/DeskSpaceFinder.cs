// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CrypticCabinet.Utils;
using Meta.XR.Samples;
using UnityEngine;
using static CrypticCabinet.Utils.MathsUtils;
using Random = UnityEngine.Random;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Generates a distance field on plane accounting for blocking objects.
    ///     Includes functionality to return a position on the plane for a given radius.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class DeskSpaceFinder : MonoBehaviour, ISpaceFinder
    {
        /// <summary>
        /// The width and depth of the desk.
        /// </summary>
        public Vector2 DeskSize;

        /// <summary>
        /// Target size of the cells.
        /// </summary>
        [SerializeField] private float m_spacing = 0.05f;

        /// <summary>
        /// Two dimensional list of cells.  
        /// </summary>
        private List<List<Cell>> m_cells = new();

        /// <summary>
        /// The farthest distance a cell is from an edge, this is used for visualization. 
        /// </summary>
        private float m_maxDist;

        private bool m_debugViewEnabled;

        private bool m_isSetUp;

        /// <summary>
        /// The root transform that has all child cells game objects
        /// </summary>
        private Transform m_cellsRootTransform;


        /// <summary>
        /// Struct describing the cell of the distance field.
        /// </summary>
        private struct Cell
        {
            /// <summary>
            /// Transform of the debug visualization cube
            /// </summary>
            public Transform CellDebugRoot;

            public Renderer DebugRenderer;

            /// <summary>
            /// The distance to either a blocked cell or edge of the table
            /// </summary>
            public float DistToBlockedArea;

            private float m_originalDistanceToBlockedArea;

            /// <summary>
            /// The position in desk space
            /// </summary>
            public Vector2 LocalPosition;

            /// <summary>
            /// Flag to indicate if the cell is blocked entirely.
            /// </summary>
            public bool Blocked;

            private bool m_originallyBlocked;

            public void Reset()
            {
                DistToBlockedArea = m_originalDistanceToBlockedArea;
                Blocked = m_originallyBlocked;
            }

            public void RememberDistance()
            {
                m_originalDistanceToBlockedArea = DistToBlockedArea;
                m_originallyBlocked = Blocked;
            }
        }

        public bool HasSetUpCompleted() => m_isSetUp;
        public void CleanUp()
        {
            foreach (var column in m_cells)
            {
                foreach (var cell in column)
                {
                    Destroy(cell.CellDebugRoot.gameObject);
                }
            }

            if (m_cellsRootTransform != null)
            {
                Destroy(m_cellsRootTransform.gameObject);
            }
        }

        public void GetFinderTransform(out Matrix4x4 localToWorldMatrix) => localToWorldMatrix = transform.localToWorldMatrix;

        public void GetFinderSize(out Vector3 worldSize) => worldSize = DeskSize;

        public bool RequestRandomLocation(Vector3 locationToFace, float objectRadius, out Vector3 foundPosition, out Quaternion foundRotation, bool markAsBlocked = false, float edgeDistance = -1)
        {
            foundRotation = Quaternion.identity;
            return RequestCenterLocation(objectRadius, out foundPosition, markAsBlocked);
        }

        public bool RequestRandomLocation(Vector3 locationToFace, Vector3 objectDimensions, out Vector3 foundPosition,
            out Quaternion foundRotation, bool markAsBlocked = false)
        {
            Debug.LogWarning("DeskSpaceFinder does not implement this version of RequestRandomLocation.");

            foundRotation = Quaternion.identity;
            foundPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Returns a location on the desk for a given radius, if there's no location that
        /// can safely place the object returns false.
        /// </summary>
        /// <param name="radius">Radius of the object to place.</param>
        /// <param name="foundPosition">The found position in world space.</param>
        /// <param name="markAsBlocked">Updates the distance field to be blocked once placed.</param>
        /// <returns>True if the returns position is valid, false if not.</returns>
        public bool RequestCenterLocation(float radius, out Vector3 foundPosition, bool markAsBlocked = false, float edgeDistance = -1)
        {
            var foundX = -1;
            var foundY = -1;
            var currentDistance = float.MinValue;

            for (var x = 0; x < m_cells.Count; x++)
            {
                var column = m_cells[x];
                for (var y = 0; y < column.Count; y++)
                {
                    var cell = column[y];
                    if (cell.DistToBlockedArea >= radius && cell.DistToBlockedArea > currentDistance)
                    {
                        currentDistance = cell.DistToBlockedArea;
                        foundX = x;
                        foundY = y;
                    }
                }
            }

            if (foundX >= 0 && foundY >= 0)
            {
                var cell = m_cells[foundX][foundY];
                foundPosition = transform.localToWorldMatrix.MultiplyPoint(cell.LocalPosition);

                if (markAsBlocked)
                {
                    CalculateBlockedAreaFromRadius(cell.LocalPosition, radius);
                }

                return true;
            }

            foundPosition = Vector3.zero;
            return false;
        }


        public void RememberDistanceField()
        {
            foreach (var column in m_cells)
            {
                for (var y = 0; y < column.Count; y++)
                {
                    var cell = column[y];
                    cell.RememberDistance();
                    column[y] = cell;
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


        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector2 objectSize) => CalculateBlockedArea(objectTransform, new Vector3(objectSize.x, objectSize.y));

        /// <summary>
        /// Recalculates the distance field accounting for a new Scene object.
        /// First checks if the new object blocks any cells then recalculates the cell distances.
        /// </summary>
        /// <param name="objectTransform">The transform matrix describing the location of the object.</param>
        /// <param name="objectSize">The scale of the object.</param>
        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector3 objectSize)
        {
            // Iterates over the cells in the grid checking if cell is inside the object.
            // Flags the blocked cells then stores the indexes of covered cells indexes in hitPoints.
            var hitPoints = new List<Tuple<int, int>>();
            for (var x = 0; x < m_cells.Count; x++)
            {
                for (var y = 0; y < m_cells[x].Count; y++)
                {
                    var cell = m_cells[x][y];

                    var isIn = IsPointWithinColumnSpace(objectTransform, objectSize, cell.CellDebugRoot.position);
                    if (!isIn)
                    {
                        continue;
                    }

                    cell.Blocked = true;
                    cell.DistToBlockedArea = 0;

                    hitPoints.Add(new Tuple<int, int>(x, y));
                    m_cells[x][y] = cell;
                }
            }

            RememberDistanceField();
            UpdateDistanceField(hitPoints, true);
        }


        private void CalculateBlockedAreaFromRadius(Vector2 localPosition, float localRadius)
        {
            var hitPoints = new List<Tuple<int, int>>();
            for (var x = 0; x < m_cells.Count; x++)
            {
                for (var y = 0; y < m_cells[x].Count; y++)
                {
                    var cell = m_cells[x][y];
                    var isIn = Vector2.Distance(localPosition, cell.LocalPosition) <= localRadius;

                    if (!isIn)
                    {
                        continue;
                    }

                    cell.Blocked = true;
                    cell.DistToBlockedArea = 0;

                    hitPoints.Add(new Tuple<int, int>(x, y));
                    m_cells[x][y] = cell;
                }
            }

            UpdateDistanceField(hitPoints, false);
        }


        private void UpdateDistanceField(IReadOnlyList<Tuple<int, int>> hitPoints, bool rememberDistanceField)
        {
            // Iterates over the found covered and recalculates the distances to include the newly blocked cells.
            for (var i = 0; i < hitPoints.Count; i++)
            {
                var (hitX, hitY) = hitPoints[i];
                var hitCellPosition = m_cells[hitX][hitY].LocalPosition;
                for (var x = 0; x < m_cells.Count; x++)
                {
                    for (var y = 0; y < m_cells[x].Count; y++)
                    {
                        var cell = m_cells[x][y];
                        if (!cell.Blocked)
                        {
                            var hitDist = Vector2.Distance(cell.LocalPosition, hitCellPosition);
                            if (hitDist < cell.DistToBlockedArea)
                            {
                                cell.DistToBlockedArea = hitDist;
                                m_cells[x][y] = cell;
                            }
                        }
                    }
                }
            }

            if (rememberDistanceField)
            {
                RememberDistanceField();
            }

            UpdateCellVisualisations();
        }

        /// <summary>
        /// Generates the cells for the desk surface.
        /// </summary>
        /// <param name="debugMaterial"></param>
        public void GenerateDeskCells(Material debugMaterial)
        {
            if (m_cellsRootTransform == null)
            {
                m_cellsRootTransform = new GameObject("DeskCells").transform;
                m_cellsRootTransform.SetParent(transform, false);
            }
            // Calculate how many cells will make up the length of the desk by dividing the length by the the target 
            // cell size, then round down. Dividing the length by the cell count will ensure the cells will fit the desk perfectly. 
            // This method results in cells that are evenly over sized to fit the desk.
            var xCellCount = Mathf.FloorToInt(DeskSize.x / m_spacing);
            var xCellSize = DeskSize.x / xCellCount;

            // Repeat fot the depth of the desk.
            var yCellCount = Mathf.FloorToInt(DeskSize.y / m_spacing);
            var yCellSize = DeskSize.y / yCellCount;

            var startX = -(DeskSize.x / 2) + xCellSize / 2;
            var startY = -(DeskSize.y / 2) + yCellSize / 2;
            m_maxDist = float.MinValue;

            m_cells = new List<List<Cell>>(xCellCount);

            // Creates the cells and calculates the distance to the edges of the desk.
            for (var x = 0; x < xCellCount; x++)
            {
                m_cells.Add(new(yCellCount));
                for (var y = 0; y < yCellCount; y++)
                {
                    var position = new Vector2(startX + x * xCellSize, startY + yCellSize * y);
                    var debugVisualiser = AddDebugVisualiser(position);
                    var debugRenderer = debugVisualiser.GetComponent<Renderer>();
                    debugRenderer.material = debugMaterial;
                    debugRenderer.material.color = Color.green;
                    debugRenderer.enabled = false;
                    var distance = DistanceToNearestEdge(position);
                    m_maxDist = Mathf.Max(m_maxDist, distance);
                    m_cells[x].Add(new Cell
                    {
                        CellDebugRoot = debugVisualiser,
                        Blocked = false,
                        LocalPosition = position,
                        DistToBlockedArea = distance,
                        DebugRenderer = debugRenderer
                    });

#if UNITY_EDITOR
                    debugVisualiser.gameObject.name = distance.ToString(CultureInfo.InvariantCulture);
#endif
                }
            }

            m_debugViewEnabled = false;
            RememberDistanceField();
            m_isSetUp = true;
        }

        /// <summary>
        /// Creates a debug cube at the location provided and sets it as a child to this GameObject.
        /// </summary>
        /// <param name="localPosition">The desk space location to be transformed into world space.</param>
        /// <returns>The transform of the created cube.</returns>
        private Transform AddDebugVisualiser(Vector3 localPosition)
        {
            // Create a cube primitive.
            var visualiser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualiser.layer = LayerMask.NameToLayer(StringConstants.SCENE_UNDERSTANDING_LAYER);
            var visualiserTransform = visualiser.transform;

            var thisTransform = transform;

            // set the scale to be the size of the cells
            visualiserTransform.localScale = Vector3.one * m_spacing;
            // Set the position of the cube in world space correctly placed relative to this game object.
            visualiserTransform.position = transform.TransformPoint(localPosition);
            // set rotation to match this object.
            visualiserTransform.localRotation = thisTransform.rotation;

            visualiserTransform.SetParent(m_cellsRootTransform, true);

            return visualiserTransform;
        }

        /// <summary>
        /// Checks if a point is within a vertical column. The check is against a vertical column rather than
        /// if inside a cube because the cube is likely above the desk and not extending through it. As a result
        /// all objects on a desk would not strictly block cells.
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

        /// <summary>
        /// Tests all 4 edges of the desk to find the nearest edge to a point in local space of the table.
        /// </summary>
        /// <param name="point">Point on the table in table space.</param>
        /// <returns>The shortest distance to an edge in local space.</returns>
        private float DistanceToNearestEdge(Vector2 point)
        {
            var deskExtends = DeskSize * 0.5f;
            var a = Mathf.Sqrt(DistanceToLineSegmentSquared(point, new Vector2(-deskExtends.x, deskExtends.y), new Vector2(deskExtends.x, deskExtends.y)));
            var b = Mathf.Sqrt(DistanceToLineSegmentSquared(point, new Vector2(deskExtends.x, deskExtends.y), new Vector2(deskExtends.x, -deskExtends.y)));
            var c = Mathf.Sqrt(DistanceToLineSegmentSquared(point, new Vector2(deskExtends.x, -deskExtends.y), new Vector2(-deskExtends.x, -deskExtends.y)));
            var d = Mathf.Sqrt(DistanceToLineSegmentSquared(point, new Vector2(-deskExtends.x, -deskExtends.y), new Vector2(-deskExtends.x, deskExtends.y)));

            return Mathf.Min(a, b, c, d);
        }



        /// <summary>
        /// Updates the colours of the debug cubes and sets the distance text in the hierarchy.
        /// Colours are faded from red to green based on the maxDist variable and each cells distToBlockedArea variable.
        /// </summary>
        private void UpdateCellVisualisations()
        {
            foreach (var column in m_cells)
            {
                foreach (var cell in column)
                {
                    var ren = cell.DebugRenderer;
                    if (ren != null)
                    {
                        ren.material.color = cell.Blocked ? Color.red : Color.green;
                    }
#if UNITY_EDITOR
                    cell.CellDebugRoot.gameObject.name = cell.DistToBlockedArea.ToString(CultureInfo.InvariantCulture);
#endif
                }
            }
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