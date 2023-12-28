// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Interactions;
using CrypticCabinet.Passthrough;
using CrypticCabinet.Utils;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.Puzzles.UVPuzzle
{
    /// <summary>
    ///     Describes the behaviour for the UV machine of the UV puzzle.
    /// </summary>
    public class UvLightMachine : NetworkBehaviour
    {
        [SerializeField] private AudioSource m_audioSource;
        private ScrewSnapZone m_screwSnapZone;

        [Networked(OnChanged = nameof(OnLightSwitchChanged))]
        private bool LightSwitchOn { get; set; }

        private UvLightBulb m_currentSnappedBulb;
        private bool m_uvVfxEnabled;
        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Start()
        {
            m_screwSnapZone = GetComponentInChildren<ScrewSnapZone>();

            if (m_screwSnapZone == null)
            {
                Debug.LogError("Missing screw snap zone", this);
            }

            m_screwSnapZone.OnObjectCompleteScrew.AddListener(ScrewComplete);
            m_screwSnapZone.OnObjectStartUnscrew.AddListener(OnBulbUnscrew);


            var uvLightClues = FindObjectsOfType<UvLightClue>();
            foreach (var clue in uvLightClues)
            {
                clue.SetEnabled(false);

            }
        }

        private void OnDisable()
        {
            SetLightOn(false);
            PassthroughChanger.Instance.SetPassthroughDefaultLut();
        }

        /// <summary>
        ///     Set the specified UV light bulb as the current snapped onto the UV machine.
        /// </summary>
        /// <param name="bulb">The UV light bulb that snapped into the UV machine.</param>
        public void SetCurrentBulb(UvLightBulb bulb)
        {
            m_currentSnappedBulb = bulb;
            UpdateLight();
        }

        private void OnBulbUnscrew(ScrewableObject bulb)
        {
            if (m_currentSnappedBulb != null)
            {
                m_currentSnappedBulb.SetOn(false);
            }

            m_currentSnappedBulb = null;
            UpdateLight();
        }

        private void ScrewComplete(ScrewableObject bulb)
        {
            m_currentSnappedBulb = bulb.GetComponentInChildren<UvLightBulb>();
            m_audioSource.Play();
            UpdateLight();
        }

        /// <summary>
        ///     Update the state of the attached UV light bulb depending on the value of lightOn.
        /// </summary>
        /// <param name="lightOn">True to enable the light, false otherwise.</param>
        public void SetLightOn(bool lightOn)
        {
            EnsureHaveStateAuthority();
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                LightSwitchOn = lightOn;
                UpdateLight();
            }, HasStateAuthority);
        }

        /// <summary>
        ///     Requests state authority if the current user does not have it.
        /// </summary>
        private void EnsureHaveStateAuthority()
        {
            if (!HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }
        }

        private static void OnLightSwitchChanged(Changed<UvLightMachine> changed)
        {
            changed.Behaviour.UpdateLight();
        }

        private void UpdateLight()
        {
            if (m_currentSnappedBulb == null)
            {
                SetPassthroughEnabled(false);
                return;
            }

            SetPassthroughEnabled(LightSwitchOn && !m_currentSnappedBulb.IsBroken);

            m_currentSnappedBulb.SetOn(LightSwitchOn);
        }

        private void SetPassthroughEnabled(bool effectEnabled)
        {
            if (effectEnabled)
            {
                if (!m_uvVfxEnabled)
                {
                    m_uvVfxEnabled = true;
                    PassthroughChanger.Instance.SetPassthroughUvLightRoomLut();
                }
            }
            else
            {
                if (m_uvVfxEnabled)
                {
                    m_uvVfxEnabled = false;
                    PassthroughChanger.Instance.SetPassthroughUvLightRoomLut();
                }
            }
        }
    }
}