// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrypticCabinet.Utils;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     Defines the controls for the orrery puzzle.
    /// </summary>
    public class OrreryControl : NetworkBehaviour
    {
        [SerializeField] private PhysicsToggleSwitch m_reset;
        [SerializeField] private AnimationCurve m_motionCurve;
        [SerializeField] private float m_planetMovementQueueDelay = 0.4f;
        [SerializeField] private Animator m_openDrawAnimation;
        [SerializeField] private string m_openDrawAnimKey = "OpenDraw";
        [SerializeField] private AudioSource m_orreryAudio;
        [SerializeField] private AudioSource m_drawerAudio;
        [SerializeField] private AudioSource m_planetCorrectAudio;
        [SerializeField] private List<Planets> m_audioTrackedPlanets = new();
        [SerializeField] private float m_audioStopDelayTime;
        [SerializeField] private GameObject m_directionalApparatus;

        /// <summary>
        ///     Identify the specific planet of the orrery.
        /// </summary>
        public enum Planets
        {
            NONE,
            MERCURY,
            VENUS,
            EARTH,
            MARS,
            JUPITER,
            SATURN,
            SUN
        }

        /// <summary>
        ///     Serializes the planet position, angle and motion time.
        /// </summary>
        [Serializable]
        public struct PlanetStatusData
        {
            public Planets Planet;
            public Transform Position;
            public float InitialAngle;
            public float MotionTime;
            [HideInInspector] public float CurrentAngle;
            public float LastAngle;
            [HideInInspector] public float TargetAngle;
        }

        /// <summary>
        ///     Serialize the planet rotation.
        /// </summary>
        [Serializable]
        public struct PlanetRotation
        {
            public Planets Planet;
            public float Angle;
        }

        /// <summary>
        ///     Serialize the effect of a specific button of the orrery over the planets rotations.
        /// </summary>
        [Serializable]
        public struct ButtonAffectData
        {
            public PhysicsToggleSwitch ToggleSwitch;
            [HideInInspector] public Collider SwitchCollider;
            public int ButtonIndex;
            public List<PlanetRotation> PlanetRotations;
            public bool ButtonCorrectAnswerState;
            [HideInInspector] public bool Applied;
        }

        [SerializeField] private List<PlanetStatusData> m_planetReference = new();
        [SerializeField] private List<ButtonAffectData> m_buttonAffects = new();

        private readonly Queue<int> m_buttonPressedQueue = new();
        private UniTask m_planetMovementProcessingTask;
        private UniTask m_resetPlanetsTask;

        private readonly List<OrreryPlanetSocket> m_planetSockets = new();
        private bool m_placedPlanetsCorrect;
        private bool m_buttonsInCorrectOrder;

        private void Start()
        {
            InitPlanetData();
            InitButtons();
            m_reset.SwitchToggled.AddListener(ResetPlanets);
        }

        private void OnDestroy()
        {
            m_reset.SwitchToggled.RemoveListener(ResetPlanets);
        }

        private bool PlanetMotionEnabled()
        {
            return m_placedPlanetsCorrect && !m_buttonsInCorrectOrder;
        }

        private void ApplyRotation(int buttonIndex)
        {
            m_buttonPressedQueue.Enqueue(buttonIndex);
            if (m_planetMovementProcessingTask.Status != UniTaskStatus.Pending)
            {
                m_planetMovementProcessingTask = ProcessButtonPressed();
            }
        }

        private async UniTask ProcessButtonPressed()
        {
            SetButtonCollidersEnabledRpc(false);
            while (m_buttonPressedQueue.Count > 0)
            {
                var buttonIndex = m_buttonPressedQueue.Dequeue();
                var foundButtonIndex = m_buttonAffects.FindIndex(data => data.ButtonIndex == buttonIndex);
                if (foundButtonIndex >= 0)
                {
                    var buttonAffect = m_buttonAffects[foundButtonIndex];
                    buttonAffect.Applied = !buttonAffect.Applied;
                    m_buttonAffects[foundButtonIndex] = buttonAffect;
                    await SetRotations(buttonAffect);
                    await UniTask.Delay(TimeSpan.FromSeconds(m_planetMovementQueueDelay), DelayType.DeltaTime);
                }
            }

            SetButtonCollidersEnabledRpc(true);
            CheckCorrectState();
        }

        private async UniTask SetRotations(ButtonAffectData data)
        {
            var motionPlanets = new List<UniTask>();
            foreach (var planetRotation in data.PlanetRotations)
            {
                motionPlanets.Add(
                    RotatePlanet(
                        planetRotation.Planet, data.Applied ? planetRotation.Angle : -planetRotation.Angle));
            }

            await UniTask.WhenAll(motionPlanets);
        }

        private async UniTask RotatePlanet(Planets planet, float rotation)
        {
            var foundPlanetIndex = m_planetReference.FindIndex(data => data.Planet == planet);
            if (foundPlanetIndex >= 0)
            {
                var planetStatusData = m_planetReference[foundPlanetIndex];
                planetStatusData.LastAngle = planetStatusData.CurrentAngle;
                planetStatusData.CurrentAngle += rotation;

                await MovePlanetAsync(
                    planetStatusData.Position, planetStatusData.CurrentAngle, rotation,
                    planetStatusData.MotionTime);

                m_planetReference[foundPlanetIndex] = planetStatusData;
            }
        }


        private void ResetPlanets(bool toggled)
        {
            if (PlanetMotionEnabled() && toggled)
            {
                if (m_resetPlanetsTask.Status != UniTaskStatus.Pending &&
                    m_planetMovementProcessingTask.Status != UniTaskStatus.Pending)
                {
                    m_resetPlanetsTask = ResetPlanetsAsync();
                    m_orreryAudio.Play();
                }
            }
            else
            {
                m_reset.SetSwitchState(false, true);
            }
        }

        private async UniTask ResetPlanetsAsync()
        {
            SetButtonCollidersEnabledRpc(false);
            for (var i = 0; i < m_buttonAffects.Count; i++)
            {
                var buttonAffect = m_buttonAffects[i];
                buttonAffect.Applied = false;
                buttonAffect.ToggleSwitch.SetSwitchState(buttonAffect.Applied, true);
                m_buttonAffects[i] = buttonAffect;
            }

            var planetResets = new List<UniTask>();
            for (var i = 0; i < m_planetReference.Count; i++)
            {
                var statusData = m_planetReference[i];
                statusData.CurrentAngle = statusData.InitialAngle;
                var rotationAmount = statusData.CurrentAngle - statusData.Position.localRotation.eulerAngles.y;
                planetResets.Add(
                    MovePlanetAsync(
                        statusData.Position, statusData.CurrentAngle, rotationAmount, statusData.MotionTime));
                m_planetReference[i] = statusData;
            }

            await UniTask.WhenAll(planetResets);
            m_reset.SetSwitchState(false, true);
            m_orreryAudio.Stop();
            SetButtonCollidersEnabledRpc(true);
        }

        private void InitPlanetData()
        {
            for (var i = 0; i < m_planetReference.Count; i++)
            {
                var statusData = m_planetReference[i];
                statusData.CurrentAngle = statusData.InitialAngle;
                statusData.LastAngle = statusData.CurrentAngle;
                statusData.Position.localRotation = Quaternion.Euler(0, statusData.CurrentAngle, 0);
                statusData.TargetAngle = m_buttonAffects.Sum(
                    data =>
                    {
                        return data.PlanetRotations.Where(rotation => rotation.Planet == statusData.Planet).
                            Sum(rotation => rotation.Angle);
                    });
                statusData.TargetAngle += statusData.InitialAngle;
                m_planetReference[i] = statusData;
            }
        }

        private void InitButtons()
        {
            m_reset.SetSwitchState(false, true);

            for (var i = 0; i < m_buttonAffects.Count; i++)
            {
                var buttonAffectData = m_buttonAffects[i];
                buttonAffectData.ToggleSwitch.SwitchToggled.AddListener(
                    _ =>
                    {
                        if (PlanetMotionEnabled())
                        {
                            ApplyRotation(buttonAffectData.ButtonIndex);
                            StartOrreryClickAudioRpc();
                        }
                        else
                        {
                            buttonAffectData.ToggleSwitch.SetSwitchState(buttonAffectData.Applied, true);
                        }
                    });
                buttonAffectData.SwitchCollider = buttonAffectData.ToggleSwitch.GetComponentInChildren<Collider>();
                m_buttonAffects[i] = buttonAffectData;
            }
        }

        private async UniTask MovePlanetAsync(Transform target, float rotationTarget, float rotationAmount,
            float motionTime)
        {
            float time = 0;
            var startAngle = target.localRotation.eulerAngles.y;
            var calculatedAnimationTime = Mathf.Abs(rotationAmount) / 360 * motionTime;

            while (time < calculatedAnimationTime)
            {
                var timePercent = Mathf.InverseLerp(0, calculatedAnimationTime, time);
                var adjustedTimePercent = m_motionCurve.Evaluate(timePercent);
                var animatedAngle = Mathf.Lerp(0, rotationAmount, adjustedTimePercent) + startAngle;
                target.localRotation = Quaternion.Euler(0, animatedAngle, 0);
                time += Time.deltaTime;
                await UniTask.Yield();
            }

            target.localRotation = Quaternion.Euler(0, rotationTarget, 0);
            _ = StartCoroutine(nameof(StopAudio));
        }

        /// <summary>
        ///     Register a planet socket to the list of socket in the orrery.
        /// </summary>
        /// <param name="orreryPlanetSocket">The socket to add to the list of sockets of the orrery</param>
        public void RegisterPlanetSocket(OrreryPlanetSocket orreryPlanetSocket)
        {
            m_planetSockets.Add(orreryPlanetSocket);
        }

        /// <summary>
        ///     Checks if the planets snapped into the orrery puzzle are in the correct position to solve the puzzle.
        /// </summary>
        public void CheckSnappedPlanetsCorrect()
        {
            m_placedPlanetsCorrect = m_planetSockets.All(socket => socket.PlanetCorrect);
        }

        private void CheckCorrectState()
        {
            var fireAudio = m_planetReference.Any(
                data => (Mathf.Abs(data.TargetAngle - data.CurrentAngle) < 0.05f) &&
                        (Mathf.Abs(data.CurrentAngle - data.LastAngle) > 0.001f) &&
                        m_audioTrackedPlanets.Contains(data.Planet));

            if (fireAudio && m_planetCorrectAudio != null)
            {
                m_planetCorrectAudio.Play();
            }

            if (m_buttonAffects.All(data => data.Applied == data.ButtonCorrectAnswerState))
            {
                m_buttonsInCorrectOrder = true;
                OpenOrreryDrawRpc();
            }
        }

        /// <summary>
        ///     Opens the drawer for all connected players via RPC
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void OpenOrreryDrawRpc()
        {
            if (m_directionalApparatus != null)
            {
                m_directionalApparatus.SetActive(true);
            }

            m_openDrawAnimation.SetBool(m_openDrawAnimKey, true);
            m_drawerAudio.Play();
        }

        private IEnumerator StopAudio()
        {
            yield return new WaitForSeconds(m_audioStopDelayTime);
            StopOrreryClickAudioRpc();
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void StartOrreryClickAudioRpc()
        {
            m_orreryAudio.Play();
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void StopOrreryClickAudioRpc()
        {
            m_orreryAudio.Stop();
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void SetButtonCollidersEnabledRpc(bool buttonsEnabled)
        {
            foreach (var affectData in m_buttonAffects)
            {
                affectData.SwitchCollider.enabled = buttonsEnabled;
            }
        }
    }
}