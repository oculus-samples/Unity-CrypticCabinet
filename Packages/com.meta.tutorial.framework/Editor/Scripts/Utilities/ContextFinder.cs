// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.Tutorial.Framework.Hub.Contexts;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    /// <summary>
    /// Finds all assets of a given type in the project.
    /// </summary>
    public static class ContextFinder
    {
        /// <summary>
        /// Finds all assets of a given type in the project.
        /// </summary>
        /// <typeparam name="T">The type of asset to find.</typeparam>
        /// <returns>A list of all assets of the given type in the project.</returns>
        public static List<T> FindAllContextAssets<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var assets = new List<T>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    if (!assets.Contains(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }

            assets.Sort();

            return assets;
        }

        /// <summary>
        /// Finds all the contexts that should be shown on startup.
        /// </summary>
        /// <returns>A list of all the contexts that should be shown on startup.</returns>
        public static List<MetaHubContext> FindStartupContexts()
        {
            var contexts = FindAllContextAssets<MetaHubContext>();
            return contexts.FindAll(context => context.ShowOnStartup);
        }
    }
}