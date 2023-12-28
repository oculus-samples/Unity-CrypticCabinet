// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using CrypticCabinet.SceneManagement;
using Oculus.Interaction.Input;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     The manager responsible for the object placements in the room during scene setup.
    /// </summary>
    public class ObjectPlacementManager : Meta.Utilities.Singleton<ObjectPlacementManager>
    {
        private void Update()
        {
            if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Alpha1))
            {
                ShufflePlacements();
            }
        }

        public ObjectPlacementManager(SceneObject o)
        {
        }

        public enum LoadableSceneObjects
        {
            ORRERY,
            CABINET,
            SAND_SHOOT,
            WINDOW_PULLY,
            LIGHT_PLATE,
            BUCKET,
            MINI_GENERATOR_SMALL,
            MAIN_GENERATOR,
            MAIN_GENERATOR_COIL,
            GENERATOR_SCHEMATIC1,
            DIRECTIONAL_APPARATUS,
            MINI_GENERATOR_MEDIUM,
            MINI_GENERATOR_LARGE,
            ATLAS_STATUE,
            LIGHT_BULB_UV_MACHINE,
            UV_SIGN_CLUE,
            UV_SIGN_TEXT1,
            UV_SIGN_TEXT2,
            UV_SIGN_TEXT3,
            SAFE_PUZZLE,
            GENERATOR_SCHEMATIC2
        }

        [Serializable]
        public struct SceneObject
        {
            public string ObjectName;
            public ObjectPlacementVisualiser DisplayPrefab;
            public Vector3 MainPosition;
            public Vector3 WallPosition;
            public Quaternion WallRotation;
            public bool SuccessfullyPlaced;
            public Quaternion MainRotation;
            [HideInInspector] public ObjectPlacementVisualiser SpawnedDisplayObject;
        }

        [SerializeField] private GameObject m_spawnUi;
        [SerializeField] private List<SceneObject> m_againstWallObject = new();
        [SerializeField] private List<SceneObject> m_onWallObject = new();
        [SerializeField] private List<SceneObject> m_deskObject = new();
        [SerializeField] private List<SceneObject> m_floorObject = new();
        [SerializeField] private List<SceneObject> m_anyHorizontalObject = new();

        public Action ConfirmObjectPlacementsCallback;

        private List<GameObject> m_spawnedVisualiserObjects = new();
        private SceneUnderstandingLocationPlacer m_sceneUnderstandingLocationPlacer;

        private OVRCameraRigRef m_cameraRig;

        private void Start()
        {
            m_spawnedVisualiserObjects = new List<GameObject>();
            m_sceneUnderstandingLocationPlacer = FindObjectOfType<SceneUnderstandingLocationPlacer>();
            if (m_sceneUnderstandingLocationPlacer == null)
            {
                Debug.LogError("Missing SceneUnderstandingLocationPlacer in scene unable to layout objects.");
            }

            FindFloorWallLocation();
            FindWallLocation();
            FindDeskLocation();
            FindFloorLocation();
            FindHorizontalLocation();

            RequestCollidersDisabled();

            ShuffleIfNeeded();

            RandomlyPlaceFailedObjects();
        }

        private void RandomlyPlaceFailedObjects()
        {
            var ovrCameraRig = FindObjectOfType<OVRCameraRig>();
            var userPos = ovrCameraRig.centerEyeAnchor.position;

            foreach (var againstWallObject in m_againstWallObject.Where(o => !o.SuccessfullyPlaced).ToList())
            {
                m_sceneUnderstandingLocationPlacer.RequestTotallyRandomWallLocation(1, out var position, out var rotation);
                position.y = 0;
                PlaceObject(againstWallObject, position, rotation);
            }

            foreach (var wallObject in m_onWallObject.Where(o => !o.SuccessfullyPlaced).ToList())
            {
                m_sceneUnderstandingLocationPlacer.RequestTotallyRandomWallLocation(1, out var position, out var rotation);
                PlaceObject(wallObject, position, rotation);
            }

            foreach (var deskObject in m_deskObject.Where(o => !o.SuccessfullyPlaced).ToList())
            {
                m_sceneUnderstandingLocationPlacer.RequestTotallyRandomDeskLocation(out var position);
                PlaceObjectLookAtUser(deskObject, userPos, position);
            }

            foreach (var floorObject in m_floorObject.Where(o => !o.SuccessfullyPlaced).ToList())
            {
                m_sceneUnderstandingLocationPlacer.RequestTotallyRandomFloorLocation(out var position);
                PlaceObjectLookAtUser(floorObject, userPos, position);
            }

            foreach (var sceneObject in m_anyHorizontalObject.Where(o => !o.SuccessfullyPlaced).ToList())
            {
                m_sceneUnderstandingLocationPlacer.RequestTotallyRandomFloorLocation(out var position);
                PlaceObjectLookAtUser(sceneObject, userPos, position);
            }
        }

        private static void PlaceObject(SceneObject sceneObject, Vector3 targetPosition, Quaternion targetRotation)
        {
            var placedObjectTransform = sceneObject.SpawnedDisplayObject.transform;
            placedObjectTransform.position = targetPosition;
            placedObjectTransform.rotation = targetRotation;
            sceneObject.SpawnedDisplayObject.UpdateLocation();
        }

        private static void PlaceObjectLookAtUser(SceneObject sceneObject, Vector3 userPosition, Vector3 targetPosition)
        {
            var placedObjectTransform = sceneObject.SpawnedDisplayObject.transform;
            placedObjectTransform.position = targetPosition;
            userPosition.y = placedObjectTransform.position.y;
            placedObjectTransform.LookAt(userPosition);
            sceneObject.SpawnedDisplayObject.UpdateLocation();
        }

        private void ShuffleIfNeeded(int maxRetries = 3)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (DetectFailedPlacements() > 0)
                {
                    ShufflePlacements();
                }
                else
                {
                    return;
                }
            }
        }

        private int DetectFailedPlacements()
        {
            var againstWallObjectFailed = m_againstWallObject.Count(o => !o.SuccessfullyPlaced);
            var onWallObjectFailed = m_onWallObject.Count(o => !o.SuccessfullyPlaced);
            var deskObjectFailed = m_deskObject.Count(o => !o.SuccessfullyPlaced);
            var floorObjectFailed = m_floorObject.Count(o => !o.SuccessfullyPlaced);
            var anyHorizontalObjectFailed = m_anyHorizontalObject.Count(o => !o.SuccessfullyPlaced);
            var totalFailed = againstWallObjectFailed + onWallObjectFailed + deskObjectFailed + floorObjectFailed +
                              anyHorizontalObjectFailed;

            return totalFailed;
        }

        [ContextMenu("Confirm objects placement")]
        public void ConfirmObjectPlacements()
        {
            CleanUp();

            ConfirmObjectPlacementsCallback?.Invoke();
        }

        public void CleanUp()
        {
            foreach (var visualiserObject in m_spawnedVisualiserObjects)
            {
                Destroy(visualiserObject);
            }

            if (m_spawnUi != null)
            {
                m_spawnUi.SetActive(false);
            }
        }

        [ContextMenu("Shuffle placements")]
        public void ShufflePlacements()
        {
            foreach (var visualiserObject in m_spawnedVisualiserObjects)
            {
                Destroy(visualiserObject);
            }

            SceneUnderstandingReset();

            FindFloorWallLocation();
            FindWallLocation();
            FindDeskLocation();
            FindFloorLocation();
            FindHorizontalLocation();

            RequestCollidersDisabled();

            RandomlyPlaceFailedObjects();
        }

        public List<SceneObject> RequestObjects(LoadableSceneObjects objectType)
        {
            var returnObjects = new List<SceneObject>();
            returnObjects.AddRange(m_againstWallObject.Where(o => o.DisplayPrefab.GetObjectType == objectType).ToList());
            returnObjects.AddRange(m_onWallObject.Where(o => o.DisplayPrefab.GetObjectType == objectType).ToList());
            returnObjects.AddRange(m_deskObject.Where(o => o.DisplayPrefab.GetObjectType == objectType).ToList());
            returnObjects.AddRange(m_floorObject.Where(o => o.DisplayPrefab.GetObjectType == objectType).ToList());
            returnObjects.AddRange(
                m_anyHorizontalObject.Where(o => o.DisplayPrefab.GetObjectType == objectType).ToList());
            return returnObjects;
        }

        public void UpdatePlacedObject(LoadableSceneObjects objectType, Vector3 mainPosition, Vector3 wallPosition,
            Quaternion mainRotation, Quaternion wallRotation)
        {
            UpdateObject(ref m_againstWallObject, objectType, mainPosition, wallPosition, mainRotation, wallRotation);
            UpdateObject(ref m_onWallObject, objectType, mainPosition, wallPosition, mainRotation, wallRotation);
            UpdateObject(ref m_deskObject, objectType, mainPosition, wallPosition, mainRotation, wallRotation);
            UpdateObject(ref m_floorObject, objectType, mainPosition, wallPosition, mainRotation, wallRotation);
            UpdateObject(ref m_anyHorizontalObject, objectType, mainPosition, wallPosition, mainRotation, wallRotation);
        }

        private static void UpdateObject(ref List<SceneObject> objects, LoadableSceneObjects objectType, Vector3 mainPosition,
            Vector3 wallPosition,
            Quaternion mainRotation, Quaternion wallRotation)
        {
            for (var i = 0; i < objects.Count; i++)
            {
                var sceneObject = objects[i];
                if (sceneObject.DisplayPrefab.GetObjectType == objectType)
                {
                    sceneObject.WallPosition = wallPosition;
                    sceneObject.MainPosition = mainPosition;
                    sceneObject.WallRotation = wallRotation;
                    sceneObject.MainRotation = mainRotation;
                    objects[i] = sceneObject;
                }
            }
        }

        private void FindFloorWallLocation()
        {
            m_againstWallObject.Sort((l, r) => r.DisplayPrefab.GetWallObjectWidth.CompareTo(l.DisplayPrefab.GetWallObjectWidth));

            for (var i = 0; i < m_againstWallObject.Count; i++)
            {
                var sceneObject = m_againstWallObject[i];

                var successfullyPlaced = RequestRandomFloorWallLocation(ref sceneObject, 1.05f);

                sceneObject.SuccessfullyPlaced = successfullyPlaced;
                m_againstWallObject[i] = sceneObject;
                if (sceneObject.DisplayPrefab == null)
                {
                    Debug.LogError("Scene Object without object visualizer! Skip placement");
                    continue;
                }

                var objectPlacementVisualiser = Instantiate(sceneObject.DisplayPrefab);
                objectPlacementVisualiser.Setup(
                    this, sceneObject.MainPosition, sceneObject.WallPosition, sceneObject.WallRotation);
                m_spawnedVisualiserObjects.Add(objectPlacementVisualiser.gameObject);

                sceneObject.SpawnedDisplayObject = objectPlacementVisualiser;
                m_againstWallObject[i] = sceneObject;
            }
        }

        private bool RequestRandomFloorWallLocation(ref SceneObject sceneObject, float sizeMultiplier)
        {
            return m_sceneUnderstandingLocationPlacer.RequestRandomFloorWallLocation(
                sceneObject.DisplayPrefab.GetRadius * sizeMultiplier,
                sceneObject.DisplayPrefab.GetWallObjectWidth * sizeMultiplier,
                sceneObject.DisplayPrefab.GetWallObjectHeight,
                out sceneObject.MainPosition,
                out sceneObject.WallPosition,
                out sceneObject.WallRotation,
                0);
        }

        private void FindWallLocation()
        {
            m_onWallObject.Sort((l, r) => r.DisplayPrefab.GetRadius.CompareTo(l.DisplayPrefab.GetRadius));

            for (var i = 0; i < m_onWallObject.Count; i++)
            {
                var sceneObject = m_onWallObject[i];
                var successfullyPlaced = m_sceneUnderstandingLocationPlacer.RequestRandomWallLocation(
                    sceneObject.DisplayPrefab.GetWallObjectHeight,
                    sceneObject.DisplayPrefab.GetWallObjectWidth,
                    sceneObject.DisplayPrefab.GetWallObjectVerticalSize,
                    ignoreSceneBlocked: false,
                    out sceneObject.MainPosition,
                    out sceneObject.WallRotation);

                if (!successfullyPlaced)
                {
                    // Try again but allow placing over scene blocked cells.
                    successfullyPlaced = m_sceneUnderstandingLocationPlacer.RequestRandomWallLocation(
                        sceneObject.DisplayPrefab.GetWallObjectHeight,
                        sceneObject.DisplayPrefab.GetWallObjectWidth,
                        sceneObject.DisplayPrefab.GetWallObjectVerticalSize,
                        ignoreSceneBlocked: true,
                        out sceneObject.MainPosition,
                        out sceneObject.WallRotation);
                }

                sceneObject.WallPosition = sceneObject.MainPosition;
                sceneObject.SuccessfullyPlaced = successfullyPlaced;
                m_onWallObject[i] = sceneObject;
                if (sceneObject.DisplayPrefab == null)
                {
                    Debug.LogError("Scene Object without object visualizer! Skip placement");
                    continue;
                }

                var objectPlacementVisualiser = Instantiate(sceneObject.DisplayPrefab);
                objectPlacementVisualiser.Setup(
                    this, sceneObject.MainPosition, sceneObject.WallPosition, sceneObject.WallRotation);
                m_spawnedVisualiserObjects.Add(objectPlacementVisualiser.gameObject);

                sceneObject.SpawnedDisplayObject = objectPlacementVisualiser;
                m_onWallObject[i] = sceneObject;
            }
        }

        private void FindDeskLocation()
        {
            m_deskObject.Sort((l, r) => r.DisplayPrefab.GetRadius.CompareTo(l.DisplayPrefab.GetRadius));

            for (var i = 0; i < m_deskObject.Count; i++)
            {
                var sceneObject = m_deskObject[i];
                var successfullyPlaced = RequestRandomDeskLocation(ref sceneObject, 1.05f);

                if (!successfullyPlaced)
                {
                    successfullyPlaced = RequestRandomDeskLocation(ref sceneObject, 0.5f);
                }

                if (!successfullyPlaced)
                {
                    // failed to find any desk location looking for any horizontal location
                    successfullyPlaced = RequestRandomLocation(ref sceneObject, 1.05f);
                }

                sceneObject.SuccessfullyPlaced = successfullyPlaced;
                sceneObject.WallPosition = sceneObject.MainPosition;
                sceneObject.MainRotation = GetRotationTowardsUser(sceneObject.MainPosition, sceneObject.DisplayPrefab.GetFlipFaceDir);
                m_deskObject[i] = sceneObject;
                if (sceneObject.DisplayPrefab == null)
                {
                    Debug.LogError("Scene Object without object visualizer! Skip placement");
                    continue;
                }

                var objectPlacementVisualiser = Instantiate(sceneObject.DisplayPrefab);
                objectPlacementVisualiser.Setup(this, sceneObject.MainPosition);
                m_spawnedVisualiserObjects.Add(objectPlacementVisualiser.gameObject);

                sceneObject.SpawnedDisplayObject = objectPlacementVisualiser;
                m_deskObject[i] = sceneObject;
            }
        }

        private bool RequestRandomDeskLocation(ref SceneObject sceneObject, float radiusMultiplier)
        {
            return m_sceneUnderstandingLocationPlacer.RequestRandomDeskLocation(GetUserPosition(),
                sceneObject.DisplayPrefab.GetRadius * radiusMultiplier, out sceneObject.MainPosition);
        }

        private void FindFloorLocation()
        {
            m_floorObject.Sort((l, r) => r.DisplayPrefab.GetRadius.CompareTo(l.DisplayPrefab.GetRadius));

            for (var i = 0; i < m_floorObject.Count; i++)
            {
                var sceneObject = m_floorObject[i];
                var successfullyPlaced = RequestRandomFloorLocation(ref sceneObject, 1.05f);

                if (!successfullyPlaced)
                {
                    successfullyPlaced = RequestRandomFloorLocation(ref sceneObject, 0.5f);
                }

                if (!successfullyPlaced)
                {
                    successfullyPlaced = RequestRandomFloorLocation(ref sceneObject, 0.01f);
                }

                sceneObject.WallPosition = sceneObject.MainPosition;
                sceneObject.SuccessfullyPlaced = successfullyPlaced;
                sceneObject.WallRotation = sceneObject.MainRotation;

                m_floorObject[i] = sceneObject;
                if (sceneObject.DisplayPrefab == null)
                {
                    Debug.LogError("Scene Object without object visualizer! Skip placement");
                    continue;
                }
                var objectPlacementVisualiser = Instantiate(sceneObject.DisplayPrefab);
                objectPlacementVisualiser.Setup(this, sceneObject.MainPosition, sceneObject.MainRotation);
                m_spawnedVisualiserObjects.Add(objectPlacementVisualiser.gameObject);

                sceneObject.SpawnedDisplayObject = objectPlacementVisualiser;
                m_floorObject[i] = sceneObject;
            }
        }

        private bool RequestRandomFloorLocation(ref SceneObject sceneObject, float radiusMultiplier)
        {
            return m_sceneUnderstandingLocationPlacer.RequestRandomFloorLocation(GetUserPosition(),
                sceneObject.DisplayPrefab.GetObjectDimensions * radiusMultiplier, out sceneObject.MainPosition, out sceneObject.MainRotation);
        }

        private void FindHorizontalLocation()
        {
            m_anyHorizontalObject.Sort((l, r) => r.DisplayPrefab.GetRadius.CompareTo(l.DisplayPrefab.GetRadius));

            for (var i = 0; i < m_anyHorizontalObject.Count; i++)
            {
                var sceneObject = m_anyHorizontalObject[i];
                var successfullyPlaced = RequestRandomLocation(ref sceneObject, 1.05f);

                if (!successfullyPlaced)
                {
                    successfullyPlaced = RequestRandomLocation(ref sceneObject, 0.5f);
                }

                if (!successfullyPlaced)
                {
                    successfullyPlaced = RequestRandomLocation(ref sceneObject, 0.01f);
                }

                if (sceneObject.DisplayPrefab == null)
                {
                    Debug.LogError("Scene Object without object visualizer! Skip placement");
                    continue;
                }

                sceneObject.WallPosition = sceneObject.MainPosition;
                sceneObject.SuccessfullyPlaced = successfullyPlaced;
                sceneObject.MainRotation = GetRotationTowardsUser(sceneObject.MainPosition, sceneObject.DisplayPrefab.GetFlipFaceDir);
                m_anyHorizontalObject[i] = sceneObject;

                var objectPlacementVisualiser = Instantiate(sceneObject.DisplayPrefab);
                objectPlacementVisualiser.Setup(this, sceneObject.MainPosition);
                m_spawnedVisualiserObjects.Add(objectPlacementVisualiser.gameObject);

                sceneObject.SpawnedDisplayObject = objectPlacementVisualiser;
                m_anyHorizontalObject[i] = sceneObject;
            }
        }

        private bool RequestRandomLocation(ref SceneObject sceneObject, float radiusMultiplier)
        {
            return m_sceneUnderstandingLocationPlacer.RequestRandomLocation(GetUserPosition(),
                sceneObject.DisplayPrefab.GetRadius * radiusMultiplier, out sceneObject.MainPosition);
        }

        private void RequestCollidersDisabled()
        {
            m_sceneUnderstandingLocationPlacer.RequestCollidersDisabled();
        }

        private void SceneUnderstandingReset()
        {
            m_sceneUnderstandingLocationPlacer.ResetSceneUnderstanding();
        }

        private Quaternion GetRotationTowardsUser(Vector3 objectPosition, bool flipFaceDirection)
        {
            objectPosition.y = 0;

            if (m_cameraRig == null)
            {
                m_cameraRig = FindObjectOfType<OVRCameraRigRef>();
            }

            var headsetPos = m_cameraRig.CameraRig.centerEyeAnchor.position;
            headsetPos.y = 0;

            var lookDir = flipFaceDirection ? objectPosition - headsetPos : headsetPos - objectPosition;
            return Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        private Vector3 GetUserPosition()
        {
            if (m_cameraRig == null)
            {
                m_cameraRig = FindObjectOfType<OVRCameraRigRef>();
            }

            return m_cameraRig.CameraRig.centerEyeAnchor.position;
        }
    }
}