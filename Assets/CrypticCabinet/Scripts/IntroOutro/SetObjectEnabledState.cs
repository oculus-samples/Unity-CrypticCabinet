// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Retrieves all EnabledStateTarget components and triggers on them the new enabled state according to
    ///     the desired new state.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SetObjectEnabledState : MonoBehaviour
    {
        /// <summary>
        ///     Applies the new states to all EnabledStateTarget found.
        /// </summary>
        public void SetObjectStates()
        {
            var stateTargets = FindObjectsByType<EnabledStateTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var stateTarget in stateTargets)
            {
                if (stateTarget != null)
                {
                    stateTarget.ApplyNewStates();
                }
            }
        }

        /// <summary>
        ///     Reverts the new states of all EnabledStateTarget found to their old state.
        /// </summary>
        public void ResetObjectStates()
        {
            var stateTargets = FindObjectsByType<EnabledStateTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var stateTarget in stateTargets)
            {
                if (stateTarget != null)
                {
                    stateTarget.RevertStatesStates();
                }
            }
        }
    }
}