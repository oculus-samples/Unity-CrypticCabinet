// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     This is added to the prefab that the <see cref="MRUK"/> spawns that describes a Scene object.
    ///     Once loaded passes the details of the loaded object to the <see cref="SceneUnderstandingLocationPlacer"/>.
    /// </summary>

    [DefaultExecutionOrder(30)]
    public class SceneRoomObject : MonoBehaviour
    {
        /// <summary>
        /// The anchor on this object.
        /// </summary>
        private MRUKAnchor m_sceneAnchor;
        /// <summary>
        /// The classification of this object.
        /// </summary>
        private MRUKAnchor.SceneLabels m_classification;
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
            m_sceneAnchor = transform.parent.GetComponent<MRUKAnchor>();
            m_classification = m_sceneAnchor.Label;
            Debug.Assert(m_sceneAnchor is not null, "SceneAnchor is null");

            m_sceneUnderstandingLocationPlacer = placer;

            LogObject();
        }

        /// <summary>
        /// Passes the information about this object back to the space finding manager depending on the
        /// classification of this object. 
        /// </summary>
        private void LogObject()
        {
            switch (m_classification)
            {
                case MRUKAnchor.SceneLabels.WALL_FACE:
                    m_sceneUnderstandingLocationPlacer.AddWall(m_sceneAnchor);
                    break;
                case MRUKAnchor.SceneLabels.FLOOR:
                    var floorSpaceFinder = m_sceneAnchor.gameObject.AddComponent<FloorSpaceFinder>();
                    m_sceneUnderstandingLocationPlacer.AddFloor(floorSpaceFinder);
                    break;
                case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                    m_sceneUnderstandingLocationPlacer.AddOpening(m_sceneAnchor);
                    break;
                case MRUKAnchor.SceneLabels.DOOR_FRAME:
                    m_sceneUnderstandingLocationPlacer.AddOpening(m_sceneAnchor);
                    break;
                case MRUKAnchor.SceneLabels.COUCH:
                    m_sceneUnderstandingLocationPlacer.AddCouch(m_sceneAnchor);
                    break;
                case MRUKAnchor.SceneLabels.TABLE:
                    m_sceneUnderstandingLocationPlacer.AddDesk(m_sceneAnchor);
                    break;
                default:
                    if (m_sceneAnchor.VolumeBounds.HasValue)
                    {
                        m_sceneUnderstandingLocationPlacer.AddVolumeObject(m_sceneAnchor);
                    }
                    break;
            }
        }
    }
}
