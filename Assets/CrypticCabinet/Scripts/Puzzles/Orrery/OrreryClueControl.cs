// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.Puzzles.Orrery
{
    /// <summary>
    ///     Controls the clue that is shown to the user when the light beam hit the glass orb in the orrery.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class OrreryClueControl : MonoBehaviour
    {
        private const string ANIMATION_PARAMETER_FLOAT_NAME = "MotionTime";
        private const string SHADER_POSITION_START = "_Position_Start";

        [SerializeField] private Renderer m_clueRenderer;
        [SerializeField] private GameObject m_sunGlowEffectGO;
        [SerializeField] private GameObject m_cueGO;
        [SerializeField] private Animator m_cueAnimator;

        [SerializeField] private float m_animationDurationSec = 3.0f;

        private Coroutine m_animateCoroutine;
        private float m_animationTime;
        private int m_animationParameterHash;
        private int m_shaderParameterHash;

        //Animation will start to play and this is the goal state.
        private bool m_desiredState;

        private void Start()
        {
            if (m_clueRenderer == null)
            {
                Debug.LogError("ClueControl: renderer not set");
            }

            if (m_clueRenderer.sharedMaterial.HasVector(SHADER_POSITION_START))
            {
                m_shaderParameterHash = Shader.PropertyToID(SHADER_POSITION_START);
            }
            else
            {
                Debug.LogError("ClueControl: material on projection missing property " + SHADER_POSITION_START);
            }
            if (m_sunGlowEffectGO == null)
            {
                Debug.LogError("ClueControl: Sun glow effect not set");
            }
            if (m_cueGO == null)
            {
                Debug.LogError("ClueControl: cue GameObject not set");
            }
            if (m_cueAnimator == null)
            {
                Debug.LogError("ClueControl: cue Animator not set");
            }

            m_cueGO.SetActive(false);
            m_animationParameterHash = Animator.StringToHash(ANIMATION_PARAMETER_FLOAT_NAME);
            m_cueAnimator.speed = 0f;
            m_cueAnimator.SetFloat(m_animationParameterHash, 0.0f);
        }

        public void SetCueSate(bool isCueEnabled)
        {
            if (m_desiredState != isCueEnabled)
            {
                m_desiredState = isCueEnabled;

                //Need to change the bounds as the mesh is resized inside the shader
                //procedurally
                var mesh = m_clueRenderer.gameObject.GetComponent<MeshFilter>().mesh;
                mesh.bounds = new Bounds(Vector3.zero, 100f * Vector3.one);

                m_animateCoroutine ??= StartCoroutine(nameof(AnimateCue));
            }
        }

        /// <summary>
        ///     This method is used to animate a cue using a coroutine.
        /// </summary>
        private IEnumerator AnimateCue()
        {
            // Set the shader parameter to the position of the sun glow effect
            m_clueRenderer.sharedMaterial.SetVector(m_shaderParameterHash, m_sunGlowEffectGO.transform.position);

            m_cueGO.SetActive(true);

            // Continue the loop until the desired animation state and time conditions are met
            while ((!m_desiredState && m_animationTime >= 0.0f) ||
                   (m_desiredState && m_animationTime <= m_animationDurationSec))
            {
                // Update the animation time based if it wants to activate or deactivate
                m_animationTime += m_desiredState ? Time.deltaTime : -Time.deltaTime;

                // Update the cue animator's float parameter for animation progress
                m_cueAnimator.SetFloat(m_animationParameterHash, Mathf.Clamp(m_animationTime, 0f, m_animationDurationSec));

                // Yield to the next frame without blocking the main thread
                yield return null;
            }

            // Ensure that the cue game object matches the desired state after animation
            if (!m_desiredState)
            {
                m_cueGO.SetActive(m_desiredState);
            }

            // Reset the coroutine reference to indicate it has completed
            m_animateCoroutine = null;
        }
    }
}
