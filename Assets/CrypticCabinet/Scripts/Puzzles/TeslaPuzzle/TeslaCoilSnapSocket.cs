// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Represents the snapping area for a Tesla coil snappable.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class TeslaCoilSnapSocket : MonoBehaviour
    {
        public bool IsTeslaCoilSnapped { get; private set; }

        /// <summary>
        ///     Snap interactable responsible for hover and snap events dispatching.
        /// </summary>
        [SerializeField] private SnapInteractable m_snapInteractable;

        /// <summary>
        ///     Triggered when the snap has completed, and the tesla coil is in place.
        /// </summary>
        [SerializeField] private UnityEvent m_onTeslaCoilSnapped;

        /// <summary>
        ///     Triggered when the tesla coil is within the distance threshold for the snapping.
        /// </summary>
        [SerializeField] private UnityEvent m_onTeslaCoilNearby;

        /// <summary>
        ///     Triggered when the tesla coil is  no more within the distance threshold for the snapping.
        ///     Triggered only once.
        /// </summary>
        [SerializeField] private UnityEvent m_onTeslaCoilNoMoreInRange;

        /// <summary>
        ///     The snappable tesla coil that should be put by the user into this socket.
        /// </summary>
        private TeslaCoilSnappable m_teslaCoil;

        private void Start()
        {
            if (m_teslaCoil == null)
            {
                var teslaCoilSnappables = FindObjectsOfType<TeslaCoilSnappable>();
                // We should have a single tesla socket in the scene
                Debug.Assert(teslaCoilSnappables.Length == 1, "More than a tesla coil snappable found in scene!");
                m_teslaCoil = teslaCoilSnappables[0];
                Debug.Assert(m_teslaCoil != null, "Invalid Tesla snappable for tesla puzzle socket");
            }

            if (m_snapInteractable != null)
            {
                m_snapInteractable.WhenInteractorViewAdded += WhenInteractorViewAdded;
                m_snapInteractable.WhenInteractorViewRemoved += OnWhenInteractorViewRemoved;
                m_snapInteractable.WhenSelectingInteractorViewAdded += WhenSelectingInteractorViewAdded;
                m_snapInteractable.WhenSelectingInteractorViewRemoved += OnWhenSelectingInteractorViewRemoved;
            }
            else
            {
                Debug.LogError("Snap interactable missing in Tesla coil snap socket!");
            }
        }

        private void WhenInteractorViewAdded(IInteractorView obj)
        {
            // On hover
            var interactor = obj.Data as SnapInteractor;
            if (interactor == null)
            {
                Debug.LogError("Data is not a valid snap interactor!");
                return;
            }

            var teslaCoil = interactor.GetComponentInParent<TeslaCoilSnappable>();
            if (teslaCoil != null)
            {
                Debug.Log("Tesla coil hover");
                // The tesla coil was not in range, now it is.
                m_onTeslaCoilNearby?.Invoke();
            }
        }

        private void OnWhenInteractorViewRemoved(IInteractorView obj)
        {
            // On un-hover
            var interactor = obj.Data as SnapInteractor;
            if (interactor == null)
            {
                Debug.LogError("Data is not a valid snap interactor!");
                return;
            }

            var teslaCoil = interactor.GetComponentInParent<TeslaCoilSnappable>();
            if (teslaCoil != null)
            {
                Debug.Log("Tesla coil un-hover");
                // The tesla coil was in range, now it is no more.
                m_onTeslaCoilNoMoreInRange?.Invoke();
            }
        }

        private void WhenSelectingInteractorViewAdded(IInteractorView obj)
        {
            // On snapped
            var interactor = obj.Data as SnapInteractor;
            if (interactor == null)
            {
                Debug.LogError("Data is not a valid snap interactor!");
                return;
            }

            var teslaCoil = interactor.GetComponentInParent<TeslaCoilSnappable>();
            if (teslaCoil != null)
            {
                Debug.Log("Tesla coil snapped");
                IsTeslaCoilSnapped = true;
                m_onTeslaCoilSnapped?.Invoke();
                teslaCoil.LockObjectCoil();
            }
        }

        private void OnWhenSelectingInteractorViewRemoved(IInteractorView obj)
        {
            // On unsnapped
            var interactor = obj.Data as SnapInteractor;
            if (interactor == null)
            {
                Debug.LogError("Data is not a valid snap interactor!");
                return;
            }

            var teslaCoil = interactor.GetComponentInParent<TeslaCoilSnappable>();
            if (teslaCoil != null)
            {
                Debug.Log("Tesla coil unsnapped");
                IsTeslaCoilSnapped = false;
            }
        }

        private void OnDestroy()
        {
            if (m_snapInteractable != null)
            {
                m_snapInteractable.WhenInteractorViewAdded -= WhenInteractorViewAdded;
                m_snapInteractable.WhenInteractorViewRemoved -= OnWhenInteractorViewRemoved;
                m_snapInteractable.WhenSelectingInteractorViewAdded -= WhenSelectingInteractorViewAdded;
                m_snapInteractable.WhenSelectingInteractorViewRemoved -= OnWhenSelectingInteractorViewRemoved;
            }
        }
    }
}
