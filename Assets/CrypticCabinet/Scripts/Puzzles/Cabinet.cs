// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Photon;
using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles
{
    /// <summary>
    ///     Spawns the objects inside of the cabinet that the user interacts with.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class Cabinet : NetworkBehaviour
    {
        [SerializeField] private GameObject m_cabinetPuzzleObjects;
        [SerializeField] private SkinnedMeshRenderer m_crystalRenderer;
        private NetworkObject m_cabinetObjects;
        private static readonly int s_gemColour = Shader.PropertyToID("_Gem_Colour");
        private static readonly int s_inOut = Shader.PropertyToID("_InOut");

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                if (m_cabinetPuzzleObjects == null)
                {
                    Debug.LogError("Missing reference to cabinet objects.");
                    return;
                }

                var thisTransform = transform;
                m_cabinetObjects = PhotonConnector.Instance.Runner.Spawn(
                    m_cabinetPuzzleObjects, thisTransform.position, thisTransform.rotation);
                ResetCrystalMaterial();
            }
        }

        private void ResetCrystalMaterial()
        {
            _ = ColorUtility.TryParseHtmlString("#47647E00", out var color);
            m_crystalRenderer.material.SetColor(s_gemColour, color);
            m_crystalRenderer.material.SetFloat(s_inOut, 1f);
        }

        private void Update()
        {
            if (!HasStateAuthority || m_cabinetObjects == null)
            {
                return;
            }

            var thisTransform = transform;
            var objectsTransform = m_cabinetObjects.transform;
            objectsTransform.position = thisTransform.position;
            objectsTransform.rotation = thisTransform.rotation;
        }
    }
}
