// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Utilities;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Central manager for all surface finding, takes in the objects created by the <see cref="OVRSceneManager"/>
    ///     and passed the relevant information to the space finding mangers.
    ///     Once set up also handles moving the debug visual props to the appropriate locations.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SceneUnderstandingLocationPlacer : Singleton<SceneUnderstandingLocationPlacer>
    {
        /// List of wall, openings and objects created by the scene manger.
        private readonly List<MRUKAnchor> m_openings = new();
        private readonly List<MRUKAnchor> m_walls = new();
        private readonly List<MRUKAnchor> m_objects = new();
        private readonly List<MRUKAnchor> m_blockingPlanes = new();
        private readonly List<DeskSpaceFinder> m_deskSpaceFinders = new();
        private readonly List<FloorSpaceFinder> m_floorSpaceFinders = new();
        // horizontal includes desks and floor
        private readonly List<ISpaceFinder> m_horizontalSurfaces = new();

        /// <summary>
        /// Debug material passed to <see cref="WallSpaceFinder"/>
        /// </summary>
        public Material DebugMaterial;

        public Action OnSceneLoadingFailed;

        /// <summary>
        /// System that calculates the safe locations of objects 
        /// </summary>
        private WallSpaceFinder m_wallSpaceFinder;

        /// <summary>
        /// Flag to indicate of the processing of the scene loading is completed.
        /// </summary>
        private bool m_sceneLoadingComplete;

        [SerializeField] private float m_targetWallCellSize = 0.15f;

        private bool m_sceneManagerAlreadyLoaded;

        private bool m_initialized = false;

        private void Start()
        {
            Init();
        }

        /// <summary>
        /// Set up the MRUK and load scene data.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            Init();
        }

        /// <summary>
        /// Un-registers the callbacks from the scene manager.
        /// </summary>
        private void OnDisable()
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(SceneModelLoadedSuccessfully);
            m_sceneLoadingComplete = false;
            m_initialized = false;
        }

        private void Init()
        {
            if (m_initialized || MRUK.Instance == null)
            {
                return;
            }

            MRUK.Instance.SceneLoadedEvent.AddListener(SceneModelLoadedSuccessfully);
            m_sceneLoadingComplete = false;
            m_initialized = true;
        }

        /// <summary>
        /// Clears the lists of previously found and registered scene objects. 
        /// </summary>
        private void ClearRoomObjects()
        {
            m_walls.Clear();
            m_openings.Clear();
            m_objects.Clear();
            m_blockingPlanes.Clear();
            m_horizontalSurfaces.Clear();
            m_floorSpaceFinders.Clear();
            m_deskSpaceFinders.Clear();
        }

        public void ResetHorizontalBlockedAreas()
        {
            foreach (var horizontalSurface in m_horizontalSurfaces)
            {
                horizontalSurface.ResetDistanceField();
            }
        }

        /// <summary>
        /// Callback for if the scene loads correctly.
        /// Triggers the loading of scene objects into <see cref="m_wallSpaceFinder"/>
        /// </summary>
        private void SceneModelLoadedSuccessfully()
        {
            _ = StartCoroutine(LoadSpaceFindingSystem());
        }

        /// <summary>
        /// Finds all SceneRoomObjects and loads them into the space finding calculator.
        /// </summary>
        private IEnumerator LoadSpaceFindingSystem()
        {
            //need to yield for 1 frame after SceneLoaded callback so anchors have time to populate
            yield return null;
            m_sceneLoadingComplete = false;

            m_wallSpaceFinder?.CleanUp();
            m_wallSpaceFinder = new WallSpaceFinder(DebugMaterial) { TargetCellSize = m_targetWallCellSize };

            var sceneRoomObjects = FindObjectsByType<SceneRoomObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var roomObject in sceneRoomObjects)
            {
                roomObject.SetUpObject(this);
            }

            // Generate horizontal
            foreach (var deskSpaceFinder in m_deskSpaceFinders)
            {
                deskSpaceFinder.GenerateDeskCells(DebugMaterial);
                yield return null;
            }

            // Do floors next since they need time to generate the mesh
            foreach (var floorSpaceFinder in m_floorSpaceFinders)
            {
                while (!floorSpaceFinder.IsReadyToGenerate)
                {
                    yield return null;
                }
                floorSpaceFinder.GenerateCells();
                yield return null;
            }

            foreach (var wall in m_walls)
            {
                if (wall.PlaneRect.HasValue)
                {
                    m_wallSpaceFinder.AddWall(wall.transform, wall.PlaneRect.Value.size);
                }

                yield return null;
            }

            foreach (var opening in m_openings)
            {
                if (opening.PlaneRect.HasValue)
                {
                    m_wallSpaceFinder.AddPlaneObject(opening.transform.localToWorldMatrix, opening.PlaneRect.Value.size);
                }
            }
            yield return null;

            foreach (var objectVolume in m_objects)
            {
                if (objectVolume.VolumeBounds.HasValue)
                {
                    m_wallSpaceFinder.AddVolumeObject(objectVolume.transform.localToWorldMatrix, objectVolume.VolumeBounds.Value.size);
                }

                foreach (var surface in m_horizontalSurfaces)
                {
                    if (objectVolume.VolumeBounds.HasValue)
                    {
                        surface.CalculateBlockedArea(objectVolume.transform.localToWorldMatrix, objectVolume.VolumeBounds.Value.size);
                    }
                }
            }
            yield return null;

            foreach (var desk in m_deskSpaceFinders)
            {
                foreach (var floor in m_floorSpaceFinders)
                {
                    desk.GetFinderTransform(out var localToWorldMatrix);
                    desk.GetFinderSize(out var size);
                    floor.CalculateBlockedArea(localToWorldMatrix, size);
                }
            }
            yield return null;

            foreach (var plane in m_blockingPlanes)
            {
                foreach (var floor in m_floorSpaceFinders)
                {
                    floor.CalculateBlockedArea(plane.transform.localToWorldMatrix, plane.PlaneRect?.size ?? Vector2.zero);
                }
            }
            yield return null;

            m_wallSpaceFinder.SetDebugVisible(false);
            yield return null;
            foreach (var spaceFinder in m_horizontalSurfaces)
            {
                spaceFinder?.SetDebugViewEnabled(false);
                yield return null;
            }

            m_sceneLoadingComplete = true;
        }

        public bool HasLoadingCompleted()
        {
            return m_sceneLoadingComplete && m_horizontalSurfaces.All(finder => finder.HasSetUpCompleted());
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.One) && OVRInput.Get(OVRInput.Button.PrimaryThumbstick))
            {
                ToggleDebugView();
            }
        }

        /// <summary>
        /// Toggles the debug cell visibility. 
        /// </summary>
        [ContextMenu("Toggle Debug")]
        public void ToggleDebugView()
        {
            RequestCollidersEnabled();
            m_wallSpaceFinder?.ToggleCellVisibility();
            foreach (var spaceFinder in m_horizontalSurfaces)
            {
                spaceFinder?.ToggleDebugView();
            }
        }

        public bool RequestRandomLocation(Vector3 locationToFace, float radius, out Vector3 foundPosition)
        {
            return RequestLocation(locationToFace, radius, out foundPosition, out _, m_horizontalSurfaces);
        }

        public bool RequestRandomFloorLocation(Vector3 locationToFace, Vector3 objectDimensions, out Vector3 foundPosition, out Quaternion foundRotation, float edgeDistance = -1)
        {
            return RequestLocation(locationToFace, objectDimensions, out foundPosition, out foundRotation, m_floorSpaceFinders, edgeDistance: edgeDistance);
        }

        public bool RequestRandomFloorWallLocation(float halfDepth, float width, float height,
            out Vector3 foundFloorPosition, out Vector3 foundWallPosition, out Quaternion foundWallRotation,
            Vector2 edgeDistance)
        {
            foreach (var floorSpaceFinder in m_floorSpaceFinders)
            {
                if (!floorSpaceFinder)
                {
                    continue;
                }

                var result = m_wallSpaceFinder.RequestRandomLocationCustomWithFloor(
                    halfDepth, width, height, edgeDistance, out foundFloorPosition, out foundWallRotation,
                    floorSpaceFinder.CheckPhysicsResultIsClear,
                    floorSpaceFinder.BlockPhysicsResult, true);

                if (result)
                {
                    foundFloorPosition.y = 0;
                    foundWallPosition = foundFloorPosition;
                    foundWallPosition.y += height;
                    return true;
                }
            }

            foundWallPosition = Vector3.zero;
            foundWallRotation = Quaternion.identity;
            foundFloorPosition = Vector3.zero;
            return false;
        }

        public bool RequestRandomDeskLocation(Vector3 locationToFace, float radius, out Vector3 foundPosition)
        {
            return RequestLocation(locationToFace, radius, out foundPosition, out _, m_deskSpaceFinders);
        }

        private static bool RequestLocation(Vector3 locationToFace, float radius, out Vector3 foundPosition, out Quaternion foundRotation,
            IReadOnlyList<ISpaceFinder> finders, float edgeDistance = -1)
        {
            var startIndex = Random.Range(0, finders.Count);
            for (var i = 0; i < finders.Count; i++)
            {
                var index = (startIndex + i) % finders.Count;

                var spaceFinder = finders[index];
                if (spaceFinder != null)
                {
                    if (spaceFinder.RequestRandomLocation(locationToFace, radius, out foundPosition, out foundRotation, markAsBlocked: true,
                            edgeDistance: edgeDistance))
                    {
                        return true;
                    }
                }
            }

            foundPosition = Vector3.zero;
            foundRotation = Quaternion.identity;
            return false;
        }

        private static bool RequestLocation(Vector3 locationToFace, Vector3 dimensions, out Vector3 foundPosition, out Quaternion foundRotation,
            IReadOnlyList<ISpaceFinder> finders, float edgeDistance = -1)
        {
            var startIndex = Random.Range(0, finders.Count);
            for (var i = 0; i < finders.Count; i++)
            {
                var index = (startIndex + i) % finders.Count;

                var spaceFinder = finders[index];
                if (spaceFinder != null)
                {
                    if (spaceFinder is FloorSpaceFinder floor)
                    {
                        locationToFace = floor.FloorCenterPosition;
                    }

                    if (spaceFinder.RequestRandomLocation(locationToFace, dimensions, out foundPosition, out foundRotation, markAsBlocked: true))
                    {
                        return true;
                    }
                }
            }

            foundPosition = Vector3.zero;
            foundRotation = Quaternion.identity;
            return false;
        }

        public bool RequestRandomWallLocation(float heightOffFloor, float objectWidth, float objectHeight,
            Vector2 edgeDistance, bool ignoreSceneBlocked, out Vector3 foundPosition, out Quaternion foundRotation)
        {
            if (m_wallSpaceFinder != null)
            {
                return m_wallSpaceFinder.QueryForSafeWallLocation(heightOffFloor, objectHeight, objectWidth,
                    edgeDistance, out foundPosition, out foundRotation,
                    ignoreSceneBlocked);
            }

            foundPosition = Vector3.zero;
            foundRotation = Quaternion.identity;
            return false;
        }

        /// <summary>
        /// Adds wall plane to the wall list.
        /// </summary>
        /// <param name="plane">Plane that describes a wall.</param>
        public void AddWall(MRUKAnchor plane)
        {
            m_walls.Add(plane);
        }

        /// <summary>
        /// Adds a doors and windows to the openings list.
        /// </summary>
        /// <param name="doorPlane">Plane that describes a door or window.</param>
        public void AddOpening(MRUKAnchor doorPlane)
        {
            m_openings.Add(doorPlane);
        }

        /// <summary>
        /// Adds any object described with a volume such as storage screens and plants.
        /// </summary>
        /// <param name="objectVolume">Object's volume.</param>
        public void AddVolumeObject(MRUKAnchor objectVolume)
        {
            m_objects.Add(objectVolume);
        }

        /// <summary>
        /// Adds a desk to the general manager, adds a <see cref="DeskSpaceFinder"/> to the object.
        /// </summary>
        /// <param name="plane">Plane that describes a desk.</param>
        public void AddDesk(MRUKAnchor deskAnchor)
        {
            var deskSpaceFinder = deskAnchor.gameObject.AddComponent<DeskSpaceFinder>();
            deskSpaceFinder.DeskSize = deskAnchor.PlaneRect?.size ?? deskAnchor.VolumeBounds?.size ?? Vector2.zero;
            m_deskSpaceFinders.Add(deskSpaceFinder);
            m_horizontalSurfaces.Add(deskSpaceFinder);
        }

        /// <summary>
        /// Stores couch planes to pass to space managers.
        /// </summary>
        /// <param name="plane">Plane that describes a couch</param>
        public void AddCouch(MRUKAnchor plane)
        {
            m_blockingPlanes.Add(plane);
        }

        /// <summary>
        /// Stores any <see cref="FloorSpaceFinder"/>s 
        /// </summary>
        /// <param name="floorSpaceFinder"></param>
        public void AddFloor(FloorSpaceFinder floorSpaceFinder)
        {
            floorSpaceFinder.DebugMaterial = DebugMaterial;
            m_floorSpaceFinders.Add(floorSpaceFinder);
            m_horizontalSurfaces.Add(floorSpaceFinder);
        }

        public void RequestTotallyRandomWallLocation(float minHeight, out Vector3 position, out Quaternion rotation)
        {
            m_wallSpaceFinder.PickRandomWallCell(minHeight, out position, out rotation);
        }

        public void RequestTotallyRandomFloorLocation(out Vector3 position)
        {
            var foundFloors = m_floorSpaceFinders;

            if (foundFloors.Count > 0)
            {
                var floor = foundFloors[Random.Range(0, foundFloors.Count)];

                if (floor != null)
                {
                    floor.RequestTrulyRandomLocation(out position);
                    return;
                }
            }

            position = Vector3.zero;
        }

        public void RequestTotallyRandomDeskLocation(out Vector3 position)
        {
            var foundDesks = m_horizontalSurfaces.Where(finder => finder is DeskSpaceFinder or FloorSpaceFinder).ToList();

            if (foundDesks.Count > 0)
            {
                var deskSpaceFinder = foundDesks[Random.Range(0, foundDesks.Count)];

                if (deskSpaceFinder != null)
                {
                    deskSpaceFinder.RequestTrulyRandomLocation(out position);
                    return;
                }
            }

            position = Vector3.zero;
        }

        public void RequestCollidersDisabled()
        {
            m_wallSpaceFinder.SetWallCollidersEnabled(false);

            foreach (var surface in m_horizontalSurfaces)
            {
                surface.SetSearchCollidersActive(false);
            }
        }

        public void RequestCollidersEnabled()
        {
            m_wallSpaceFinder.SetWallCollidersEnabled(true);

            foreach (var surface in m_horizontalSurfaces)
            {
                surface.SetSearchCollidersActive(true);
            }
        }

        public void ResetSceneUnderstanding()
        {
            m_wallSpaceFinder.SetWallCollidersEnabled(true);

            foreach (var surface in m_horizontalSurfaces)
            {
                surface.SetSearchCollidersActive(true);
            }

            foreach (var horizontalSurface in m_horizontalSurfaces)
            {
                horizontalSurface.ResetDistanceField();
            }

            m_wallSpaceFinder.ResetWallPlacements();
        }

        public void CleanUp()
        {
            m_wallSpaceFinder?.CleanUp();

            foreach (var surface in m_horizontalSurfaces)
            {
                surface.CleanUp();
            }

            foreach (var horizontalSurface in m_horizontalSurfaces)
            {
                horizontalSurface.CleanUp();
            }

            ClearRoomObjects();
        }
    }
}