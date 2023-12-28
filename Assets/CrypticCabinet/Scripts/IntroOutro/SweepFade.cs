// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using CrypticCabinet.Utils;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Controls the sweep fade effect for the specified fade targets.
    /// </summary>
    public class SweepFade : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float m_sweepTime;
        [SerializeField] private float m_sweepAngle = 30;
        [SerializeField] private bool m_invertSweepDirection = true;
        [SerializeField] private bool m_invertObjectReveal = true;
        [SerializeField] private Transform m_rotationStartPos;

        private readonly List<FadeTarget> m_targets = new();
        private Vector3 m_calculatedCenter;

        private void Start()
        {
            ForcePlaceObjects();
            m_targets.Clear();
            m_targets.AddRange(FindObjectsOfType<FadeTarget>());
            CalculateCenterPoint();
            UpdateSweep();
        }

        public void SetSweepTime(double sweepPercent)
        {
            m_sweepTime = !m_invertSweepDirection ? (float)sweepPercent : (float)(1.0 - sweepPercent);

            UpdateSweep();
        }

        private void UpdateSweep()
        {
            var sweepAnglePercentage = m_sweepAngle / 360.0f;
            var halfAngle = sweepAnglePercentage * 0.5f;
            var currentSweepTime = Mathf.Lerp(-halfAngle, 1 + halfAngle, m_sweepTime);

            var sweepStartPos = m_rotationStartPos.position;
            sweepStartPos.y = 0;
            var sweepStartDirection = (m_calculatedCenter - sweepStartPos).normalized;

            foreach (var target in m_targets)
            {
                UpdateTarget(currentSweepTime, halfAngle, target, sweepStartDirection);
            }
        }

        private void UpdateTarget(float sweepPercentage, float halfSweepAnglePercentage, FadeTarget target,
            Vector3 sweepStartDirection)
        {
            target.OnFadeStart?.Invoke();
            var targetPos = target.Transform.position;
            targetPos.y = 0;

            var angle = Vector3.SignedAngle(
                sweepStartDirection, (targetPos - m_calculatedCenter).normalized, Vector3.up);
            angle += 180;

            var anglePercent = angle / 360.0f;

            var inverseLerp = m_invertObjectReveal
                ? InverseLerpUnClamped(
                    anglePercent - halfSweepAnglePercentage, anglePercent + halfSweepAnglePercentage, sweepPercentage)
                : InverseLerpUnClamped(
                    anglePercent + halfSweepAnglePercentage, anglePercent - halfSweepAnglePercentage, sweepPercentage);
            target.UpdateVisuals(inverseLerp);
        }

        private static float InverseLerpUnClamped(float a, float b, float value)
        {
            return Mathf.Abs(a - b) > 0.00001f ? ((value - a) / (b - a)) : 0.0f;
        }

        private void CalculateCenterPoint()
        {
            if (m_targets.Count == 0)
            {
                m_calculatedCenter = Vector3.zero;
            }

            var centerPoint = Vector3.zero;

            foreach (var fadeTarget in m_targets)
            {
                centerPoint += fadeTarget.Transform.position;
            }

            centerPoint /= m_targets.Count;
            centerPoint.y = 0;
            m_calculatedCenter = centerPoint;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            var sweepStartPos = m_rotationStartPos.position;
            sweepStartPos.y = 0;
            var direction = sweepStartPos - m_calculatedCenter;
            direction = Quaternion.AngleAxis(Mathf.Lerp(0, 360, m_sweepTime), Vector3.up) * direction;
            Gizmos.DrawRay(m_calculatedCenter, direction);
        }

        private void ForcePlaceObjects()
        {
            var simpleSceneObjectPlacers = GetComponentsInChildren<SimpleSceneObjectPlacer>();
            foreach (var placer in simpleSceneObjectPlacers)
            {
                placer.PlaceUsingSpawner();
            }
        }
    }
}