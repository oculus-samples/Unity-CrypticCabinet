// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Meta.Tutorial.Framework.Hub.Interfaces
{
    public interface IWindowUpdater
    {
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
    }
}