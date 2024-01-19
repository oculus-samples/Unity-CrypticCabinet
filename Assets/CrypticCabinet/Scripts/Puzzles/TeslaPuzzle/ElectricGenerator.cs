// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Audio;
using CrypticCabinet.Utils.InteractiveObjects;
using Fusion;
using UnityEngine;
using UnityEngine.VFX;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Convenient enum to identify the different generators in the Tesla puzzle.
    /// </summary>
    public enum ElectricGeneratorID
    {
        UNKNOWN,
        MG1,
        MG2,
        MG3,
        MAIN
    }

    /// <summary>
    ///     Represents an object that can emit electricity arcs from a start to an end position.
    ///     The end position is based on other electric generators nearby, which are distant not
    ///     less than the m_electricMaxRadius.
    ///     When the generator's electricity is disabled, no electricity arc will be shown.
    /// </summary>
    public class ElectricGenerator : NetworkBehaviour
    {
        private const string VISUAL_EFFECT_CONNECT_NAME = "Connect?";
        private const string VISUAL_EFFECT_STRONG_NAME = "Strong?";

        public ElectricGeneratorID GeneratorID => m_electricGeneratorId;
        public int Voltage => m_voltage;

        /// <summary>
        ///     True if the generator is snapped to the expected position.
        /// </summary>
        public bool IsInPlace { get; set; }

        /// <summary>
        ///     The voltage of this generator.
        /// </summary>
        [SerializeField] private int m_voltage;
        /// <summary>
        ///     An ID representing this electric generator.
        /// </summary>
        [SerializeField] private ElectricGeneratorID m_electricGeneratorId;
        /// <summary>
        ///     Prefab representing the electricity VFX with a Bezier curve from start to end point.
        /// </summary>
        [SerializeField] private VisualEffect m_electricityVfx;
        /// <summary>
        ///     The starting point from which the electric arc will be produced.
        /// </summary>
        [SerializeField] private Transform m_electricArcStart;
        /// <summary>
        ///     The ending point to which the electric arc will point to and finish its journey.
        /// </summary>
        [SerializeField] private Transform m_electricArtEnd;
        /// <summary>
        ///     The max radius below which an electric arc will be triggered when another generator
        ///     is nearby the game object of this script.
        /// </summary>
        [SerializeField] private float m_electricMaxRadius = 0.45f;
        /// <summary>
        ///     If true, the generator will show the electricity arc when another generator is nearby.
        ///     If false, no electric arc will be shown.
        /// </summary>
        [Networked(OnChanged = nameof(UpdatePowerStatus))]
        private bool IsElectricityEnabled { get; set; }

        /// <summary>
        ///     Left light bulb showing if the generator is on or off.
        /// </summary>
        [SerializeField] private MeshRenderer m_powerStatusLightBulbLeft;
        /// <summary>
        ///     Right light bulb showing if the generator is on or off.
        /// </summary>
        [SerializeField] private MeshRenderer m_powerStatusLightBulbRight;

        [SerializeField] private Material m_powerOnMaterial;
        [SerializeField] private Material m_powerOffMaterial;

        /// <summary>
        ///     The generator to which this generator is currently connected to.
        /// </summary>
        private ElectricGenerator m_connectedGenerator;

        /// <summary>
        ///     Reference to the manager that holds the list of all generators.
        /// </summary>
        private ElectricGeneratorsManager m_manager;
        private bool m_hasValidManager;

        [SerializeField] private MeshButtonSwitch m_toggleSwitch;
        [SerializeField] private MiniGeneratorAudio m_miniGeneratorAudioScript;

        /// <summary>
        ///     Injects the electric generator manager that handles the tesla puzzle pieces.
        /// </summary>
        /// <param name="manager"></param>
        public void InjectElectricGeneratorManager(ElectricGeneratorsManager manager)
        {
            m_manager = manager;
            m_hasValidManager = manager != null;
        }

        private void Start()
        {
            m_electricityVfx.gameObject.SetActive(false);
            if (m_toggleSwitch == null)
            {
                m_toggleSwitch = GetComponentInChildren<MeshButtonSwitch>();
            }

            UpdatePowerStatusUI();
        }

        private void Update()
        {
            if (!m_hasValidManager)
            {
                Debug.Log("ElectricGenerator has no manager yet, checking again next frame");
                return;
            }

            if (!IsElectricityEnabled)
            {
                return;
            }

            var managerMainGenerator = m_manager.MainGenerator;
            if (managerMainGenerator.IsTeslaCoilPlacedCorrectly)
            {
                var mainGenPos = managerMainGenerator.ArcTargetPos;
                var distanceToMainGen = Vector3.Distance(m_electricArcStart.position, mainGenPos.position);
                if (distanceToMainGen < m_electricMaxRadius)
                {
                    m_electricArtEnd.position = mainGenPos.position;
                    m_electricityVfx.SetBool(VISUAL_EFFECT_STRONG_NAME, true);
                    m_electricityVfx.SetBool(VISUAL_EFFECT_CONNECT_NAME, true);
                    m_miniGeneratorAudioScript.ChangeClip(true);
                    return;
                }
            }

            // Look for closest generator not yet connected, within the range.
            ElectricGenerator candidate = null;
            var minDistance = float.MaxValue;
            var currentPosition = transform.position;
            var foundValidTarget = false;
            foreach (var miniGenerator in m_manager.MiniGenerators)
            {
                if (miniGenerator.m_electricGeneratorId == m_electricGeneratorId)
                {
                    continue;
                }

                var position = miniGenerator.transform.position;
                var distance = Vector3.Distance(currentPosition, position);
                if (distance < minDistance && miniGenerator.m_connectedGenerator != this && miniGenerator.IsElectricityEnabled)
                {
                    minDistance = distance;
                    candidate = miniGenerator;
                    foundValidTarget = true;
                }
            }

            if (foundValidTarget)
            {
                m_connectedGenerator = candidate;
                m_electricArtEnd.position = m_connectedGenerator.m_electricArcStart.position;
                m_electricityVfx.SetBool(VISUAL_EFFECT_STRONG_NAME, true);
                m_electricityVfx.SetBool(VISUAL_EFFECT_CONNECT_NAME, true);
                m_miniGeneratorAudioScript.ChangeClip(true);
                return;
            }

            m_electricityVfx.SetBool(VISUAL_EFFECT_STRONG_NAME, false);
            m_electricityVfx.SetBool(VISUAL_EFFECT_CONNECT_NAME, false);
            m_miniGeneratorAudioScript.ChangeClip(false);
        }

        private void UpdatePowerStatusUI()
        {
            var powerStatusMaterial = IsElectricityEnabled ? m_powerOnMaterial : m_powerOffMaterial;
            if (m_powerStatusLightBulbLeft != null)
            {
                m_powerStatusLightBulbLeft.material = powerStatusMaterial;

            }

            if (m_powerStatusLightBulbRight != null)
            {
                m_powerStatusLightBulbRight.material = powerStatusMaterial;
            }
            if (m_toggleSwitch != null)
            {
                if (IsElectricityEnabled)
                {
                    m_toggleSwitch.SwitchOn();
                }
                else
                {
                    m_toggleSwitch.SwitchOff();
                }
            }
        }

        /// <summary>
        ///     Switch the electric generator on, for all users in game session via RPC.
        /// </summary>
        public void SwitchOn()
        {
            IsElectricityEnabled = true;
            RpcSetElectricityEnabled(IsElectricityEnabled);
        }

        /// <summary>
        ///     Switch the electric generator off, for all users in game session via RPC.
        /// </summary>
        public void SwitchOff()
        {
            IsElectricityEnabled = false;
            RpcSetElectricityEnabled(IsElectricityEnabled);
        }

        /// <summary>
        ///     Updates the power status UI for the electric generator.
        /// </summary>
        /// <param name="changed">The electric generator that changed its status.</param>
        public static void UpdatePowerStatus(Changed<ElectricGenerator> changed)
        {
            changed.Behaviour.UpdatePowerStatusUI();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RpcSetElectricityEnabled(bool electricEnabled)
        {
            IsElectricityEnabled = electricEnabled;
            if (IsElectricityEnabled)
            {
                m_electricityVfx.gameObject.SetActive(true);
                m_electricityVfx.SetBool(VISUAL_EFFECT_STRONG_NAME, false);
                m_electricityVfx.SetBool(VISUAL_EFFECT_CONNECT_NAME, false);
                m_miniGeneratorAudioScript.ChangeClip(false);
            }
            else
            {
                m_electricityVfx.gameObject.SetActive(false);
            }

        }

        /// <summary>
        ///     Return true if the electric generator is switched on, false otherwise.
        /// </summary>
        /// <returns>True if the electric generator is switched on, false otherwise.</returns>
        public bool IsSwitchedOn()
        {
            return IsElectricityEnabled;
        }

        /// <summary>
        ///     Toggle the power switch for this electric generator. If switched on it is switched off, and viceversa.
        /// </summary>
        public void TogglePowerSwitch()
        {
            if (IsElectricityEnabled)
            {
                SwitchOff();
            }
            else
            {
                SwitchOn();
            }
        }
    }
}
