// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Represents a snappable mini generator.
    /// </summary>
    public class MiniGeneratorSnappable : MonoBehaviour
    {
        public ElectricGenerator ElectricGenerator => m_electricGenerator;
        public ElectricGeneratorID MiniGeneratorID => m_miniGeneratorID;

        [SerializeField] private ElectricGeneratorID m_miniGeneratorID;
        [SerializeField] private GameObject m_root;
        [SerializeField] private ElectricGenerator m_electricGenerator;

        protected void Start()
        {
            if (m_miniGeneratorID == ElectricGeneratorID.UNKNOWN)
            {
                Debug.LogError("Mini generator ID not set on snappable!");
            }

            if (m_root == null)
            {
                m_root = gameObject;
                Debug.Log("Mini generator snappable root was not set. Automatically setting to self.");
            }
        }
    }
}
