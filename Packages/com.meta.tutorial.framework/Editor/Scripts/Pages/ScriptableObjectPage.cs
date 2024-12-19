// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Reflection;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    /// <summary>
    /// A page that displays a ScriptableObject in the Meta Hub.
    /// </summary>
    internal class ScriptableObjectPage : IMetaHubPage, IPageInfo, IComparable<IPageInfo>
    {
        private ScriptableObject m_page;

        /// <inheritdoc cref="IPageInfo.Name"/>
        public string Name { get; private set; }

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public string HierarchyName { get; private set; } = "";

        /// <inheritdoc cref="IPageInfo.Context"/>
        public string Context => ProjectName;

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public int Priority { get; private set; }

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName { get; private set; }

        /// <summary>
        /// The editor for the ScriptableObject.
        /// </summary>
        public Editor CustomEditor { get; private set; }

        /// <summary>
        /// Whether or not to show the banner for the page.
        /// </summary>
        public Contexts.TutorialConfig.Banner Banner { get; }

        /// <summary>
        /// The Parent window using this page.
        /// </summary>
        protected EditorWindow ParentWindow { get; private set; }

        private ScriptableObject Page
        {
            get
            {
                if (!m_page && !string.IsNullOrEmpty(m_scriptableObjectCachedPath))
                {
                    m_page = AssetDatabase.LoadAssetAtPath<ScriptableObject>(m_scriptableObjectCachedPath);
                }

                return m_page;
            }
        }

        private string m_scriptableObjectCachedPath; // Cached path to the ScriptableObject asset, so we can reload it if it drops between scene changes.

        /// <summary>
        /// Constructs a new <see cref="ScriptableObject"/> page.
        /// </summary>
        /// <param name="page">The <see cref="ScriptableObject"/> to display.</param>
        /// <param name="pageInfo">The <see cref="MetaHubPageAttribute"/> that contains the page information.</param>
        public ScriptableObjectPage(ScriptableObject page, MetaHubPageAttribute pageInfo)
        {
            m_page = page;
            ProjectName = pageInfo.Context;
            Priority = pageInfo.Priority;
            HierarchyName = pageInfo.HierarchyName;

            UpdatePageInfo();
        }

        /// <summary>
        /// Constructs a new <see cref="ScriptableObject"/> page.
        /// </summary>
        /// <param name="page">The <see cref="ScriptableObject"/> to display.</param>
        /// <param name="context">The context for which the page is relevant.</param>
        /// <param name="hierarchyName">The hierarchy name to distinguish the page from others with the same name.</param>
        /// <param name="priority">The priority of the page relative to other pages.</param>
        public ScriptableObjectPage(ScriptableObject page, string context, string hierarchyName = "", int priority = 0, Contexts.TutorialConfig.Banner banner = null)
        {
            m_page = page;
            ProjectName = context;
            Priority = priority;
            HierarchyName = hierarchyName;
            Banner = banner;

            UpdatePageInfo();
        }

        /// <inheritdoc cref="IMetaHubPage.OnGUI()"/>
        public void OnGUI()
        {
            if (Page)
            {
                // Create an editor for the assigned ScriptableObject
                if (CustomEditor == null || CustomEditor.target != Page)
                {
                    CustomEditor = Editor.CreateEditor(Page);
                    if (ParentWindow != null && CustomEditor is IWindowUpdater)
                    {
                        (CustomEditor as IWindowUpdater)?.RegisterWindow(ParentWindow);
                    }
                }

                // Render the ScriptableObject with its default editor
                CustomEditor.OnInspectorGUI();
            }
        }

        public void OnEnable()
        {
            InvokeLifecyle("OnEnable");
        }
        
        public void OnDisable()
        {
            InvokeLifecyle("OnDisable");
        }
        
        private void InvokeLifecyle(string lifecycleMethod)
        {
            if (null == CustomEditor)
            {
                return;
            }

            var method = CustomEditor.GetType().GetMethod(lifecycleMethod, BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance);
            _ = (method?.Invoke(CustomEditor, new object[0]));
        }

        /// <inheritdoc cref="IMetaHubPage.RegisterWindow"/>
        public void RegisterWindow(EditorWindow window)
        {
            ParentWindow = window;
            if (CustomEditor != null)
            {
                (CustomEditor as IWindowUpdater)?.RegisterWindow(ParentWindow);
            }
        }

        /// <inheritdoc cref="IMetaHubPage.UnregisterWindow"/>
        public void UnregisterWindow(EditorWindow window)
        {
            if (window == ParentWindow)
            {
                ParentWindow = null;
                if (CustomEditor != null)
                {
                    (CustomEditor as IWindowUpdater)?.UnregisterWindow(ParentWindow);
                }
            }
        }

        /// <summary>
        /// Updates the page information based on the page information.
        /// </summary>
        private void UpdatePageInfo()
        {
            if (m_page is IPageInfo info)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    Name = info.Name;
                }

                if (string.IsNullOrEmpty(ProjectName))
                {
                    ProjectName = info.Context;
                }

                if (string.IsNullOrEmpty(HierarchyName))
                {
                    HierarchyName = info.HierarchyName;
                }

                if (Priority == 0)
                {
                    Priority = info.Priority;
                }
            }
            else
            {
                Name = m_page.name;
            }

            m_scriptableObjectCachedPath = AssetDatabase.GetAssetPath(m_page);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }
    }
}