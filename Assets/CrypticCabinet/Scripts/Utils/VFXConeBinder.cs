// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Binder class for ConeInformation for the visual effect graph.
    ///     Adapted from package script: UnityEngine.VFX.Utility.VFXPlaneBinder
    /// </summary>
    [VFXBinder("Transform/Cone")]
    public class VFXConeBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEditor.VFX.Cone"), SerializeField]
        protected ExposedProperty m_property = "Cone";

        [SerializeField] private VFXCone m_coneDescriptor;

        private ExposedProperty m_position;
        private ExposedProperty m_rotation;
        private ExposedProperty m_scale;
        private ExposedProperty m_height;
        private ExposedProperty m_bottomRadius;
        private ExposedProperty m_topRadius;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        public override bool IsValid(VisualEffect component)
        {
            return m_coneDescriptor != null &&
                   m_coneDescriptor.Transform != null &&
                   component.HasVector3(m_position) &&
                   component.HasVector3(m_rotation) &&
                   component.HasVector3(m_scale) &&
                   component.HasFloat(m_height) &&
                   component.HasFloat(m_topRadius) &&
                   component.HasFloat(m_bottomRadius);
        }


        private void OnValidate()
        {
            UpdateSubProperties();
        }

        public override void UpdateBinding(VisualEffect component)
        {
            var coneTransform = m_coneDescriptor.Transform;
            component.SetVector3(m_position, coneTransform.position);
            component.SetVector3(m_rotation, coneTransform.rotation.eulerAngles);
            component.SetVector3(m_scale, coneTransform.lossyScale);
            component.SetFloat(m_height, m_coneDescriptor.Height);
            component.SetFloat(m_topRadius, m_coneDescriptor.TopRadius);
            component.SetFloat(m_bottomRadius, m_coneDescriptor.BaseRadius);
        }

        private void UpdateSubProperties()
        {
            m_position = m_property + " Transform_position";
            m_rotation = m_property + " Transform_angles";
            m_scale = m_property + " Transform_scale";
            m_height = m_property + " Height";
            m_bottomRadius = m_property + " Base Radius";
            m_topRadius = m_property + " Top Radius";
        }
    }
}
