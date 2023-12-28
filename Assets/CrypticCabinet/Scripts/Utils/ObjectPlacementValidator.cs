// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Singleton validator for the placement of all objects in the real room during scene setup.
    /// </summary>
    public class ObjectPlacementValidator : Singleton<ObjectPlacementValidator>
    {
        [SerializeField] private GameObject m_validationVisualCubePrefab;
        //Changing color for the primitive visualisation, keeping name generic as potentially
        //we want to invalidate the placement for other reasons than overlaps only.
        [SerializeField] private Color m_placementColorCorrectAccessible;
        [SerializeField] private Color m_placementColorCorrectViewable;
        [SerializeField] private Color m_placementColorIncorrect;

        private int m_incorrectPlacements;

        public GameObject GetValidationVisualCubePrefab => m_validationVisualCubePrefab;
        public Color GetPlacementColorCorrectAccessible => m_placementColorCorrectAccessible;
        public Color GetPlacementColorCorrectViewable => m_placementColorCorrectViewable;
        public Color GetPlacementColorIncorrect => m_placementColorIncorrect;
    }
}