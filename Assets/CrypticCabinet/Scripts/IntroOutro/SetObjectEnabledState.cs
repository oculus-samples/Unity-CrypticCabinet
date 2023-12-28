// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Retrieves all EnabledStateTarget components and triggers on them the new enabled state according to
    ///     the desired new state.
    /// </summary>
    public class SetObjectEnabledState : MonoBehaviour
    {
        /// <summary>
        ///     Applies the new states to all EnabledStateTarget found.
        /// </summary>
        public void SetObjectStates()
        {
            var stateTargets = FindObjectsOfType<EnabledStateTarget>();
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
            var stateTargets = FindObjectsOfType<EnabledStateTarget>();
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