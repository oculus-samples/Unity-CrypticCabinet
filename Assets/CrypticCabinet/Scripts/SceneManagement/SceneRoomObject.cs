// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     This is added to the prefab that the <see cref="OVRSceneManager"/> spawns that describes a Scene object.
    ///     Once loaded passes the details of the loaded object to the <see cref="SceneUnderstandingLocationPlacer"/>.
    /// </summary>
    [RequireComponent(typeof(OVRSceneAnchor))]
    [DefaultExecutionOrder(30)]
    public class SceneRoomObject : MonoBehaviour
    {
        /// <summary>
        /// The anchor on this object.
        /// </summary>
        private OVRSceneAnchor m_sceneAnchor;

        /// <summary>
        /// The classification of this object.
        /// </summary>
        private OVRSemanticClassification m_classification;

        /// <summary>
        /// The main scene space finding manger.
        /// </summary>
        private SceneUnderstandingLocationPlacer m_sceneUnderstandingLocationPlacer;

        /// <summary>
        /// Due to the unusual execution ordering from the Unity game object life cycle this gets called by the
        /// space finding manager to make sure this is set up when needed.
        /// </summary>
        public void SetUpObject(SceneUnderstandingLocationPlacer placer)
        {
            m_sceneAnchor = GetComponent<OVRSceneAnchor>();
            m_classification = GetComponent<OVRSemanticClassification>();
            Debug.Assert(m_sceneAnchor != null, "SceneAnchor is null");
            Debug.Assert(m_classification != null, "Classification is null");

            if (TryGetComponent<OVRScenePlane>(out var scenePlane))
            {
                scenePlane.ScaleChildren = false;
            }
            if (TryGetComponent<OVRSceneVolume>(out var sceneVolume))
            {
                sceneVolume.ScaleChildren = false;
            }

            m_sceneUnderstandingLocationPlacer = placer;

            LogObject();
        }

        /// <summary>
        /// Passes the information about this object back to the space finding manager depending on the
        /// classification of this object. 
        /// </summary>
        private void LogObject()
        {
            if (m_classification.Contains(OVRSceneManager.Classification.WallFace))
            {
                var plane = m_sceneAnchor.GetComponent<OVRScenePlane>();
                if (plane != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddWall(plane);
                }
            }

            if (m_classification.Contains(OVRSceneManager.Classification.Floor))
            {
                var plane = m_sceneAnchor.GetComponent<OVRScenePlane>();

                var floorSpaceFinder = plane.gameObject.AddComponent<FloorSpaceFinder>();
                if (floorSpaceFinder != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddFloor(floorSpaceFinder);
                }
            }

            if (m_classification.Contains(OVRSceneManager.Classification.DoorFrame) ||
                m_classification.Contains(OVRSceneManager.Classification.WindowFrame))
            {
                var plane = m_sceneAnchor.GetComponent<OVRScenePlane>();
                if (plane != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddOpening(plane);
                }
            }

            if (m_classification.Contains(OVRSceneManager.Classification.Couch))
            {
                var plane = m_sceneAnchor.GetComponent<OVRScenePlane>();
                if (plane != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddCouch(plane);
                }
            }

            if (m_classification.Contains(OVRSceneManager.Classification.Table))
            {
                var plane = m_sceneAnchor.GetComponent<OVRScenePlane>();
                if (plane != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddDesk(plane);
                }
            }

            if (m_classification.Contains(OVRSceneManager.Classification.Other) ||
                m_classification.Contains(OVRSceneManager.Classification.Storage) ||
                m_classification.Contains(OVRSceneManager.Classification.Bed) ||
                m_classification.Contains(OVRSceneManager.Classification.Screen) ||
                m_classification.Contains(OVRSceneManager.Classification.Lamp) ||
                m_classification.Contains(OVRSceneManager.Classification.Plant))
            {
                var volume = m_sceneAnchor.GetComponent<OVRSceneVolume>();
                if (volume != null)
                {
                    m_sceneUnderstandingLocationPlacer.AddVolumeObject(volume);
                }
            }
        }
    }
}