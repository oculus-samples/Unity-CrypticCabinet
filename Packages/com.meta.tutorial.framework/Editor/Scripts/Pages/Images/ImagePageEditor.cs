// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.UIComponents;
using UnityEditor;

namespace Meta.Tutorial.Framework.Hub.Pages.Images
{
    /// <summary>
    /// Custom editor for <see cref="ImagePage"/>.
    /// </summary>
    [CustomEditor(typeof(ImagePage))]
    public class ImagePageEditor : Editor
    {
        private ImagePage m_imageDisplay;
        private ImageView m_imageView;

        private void OnEnable()
        {
            m_imageDisplay = (ImagePage)target;
            m_imageView = new ImageView(this);
        }

        /// <inheritdoc cref="Editor.OnInspectorGUI()"/>
        public override void OnInspectorGUI()
        {
            if (m_imageDisplay.Image)
            {
                m_imageView.Draw(m_imageDisplay.Image);
            }
            else
            {
                // Draw the default properties
                base.OnInspectorGUI();
            }
        }
    }
}