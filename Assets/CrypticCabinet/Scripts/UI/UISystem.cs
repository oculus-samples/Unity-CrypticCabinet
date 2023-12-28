// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using CrypticCabinet.GameManagement;
using CrypticCabinet.UI.Modal;
using CrypticCabinet.Utils;
using Meta.Utilities;
using UnityEngine;

namespace CrypticCabinet.UI
{
    /// <summary>
    ///     Singleton to control the menus and windows for the game UI.
    /// </summary>
    public class UISystem : Singleton<UISystem>
    {
        [SerializeField] private GameObject m_uiModal;
        [SerializeField] private MainMenu m_mainMenu;
        [SerializeField] private Credits m_credits;
        [SerializeField] private ModalMessage m_modalMessage;
        [SerializeField] private ModalMessageWithButtons m_modalMessageWithButtons;
        [SerializeField] private MultiplayerMenu m_multiplayerMenu;
        [SerializeField] private InGameMenu m_inGameMenu;

        private Coroutine m_hideMessageCoroutine;

        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        /// <summary>
        ///     Show message with one customized button.
        ///     Note: the message automatically disappears when the user presses the button.
        /// </summary>
        /// <param name="messageText">Text for the message</param>
        /// <param name="button1Text">Text for the button</param>
        /// <param name="button1Action">Action performed when the button is pressed</param>
        public void ShowMessageWithOneButton(string messageText, string button1Text, Action button1Action)
        {
            ShowMessageWithTwoButtons(
                messageText, button1Text, button1Action, string.Empty, null);
        }

        /// <summary>
        ///     Show message with two customized buttons.
        ///     Note: the message automatically disappears when the user presses any of the two buttons.
        /// </summary>
        /// <param name="messageText">Text for the message</param>
        /// <param name="button1Text">Text for the first button</param>
        /// <param name="button1Action">Action performed when the first button is pressed</param>
        /// <param name="button2Text">Text for second button</param>
        /// <param name="button2Action">Action performed when the second button is pressed</param>
        private void ShowMessageWithTwoButtons(string messageText,
            string button1Text, Action button1Action,
            string button2Text, Action button2Action)
        {
            // Immediately hide the previous message to avoid overlap
            if (m_hideMessageCoroutine != null)
            {
                StopCoroutine(m_hideMessageCoroutine);
                m_hideMessageCoroutine = null;
            }

            // Hide all other messages
            HideAll();

            m_modalMessageWithButtons.Initialize(messageText, button1Text, button1Action,
                button2Text, button2Action, HideMessageWithButtons);

            m_uiModal.SetActive(true);
            m_modalMessageWithButtons.gameObject.SetActive(true);
        }

        private void HideMessageWithButtons()
        {
            m_modalMessageWithButtons.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }

        /// <summary>
        ///     Show text message to the user, without showing any button.
        ///     Note: To show buttons, use ShowMessageWithOneButton or ShowMessageWithTwoButtons instead.
        /// </summary>
        /// <param name="text">The text message to show</param>
        /// <param name="onHiddenCallback">Optional action to perform once the text message auto-hides</param>
        /// <param name="hideTime">How long before the message automatically hides.
        /// Set hideTime to a negative value to disable auto-hide.</param>
        public void ShowMessage(string text, PendingTasksHandler.PendingAction onHiddenCallback = null, float hideTime = 3.0f)
        {
            // Immediately hide the previous message to avoid overlap
            if (m_hideMessageCoroutine != null)
            {
                StopCoroutine(m_hideMessageCoroutine);
                HideAll();
                m_hideMessageCoroutine = null;
            }

            m_modalMessage.SetText(text);
            m_uiModal.SetActive(true);
            m_modalMessage.gameObject.SetActive(true);

            if (onHiddenCallback != null)
            {
                // Add pending action for when the message gets hidden
                m_pendingTasksHandler.TryExecuteAction(onHiddenCallback, false);
            }

            if (hideTime >= 0)
            {
                // Automatically hide message after hideTime seconds
                m_hideMessageCoroutine = StartCoroutine(HideMessageCoroutine(hideTime));
            }
        }

        private void HideMessage()
        {
            m_modalMessage.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }


        public void ShowNetworkSelectionMenu(
            Action<string> hostAction, // roomName
            Action<string> joinAction, // roomName
            string defaultRoomName = null
        )
        {
            m_multiplayerMenu.Initialize(hostAction, joinAction, defaultRoomName);
            m_uiModal.SetActive(true);
            m_multiplayerMenu.gameObject.SetActive(true);
        }

        public void HideNetworkSelectionMenu()
        {
            m_multiplayerMenu.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }


        public void ShowInGameMenu()
        {
            HideAll();
            m_uiModal.SetActive(true);
            m_inGameMenu.gameObject.SetActive(true);
        }

        public void HideInGameMenu()
        {
            m_inGameMenu.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }

        public void ShowMainMenu()
        {
            if (m_mainMenu == null)
            {
                Debug.LogError("No main menu set on UI System!");
                return;
            }

            if (GameManager.Instance.GetCurrentGamePhase() != null)
            {
                m_mainMenu.gameObject.SetActive(false);
                return;
            }

            m_uiModal.SetActive(true);
            m_mainMenu.gameObject.SetActive(true);
        }

        public void HideMainMenu()
        {
            if (m_mainMenu == null)
            {
                Debug.LogError("No main menu set on UI System!");
                return;
            }
            m_mainMenu.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }


        public void ShowCredits()
        {
            if (m_credits == null)
            {
                Debug.LogError("No credits set on UI System!");
                return;
            }
            m_uiModal.SetActive(true);
            m_credits.gameObject.SetActive(true);
        }

        public void HideCredits()
        {
            if (m_credits == null)
            {
                Debug.LogError("No credits set on UI System!");
                return;
            }
            m_credits.gameObject.SetActive(false);
            m_uiModal.SetActive(IsAnyMessageShown());
        }


        public void HideAll()
        {
            HideNetworkSelectionMenu();
            HideMessage();
            HideMessageWithButtons();
            HideInGameMenu();
            m_uiModal.SetActive(false);
        }

        private bool IsAnyMessageShown()
        {
            return m_modalMessageWithButtons.gameObject.activeInHierarchy
                   || m_modalMessage.gameObject.activeInHierarchy
                   || m_multiplayerMenu.gameObject.activeInHierarchy;
        }


        private IEnumerator HideMessageCoroutine(float hideTime)
        {
            yield return new WaitForSeconds(hideTime);
            HideMessage();

            // Make sure any leftover pending action is executed now
            m_pendingTasksHandler.ExecutePendingActions();
        }
    }
}