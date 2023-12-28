// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.GameManagement
{
    /// <summary>
    ///     Utility script that triggers the next game phase by pressing the P of the keyboard, or by pressing both
    ///     joysticks of the Quest controllers. Only meant for debugging purposes.
    /// </summary>
    public class SkipGamePhase : MonoBehaviour
    {
        public void Update()
        {
            var skipUsingVR = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) &&
                              OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            var skipUsingKeyboard = Input.GetKeyDown(KeyCode.P);

            if ((skipUsingVR || skipUsingKeyboard)
                && GameManager.Instance != null)
            {
                GameManager.Instance.NextGameplayPhase();
            }
        }
    }
}