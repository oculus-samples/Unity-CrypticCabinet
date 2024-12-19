// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using Meta.Tutorial.Framework.Hub.Parsing;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages.Markdown
{
    /// <summary>
    /// A page that displays Markdown content.
    /// </summary>
    [ScriptableObjectMetaHubPage]
    public class MetaHubMarkdownPage : ScriptableObject, IPageInfo, IComparable<IPageInfo>
    {
        [SerializeField, Tooltip("The display name of the page")]
        private string m_displayName;

        [SerializeField, Tooltip("Hierarchy Name")]
        private string m_hierarchyName;

        [SerializeField, Tooltip("The context this page will fall under")]
        private MetaHubContext m_context;

        [SerializeField, Tooltip("The Markdown file to display")]
        private TextAsset m_markdownFile;

        [SerializeField, Tooltip("The sorting priority of the page")]
        private int m_priority = 0;

        /// <inheritdoc cref="IPageInfo.Name"/>
        public string Name => m_displayName ?? name;

        /// <inheritdoc cref="IPageInfo.Context"/>
        public string Context => m_context.Name;

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public int Priority => m_priority;

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public string HierarchyName => m_hierarchyName;

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName => m_context.ProjectName;

        public string TelemetryContext => m_context.TelemetryContext;

        /// <summary>
        /// The Markdown file to display.
        /// </summary>
        public TextAsset MarkdownFile => m_markdownFile;

        public ParsedMD ParsedMarkdown { get; private set; }

        public ParsedMD TryParsedMarkdown
        {
            get
            {
                if (ParsedMarkdown == null)
                {
                    var stripXML = MarkdownText.Contains("<img");
                    ParsedMarkdown = ParsedMD.LoadFromString(MarkdownText, MarkdownRoot, false, stripXML);
                }
                return ParsedMarkdown;
            }
        }

        [SerializeField][HideInInspector] private string m_overrideMarkdownText;
        [SerializeField][HideInInspector] private string m_overrideMarkdownRoot;
        public string MarkdownText => string.IsNullOrEmpty(m_overrideMarkdownText) ? m_markdownFile?.text : m_overrideMarkdownText;

        public string MarkdownRoot =>
            string.IsNullOrEmpty(m_overrideMarkdownRoot)
            ? (m_markdownFile ? AssetDatabase.GetAssetPath(m_markdownFile) : "")
            : m_overrideMarkdownRoot;

        public void OverrideMarkdownText(string text = "", string root = "./", bool reduceTitleLevelBy1 = false, bool stripXML = true)
        {
            m_overrideMarkdownText = text;
            m_overrideMarkdownRoot = root;

            if (string.IsNullOrEmpty(m_overrideMarkdownText))
            {
                ParsedMarkdown = null; // clear the parsed markdown
            }
            else
            {
                ParsedMarkdown = ParsedMD.LoadFromString(text, root, reduceTitleLevelBy1, stripXML);
            }
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }

        /// <summary>
        /// Override the context of this page.
        /// </summary>
        /// <param name="prefixHierarchyName">Prepended to the displayName for the foldout hierarchy to ensure unique value across many pages.</param>
        public void OverrideContext(MetaHubContext context, int overridePriority = -1, string prefixHierarchyName = "", string displayName = null)
        {
            m_context = context;
            m_priority = overridePriority < 0 ? context.Priority : overridePriority;
            m_displayName = string.IsNullOrEmpty(displayName) ? context.Title : displayName;
            m_hierarchyName = prefixHierarchyName + m_displayName;
        }
    }
}