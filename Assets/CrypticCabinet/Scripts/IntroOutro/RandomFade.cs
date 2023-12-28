// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Manages the fading effect of multiple fade targets, optionally using an offset for the start of the fading effect.
    /// </summary>
    public class RandomFade : MonoBehaviour
    {
        private struct FadeTargetData
        {
            public FadeTarget FadeTarget;
            public float Offset;
        }

        [SerializeField] private float m_fadeTime = 1.0f;
        [SerializeField] private float m_randomStartOffsetPercentage = 0.2f;
        [SerializeField] private AnimationCurve m_fadeAnimationCurve;

        [SerializeField] private bool m_invertSweepDirection;
        private readonly List<FadeTargetData> m_targets = new();

        private void Start()
        {
            m_targets.Clear();
            var fadeTargets = FindObjectsOfType<FadeTarget>();
            var random = new System.Random(87248752);
            foreach (var fadeTarget in fadeTargets)
            {
                m_targets.Add(new FadeTargetData
                {
                    FadeTarget = fadeTarget,
                    Offset = Mathf.Lerp(0.0f, m_randomStartOffsetPercentage, (float)random.NextDouble())
                });
            }
            UpdateFade();
        }

        public void SetFadeTime(double sweepPercent)
        {
            m_fadeTime = !m_invertSweepDirection ? (float)sweepPercent : (float)(1.0 - sweepPercent);

            UpdateFade();
        }

        private void UpdateFade()
        {
            foreach (var target in m_targets)
            {
                var adjustedTime = Mathf.Lerp(-target.Offset, 1, m_fadeTime);
                target.FadeTarget.UpdateVisuals(m_fadeAnimationCurve.Evaluate(adjustedTime));
            }
        }
    }
}