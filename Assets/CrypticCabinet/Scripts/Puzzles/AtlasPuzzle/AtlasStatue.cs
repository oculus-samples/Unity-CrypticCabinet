// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.AtlasPuzzle
{
    /// <summary>
    ///     Controls the behavior of the Atlas statue.
    /// </summary>
    [RequireComponent(typeof(SnapInteractable))]
    public class AtlasStatue : NetworkBehaviour
    {
        private const string ANIMATION_PARAMETER_FLOAT_NAME = "MotionTime";

        [SerializeField] private UnityEvent<bool> m_onStoneInserted;
        [SerializeField] private UnityEvent m_onPuzzleComplete;
        [SerializeField] private UnityEvent<float> m_animationNormalTime;
        [SerializeField] private GameObject m_statueGO;
        [SerializeField] private float m_animationTime = 2f;
        [SerializeField] private Collider m_sunCollider;

        [SerializeField] private Animator m_standAnimator;

        [Networked(OnChanged = nameof(UpdatePuzzleComplete))]
        private bool PuzzleComplete { get; set; }

        [Networked(OnChanged = nameof(UpdateCurrentOpenness))]
        private float CurrentOpenness { get; set; }

        private float m_previousOpenness;

        [Networked]
        private bool IsOpening { get; set; }

        private Coroutine m_animationCoroutine;
        private bool m_isAtlasStatueSpawned;

        private void Start()
        {
            IsOpening = false;
            var snapper = GetComponent<SnapInteractable>();
            snapper.WhenSelectingInteractorViewAdded += WhenSelectingInteractorViewAdded;
            snapper.WhenSelectingInteractorViewRemoved += OnWhenSelectingInteractorViewRemoved;
            m_previousOpenness = CurrentOpenness;

            if (m_statueGO == null)
            {
                Debug.LogError("Error, no statue gameobject set");
            }

            if (m_sunCollider == null)
            {
                Debug.LogError("Error, no sun collider set");
            }

            if (m_standAnimator == null)
            {
                Debug.LogError("Error, animator component not referenced on start");
            }

            //Setting animation to barely open to show the player a halo coming from the window.
            m_standAnimator.SetFloat(
                Animator.StringToHash(ANIMATION_PARAMETER_FLOAT_NAME), Mathf.Clamp(CurrentOpenness, 0.0f, 0.999f));

            m_isAtlasStatueSpawned = true;
            _ = StartCoroutine(nameof(UpdateOpenness));
        }

        private void OnDestroy()
        {
            var snapper = GetComponent<SnapInteractable>();
            snapper.WhenSelectingInteractorViewAdded -= WhenSelectingInteractorViewAdded;
            snapper.WhenSelectingInteractorViewRemoved -= OnWhenSelectingInteractorViewRemoved;

            m_isAtlasStatueSpawned = false;
            StopCoroutine(nameof(UpdateOpenness));
        }

        private void WhenSelectingInteractorViewAdded(IInteractorView view)
        {
            var snapable = view.Data as SnapInteractor;
            if (snapable == null)
            {
                return;
            }

            var atlasStoneComp = snapable.gameObject.GetComponentInParent<AtlasStone>();
            if (atlasStoneComp != null)
            {
                IsOpening = true;
                Debug.Log("Atlas stone in position!");
                m_onStoneInserted?.Invoke(true);
                if (!PuzzleComplete)
                {
                    PuzzleComplete = true;
                }
            }
        }

        private void OnWhenSelectingInteractorViewRemoved(IInteractorView view)
        {
            var snapable = view.Data as SnapInteractor;
            if (snapable == null)
            {
                return;
            }

            var atlasStoneComp = snapable.gameObject.GetComponentInParent<AtlasStone>();
            if (atlasStoneComp != null)
            {
                IsOpening = false;
                Debug.Log("Atlas stone removed!");
                m_onStoneInserted?.Invoke(false);
            }
        }

        private void SetAnimationTime()
        {
            if (Mathf.Abs(m_previousOpenness - CurrentOpenness) > 0.001)
            {
                if (CurrentOpenness >= 1.0f)
                {
                    if (m_sunCollider)
                    {
                        m_sunCollider.enabled = true;
                    }
                }

                m_standAnimator.SetFloat(
                    Animator.StringToHash(ANIMATION_PARAMETER_FLOAT_NAME), Mathf.Min(CurrentOpenness, 0.99f));

                m_animationNormalTime?.Invoke(CurrentOpenness);
            }
        }

        public static void UpdateCurrentOpenness(Changed<AtlasStatue> changed)
        {
            changed.Behaviour.SetAnimationTime();
        }

        public static void UpdatePuzzleComplete(Changed<AtlasStatue> changed)
        {
            if (changed.Behaviour.PuzzleComplete)
            {
                changed.Behaviour.m_onPuzzleComplete?.Invoke();
            }
        }

        /// <summary>
        ///     Coroutine to update the openness of the atlas statue door over time.
        /// </summary>
        private IEnumerator UpdateOpenness()
        {
            var timeRatio = Time.deltaTime / m_animationTime;
            while (m_isAtlasStatueSpawned)
            {
                if (HasStateAuthority)
                {
                    CurrentOpenness += (IsOpening ? 1 : -1) * timeRatio;
                    CurrentOpenness = Mathf.Clamp01(CurrentOpenness);
                    SetAnimationTime();
                }
                yield return null;
            }
        }
    }
}