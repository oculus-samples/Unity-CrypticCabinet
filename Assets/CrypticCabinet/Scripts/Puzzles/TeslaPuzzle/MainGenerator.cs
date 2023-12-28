// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.VFX;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Describes the behaviour for the Main electric generator of the Tesla puzzle.
    /// </summary>
    public class MainGenerator : NetworkBehaviour
    {
        private const string VISUAL_EFFECT_CONNECT_NAME = "Connect?";
        private const string VISUAL_EFFECT_STRONG_NAME = "Strong?";

        /// <summary>
        ///     The voltage required for the Tesla puzzle to be resolved.
        /// </summary>
        [SerializeField] private int m_currentVoltageThresholdNeeded = 10000;

        [SerializeField] private VisualEffect m_electricArcPrefabDirectionalApparatus;
        [SerializeField] private GameObject m_electricArcEndDirectionalApparatus;
        [SerializeField] private VisualEffect m_electricalOverloadVfx;
        [SerializeField] private AudioSource m_electricalOverloadAudio;

        /// <summary>
        ///     The GameObject used to show the user the current voltage of the Main electric generator.
        /// </summary>
        [SerializeField] private GameObject m_currentVoltageIndicator;
        [SerializeField] private float m_minAngleVoltageIndicator = 255.0f;
        [SerializeField] private float m_maxAngleVoltageIndicator = 465.0f;
        [SerializeField] private int m_minVoltage;

        /// <summary>
        ///     The max voltage supported by this Main electric generator.
        /// </summary>
        [SerializeField] private int m_maxVoltage = 17000;

        /// <summary>
        ///     The minimum distance required to power on the directional apparatus when the voltage is correct.
        /// </summary>
        [SerializeField] private float m_directionalApparatusDistanceThreshold = 0.5f;

        /// <summary>
        ///     How many seconds the voltage meter indicator needs to rotate until it reaches the correct
        ///     rotation representing the current voltage level.
        /// </summary>
        [SerializeField] private float m_voltageIndicatorRotationDuration = 1f;

        [Networked] private bool IsTeslaCoilInPlace { get; set; }
        public bool IsTeslaCoilPlacedCorrectly => IsTeslaCoilInPlace;
        [Networked] private bool IsMainGeneratorEnabled { get; set; }
        private int m_currentVoltage;

        private float m_directionalApparatusDistanceThresholdSquared;
        private bool m_hasValidDirectionalApparatus;
        private Transform m_electricArcEndDirectionalTransform;
        private Transform m_directionalApparatusTransform;
        private float m_currentDirectionalApparatusDistanceSquared;

        /// <summary>
        ///     Reference to the manager that holds the list of all generators.
        /// </summary>
        private ElectricGeneratorsManager m_manager;
        private bool m_hasValidManager;
        private int m_lastVoltage;
        private bool m_lastDirectionalApparatusActiveStateEnabled;

        // Coroutine reference for smooth rotation of voltage meter
        private Coroutine m_currentVoltageIndicatorRotationCoroutine;
        [SerializeField] private Transform m_arcTargetPos;
        public Transform ArcTargetPos => m_arcTargetPos;

        public void InjectElectricGeneratorManager(ElectricGeneratorsManager manager)
        {
            m_manager = manager;
            m_hasValidManager = manager != null;

            // Check if a valid directional apparatus exists on manager.
            m_hasValidDirectionalApparatus =
                manager.DirectionalApparatus != null && m_electricArcEndDirectionalApparatus != null;

            if (m_hasValidDirectionalApparatus)
            {
                m_directionalApparatusTransform = manager.DirectionalApparatus.GetDirectionalApparatusElectricContact();
                m_electricArcEndDirectionalTransform = m_electricArcEndDirectionalApparatus.transform;
            }
            else
            {
                Debug.LogError("No valid directional apparatus found on manager!");
            }
        }

        private void Start()
        {
            m_currentVoltage = 0;
            m_lastVoltage = -1;
            m_directionalApparatusDistanceThresholdSquared =
                m_directionalApparatusDistanceThreshold * m_directionalApparatusDistanceThreshold;
        }

        /// <summary>
        ///     Called when the tesla coil is in place.
        /// </summary>
        public void TeslaCoilIsInPlace()
        {
            IsTeslaCoilInPlace = true;
        }

        /// <summary>
        ///     Called when the tesla coil is not in place.
        /// </summary>
        public void TeslaCoilIsNotInPlace()
        {
            IsTeslaCoilInPlace = false;
        }

        private void Update()
        {
            if (!m_hasValidManager)
            {
                Debug.Log("No valid manager set on main generator yet. Checking again next frame");
                return;
            }

            // Update position of electric arc for directional apparatus, if required.
            if (m_hasValidDirectionalApparatus)
            {
                var position = m_directionalApparatusTransform.position;
                m_electricArcEndDirectionalTransform.position = position;
                m_currentDirectionalApparatusDistanceSquared = (position - transform.position).sqrMagnitude;
            }

            if (IsTeslaCoilInPlace)
            {
                // Ensure the total voltage of the enabled generators is matching the threshold.
                // If it does, switch on, otherwise edge cases.
                m_currentVoltage = 0;
                foreach (var generator in m_manager.MiniGenerators)
                {
                    m_currentVoltage += generator.IsSwitchedOn() && generator.IsInPlace ? generator.Voltage : 0;
                }
                // Clamp the voltage between min and max allowed by main generator
                m_currentVoltage = Mathf.Clamp(m_currentVoltage, m_minVoltage, m_maxVoltage);

                if (m_lastVoltage != m_currentVoltage)
                {
                    UpdateVoltageMeasurement();
                    UpdateDirectionalApparatusStatus();
                }
            }
            else
            {
                if (m_currentVoltage > 0)
                {
                    m_currentVoltage = 0;
                    UpdateVoltageMeasurement();

                    if (IsMainGeneratorEnabled)
                    {
                        SwitchOff();
                    }
                }
            }

            if (IsMainGeneratorEnabled && m_hasValidDirectionalApparatus && !m_lastDirectionalApparatusActiveStateEnabled &&
                m_lastDirectionalApparatusActiveStateEnabled != m_directionalApparatusTransform.gameObject.activeInHierarchy)
            {
                // Update status again, to avoid missed case where main generator is already active but directional
                // apparatus is still inside the orrery, then taken out by the user.
                UpdateDirectionalApparatusStatus();
                m_lastDirectionalApparatusActiveStateEnabled =
                    m_directionalApparatusTransform.gameObject.activeInHierarchy;
            }
        }

        /// <summary>
        ///     Update voltage meter indicator rotation, then handles internally the new voltage.
        /// </summary>
        private void UpdateVoltageMeasurement()
        {
            if (m_currentVoltageIndicator == null)
            {
                Debug.LogError("No voltage indicator set on main generator!");
                return;
            }

            Debug.Log($"Main generator has now voltage {m_currentVoltage}");

            // Cancel the previous rotationCoroutine if it's running
            if (m_currentVoltageIndicatorRotationCoroutine != null)
            {
                StopCoroutine(m_currentVoltageIndicatorRotationCoroutine);
            }

            // Start the new rotationCoroutine for smooth rotation
            m_currentVoltageIndicatorRotationCoroutine = StartCoroutine(nameof(SmoothRotateVoltageIndicator));
        }

        /// <summary>
        ///     Coroutine for smooth rotation of voltage indicator
        /// </summary>
        private IEnumerator SmoothRotateVoltageIndicator()
        {
            // Calculate the target rotation around Z-axis based on m_voltage
            var normalizedVoltage = (m_currentVoltage - m_minVoltage) / (float)(m_maxVoltage - m_minVoltage);
            var targetRotationZ = Mathf.Lerp(m_minAngleVoltageIndicator, m_maxAngleVoltageIndicator, normalizedVoltage);

            // Get the current rotation
            var startRotation = m_currentVoltageIndicator.transform.localRotation;
            var startRotationEuler = startRotation.eulerAngles;
            var startRotationX = startRotationEuler.x;
            var startRotationY = startRotationEuler.y;
            var startRotationZ = startRotationEuler.z;

            // Calculate delta angle rotation
            var deltaVoltage = m_currentVoltage - m_lastVoltage;
            var deltaVoltageAbs = Mathf.Abs(deltaVoltage);
            var totalAngle = Mathf.Abs(m_maxAngleVoltageIndicator - m_minAngleVoltageIndicator);
            var percentage = (float)Mathf.Clamp(deltaVoltageAbs, m_minVoltage, m_maxVoltage) / m_maxVoltage;
            var deltaDirection = Mathf.Sign(deltaVoltage);
            var deltaAngle = deltaDirection * percentage * totalAngle;

            // Update the last voltage to the current one
            m_lastVoltage = m_currentVoltage;

            // Time elapsed
            var elapsedTime = 0f;

            while (elapsedTime < m_voltageIndicatorRotationDuration)
            {
                // Calculate the interpolation factor
                var t = elapsedTime / m_voltageIndicatorRotationDuration;

                var interpolatedZ = startRotationZ + t * deltaAngle;
                m_currentVoltageIndicator.transform.localRotation =
                    Quaternion.Euler(startRotationX, startRotationY, interpolatedZ);

                // Fence against changes in values while the coroutine operates.
                // Avoids that the interpolation continues over the bounds of the target rotation.
                if (deltaDirection > 0 && interpolatedZ > targetRotationZ)
                {
                    break;
                }

                // Update the elapsed time
                elapsedTime += Time.deltaTime;

                yield return null; // Wait for the next frame

            }

            // Ensure the rotation ends at the exact target value to avoid precision errors
            m_currentVoltageIndicator.transform.localRotation =
                Quaternion.Euler(startRotationX, startRotationY, targetRotationZ);

            // Now that the voltage meter is showing the correct voltage, continue by handling
            // the voltage value accordingly.
            HandleVoltageChange();

            // Set the rotationCoroutine to null to indicate it's done
            m_currentVoltageIndicatorRotationCoroutine = null;
        }

        /// <summary>
        ///     Return true if the directional apparatus is within the proximity range of the main generator.
        /// </summary>
        /// <returns>True if the directional apparatus is close to the main generator.</returns>
        private bool IsDirectionalApparatusInRange()
        {
            return m_hasValidDirectionalApparatus && m_currentDirectionalApparatusDistanceSquared <= m_directionalApparatusDistanceThresholdSquared;
        }

        /// <summary>
        ///     This evaluates the current voltage, and reacts to it accordingly.
        /// </summary>
        private void HandleVoltageChange()
        {
            Debug.Log("Main generator is now handling the change of voltage");

            if (m_currentVoltage == m_currentVoltageThresholdNeeded)
            {
                // Voltage correct!
                Debug.Log("Main generator has the correct voltage, switching on");
                SwitchOn();
            }
            else
            {
                if (m_currentVoltage == 0)
                {
                    Debug.Log("Main generator has zero voltage, switching off");
                    SwitchOff();
                    return;
                }

                // Wrong voltage!
                if (m_currentVoltage < m_currentVoltageThresholdNeeded)
                {
                    Debug.Log("Main generator has not enough voltage");
                    return;
                }

                // Over voltage
                Debug.Log("Main generator has too much voltage, switching off all generators");
                // Switch off all other generators
                foreach (var generator in m_manager.MiniGenerators)
                {
                    generator.SwitchOff();
                }
                // Switch off main generator too
                if (IsMainGeneratorEnabled)
                {
                    SwitchOff();
                }

                m_electricalOverloadVfx.gameObject.SetActive(true);
                m_electricalOverloadVfx.Play();
                m_electricalOverloadAudio.Play();
            }
        }


        private void UpdateDirectionalApparatusStatus()
        {
            if (m_electricArcPrefabDirectionalApparatus == null)
            {
                Debug.LogError("m_electricArcPrefabDirectionalApparatus not set on main generator!");
                return;
            }

            if (m_hasValidDirectionalApparatus)
            {
                if (m_currentVoltage <= 0)
                {
                    if (m_electricArcPrefabDirectionalApparatus.gameObject.activeInHierarchy)
                    {
                        m_electricArcPrefabDirectionalApparatus.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (!m_electricArcPrefabDirectionalApparatus.gameObject.activeInHierarchy)
                    {
                        m_electricArcPrefabDirectionalApparatus.gameObject.SetActive(true);
                    }

                    var isStrong = m_currentVoltage >= m_currentVoltageThresholdNeeded;
                    m_electricArcPrefabDirectionalApparatus.SetBool(VISUAL_EFFECT_STRONG_NAME, isStrong);
                    m_electricArcPrefabDirectionalApparatus.SetBool(VISUAL_EFFECT_CONNECT_NAME, IsDirectionalApparatusInRange());

                    if (m_currentVoltage <= m_currentVoltageThresholdNeeded)
                    {
                        var directionalApparatus = m_manager.DirectionalApparatus;
                        if (directionalApparatus != null)
                        {
                            if (isStrong)
                            {
                                directionalApparatus.EnergyProvided(m_currentVoltage);
                                Debug.Log("Main generator forwarded its voltage to directional apparatus");
                            }
                            else
                            {
                                // Directional apparatus out of range, send zero voltage to it
                                directionalApparatus.EnergyProvided(0);
                                Debug.Log(
                                    "Main generator too far from directional apparatus or main gen off," +
                                    " sending zero volts");
                            }
                        }
                        else
                        {
                            Debug.LogError("Directional apparatus is missing!");
                        }
                    }
                }
            }
        }

        private void SwitchOn()
        {
            IsMainGeneratorEnabled = true;
            UpdateDirectionalApparatusStatus();
        }

        private void SwitchOff()
        {
            IsMainGeneratorEnabled = false;
            m_currentVoltage = 0;
            UpdateDirectionalApparatusStatus();
        }
    }
}