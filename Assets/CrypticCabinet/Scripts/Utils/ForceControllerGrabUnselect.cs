// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Utility class to force unselect when no mask button is, and disable select until at least one
    ///     mask button is pressed on the controller.
    /// </summary>
    public class ForceControllerGrabUnselect : MonoBehaviour
    {
        [SerializeField] private TouchHandGrabInteractor m_touchHandGrab;

        [SerializeField] private OVRInput.Button m_buttonMask;
        [SerializeField] private OVRInput.Controller m_controller;

        private bool m_hasTouchHandGrab;

        private void Start()
        {
            m_hasTouchHandGrab = m_touchHandGrab != null;

            if (!m_hasTouchHandGrab)
            {
                Debug.LogError("m_touchHandGrab is null in ForceControllerGrabUnselect");
                return;
            }

            // Disable automatic select, only allow that if we are pressing buttons
            m_touchHandGrab.SetComputeShouldSelectOverride(() => false, false);
            m_touchHandGrab.SetComputeShouldUnselectOverride(() => true, false);
        }

        private void Update()
        {
            if (!m_hasTouchHandGrab)
            {
                return;
            }

            // If any button on the controller buttons mask is pressed, allow select.
            // Otherwise, automatically trigger unselect and disable select.
            if (!OVRInput.Get(m_buttonMask, m_controller))
            {
                // No button of the mask is pressed, trigger unselect
                m_touchHandGrab.SetComputeShouldSelectOverride(() => false);
                m_touchHandGrab.SetComputeShouldUnselectOverride(() => true);
            }
            else if (OVRInput.GetDown(m_buttonMask, m_controller))
            {
                // If it is the first time we are pressing down, select.
                m_touchHandGrab.ClearComputeShouldUnselectOverride();
                m_touchHandGrab.SetComputeShouldSelectOverride(() => true);
            }
        }
    }
}