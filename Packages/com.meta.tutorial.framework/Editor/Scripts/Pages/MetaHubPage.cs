// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using Meta.Tutorial.Framework.Hub.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    /// <summary>
    /// A page that can be displayed in a <see cref="MetaHubBase"/>.
    /// </summary>
    public class MetaHubPage : ScriptableObject, IMetaHubPage, IPageInfo, IComparable<IPageInfo>
    {
        [SerializeField, Tooltip("The context this page will fall under")]
        private MetaHubContext m_context;

        [SerializeField, Tooltip("A prefix that will show up before the name of the page. This is a good place to insert page hierarchy etc.")]
        private string m_hierarchyName;

        [SerializeField, Tooltip("The sorting priority of the page")]
        private int m_priority;

        /// <inheritdoc cref="IPageInfo.Name"/>
        public virtual string Name => name;

        /// <inheritdoc cref="IPageInfo.Context"/>
        public virtual string Context => m_context?.Name;

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public virtual int Priority => m_priority;

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public virtual string HierarchyName => m_hierarchyName;

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public virtual string ProjectName => m_context?.ProjectName;

        /// <summary>
        /// The Parent window using this page.
        /// </summary>
        protected EditorWindow ParentWindow { get; private set; }

        /// <inheritdoc cref="IMetaHubPage.OnGUI"/>
        public virtual void OnGUI()
        {
        }

        /// <inheritdoc cref="IMetaHubPage.RegisterWindow"/>
        public void RegisterWindow(EditorWindow window)
        {
            ParentWindow = window;
        }

        /// <inheritdoc cref="IMetaHubPage.UnregisterWindow"/>
        public void UnregisterWindow(EditorWindow window)
        {
            if (window == ParentWindow)
            {
                ParentWindow = null;
            }
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }

        protected void CustomValues(string name, MetaHubContext context, int priority, string hierarchyName)
        {
            this.name = name;
            m_context = context;
            m_priority = priority;
            m_hierarchyName = hierarchyName;
        }

        public static MetaHubPage Custom(string name, MetaHubContext context, int priority, string hierarchyName)
        {
            var customInstance = CreateInstance<MetaHubPage>();
            customInstance.CustomValues(name, context, priority, hierarchyName);
            return customInstance;
        }
    }
}