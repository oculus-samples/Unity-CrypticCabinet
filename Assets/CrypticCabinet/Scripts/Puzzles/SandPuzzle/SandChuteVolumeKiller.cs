// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Utils;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Defines the sand chute volume killer for the sand puzzle.
    /// </summary>
    public class SandChuteVolumeKiller : MonoBehaviour
    {
        [SerializeField] private Transform m_pivot;
        [SerializeField] private float m_baseRadius;
        [SerializeField] private float m_topRadius;
        [SerializeField] private float m_height;

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var position = m_pivot.position;
            Gizmos.DrawSphere(position, m_baseRadius);
            position += m_pivot.up * m_height;
            Gizmos.DrawSphere(position, m_topRadius);
        }

        public void ToVfxCone(VFXCone coneDescriptor)
        {
            coneDescriptor.BaseRadius = m_baseRadius;
            coneDescriptor.TopRadius = m_topRadius;
            coneDescriptor.Height = m_height;
            coneDescriptor.Transform = m_pivot;
        }
    }
}
