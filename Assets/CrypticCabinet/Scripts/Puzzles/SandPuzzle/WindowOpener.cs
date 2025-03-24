// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using CrypticCabinet.SceneManagement;
using CrypticCabinet.Utils;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Defines the mechanism that opens the window of the sand puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class WindowOpener : NetworkBehaviour, IStateAuthorityChanged
    {
        private const string ANIMATION_PARAMETER_FLOAT_NAME = "MotionTime";

        [SerializeField] private float m_timeToFullyClose = 2.0f;
        [SerializeField] private float m_deadZoneDistanceSqr = 0.7f;
        [SerializeField] private float m_pullDistance = 0.3f;
        [SerializeField] private Transform m_windowRoot;
        [SerializeField] private GameObject m_ropeRoot;
        [SerializeField] private Transform m_ropeStartTransform;
        [SerializeField] private Animator m_windowOpeningAnimator;
        [SerializeField] private Hook m_hook;
        [SerializeField] private Rope m_rope;

        [Networked(OnChanged = nameof(UpdateWindowOpening))]
        private float NormalisedWindowOpenness { get; set; }

        [Networked(OnChanged = nameof(UpdateWindowOpening))]
        private bool FullBucketAttached { get; set; }

        private float m_initialDistanceSqr;
        private float m_oldNormalisedValue;
        private bool m_enableWindowMoverCoroutine;

        /// <summary>
        ///     Event that will fire as the window is opening, giving a normalised value
        ///     representing how much the window is opened (0.0 close, 1.0 open)
        /// </summary>
        [SerializeField] private UnityEvent<float> m_onWindowOpening;

        private IEnumerator Start()
        {
            PlaceUsingSpawner();
            if (m_hook == null)
            {
                Debug.LogError("Error, hook component not referenced on start");
            }
            m_hook.OnGameObjectAttached.AddListener(OnObjectOnHook);
            m_hook.OnGameObjectRemoved.AddListener(OnObjectUnHooked);


            if (m_windowOpeningAnimator == null)
            {
                Debug.LogError("Error, animator component not referenced on start");
            }

            // Setting animation to barely open to show the player a halo coming from the window.
            m_windowOpeningAnimator.SetFloat(Animator.StringToHash(ANIMATION_PARAMETER_FLOAT_NAME), Mathf.Clamp(NormalisedWindowOpenness, 0.2f, 0.9999f));

            // Delay starting the enum to allow the rope to calm
            yield return new WaitForSeconds(3);

            // Only start this coroutine when something is actively opening the window. Stop it when interaction ceases.
            if (HasStateAuthority)
            {
                m_enableWindowMoverCoroutine = true;
                _ = StartCoroutine(nameof(WindowMover));
            }
        }

        private void OnDestroy()
        {
            m_enableWindowMoverCoroutine = false;
            StopCoroutine(nameof(WindowMover));
            m_hook.OnGameObjectAttached.RemoveListener(OnObjectOnHook);
            m_hook.OnGameObjectRemoved.AddListener(OnObjectUnHooked);
        }

        public void StateAuthorityChanged()
        {
            Debug.Log("New State Authority for Window: " + HasStateAuthority);
            if (HasStateAuthority)
            {
                _ = StartCoroutine(nameof(WindowMover));
            }
            else
            {
                StopCoroutine(nameof(WindowMover));
            }
        }

        private void OnObjectOnHook(GameObject go)
        {
            if (go == null)
            {
                Debug.LogError("WindowOpener: On hook object is null!");
                return;
            }
            var bucket = go.GetComponent<SandBucket>();
            if (bucket != null)
            {
                FullBucketAttached = bucket.IsFull();

                if (m_rope != null)
                {
                    m_rope.AddWeightToEnd = FullBucketAttached;
                }
            }
        }

        private void OnObjectUnHooked(GameObject go)
        {
            var bucket = go.GetComponent<SandBucket>();
            if (bucket != null)
            {
                FullBucketAttached = false;

                if (m_rope != null)
                {
                    m_rope.AddWeightToEnd = false;
                }
            }
        }

        private IEnumerator WindowMover()
        {
            yield return null;

            while (m_enableWindowMoverCoroutine)
            {
                yield return null;

                if (HasStateAuthority)
                {
                    if (!FullBucketAttached)
                    {
                        var currentDistanceSqr =
                            (m_ropeStartTransform.position - m_ropeRoot.transform.position).sqrMagnitude;
                        if (currentDistanceSqr > (m_deadZoneDistanceSqr * m_deadZoneDistanceSqr))
                        {
                            NormalisedWindowOpenness = Mathf.InverseLerp(
                                m_deadZoneDistanceSqr * m_deadZoneDistanceSqr, m_pullDistance * m_pullDistance,
                                currentDistanceSqr);
                        }
                        else
                        {
                            NormalisedWindowOpenness -= Time.deltaTime;
                        }

                        if (NormalisedWindowOpenness < m_oldNormalisedValue)
                        {
                            NormalisedWindowOpenness = m_oldNormalisedValue - Time.deltaTime / m_timeToFullyClose;
                        }
                    }
                    else
                    {
                        NormalisedWindowOpenness += Time.deltaTime;
                    }

                    NormalisedWindowOpenness = Mathf.Clamp(NormalisedWindowOpenness, 0f, 1f);

                    if (Math.Abs(m_oldNormalisedValue - NormalisedWindowOpenness) > 0.001f)
                    {
                        SetWindowAnimationTime();
                    }
                }
                m_oldNormalisedValue = NormalisedWindowOpenness;
            }
        }

        private void SetWindowAnimationTime()
        {
            m_windowOpeningAnimator.SetFloat(Animator.StringToHash(ANIMATION_PARAMETER_FLOAT_NAME), Mathf.Clamp(NormalisedWindowOpenness, 0.2f, 0.9999f));
            m_onWindowOpening.Invoke(NormalisedWindowOpenness);
        }

        public static void UpdateWindowOpening(Changed<WindowOpener> changed)
        {
            changed.Behaviour.SetWindowAnimationTime();
        }

        private void PlaceUsingSpawner()
        {
            List<ObjectPlacementManager.SceneObject> positions = null;
            if (ObjectPlacementManager.Instance != null)
            {
                positions = ObjectPlacementManager.Instance.RequestObjects(ObjectPlacementManager.LoadableSceneObjects.WINDOW_PULLY);
            }

            if (m_ropeRoot == null || m_windowRoot == null || positions is not { Count: > 0 })
            {
                return;
            }

            m_ropeRoot.SetActive(false);
            m_windowRoot.rotation = positions[0].WallRotation * Quaternion.Euler(0, 180, 0);
            m_windowRoot.position = positions[0].WallPosition;
            m_ropeRoot.SetActive(true);

            var ceilingHeightDetector = FindObjectOfType<CeilingHeightDetector>();
            CeilingPlacementUtils.SetYPositionToCeiling(m_ropeRoot.transform, ceilingHeightDetector);
            m_rope.Reset();
        }
    }
}
