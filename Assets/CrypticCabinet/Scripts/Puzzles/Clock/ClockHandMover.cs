// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Puzzles.Clock
{
    /// <summary>
    ///     Represents the control of the clock hands for the clock puzzle.
    /// </summary>
    public class ClockHandMover : NetworkBehaviour
    {
        [SerializeField] private Transform m_hourHand;
        [SerializeField] private Transform m_minuteHand;
        [Range(0, 12), SerializeField] private float m_startTimeValue;
        [Range(0, 12), SerializeField] private float m_correctTimeValue;
        [Range(0, 1), SerializeField] private float m_correctTimeErrorRange;
        [Range(0, 1), SerializeField] private float m_correctTimeSelectDelay;
        [Range(0, 10), SerializeField] private float m_correctTimeStillMovingAudioDelay = 3.0f;
        [SerializeField] private UnityEvent m_correctTimeSelected;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioSource m_correctTimeSelectedAudio;

        [Networked(OnChanged = nameof(UpdateClock))]
        private float TimeValue { get; set; }

        private float m_lastTimeChangeTime = -100;
        private bool m_correctFired;
        private bool m_instantCorrectFired;

        private bool m_inTimeRange = false;
        private float m_inTimeRangeStartTime = 0;

        private void Start()
        {
            UpdateHands(m_startTimeValue);
        }

        private static void UpdateClock(Changed<ClockHandMover> changed)
        {
            changed.Behaviour.ApplyUpdatedTime();
        }

        public void UpdateHands(float movementDelta)
        {
            TimeValue += movementDelta;
            ApplyUpdatedTime();
        }

        public void SpinnerReleased()
        {
            if (!m_instantCorrectFired)
            {
                if (TimeValue > m_correctTimeValue - m_correctTimeErrorRange &&
                    TimeValue < m_correctTimeValue + m_correctTimeErrorRange)
                {
                    PlayCorrectTimeAudio();
                }
            }
        }

        private void ApplyUpdatedTime()
        {
            while (TimeValue is < 0 or > (float)12.0)
            {
                if (TimeValue < 0)
                {
                    TimeValue += 12;
                }
                else
                {
                    TimeValue -= 12.0f;
                }
            }

            var min = TimeValue - (float)Math.Truncate(TimeValue);
            var hourPercent = Mathf.InverseLerp(0, 12, TimeValue);

            m_minuteHand.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0, 360, min), Vector3.forward);
            m_hourHand.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0, 360, hourPercent), Vector3.forward);

            m_lastTimeChangeTime = Time.time;

            if (TimeValue > m_correctTimeValue - m_correctTimeErrorRange &&
                TimeValue < m_correctTimeValue + m_correctTimeErrorRange)
            {
                if (!m_inTimeRange)
                {
                    m_inTimeRangeStartTime = Time.time;
                }
                m_inTimeRange = true;
            }
            else
            {
                m_inTimeRange = false;
                m_instantCorrectFired = false;
            }
        }

        private void PlayCorrectTimeAudio()
        {
            if (!m_instantCorrectFired)
            {
                if (m_correctTimeSelectedAudio != null)
                {
                    m_correctTimeSelectedAudio.Play();
                }

                m_instantCorrectFired = true;
            }
        }

        private void Update()
        {
            if (!m_instantCorrectFired && m_inTimeRange)
            {
                if (Time.time >= m_inTimeRangeStartTime + m_correctTimeStillMovingAudioDelay)
                {
                    PlayCorrectTimeAudio();
                }
            }

            if (Time.time < m_lastTimeChangeTime + m_correctTimeSelectDelay)
            {
                return;
            }

            if (TimeValue > m_correctTimeValue - m_correctTimeErrorRange &&
                TimeValue < m_correctTimeValue + m_correctTimeErrorRange)
            {
                if (!m_correctFired)
                {
                    m_correctTimeSelected?.Invoke();
                    m_audioSource.Play();
                }

                m_correctFired = true;
            }
            else
            {
                m_correctFired = false;
            }
        }
    }
}