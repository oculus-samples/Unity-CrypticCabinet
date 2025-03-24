// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.UVPuzzle
{
    /// <summary>
    ///     Represents the UV clue that appears on the wall when the working UV light bulb is into the
    ///     UV machine and is turned on.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class UvLightClue : MonoBehaviour
    {
        [SerializeField] private GameObject m_clueObject;

        /// <summary>
        ///     Shows or hide the clue, depending on the value of lightOn.
        /// </summary>
        /// <param name="lightOn">If true, show the clue. False otherwise.</param>
        public void SetEnabled(bool lightOn)
        {
            if (m_clueObject != null)
            {
                m_clueObject.SetActive(lightOn);
            }
        }
    }
}