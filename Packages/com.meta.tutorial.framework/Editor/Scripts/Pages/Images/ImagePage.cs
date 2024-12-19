// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.Tutorial.Framework.Hub.Pages.Images
{
    /// <summary>
    /// A simple page that displays an image.
    /// </summary>
    [ScriptableObjectMetaHubPage]
    public class ImagePage : ScriptableObject, IPageInfo, IComparable<IPageInfo>
    {
        [SerializeField, Tooltip("The name of the page. If not set, the name of the asset will be used.")]
        private string m_displayName;

        [SerializeField, Tooltip("A prefix that will show up before the name of the page. This is a good place to insert page hierarchy etc.")]
        private string m_hierarchyName;

        [SerializeField, Tooltip("The context this page will fall under")]
        private MetaHubContext m_context;

        [SerializeField, Tooltip("The sorting priority of the page")]
        private int m_priority = 0;

        [SerializeField, FormerlySerializedAs("image"), Tooltip("The image to display")]
        private Texture2D m_image;

        /// <inheritdoc cref="IPageInfo.Name"/>
        public string Name => m_displayName ?? name;

        /// <inheritdoc cref="IPageInfo.Context"/>
        public string Context => m_context?.Name;

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public int Priority => m_priority;

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public string HierarchyName => m_hierarchyName;

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName => m_context?.ProjectName;

        /// <summary>
        /// The image to display.
        /// </summary>
        internal Texture2D Image => m_image;

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }
    }
}