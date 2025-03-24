// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace CrypticCabinet.UI.Modal
{
    /// <summary>
    ///     Handles the logic for a generic Modal message UI.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ModalMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_messageText;

        public void SetText(string text) => m_messageText.text = text;
    }
}