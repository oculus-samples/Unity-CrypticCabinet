// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     Handles the logic for the Main Menu UI.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public UnityEvent OnSinglePlayerPressedAction;
        public UnityEvent OnMultiPlayerPressedAction;
        public UnityEvent OnCreditsPressedAction;

        public void OnSinglePlayerPressed()
        {
            UISystem.Instance.HideMainMenu();
            PhotonConnector.Instance.StartSinglePlayerSession();
            // Any optional actions to perform after that
            OnSinglePlayerPressedAction?.Invoke();
        }

        public void OnMultiplayerPressed()
        {
            UISystem.Instance.HideMainMenu();
            PhotonConnector.Instance.StartMultiplayerSession();
            // Any optional actions to perform after that
            OnMultiPlayerPressedAction?.Invoke();
        }

        public void OnCreditsPressed()
        {
            UISystem.Instance.HideMainMenu();
            UISystem.Instance.ShowCredits();
            // Any optional actions to perform after that
            OnCreditsPressedAction?.Invoke();
        }
    }
}