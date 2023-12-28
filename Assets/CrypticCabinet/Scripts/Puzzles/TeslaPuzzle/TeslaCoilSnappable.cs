// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Represents a snappable Tesla coil.
    /// </summary>
    public class TeslaCoilSnappable : MonoBehaviour
    {
        [SerializeField] private GameObject m_root;

        private void Start()
        {
            if (m_root == null)
            {
                m_root = gameObject;
                Debug.Log("Tesla coil snappable root was not set. Automatically setting to self.");
            }
        }

        public void LockObjectCoil()
        {
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }
    }
}
