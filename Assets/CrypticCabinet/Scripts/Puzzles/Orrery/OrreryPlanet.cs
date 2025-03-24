// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     Represents a planet of the orrery puzzle.
    ///     It defines the snapping status (correct or incorrect), and the grab interactions.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class OrreryPlanet : MonoBehaviour
    {
        [SerializeField] private OrreryControl.Planets m_planet;
        public OrreryControl.Planets Planet => m_planet;

        [SerializeField] private UnityEvent m_planetInCorrectSnapArea;
        [SerializeField] private UnityEvent m_planetInWrongSnapArea;

        private Collider m_grabCollider;

        private void Start()
        {
            m_grabCollider = GetComponent<Collider>();
        }

        /// <summary>
        ///     Locks a planet so that once put in the correct position it cannot be unsnapped any longer.
        /// </summary>
        public void LockPlanet()
        {
            // Disable the grab, as the planet is now in place correctly.
            if (m_grabCollider != null)
            {
                m_grabCollider.enabled = false;
            }

            m_planetInCorrectSnapArea?.Invoke();
        }

        /// <summary>
        ///     Triggered when the planet was snapped in the wrong snap area.
        /// </summary>
        public void WrongPlanet()
        {
            // Still allow the player to pick up the planet.
            if (m_grabCollider != null)
            {
                m_grabCollider.enabled = true;
            }

            m_planetInWrongSnapArea?.Invoke();
        }
    }
}