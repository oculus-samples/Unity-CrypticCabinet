// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     Represent the snap socket for a planet in the orrery.
    ///     It holds information related to the correct planet to snap into this socket, and the currently
    ///     snapped planet into this socket.
    /// </summary>
    [RequireComponent(typeof(SnapInteractable))]
    public class OrreryPlanetSocket : MonoBehaviour
    {
        [SerializeField] private OrreryControl.Planets m_correctPlanet;
        [SerializeField] private SnapInteractable m_snapper;

        public bool PlanetCorrect => m_correctPlanet == m_snappedPlanet;

        private OrreryControl m_orreryControl;
        private OrreryControl.Planets m_snappedPlanet;

        private void Start()
        {
            m_orreryControl = GetComponentInParent<OrreryControl>();
            m_orreryControl.RegisterPlanetSocket(this);

            m_snapper.WhenSelectingInteractorViewAdded += WhenSelectingInteractorViewAdded;
            m_snapper.WhenSelectingInteractorViewRemoved += OnWhenSelectingInteractorViewRemoved;
        }

        private void SetSnappedPlanet(OrreryPlanet snappedOrreryPlanet)
        {
            m_snappedPlanet = snappedOrreryPlanet.Planet;
            m_orreryControl.CheckSnappedPlanetsCorrect();

            if (PlanetCorrect)
            {
                snappedOrreryPlanet.LockPlanet();
            }
            else
            {
                snappedOrreryPlanet.WrongPlanet();
            }
        }

        private void WhenSelectingInteractorViewAdded(IInteractorView view)
        {
            var snapable = view.Data as SnapInteractor;
            if (snapable == null)
            {
                return;
            }

            var orreryPlanet = snapable.gameObject.GetComponentInParent<OrreryPlanet>();
            if (orreryPlanet != null)
            {
                SetSnappedPlanet(orreryPlanet);
            }
        }

        private void OnWhenSelectingInteractorViewRemoved(IInteractorView view)
        {
            m_snappedPlanet = OrreryControl.Planets.NONE;
        }
    }
}