// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.SceneManagement;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     This script moves its GameObject to the same height
    ///     of the ceiling of the room.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class PlaceClueOverCeiling : MonoBehaviour
    {
        [SerializeField] private CeilingHeightDetector m_ceilingDetector;

        private float m_heightFromFloor;

        // Start is called before the first frame update
        private void Start()
        {
            m_heightFromFloor = 0;

            if (m_ceilingDetector == null)
            {
                // Note: we assume the ceiling has uniform height, so any detected ceiling
                // will be used as reference for the overall ceiling height.
                m_ceilingDetector = FindFirstObjectByType<CeilingHeightDetector>();
            }

            if (m_ceilingDetector != null)
            {
                // Update the Z value to match the one of the ceiling.
                m_heightFromFloor = m_ceilingDetector.GetCeilingHeight();
                var currentTransform = transform;
                var currentPosition = currentTransform.position;
                currentPosition.y = m_heightFromFloor;
                currentTransform.position = currentPosition;
            }
            else
            {
                Debug.LogError("PlaceClueOverCeiling did not find a valid CeilingHeightDetector!");
            }
        }
    }
}
