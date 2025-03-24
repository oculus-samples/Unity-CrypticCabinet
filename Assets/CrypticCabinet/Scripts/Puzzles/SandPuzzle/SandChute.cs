// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using CrypticCabinet.Utils;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Defines the behaviour for the sand chute of the sand puzzle.
    ///     Limitation: Only one sand bucket in the scene, and only one sand bucket can be filled at a time.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SandChute : MonoBehaviour
    {
        [SerializeField] private float m_sandVolumePerSec = 20f;

        private Coroutine m_bucketFillerCoroutine;
        private bool m_isBucketInside;
        private SandBucket m_bucket;

        private void Start()
        {
            PlaceUsingSpawner();
        }

        private void OnTriggerEnter(Collider other)
        {
            var sandBucket = other.gameObject.GetComponent<SandBucket>();

            if (sandBucket != null)
            {
                m_bucket = sandBucket;
                m_isBucketInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var sandBucket = other.gameObject.GetComponent<SandBucket>();

            if (sandBucket != null)
            {
                m_isBucketInside = false;
                m_bucket = null;
            }
        }

        private void Update()
        {
            if (m_isBucketInside && m_bucket != null)
            {
                m_bucket.ChangeSandValue(m_sandVolumePerSec * Time.deltaTime);
            }
        }

        private void PlaceUsingSpawner()
        {
            List<ObjectPlacementManager.SceneObject> positions = null;
            if (ObjectPlacementManager.Instance != null)
            {
                positions = ObjectPlacementManager.Instance.RequestObjects(ObjectPlacementManager.LoadableSceneObjects.SAND_SHOOT);
            }

            if (positions != null && positions.Count > 0)
            {
                var transformRoot = transform;
                transformRoot.rotation = positions[0].WallRotation * Quaternion.Euler(0, 180, 0);
                var calculatedPosition = positions[0].WallPosition;
                calculatedPosition.y = 0;
                transformRoot.position = calculatedPosition;
            }
        }
    }
}
