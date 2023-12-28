// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     Handles the logic for a generic Modal message UI.
    /// </summary>
    public class ModalMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_messageText;

        public void SetText(string text) => m_messageText.text = text;
    }
}