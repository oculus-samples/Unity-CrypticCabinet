// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CrypticCabinet.Utils;
using Meta.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Used to detect empty spaces on a wall for the object placement queries.
    /// </summary>
    public class WallSpaceFinder
    {
        /// <summary>
        ///     Wall data structure.
        /// </summary>
        private struct WallCells
        {
            /// <summary>
            ///     Center of the wall.
            /// </summary>
            public Transform WallRoot;

            /// <summary>
            ///     Length of the wall. 
            /// </summary>
            public float WallLength;

            public float CellSize;

            /// <summary>
            ///     The columns of the wall.
            /// </summary>
            public List<List<Cell>> WallColumnCells;
        }

        /// <summary>
        ///     Cell data of the the wall. 
        /// </summary>
        private struct Cell
        {
            /// <summary>
            /// Position of the cell.
            /// </summary>
            public Transform Transform;
            /// <summary>
            /// Collider of the cell.
            /// </summary>
            public BoxCollider Collider;
            /// <summary>
            /// Flag to indicate if the cell is blocked by anything.
            /// </summary>
            public bool IsBlockedByScene;
            /// <summary>
            /// Flag to indicate if the cell is blocked by placed objects.
            /// </summary>
            public bool IsBlockedByPlacedObject;
        }

        /// <summary>
        ///     The target size of the cells. This adjusted to ensure the cells fit the wall perfectly. 
        /// </summary>
        public float TargetCellSize = 0.5f;

        /// <summary>
        ///     All walls in the system. 
        /// </summary>
        private readonly List<WallCells> m_testableWalls = new();

        /// <summary>
        ///     Flag for if the debug should be enabled.
        /// </summary>
        private bool m_cellVisibleDebug;

        /// <summary>
        ///     The material used for the debug view.
        /// </summary>
        private readonly Material m_debugMaterial;

        /// <summary>
        ///     Passing the material allows the use of the debug view of the cells.
        /// </summary>
        /// <param name="debugMaterial">Material used for debug cell view, passing null disables the debug view.</param>
        public WallSpaceFinder(Material debugMaterial = null) => m_debugMaterial = debugMaterial;

        /// <summary>
        ///     Array of colliders used for the non allocating physics detections.
        /// </summary>
        private readonly Collider[] m_hitColliders = new Collider[5000];

        /// <summary>
        ///     Cleans up the generated cells, call this when the space finder is no longer needed.
        /// </summary>
        public void CleanUp()
        {
            foreach (var wall in m_testableWalls)
            {
                UnityEngine.Object.Destroy(wall.WallRoot.gameObject);
            }

            m_testableWalls.Clear();
        }

        /// <summary>
        ///     Toggles the debug cells if the debug material was provided.
        /// </summary>
        public void ToggleCellVisibility()
        {
            UpdateDebugVisuals();
            m_cellVisibleDebug = !m_cellVisibleDebug;
            UpdateWallVisibility();
        }

        /// <summary>
        ///     Sets the debug cell visibility if the debug material was provided.
        /// </summary>
        /// <param name="visible">True to enable the cells.</param>
        public void SetDebugVisible(bool visible)
        {
            m_cellVisibleDebug = visible;
            UpdateWallVisibility();
        }

        /// <summary>
        ///     Finds the debug renderers and sets the enabled state of the renderer.
        /// </summary>
        private void UpdateWallVisibility()
        {
            foreach (var renderer in m_testableWalls.SelectMany(wall => wall.WallRoot.GetComponentsInChildren<Renderer>()))
            {
                renderer.enabled = m_cellVisibleDebug;
            }
        }

        /// <summary>
        ///     Uses the position matrix and the bounds of a wall to generate the cells used to find safe locations
        ///     on wall surfaces. This assumes the location provided is in the middle of the wall.
        ///     Note: All walls should be added before trying to add any other scene objects or request a wall location.
        /// </summary>
        /// <param name="locationMatrix">The matrix describing the position and rotation of the wall.</param>
        /// <param name="bounds">The size of the wall.</param>
        public void AddWall(Matrix4x4 locationMatrix, Vector2 bounds)
        {
            // Create a new wall root object.
            var newWallRoot = new GameObject("Wall");
            newWallRoot.transform.SetPositionAndRotation(locationMatrix.GetPosition(), locationMatrix.rotation);

            // Create the new wall data structure. 
            var newWall = new WallCells
            {
                WallRoot = newWallRoot.transform,
                WallLength = bounds.x,
                WallColumnCells = new List<List<Cell>>()
            };

            // Calculate how many cells will make up the length of the wall by dividing the length by the the target 
            // cell size, then round down. Dividing the length by the cell count will ensure the cells will fit the wall perfectly. 
            // This method results in cells that are evenly over sized to fit the wall.
            var xCellCount = Mathf.FloorToInt(bounds.x / TargetCellSize);
            var xCellSize = bounds.x / xCellCount;

            // Repeat fot the height of the walls.
            var yCellCount = Mathf.FloorToInt(bounds.y / TargetCellSize);
            var yCellSize = bounds.y / yCellCount;

            var extendsX = bounds.x / 2;
            var extendsY = bounds.y / 2;

            newWall.CellSize = xCellSize;

            m_testableWalls.Add(newWall);

            // Iterate over the wall creating the cells 
            for (var xIndex = 0; xIndex < xCellCount; xIndex++)
            {
                // Calculate the x position, starting at the far left, moving across the width of a cell
                // Also pad the width of half a cell to align neatly. 
                var x = -extendsX + xCellSize * 0.5f + xCellSize * xIndex;
                var verticalWallCells = new List<Cell>();

                for (var yIndex = 0; yIndex < yCellCount; yIndex++)
                {
                    // Similar position calculation for vertical. 
                    var y = -extendsY + yCellSize * 0.5f + yCellSize * yIndex;
                    // Create a new GameObject for the cell and set the parent to be the wall. 
                    var newCell = new GameObject($@"{x.ToString(CultureInfo.InvariantCulture)}, {y.ToString(CultureInfo.InvariantCulture)}");
                    newCell.transform.SetParent(newWallRoot.transform);
                    // Set the local position using the calculated variables 
                    newCell.transform.SetLocalPositionAndRotation(new Vector3(x, y, 0), Quaternion.identity);
                    // Add box collider and set the size to be the calculated cell dimensions. 
                    var boxCollider = newCell.AddComponent<BoxCollider>();
                    boxCollider.size = new Vector3(xCellSize, yCellSize, 0.6f);

                    // Add this new cell to the array of cells for this column.
                    verticalWallCells.Add(new Cell
                    {
                        Transform = newCell.transform,
                        Collider = boxCollider,
                        IsBlockedByScene = false,
                        IsBlockedByPlacedObject = false
                    });

                    // Add a cube for debugging purposes if the debugging material is available.
                    if (!m_debugMaterial)
                    {
                        continue;
                    }

                    var cubeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubeVisual.layer = LayerMask.NameToLayer(StringConstants.SCENE_UNDERSTANDING_LAYER);
                    cubeVisual.transform.SetParent(newCell.transform);
                    cubeVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    cubeVisual.transform.localScale = boxCollider.size * 0.9f;
                    var renderer = cubeVisual.GetComponent<Renderer>();
                    renderer.material = m_debugMaterial;
                    renderer.material.color = new Color(0, 1, 0, 0.3f);
                }

                // Add the full colum of cells to the wall before moving across to the next column of cells.
                newWall.WallColumnCells.Add(verticalWallCells);
            }

            newWallRoot.SetLayerToChilds(LayerMask.NameToLayer(StringConstants.SCENE_UNDERSTANDING_LAYER));
        }

        /// <summary>
        ///     Calculates what cells a 2D object blocks and flags affected cells.
        /// </summary>
        /// <param name="objectLocationMatrix">The matrix describing the position and rotation of the object being added.</param>
        /// <param name="objectDimensions">The size of the object.</param>
        public void AddPlaneObject(Matrix4x4 objectLocationMatrix, Vector2 objectDimensions)
        {
            // Pads the size into a 3D object and calls AddVolumeObject.
            Vector3 dimensions3D = objectDimensions;
            dimensions3D.z = 0.2f;
            AddVolumeObject(objectLocationMatrix, dimensions3D);
        }

        /// <summary>
        ///     Searches the cell representation of the room for the given object and flags the blocked cells.
        /// </summary>
        /// <param name="objectLocationMatrix">The matrix describing the position and rotation of the object being added.</param>
        /// <param name="objectDimensions">The size of the object.</param>
        public void AddVolumeObject(Matrix4x4 objectLocationMatrix, Vector3 objectDimensions)
        {
            // Finds all cell colliders that overlap the object given the position and dimensions
            var adjustedPosition = objectLocationMatrix.GetPosition();
            adjustedPosition -= Vector3.up * (objectDimensions.z * 0.5f);
            var hitCount = Physics.OverlapBoxNonAlloc(adjustedPosition, objectDimensions * 0.5f,
                m_hitColliders, objectLocationMatrix.rotation);

            // Iterate over found collider.
            for (var i = 0; i < hitCount; ++i)
            {
                // Finds the index of the cell containing the collider.
                var (wallIndex, columnIndex, cellIndex) = GetCellForCollider(m_hitColliders[i]);
                if (wallIndex < 0)
                {
                    continue;
                }

                // Update the referenced cell as being blocked and updates the debug renderer colour if needed. 
                var cell = m_testableWalls[wallIndex].WallColumnCells[columnIndex][cellIndex];
                cell.IsBlockedByScene = true;
                if (m_debugMaterial)
                {
                    var renderer = cell.Transform.gameObject.GetComponentInChildren<Renderer>();
                    renderer.material.color = new Color(1, 0, 0, 0.3f);
                }

                m_testableWalls[wallIndex].WallColumnCells[columnIndex][cellIndex] = cell;
            }
        }

        /// <summary>
        ///     Finds the indexes of the cell for a given collider.
        /// </summary>
        /// <param name="collider">The collider that should be in a cell.</param>
        /// <returns>Returns the array index for the wall, column and cell.</returns>
        private (int wallIndex, int columnIndex, int cellIndex) GetCellForCollider(Collider collider)
        {
            for (var wallIndex = 0; wallIndex < m_testableWalls.Count; wallIndex++)
            {
                var wall = m_testableWalls[wallIndex];
                for (var columnIndex = 0; columnIndex < wall.WallColumnCells.Count; columnIndex++)
                {
                    var wallColumn = wall.WallColumnCells[columnIndex];
                    for (var cellIndex = 0; cellIndex < wallColumn.Count; cellIndex++)
                    {
                        if (wallColumn[cellIndex].Collider.GetInstanceID() == collider.GetInstanceID())
                        {
                            return (wallIndex, columnIndex, cellIndex);
                        }
                    }
                }
            }

            return (-1, -1, -1);
        }

        /// <summary>
        ///     Finds a random cell roughly at the requested height. Returned cell is likely not actually at the height request.
        /// </summary>
        /// <param name="heightOffFloor">The height to search for.</param>
        /// <param name="objectHeight">The height of the object for the requested cell.</param>
        /// <param name="objectWidth">The width of the object for the requested cell.</param>
        /// <param name="position">The position of the resulting cell.</param>
        /// <param name="rotation">The rotation of the resulting cell.</param>
        /// <param name="ignoreSceneBlocked">Flag to indicate if this should allow being placed over scene blocked cells.</param>
        /// <param name="markAsBlocked">When true, the returned cell is marked as blocked, otherwise it is unchanged.</param>
        /// <returns>Transform of the found cell.</returns>
        public bool QueryForSafeWallLocation(float heightOffFloor, float objectHeight, float objectWidth,
            out Vector3 position, out Quaternion rotation, bool ignoreSceneBlocked, bool markAsBlocked = true)
        {
            return InternalQueryForSafeWallLocation(heightOffFloor, objectHeight, objectWidth, 0.1f,
                out position, out rotation, ignoreSceneBlocked, markAsBlocked);
        }

        private bool InternalQueryForSafeWallLocation(float heightOffFloor, float objectHeight,
            float objectWidth, float objectDepth,
            out Vector3 position, out Quaternion rotation, bool ignoreSceneBlocked, bool markAsBlocked = true,
            Func<Collider[], int, bool> checkPhysicsResultIsClear = null,
            Action<Collider[], int> blockPhysicsResult = null)
        {
            var testExtends = new Vector3(objectWidth * 0.5f, objectHeight * 0.5f, objectDepth * 1.1f);

            var testPos = new Vector3(0, heightOffFloor, 0);

            var startOffset = Random.Range(0, m_testableWalls.Count);

            for (var i = 0; i < m_testableWalls.Count; i++)
            {
                var wallIndex = startOffset + i;
                if (wallIndex >= m_testableWalls.Count)
                {
                    wallIndex -= m_testableWalls.Count;
                }

                var halfTestLength = (m_testableWalls[wallIndex].WallLength - objectWidth) * 0.5f;
                var cellSize = m_testableWalls[wallIndex].CellSize;

                for (var xPos = -halfTestLength; xPos < halfTestLength; xPos += cellSize)
                {
                    testPos.x = xPos;

                    var worldSpacePoint = m_testableWalls[wallIndex].WallRoot.localToWorldMatrix.MultiplyPoint(testPos);
                    worldSpacePoint.y = heightOffFloor;

                    var hitBoxes = Physics.OverlapBoxNonAlloc(
                        worldSpacePoint, testExtends, m_hitColliders, m_testableWalls[wallIndex].WallRoot.rotation);

                    var floorClear = true;
                    if (checkPhysicsResultIsClear != null)
                    {
                        floorClear = checkPhysicsResultIsClear(m_hitColliders, hitBoxes);
                    }

                    if (CheckColliderBlocked(m_hitColliders, hitBoxes, wallIndex, ignoreSceneBlocked) && floorClear)
                    {
                        position = worldSpacePoint;
                        rotation = m_testableWalls[wallIndex].WallRoot.rotation;

                        if (markAsBlocked)
                        {
                            MarkHitCellsAsBlocked(m_hitColliders, hitBoxes, wallIndex);
                            blockPhysicsResult?.Invoke(m_hitColliders, hitBoxes);
                        }

                        return true;
                    }
                }
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        public bool RequestRandomLocationCustomWithFloor(float halfDepth, float width, float height,
            out Vector3 position, out Quaternion rotation,
            Func<Collider[], int, bool> checkPhysicsResultIsClear, Action<Collider[], int> blockPhysicsResult, bool markAsBlocked)
        {
            return InternalQueryForSafeWallLocation(
                height * 0.48f, height, width, halfDepth, out position, out rotation, false,
                markAsBlocked, checkPhysicsResultIsClear, blockPhysicsResult);
        }

        /// <summary>
        ///     Checks if any of the colliders passed in are part of a particular wall and if they are blocked or not.
        /// </summary>
        /// <param name="hitColliders">Array of colliders that overlap the select area.</param>
        /// <param name="hitCount">Number of hit colliders.</param>
        /// <param name="wallIndex">The index of the wall to check against.</param>
        /// <param name="ignoreSceneBlocked">Override for if the wall check should ignore scene the scene blocked state.</param>
        /// <returns>Returns true there's no blocking cells.</returns>
        private bool CheckColliderBlocked(Collider[] hitColliders, int hitCount, int wallIndex, bool ignoreSceneBlocked)
        {
            for (var j = 0; j < hitCount; j++)
            {
                foreach (var column in m_testableWalls[wallIndex].WallColumnCells)
                {
                    for (var y = 0; y < column.Count; y++)
                    {
                        if (column[y].Collider != hitColliders[j])
                        {
                            continue;
                        }

                        if (ignoreSceneBlocked ? column[y].IsBlockedByPlacedObject :
                               (column[y].IsBlockedByScene || column[y].IsBlockedByPlacedObject))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void MarkHitCellsAsBlocked(Collider[] hitColliders, int hitCount, int wallIndex)
        {
            for (var j = 0; j < hitCount; j++)
            {
                foreach (var column in m_testableWalls[wallIndex].WallColumnCells)
                {
                    for (var y = 0; y < column.Count; y++)
                    {
                        if (column[y].Collider != hitColliders[j])
                        {
                            continue;
                        }

                        var cell = column[y];
                        cell.IsBlockedByPlacedObject = true;
                        column[y] = cell;
                    }
                }
            }
        }

        public void PickRandomWallCell(float minHeight, out Vector3 position, out Quaternion rotation)
        {
            if (m_testableWalls is { Count: > 0 })
            {
                var wallIndex = Random.Range(0, m_testableWalls.Count);
                var wall = m_testableWalls[wallIndex];
                var columnIndex = Random.Range(0, wall.WallColumnCells.Count);
                var cells = wall.WallColumnCells[columnIndex].Where(cell => cell.Transform.position.y > minHeight).
                    ToList();
                if (cells.Any())
                {
                    var cell = cells[Random.Range(0, cells.Count)];
                    if (cell.Transform != null)
                    {
                        position = cell.Transform.position;
                        rotation = cell.Transform.rotation;
                        return;
                    }
                }
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        private void UpdateDebugVisuals()
        {
            var blockedColour = new Color(1, 0, 0);
            var freeColour = new Color(0, 1, 0);
            foreach (var wall in m_testableWalls)
            {
                foreach (var column in wall.WallColumnCells)
                {
                    foreach (var cell in column)
                    {
                        if (m_debugMaterial)
                        {
                            var renderer = cell.Transform.gameObject.GetComponentInChildren<Renderer>();
                            renderer.material.color = (cell.IsBlockedByScene || cell.IsBlockedByPlacedObject) ? blockedColour : freeColour;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Sets the active state of the colliders used for finding safe wall locations.
        /// </summary>
        /// <param name="wallsEnabled"></param>
        public void SetWallCollidersEnabled(bool wallsEnabled)
        {
            foreach (var wall in m_testableWalls)
            {
                wall.WallRoot.gameObject.SetActive(wallsEnabled);
            }
        }

        public void ResetWallPlacements()
        {
            for (var wallIndex = 0; wallIndex < m_testableWalls.Count; wallIndex++)
            {
                var testableWall = m_testableWalls[wallIndex];
                foreach (var t in testableWall.WallColumnCells)
                {
                    for (var row = 0; row < t.Count; row++)
                    {
                        var cell = t[row];
                        cell.IsBlockedByPlacedObject = false;
                        t[row] = cell;
                    }
                }
                m_testableWalls[wallIndex] = testableWall;
            }
        }
    }
}