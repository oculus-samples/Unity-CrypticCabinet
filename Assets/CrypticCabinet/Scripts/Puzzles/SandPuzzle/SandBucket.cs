// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.Utils;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.VFX;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Defines the interactions of the sand bucket, including the particles for the sand into it.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(SkinnedMeshRenderer), typeof(Rigidbody), typeof(RepositionWhenFarAway))]
    public class SandBucket : MonoBehaviour
    {
        private const string BUCKET_BOTTOM_SAND_LEVEL = "_BottomSandLerp";
        private const string PARTICLE_RATE_LABEL = "ParticleRate";
        private const float MIN_PARTICLES_RATE = 100;
        private const float MAX_PARTICLES_RATE = 1200;

        /// <summary>
        ///     The minimum amount of sand that stays into the bucket.
        /// </summary>
        [SerializeField] private float m_minSandValue;

        /// <summary>
        ///     The max amount of sand that can fit into the bucket.
        /// </summary>
        [SerializeField] private float m_maxSandValue = 100f;

        /// <summary>
        ///     The bucket can be filled up to this point, it will start to drip out slowly.
        /// </summary>
        [SerializeField] private float m_overflowingSandValue = 110f;

        /// <summary>
        ///     The max mass supported by the bucket.
        /// </summary>
        [SerializeField] private float m_maxMass = 100f;

        /// <summary>
        ///     How much sand is lost every sec when overfilled.
        /// </summary>
        [SerializeField] private float m_overflowChangeSandValueSec = 2f;

        /// <summary>
        ///     Dead zone that allow user to turn the bucket a bit without losing sand.
        /// </summary>
        [SerializeField] private float m_overflowInclinationMinDotProduct = 0.8f;

        /// <summary>
        ///     How much losing sand will increase the rate of the particle system.
        /// </summary>
        [SerializeField] private float m_particleRateMultiplierOnSandOverflow = 200f;

        /// <summary>
        ///     How long it takes to lose sand.
        /// </summary>
        [SerializeField] private float m_losingSandOverflowTimeSecs = 1.0f;

        /// <summary>
        ///     How long (max) should we display the visual effect.
        /// </summary>
        [SerializeField] private float m_maxEffectTimeSecs = 1f;

        /// <summary>
        ///     The animation curve for the sand level.
        /// </summary>
        [SerializeField] private AnimationCurve m_bottomBucketSandnessCurve;

        private bool m_grabState;
        private bool m_isHooked;
        private Rigidbody m_bucketRigidBody;
        private SkinnedMeshRenderer m_buckedSkinnedRenderer;
        private VisualEffect m_sandVisualEffect;

        /// <summary>
        ///     How much sand the bucket has.
        /// </summary>
        private float m_sandValue;

        /// <summary>
        ///     Sand that has been lost, will trail for a while to keep the effect alive.
        /// </summary>
        private float m_sandLostValue;

        private int m_sandLevelPropertyId;
        private Material m_bucketSharedMaterial;

        private void Start()
        {
            m_bucketRigidBody = GetComponent<Rigidbody>();
            m_buckedSkinnedRenderer = GetComponent<SkinnedMeshRenderer>();
            m_sandVisualEffect = GetComponentInChildren<VisualEffect>();
            if (!m_sandVisualEffect.HasFloat(PARTICLE_RATE_LABEL))
            {
                Debug.LogError("Bucket Error: Particle effect for bucket overflow missing float property " + PARTICLE_RATE_LABEL);
            }
            m_sandVisualEffect.SetFloat(PARTICLE_RATE_LABEL, 0f);


            if (m_buckedSkinnedRenderer.sharedMaterials.Length == 2)
            {
                m_bucketSharedMaterial = m_buckedSkinnedRenderer.sharedMaterials[1];
                if (!m_bucketSharedMaterial.HasFloat(BUCKET_BOTTOM_SAND_LEVEL))
                {
                    Debug.LogError("Bucket Error: shader is missing property " + BUCKET_BOTTOM_SAND_LEVEL);
                }
                m_sandLevelPropertyId = Shader.PropertyToID(BUCKET_BOTTOM_SAND_LEVEL);
                m_bucketSharedMaterial.SetFloat(m_sandLevelPropertyId, 0f);
            }
            else
            {
                Debug.LogError("Bucket Error: has invalid number of materials, should be 2!");
            }
            // Starting experience with a small use of the VFX so that unity load it when is acceptable to have some performance issue.
            m_sandLostValue = 10.0f;
        }

        private void Update()
        {
            if (m_isHooked)
            {
                return;
            }

            var sandLostThisTick = 0f;

            // Check if bucket is overflowing
            if (m_sandValue - m_sandLostValue > m_maxSandValue)
            {
                var overflowSandValue = Time.deltaTime * m_overflowChangeSandValueSec / m_losingSandOverflowTimeSecs;
                var futureSandValue = Mathf.Clamp(m_sandValue - overflowSandValue - m_sandLostValue, m_maxSandValue, m_overflowingSandValue);
                sandLostThisTick = m_sandValue - futureSandValue;
            }

            var capSidedFactor = Mathf.Clamp01(Vector3.Dot(transform.up, Vector3.up));

            if (capSidedFactor < m_overflowInclinationMinDotProduct &&
                m_sandValue - m_sandLostValue > m_maxSandValue * capSidedFactor)
            {
                var newSandValue = Mathf.Clamp(
                    m_sandValue - m_sandLostValue - m_maxSandValue * capSidedFactor,
                    m_maxSandValue * capSidedFactor,
                    Mathf.Infinity);
                sandLostThisTick = Mathf.Max(m_sandValue - newSandValue, 0f);
            }

            m_sandLostValue += sandLostThisTick;

            if (m_sandLostValue > 0.0f)
            {
                var lostSandTickVFX = m_maxSandValue * Time.deltaTime / m_maxEffectTimeSecs;
                var particlesRate = lostSandTickVFX * m_particleRateMultiplierOnSandOverflow;
                m_sandVisualEffect.SetFloat(PARTICLE_RATE_LABEL, Mathf.Clamp(particlesRate, MIN_PARTICLES_RATE, MAX_PARTICLES_RATE));

                m_sandLostValue = Mathf.Clamp(
                    m_sandLostValue - lostSandTickVFX,
                    0f,
                    Mathf.Infinity);

                ChangeSandValue(-lostSandTickVFX);
            }
            else
            {
                if (m_sandVisualEffect.GetFloat(PARTICLE_RATE_LABEL) > 0f)
                {
                    m_sandVisualEffect.SetFloat(PARTICLE_RATE_LABEL, 0f);
                }
            }
        }

        public void ChangeSandValue(float deltaSand)
        {
            if (m_isHooked)
            {
                return;
            }

            m_sandValue = Mathf.Clamp(m_sandValue + deltaSand, m_minSandValue, m_overflowingSandValue);
            var normalisedValue = Mathf.Clamp01(m_sandValue / m_maxSandValue);

            m_buckedSkinnedRenderer.SetBlendShapeWeight(0, normalisedValue * 100f);
            m_bucketSharedMaterial.SetFloat(m_sandLevelPropertyId, m_bottomBucketSandnessCurve.Evaluate(normalisedValue));
            m_bucketRigidBody.mass = normalisedValue * m_maxMass;
        }

        public bool IsFull()
        {
            // When we attach the bucket, it could capsize a bit because of physics, so we need to account on this minimum amount.
            return m_sandValue >= (m_maxSandValue * m_overflowInclinationMinDotProduct);
        }

        public void SetIsHooked(bool hooked)
        {
            m_isHooked = hooked;
        }
    }
}
