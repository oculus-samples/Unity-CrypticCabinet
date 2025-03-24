// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Utilities;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     A convenient modal window with two customizable buttons that can show
    ///     a text message to the user and provide actions on button press.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ModalMessageWithButtons : ModalMessage
    {
        [SerializeField] private GameObject m_button1;
        [SerializeField] private GameObject m_button2;
        [SerializeField] private TextMeshProUGUI m_button1Text;
        [SerializeField] private TextMeshProUGUI m_button2Text;

        private Action m_onButton1PressedAction;
        private Action m_onButton2PressedAction;
        private Action m_onAnyButtonPressedAction;

        /// <summary>
        ///     Call this to initialize the buttons for this modal.
        ///     If the button text is set to String.Empty, the button will not show in the modal.
        /// </summary>
        /// <param name="message">Message to show on this modal.</param>
        /// <param name="button1Text">Text for button 1</param>
        /// <param name="button1Action">Action to perform when button 1 is pressed</param>
        /// <param name="button2Text">Text for button 2</param>
        /// <param name="button2Action">Action to perform when button 2 is pressed</param>
        /// <param name="onAnyButtonPressed">Optional action to perform when any of the buttons is pressed</param>
        public void Initialize(string message, string button1Text, Action button1Action,
            string button2Text, Action button2Action, Action onAnyButtonPressed)
        {
            // Clear all states before initializing.
            Uninitialize();

            SetText(message);
            m_onButton1PressedAction = button1Action;
            m_onButton2PressedAction = button2Action;

            if (m_button1Text == null)
            {
                Debug.LogError("m_button1Text not assigned: cannot show text for button!");
            }
            else
            {
                m_button1Text.text = button1Text;

                if (m_button1 == null)
                {
                    Debug.LogError("No button 1 defined in ModalMessageWithButtons!");
                }
                else
                {
                    // Disable button 1
                    m_button1.SetActive(!button1Text.IsNullOrEmpty());
                }
            }

            if (m_button2Text == null)
            {
                Debug.LogError("m_button2Text not assigned: cannot show text for button!");
            }
            else
            {
                m_button2Text.text = button2Text;

                if (m_button2 == null)
                {
                    Debug.LogError("No button 2 defined in ModalMessageWithButtons!");
                }
                else
                {
                    // Disable button 2
                    m_button2.SetActive(!button2Text.IsNullOrEmpty());
                }
            }

            m_onAnyButtonPressedAction = onAnyButtonPressed;
        }

        /// <summary>
        ///     Call this function to clear all fields of this modal.
        /// </summary>
        public void Uninitialize()
        {
            SetText(string.Empty);
            m_onButton1PressedAction = null;
            m_onButton2PressedAction = null;

            if (m_button1 != null)
            {
                m_button1.SetActive(false);
            }
            if (m_button2 != null)
            {
                m_button2.SetActive(false);
            }
            if (m_button1Text != null)
            {
                m_button1Text.text = string.Empty;
            }
            if (m_button2Text != null)
            {
                m_button2Text.text = string.Empty;
            }
        }

        /// <summary>
        ///     Run the callback associated to the button 1 click.
        /// </summary>
        public void OnButton1Clicked()
        {
            m_onAnyButtonPressedAction?.Invoke();
            m_onButton1PressedAction?.Invoke();
        }

        /// <summary>
        ///     Run the callback associated to the button 2 click.
        /// </summary>
        public void OnButton2Clicked()
        {
            m_onAnyButtonPressedAction?.Invoke();
            m_onButton2PressedAction?.Invoke();
        }
    }
}
