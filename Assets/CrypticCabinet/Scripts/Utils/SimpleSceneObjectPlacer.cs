// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Establishes where a specific object should be placed into the real room depending on the
    ///     object type, and on whether it should be placed on a wall or at ground level.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SimpleSceneObjectPlacer : MonoBehaviour
    {
        [SerializeField] private ObjectPlacementManager.LoadableSceneObjects m_objectType;
        [SerializeField] private bool m_isWallObject;
        [SerializeField] private bool m_placeAtGroundLevel;

        [SerializeField] private UnityEvent m_onObjectPlaced;

        private bool m_spawned;

        private void Start()
        {
            PlaceUsingSpawner();
            m_onObjectPlaced?.Invoke();
        }

        public void PlaceUsingSpawner()
        {
            if (m_spawned)
            {
                return;
            }

            List<ObjectPlacementManager.SceneObject> positions = null;
            if (ObjectPlacementManager.Instance != null)
            {
                positions = ObjectPlacementManager.Instance.RequestObjects(m_objectType);
            }

            var currentTransform = transform;

            if (positions is { Count: > 0 })
            {
                if (m_isWallObject)
                {
                    currentTransform.position = positions[0].WallPosition;
                    currentTransform.rotation = positions[0].WallRotation;
                }
                else
                {
                    currentTransform.rotation = positions[0].MainRotation;
                    currentTransform.position = positions[0].MainPosition;
                }
            }

            if (m_placeAtGroundLevel)
            {
                var transformPosition = currentTransform.position;
                transformPosition.y = 0;
                currentTransform.position = transformPosition;
            }

            m_spawned = true;
        }
    }
}