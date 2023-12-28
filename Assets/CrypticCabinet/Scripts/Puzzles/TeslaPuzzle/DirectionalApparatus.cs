// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using CrypticCabinet.Utils;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Describes the behaviour for the directional apparatus for the Tesla puzzle.
    /// </summary>
    public class DirectionalApparatus : MonoBehaviour
    {
        [SerializeField] private float m_energyThresholdForActivation = 9999f;
        [SerializeField] private GameObject m_emitterGameObject;
        [SerializeField] private Transform m_electricArcTransform;
        [SerializeField] private UnityEvent m_onApparatusActivation;
        [SerializeField] private string m_cabinetTag;
        [SerializeField] private float m_maxAngularDistance = 20f;
        [SerializeField] private float m_rayActiveForSecs = 2.0f;

        private bool m_energyFlowing;
        private GameObject m_cabinet;
        private Coroutine m_updateCoroutine;

        [SerializeField] private VisualEffect m_rayVisualEffect;
        [SerializeField] private Transform m_rayTargetPosition;

        [SerializeField] private float m_spinTime = 1.5f;
        [SerializeField] private float m_spinRadius = 0.3f;
        private Vector3 m_pos;
        private bool m_cabinetShot;

        private void Start()
        {
            m_pos = new Vector3();
            if (m_rayVisualEffect == null)
            {
                m_rayVisualEffect = GetComponentInChildren<VisualEffect>();
            }
        }

        /// <summary>
        ///     Returns the transform where an electric arc can end to when
        ///     connecting to the directional apparatus.
        /// </summary>
        /// <returns>Transform for the end position of a candidate electric arc</returns>
        public Transform GetDirectionalApparatusElectricContact()
        {
            return m_electricArcTransform;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            var position = m_emitterGameObject.transform.position;
            Gizmos.DrawLine(position, position + m_emitterGameObject.transform.right);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void OnActivationRpc()
        {
            if (m_updateCoroutine != null)
            {
                Debug.Log("OnActivationRpc: Directional apparatus stopping coroutine at frame " + Time.frameCount);
                StopCoroutine(m_updateCoroutine);
                m_updateCoroutine = null;
            }

            m_cabinetShot = true;
            m_rayTargetPosition.position = m_cabinet.transform.position;
            _ = StartCoroutine(DisableAfterATime(m_cabinet.transform));
            m_rayVisualEffect.gameObject.SetActive(true);
            _ = StartCoroutine(DelayTriggerEvent());
        }

        private IEnumerator DelayTriggerEvent()
        {
            yield return new WaitForSeconds(m_rayActiveForSecs);
            m_onApparatusActivation?.Invoke();
        }

        private IEnumerator DisableAfterATime(Transform targetTransform)
        {
            float currentActiveTime = 0;
            while (currentActiveTime < m_rayActiveForSecs)
            {
                yield return null;
                currentActiveTime += Time.deltaTime;
                m_rayTargetPosition.position = targetTransform.position;
            }

            m_rayVisualEffect.gameObject.SetActive(false);
        }

        public void EnergyProvided(float energyValue)
        {
            if (!isActiveAndEnabled)
            {
                Debug.Log("Directional apparatus is still disabled / inside the orrery, no ray will be emitted.");
                return;
            }

            Debug.Log("EnergyProvided called at frame " + Time.frameCount);
            if (energyValue >= m_energyThresholdForActivation)
            {
                var cabinets = GameObject.FindGameObjectsWithTag(m_cabinetTag);
                if (cabinets.Length != 1)
                {
                    var message = cabinets.Length == 0 ? "No cabinets in the scene!" : "Too may cabinets in the scene!";
                    Debug.LogError(message);
                    return;
                }

                m_cabinet = cabinets[0];
                m_energyFlowing = true;
                m_rayVisualEffect.gameObject.SetActive(true);

                if (m_updateCoroutine != null)
                {
                    Debug.Log("Directional apparatus stopping coroutine at frame " + Time.frameCount);
                    StopCoroutine(m_updateCoroutine);
                    m_updateCoroutine = null;
                }

                // Note: we might want to fence this to avoid starting the coroutine within the same frame.
                Debug.Log("Directional apparatus starting coroutine at frame " + Time.frameCount);
                m_updateCoroutine = StartCoroutine(nameof(ApparatusActive));
            }
            else
            {
                Debug.Log("Directional apparatus disabling at frame " + Time.frameCount);
                m_energyFlowing = false;
                m_rayVisualEffect.gameObject.SetActive(false);
            }
        }

        private IEnumerator ApparatusActive()
        {
            Debug.Log("Directional apparatus calling ApparatusActive at frame " + Time.frameCount);
            while (m_energyFlowing)
            {
                var emitterTransform = m_emitterGameObject.transform;
                var emitterPosition = emitterTransform.position;
                var nearestCabinetPos = MathsUtils.NearestPointOnLine(
                    m_cabinet.transform.position, Vector3.up, emitterPosition);
                var heading = nearestCabinetPos - emitterPosition;
                var angularDistance = Vector3.SignedAngle(emitterTransform.forward, heading, emitterTransform.up);

                if (Mathf.Abs(angularDistance) < m_maxAngularDistance)
                {
                    Debug.Log("Directional apparatus activated.");
                    OnActivationRpc();
                    break;
                }

                if (!m_cabinetShot)
                {

                    // This animates the sparking animation while waiting for the user to point at the cabinet.
                    var angle = Mathf.Lerp(0, 350, Mathf.InverseLerp(0, m_spinTime, Time.time % m_spinTime));
                    var randomRadius = Mathf.Lerp(0, m_spinRadius, Mathf.PerlinNoise1D(Time.time + 40));
                    m_pos.x = randomRadius * Mathf.Cos(angle * Mathf.Deg2Rad);
                    m_pos.y = randomRadius * Mathf.Sin(angle * Mathf.Deg2Rad);
                    m_pos.z = Mathf.Lerp(0.2f, 0.5f, Mathf.PerlinNoise1D(Time.time));
                    m_rayTargetPosition.position =
                        m_emitterGameObject.transform.localToWorldMatrix.MultiplyPoint(m_pos);
                }
                else
                {
                    m_rayTargetPosition.position = m_cabinet.transform.position;
                }

                yield return null;
            }
            Debug.Log("Directional apparatus energy flowing is now false at frame " + Time.frameCount);
        }
    }
}
