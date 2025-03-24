// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Clock
{
    /// <summary>
    ///     Disables an animator when the OnStateExit is called. Used for the door of the clock puzzle.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class DoorOpenComplete : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.enabled = false;
        }
    }
}