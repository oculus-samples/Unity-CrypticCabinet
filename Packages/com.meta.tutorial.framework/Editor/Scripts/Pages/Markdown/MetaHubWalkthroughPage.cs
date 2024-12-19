// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    [ScriptableObjectMetaHubPage]
    public class MetaHubWalkthroughPage : ScriptableObject, IPageInfo
    {
        [SerializeField, Tooltip("The display name of the page")]
        private string m_displayName;

        [SerializeField, Tooltip("A prefix that will show up before the name of the page. This is a good place to insert page hierarchy etc.")]
        private string m_hierarchyName;

        [SerializeField, Tooltip("The context this page will fall under")]
        private MetaHubContext m_context;

        [SerializeField, Tooltip("The Markdown file to display")]
        private TextAsset m_markdownFile;

        [SerializeField, Tooltip("The sorting priority of the page")]
        private int m_priority = 0;

        /// <inheritdoc cref="IPageInfo.Name"/>
        public string Name
        {
            get => m_displayName ?? name;
            set => m_displayName = value;
        }

        /// <inheritdoc cref="IPageInfo.Context"/>
        public string Context => m_context.Name;

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public int Priority => m_priority;

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public string HierarchyName => m_hierarchyName;

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName => m_context?.ProjectName;

        public string TelemetryContext => m_context?.TelemetryContext;

        [field: SerializeField]
        public TutorialReferencesContext.DynamicReferenceEntry[] References { get; set; }

        public void OverrideContext(MetaHubContext metaHubContext)
        {
            m_context = metaHubContext;
        }
    }
}