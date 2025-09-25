// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Represents the snapping area for a a mini generator snappable.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class MiniGeneratorSnapSocket : MonoBehaviour
    {
        private const int NUMBER_OF_MINI_GENERATORS = 3;

        /// <summary>
        ///     Snap interactable responsible for hover and snap events dispatching.
        /// </summary>
        [SerializeField] private SnapInteractable m_snapInteractable;

        /// <summary>
        ///     Triggered when the mini generator is within the distance threshold for the snapping.
        /// </summary>
        [SerializeField] private UnityEvent m_onMiniGeneratorNearby;

        /// <summary>
        ///     Triggered when the mini generator is  no more within the distance threshold for the snapping.
        ///     Triggered only once.
        /// </summary>
        [SerializeField] private UnityEvent m_onMiniGeneratorNoMoreInRange;

        /// <summary>
        ///     The transform that the snapped object will assume once snapped.
        /// </summary>
        [SerializeField] private GameObject m_targetSnapDestination;

        private MiniGeneratorSnappable m_currentlySnappingGenerator;
        private MiniGeneratorSnappable m_currentlySnappedGenerator;

        /// <summary>
        ///     The snappable mini generator that should be put by the user into this socket.
        /// </summary>
        private MiniGeneratorSnappable[] m_miniGenerators;
        private bool m_isInitialized;

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

        private void OnWhenSelectingInteractorViewRemoved(IInteractorView obj)
        {
            // On unsnapped
            var interactor = obj.Data as SnapInteractor;
            if (interactor == null)
            {
                Debug.LogError("Data is not a valid snap interactor!");
                return;
            }

            Debug.Log("Mini generator unsnapped");
            var generator = interactor.gameObject.GetComponentInParent<MiniGeneratorSnappable>();
            if (generator == null || generator.MiniGeneratorID != m_currentlySnappedGenerator.MiniGeneratorID)
            {
                return;
            }

            // The generator is being grabbed away.
            // Promote automatically this generator to be still in snapping phase,
            // but reset the currently snapped one to null, since no more generators
            // are currently snapped in this area.
            m_currentlySnappingGenerator = m_currentlySnappedGenerator;
            m_currentlySnappedGenerator = null;

            if (m_currentlySnappingGenerator != null && m_currentlySnappingGenerator.ElectricGenerator != null)
            {
                m_currentlySnappingGenerator.ElectricGenerator.IsInPlace = false;
            }
            else
            {
                Debug.LogError("Snapping mini generator was null!");
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

            Debug.Log("Mini generator snapped");
            var generator = interactor.GetComponentInParent<MiniGeneratorSnappable>();

            // As long as this is a mini generator, it is a valid snap.
            // We do not need to check against the mini generator ID according to gameplay design.
            if (generator == null)
            {
                return;
            }

            // We need to snap the generator, and remove it from snapping phase.
            m_currentlySnappedGenerator = m_currentlySnappingGenerator;
            m_currentlySnappingGenerator = null;

            if (m_currentlySnappedGenerator != null && m_currentlySnappedGenerator.ElectricGenerator != null)
            {
                m_currentlySnappedGenerator.ElectricGenerator.IsInPlace = true;
            }
            else
            {
                Debug.LogError("Snapped mini generator was null!");
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

            Debug.Log("Mini generator un-hover");
            var generator = interactor.GetComponentInParent<MiniGeneratorSnappable>();
            if (generator != null && generator.MiniGeneratorID == m_currentlySnappingGenerator.MiniGeneratorID)
            {
                m_currentlySnappingGenerator = null;
            }
            else
            {
                // No mini generator is in range
                m_currentlySnappedGenerator = null;
                m_onMiniGeneratorNoMoreInRange?.Invoke();
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

            Debug.Log("Mini generator hover");
            var generator = interactor.GetComponentInParent<MiniGeneratorSnappable>();
            if (generator != null)
            {
                m_currentlySnappingGenerator = generator;
                m_onMiniGeneratorNearby?.Invoke();
            }
        }

        private void Update()
        {
            if (m_isInitialized)
            {
                return;
            }

            if (m_miniGenerators == null)
            {
                var miniGeneratorsSnappables = FindObjectsByType<MiniGeneratorSnappable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (miniGeneratorsSnappables.Length == NUMBER_OF_MINI_GENERATORS)
                {
                    m_miniGenerators = miniGeneratorsSnappables;
                    if (m_targetSnapDestination == null)
                    {
                        m_targetSnapDestination = gameObject;
                        Debug.Log("Mini generator snap socket has no snap transform set. Automatically setting to self.");
                    }
                    m_isInitialized = true;
                }
                else
                {
                    return;
                }
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
                Debug.LogError("Snap interactable missing in mini generator snap socket!");
            }
        }
    }
}
