// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     Retrieves the OrreryClueControl and update the cue state over it.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class CueFinder : MonoBehaviour
    {
        private OrreryClueControl m_orreryClueControl;

        private bool TryGetCueGO()
        {
            m_orreryClueControl = FindObjectOfType<OrreryClueControl>(true);
            if (m_orreryClueControl == null)
            {
                Debug.LogError("CueFinder could not find GO with CueMarker component");
                return false;
            }

            return true;
        }

        public void FindCueSetActiveState(bool activeState)
        {
            if (m_orreryClueControl != null || TryGetCueGO())
            {
                m_orreryClueControl.SetCueSate(activeState);
            }
        }
    }
}
