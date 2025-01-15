// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Meta.Tutorial.Framework.Hub.Interfaces
{
    /// <summary>
    /// Adheres to drawing a Meta Hub documentation page.
    /// </summary>
    public interface IMetaHubPage
    {
        /// <summary>
        /// Draws the GUI for the page.
        /// </summary>
        void OnGUI();

#if UNITY_EDITOR
        /// <summary>
        /// Register the window It's associated to.
        /// </summary>
        /// <param name="window">Editor window</param>
        void RegisterWindow(EditorWindow window);

        /// <summary>
        /// Unregister the window It's associated to.
        /// </summary>
        /// <param name="window">Editor window</param>
        void UnregisterWindow(EditorWindow window);
#endif
    }
}