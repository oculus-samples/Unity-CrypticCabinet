// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.TeslaPuzzle
{
    /// <summary>
    ///     Handles the list of generators present in the scene for the Tesla puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ElectricGeneratorsManager : MonoBehaviour
    {
        /// <summary>
        ///     For this scenario, we only have one main generator.
        /// </summary>
        private const int NUMBER_OF_MAIN_GENERATORS = 1;

        /// <summary>
        ///     For this scenario, we only have three mini generators.
        /// </summary>
        private const int NUMBER_OF_MINI_GENERATORS = 3;

        /// <summary>
        ///     For this scenario, we only have one directional apparatus.
        /// </summary>
        private const int NUMBER_OF_DIRECTIONAL_DEVICES = 1;

        /// <summary>
        ///     List of all mini generators available in the scene.
        /// </summary>
        public List<ElectricGenerator> MiniGenerators;

        /// <summary>
        ///     Reference to the main generator in the scene.
        /// </summary>
        public MainGenerator MainGenerator;

        /// <summary>
        ///     Reference to the directional apparatus in the scene.
        /// </summary>
        public DirectionalApparatus DirectionalApparatus;

        private bool m_initSuccessful;

        private void Update()
        {
            if (m_initSuccessful)
            {
                return;
            }

            if (MiniGenerators.Count == 0)
            {
                var miniGenerators = FindObjectsOfType<ElectricGenerator>();

                // Once we find all mini generators, we add them to our references
                if (miniGenerators.Length == NUMBER_OF_MINI_GENERATORS)
                {
                    MiniGenerators.AddRange(miniGenerators);
                }
            }

            if (MainGenerator == null)
            {
                var mainGenerators = FindObjectsOfType<MainGenerator>();
                Debug.Assert(
                    mainGenerators.Length == NUMBER_OF_MAIN_GENERATORS,
                    "Mismatch for the number of expected main generators in the scene!");
                // For this specific scenario we only want one main generator. Pick the first one found in the scene.
                MainGenerator = mainGenerators[0];
            }

            if (DirectionalApparatus == null)
            {
                var directionalDevices = FindObjectsOfType<DirectionalApparatus>(true);
                if (directionalDevices.Length == NUMBER_OF_DIRECTIONAL_DEVICES)
                {
                    // For this specific scenario we only want one directional apparatus. Pick the first one found in the scene.
                    DirectionalApparatus = directionalDevices[0];
                }
                else
                {
                    Debug.Log("No directional apparatus found in the scene yet, checking again next frame");
                }
            }

            // Check if all references have been found
            m_initSuccessful = MiniGenerators.Count == NUMBER_OF_MINI_GENERATORS
                               && DirectionalApparatus != null && MainGenerator != null;

            if (!m_initSuccessful)
            {
                return;
            }

            // All references have been found. Injecting the manager into the relevant ones.
            if (MainGenerator != null)
            {
                MainGenerator.InjectElectricGeneratorManager(this);
            }

            foreach (var miniGenerator in MiniGenerators)
            {
                miniGenerator.InjectElectricGeneratorManager(this);
            }
        }
    }
}
