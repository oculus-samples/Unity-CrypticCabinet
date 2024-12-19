// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.UIComponents
{
    /// <summary>
    /// A class to display an image in the editor.
    /// </summary>
    public class ImageView
    {
        private EditorWindow m_editorWindow;
        private Editor m_editor;
        private Vector2 m_pan;
        private float m_zoom = -1f;

        /// <summary>
        /// Constructs a new image view.
        /// </summary>
        /// <param name="editorWindow">The editor window to draw the image in.</param>
        public ImageView(EditorWindow editorWindow) => m_editorWindow = editorWindow;

        /// <summary>
        /// Constructs a new image view.
        /// </summary>
        /// <param name="editor">The editor to draw the image in.</param>
        public ImageView(Editor editor) => m_editor = editor;

        private float ViewHeight => m_editorWindow ? m_editorWindow.position.height : Screen.height;
        private float ViewWidth => m_editorWindow ? m_editorWindow.position.width : EditorGUIUtility.currentViewWidth;

        /// <summary>
        /// Draws the image in the editor.
        /// </summary>
        /// <param name="image">The image to draw.</param>
        public void Draw(Texture2D image)
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            var windowRect = GUILayoutUtility.GetLastRect();
            if (windowRect.width <= 1 && windowRect.height <= 1)
            {
                return;
            }

            if (image == null)
            {
                EditorGUILayout.HelpBox("No Texture2D assigned.", MessageType.Info);
                return;
            }

            // Handle input for panning and zooming
            HandleInput();

            GUI.BeginGroup(windowRect);
            {
                var imageWidth = image.width * m_zoom;
                var imageHeight = image.height * m_zoom;

                if (m_zoom < 0 || (imageWidth < windowRect.width && imageHeight < windowRect.height))
                {
                    var widthScale = windowRect.width / image.width;
                    var heightScale = windowRect.height / image.height;
                    m_zoom = Mathf.Min(widthScale, heightScale);
                }

                if (imageWidth < windowRect.width) m_pan.x = (windowRect.width - imageWidth) / 2.0f;
                else if (m_pan.x + imageWidth < windowRect.width) m_pan.x += windowRect.width - (m_pan.x + imageWidth);

                if (imageHeight < windowRect.height) m_pan.y = (windowRect.height - imageHeight) / 2.0f;
                else if (m_pan.y + imageHeight < windowRect.height) m_pan.y += windowRect.height - (m_pan.y + imageHeight);

                if (m_pan.x > 0) m_pan.x = 0;
                if (m_pan.y > 0) m_pan.y = 0;

                if (imageHeight < windowRect.height) m_pan.y = (windowRect.height - imageHeight) / 2.0f;

                GUI.DrawTexture(new Rect(m_pan.x, m_pan.y, image.width * m_zoom, image.height * m_zoom), image,
                    ScaleMode.ScaleAndCrop);
            }
            GUI.EndGroup();
        }

        /// <summary>
        /// Repaints the editor window or editor.
        /// </summary>
        private void Repaint()
        {
            if (m_editorWindow)
            {
                m_editorWindow.Repaint();
            }
            else if (m_editor)
            {
                m_editor.Repaint();
            }
        }

        /// <summary>
        /// Handles input for panning and zooming.
        /// </summary>
        private void HandleInput()
        {
            var e = Event.current;

            // Panning
            if (e.type == EventType.MouseDown)
            {
                e.Use();
            }

            if (e.type == EventType.MouseDrag)
            {
                m_pan += e.delta;
                e.Use();
            }

            // Zooming
            if (e.type == EventType.ScrollWheel)
            {
                var zoomDelta = -e.delta.y * Styles.ZOOM_SPEED;
                m_zoom = Styles.ClampZoom(m_zoom + zoomDelta);
                e.Use();
            }
        }
    }
}