// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Tutorial.Framework.Hub.Attributes;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    /// <summary>
    /// Finds all pages in the project.
    /// </summary>
    internal static class PageFinder
    {
        private static List<Type> s_pages;

        /// <summary>
        /// Finds all pages in the project.
        /// </summary>
        /// <returns>A list of all page types in the project.</returns>
        internal static List<Type> FindPages()
        {
            if (null == s_pages)
            {
                s_pages = ReflectionUtils.GetTypesWithAttribute<MetaHubPageAttribute>();
            }

            return s_pages;
        }

        /// <summary>
        /// Gets the page info for a given type.
        /// </summary>
        /// <param name="type">The type for which to get the page info.</param>
        /// <returns>The page info for the given type, or null if the type does not have page info.</returns>
        internal static MetaHubPageAttribute GetPageInfo(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(MetaHubPageAttribute), false);
            return attributes.Length > 0 ? (MetaHubPageAttribute)attributes[0] : null;
        }

        /// <summary>
        /// Finds all pages of a given type in the project.
        /// </summary>
        /// <param name="t">The type of page to find. This type must be a subclass of <see cref="ScriptableObject"/>.</param>
        /// <returns>A list of all pages of the given type in the project.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified type is not a subclass of <see cref="ScriptableObject"/>.</exception>
        internal static List<ScriptableObject> FindPages(Type t)
        {
            return !typeof(ScriptableObject).IsAssignableFrom(t)
                ? throw new ArgumentException("The specified type must be a ScriptableObject.")
                : FindPages(t.Name);
        }

        /// <summary>
        /// Finds all pages of a given type in the project.
        /// </summary>
        /// <param name="type">The name of the type of page to find. This should be the name of the type as it appears in the project.</param>
        /// <returns>A list of all pages of the given type in the project.</returns>
        public static List<ScriptableObject> FindPages(string type)
        {
            var pages = new List<ScriptableObject>();
            var guids = AssetDatabase.FindAssets($"t:{type}");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    pages.Add(asset);
                }
            }

            pages.Sort((a, b) =>
            {
                var aInfo = GetPageInfo(a.GetType());

                if (null == aInfo)
                {
                    return -1;
                }

                var bInfo = GetPageInfo(b.GetType());

                return null == bInfo ? 1 : aInfo.CompareTo(bInfo);
            });

            return pages;
        }
    }
}