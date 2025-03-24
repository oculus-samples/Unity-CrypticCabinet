// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Used for setting the vertical position the rope should be clipped at using the shader:
    ///     S_Master_IntroOutro_unlit_HeightClip.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class SetRopeShaderHeight : MonoBehaviour
    {
        [SerializeField] private Renderer m_dummyRopeRenderer;
        [SerializeField] private string m_fieldName = "_WorldVerticalCutoff";

        public IEnumerator Start()
        {
            while (Application.isPlaying && m_dummyRopeRenderer != null)
            {
                m_dummyRopeRenderer.material.SetFloat(m_fieldName, transform.position.y);
                yield return new WaitForSeconds(60);
            }
        }
    }
}