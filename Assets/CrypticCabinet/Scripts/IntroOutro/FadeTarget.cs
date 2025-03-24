// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.IntroOutro
{
    /// <summary>
    ///     Controls the fading of a specified target.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class FadeTarget : MonoBehaviour
    {
        [SerializeField] private Transform m_transform;
        [SerializeField] private Renderer[] m_renderer;
        [SerializeField] private GameObject[] m_jointObjectToEnable;
        [SerializeField] private string m_enableShaderPropertyName = "_UseIntroOutro";
        [SerializeField] private string m_shaderPropertyName = "_InOut";
        public UnityEvent OnFadeStart;
        public UnityEvent OnFadeEnd;
        public Transform Transform => m_transform;

        private MaterialPropertyBlock m_materialPropertyBlock;

        private void Awake()
        {
            if (m_renderer == null || m_renderer.Length == 0)
            {
                m_renderer = GetComponentsInChildren<Renderer>(true);
            }

            if (m_transform == null)
            {
                m_transform = transform;
            }

            m_materialPropertyBlock = new MaterialPropertyBlock();
        }

        public void UpdateVisuals(float value)
        {
            if (value <= 0)
            {
                if (gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(false);
                    ApplyMaterialSetting(value);
                    SetOtherObjectActiveState(false);
                }
            }
            else
            {
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(true);
                    SetOtherObjectActiveState(true);
                }

                ApplyMaterialSetting(value);
            }
            OnFadeEnd?.Invoke();
        }

        private void SetOtherObjectActiveState(bool active)
        {
            foreach (var o in m_jointObjectToEnable)
            {
                if (o != null)
                {
                    o.SetActive(active);
                }
            }
        }

        private void ApplyMaterialSetting(float value)
        {
            m_materialPropertyBlock.SetFloat(m_shaderPropertyName, value);
            m_materialPropertyBlock.SetFloat(m_enableShaderPropertyName, 1.0f);

            foreach (var r in m_renderer)
            {
                r.SetPropertyBlock(m_materialPropertyBlock);
            }
        }
    }
}