// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.GameManagement;
using CrypticCabinet.Photon;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     Handles the logic for the Credits UI.
    /// </summary>
    public class Credits : MonoBehaviour
    {
        public void OnBackClicked()
        {
            if (PhotonConnector.Instance.Runner != null)
            {
                if (PhotonConnector.Instance.Runner.IsConnectedToServer)
                {
                    PhotonConnector.Instance.Runner.Disconnect(PhotonConnector.Instance.Runner.LocalPlayer);
                }

                _ = PhotonConnector.Instance.Shutdown();
            }

            GameManager.Instance.RestartGameplay();
        }
    }
}