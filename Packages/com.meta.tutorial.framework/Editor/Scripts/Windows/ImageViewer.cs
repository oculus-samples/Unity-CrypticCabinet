// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Pages.Images;
using Meta.Tutorial.Framework.Hub.UIComponents;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Windows
{
    /// <summary>
    /// A window that displays an image.
    /// </summary>
    public class ImageViewer : EditorWindow
    {
        private EmbeddedImage m_image;
        private ImageView m_imageView;
        private int m_lastFrame;

        /// <summary>
        /// Show the window with the given image and title.
        /// </summary>
        /// <param name="image">The image to display.</param>
        /// <param name="title">The title of the window.</param>
        public static void ShowWindow(EmbeddedImage image, string title)
        {
            var window = CreateInstance<ImageViewer>();
            window.m_image = image;
            window.titleContent = new GUIContent(title);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            m_imageView ??= new ImageView(this);

            m_imageView.Draw(m_image.CurrentTexture);
        }

        private void OnEditorUpdate()
        {
            if (m_image != null && (m_image.Update() || m_image.DidFrameChange(ref m_lastFrame)))
            {
                Repaint();
            }
        }
    }
}