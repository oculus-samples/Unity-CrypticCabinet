// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Interactions;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.VFX;

namespace CrypticCabinet.Puzzles.UVPuzzle
{
    /// <summary>
    ///     Defines the UV light bulb for the UV machine puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(ScrewableObject))]
    public class UvLightBulb : MonoBehaviour
    {
        public bool IsBroken => m_isBroken;
        [SerializeField] private bool m_isBroken;
        [SerializeField] private UvLightMachine m_uvLightMachine;
        [SerializeField] private Renderer m_rend;
        [SerializeField] private VisualEffect m_sparks;

        private static readonly int s_eColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            if (m_uvLightMachine != null)
            {
                m_uvLightMachine.SetCurrentBulb(this);
            }
        }

        /// <summary>
        ///     Enables or disables the light, if it is not the broken one.
        /// </summary>
        /// <param name="lightOn">True to enable the light, false otherwise.</param>
        public void SetOn(bool lightOn)
        {
            var uvLightClues = FindObjectsByType<UvLightClue>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var clue in uvLightClues)
            {
                clue.SetEnabled(lightOn && !m_isBroken);

            }
            HandleBulbEffects(lightOn);
        }

        private void HandleBulbEffects(bool lightOn)
        {
            if (lightOn && !m_isBroken)
            {
                m_rend.material.SetColor(s_eColor, new Color(0.53f, 0.32f, 1f));
            }

            if (!lightOn && !m_isBroken)
            {
                m_rend.material.SetColor(s_eColor, Color.black);
                return;
            }

            if (lightOn && m_isBroken)
            {
                m_sparks.gameObject.SetActive(true);
                return;
            }

            if (!lightOn && m_isBroken)
            {
                m_sparks.gameObject.SetActive(false);

            }
        }
    }
}