// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Pages;
using Meta.Tutorial.Framework.Hub.Pages.Markdown;
using Meta.Tutorial.Framework.Hub.Parsing;
using Meta.Tutorial.Framework.Windows;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{
    [MetaHubContext(TutorialFrameworkHub.CONTEXT)]
#if META_EDIT_TUTORIALS
    [CreateAssetMenu(fileName = "TutorialMarkdownContext", menuName = "Meta Tutorial Hub/Tutorial Markdown Context", order = 2)]
#endif
    public class TutorialMarkdownContext : BaseTutorialHubContext
    {
        [Serializable]
        public class PageConfig
        {
            public string SectionTitle; // The title of the section
            public bool ShowSectionInContext; // If true, the section will be included in the context of this section
            public bool ShowSectionAsChild; // If true, the section will be shown as a child of this section
            public bool HideTitle; // If true, the section will not show the title

            // replaces all spaces with '-' and strips out all other non-alphanumeric characters
            public string SectionTitleAsAnchorId => Regex.Replace(SectionTitle.Replace(" ", "-"), @"[^a-zA-Z0-9\-]", string.Empty).ToLower();
        }

        [SerializeField] private string m_markdownPath = "./README.md";
        [SerializeField, Tooltip("When all content is a level 1 because the header is level 0 we can bring them to level 0")] 
        private bool m_reduceTitleLevelBy1 = false;
        public string MarkdownPath => m_markdownPath;

        [SerializeField] private List<PageConfig> m_pageConfigs;

        private bool m_isValid;
        private string m_lastLoadedPath;
        private bool m_lastReduceTitleLevelValue;
        private Dictionary<string, PageConfig> m_savedPageConfigs = new ();
        private ParsedMD m_parsedMarkdown;

        /// <summary>
        /// Returns true if all pageConfig entries have showSectionInContext set to true
        /// </summary>
        private bool IncludeAllInContext
        {
            get
            {
                if (m_pageConfigs == null || m_pageConfigs.Count == 0)
                {
                    return true;
                }
                foreach (var pageConfig in m_pageConfigs)
                {
                    if (!pageConfig.ShowSectionInContext)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private bool AnyAsChildren
        {
            get
            {
                if (m_pageConfigs == null || m_pageConfigs.Count == 0)
                {
                    return false;
                }
                foreach (var pageConfig in m_pageConfigs)
                {
                    if (pageConfig.ShowSectionAsChild)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public IReadOnlyList<PageConfig> PageConfigs => m_pageConfigs;

        private string ReadTextFromFile(string path)
            => System.IO.File.ReadAllText(path);

        public override PageReference[] CreatePageReferences(bool forceCreate = false)
        {
            var rootPath = System.IO.Path.GetDirectoryName(m_markdownPath) + '/';
            var pageAssetPath = GetRootPageAssetPath();
            var isInstance = CreateOrLoadInstance<MetaHubMarkdownPage>(pageAssetPath, out var markdownPage, forceCreate);
            if (isInstance)
            {
                markdownPage.OverrideMarkdownText(ReadTextFromFile(m_markdownPath), rootPath, m_reduceTitleLevelBy1);
                markdownPage.OverrideContext(this);
            }

            if (markdownPage == null)
            {
                GenerateParsedMarkdown();
            }
            else
            {
                m_parsedMarkdown = markdownPage.ParsedMarkdown;
            }

            if (IncludeAllInContext || (markdownPage && !isInstance && !AnyAsChildren))
            {
                markdownPage = InstanceToAsset(markdownPage, pageAssetPath);
                var soPage = new ScriptableObjectPage(markdownPage, TutorialName, markdownPage.HierarchyName, markdownPage.Priority, ShowBanner ? Banner : null);
                return new[]
                {
                    new PageReference()
                    {
                        Page = soPage,
                        Info = soPage
                    }
                };
            }
            else
            {
                ParsedMD.Section aggregator = null;
                var level0Indices = m_parsedMarkdown.Level0SectionIndices;
                if (isInstance)
                {
                    var processingParsedMarkdown = markdownPage.ParsedMarkdown;
                    for (var i = 0; i < level0Indices.Length; i++)
                    {
                        if (m_pageConfigs[i].ShowSectionInContext)
                        {
                            var content = processingParsedMarkdown.GetCollapsedSection0(i, m_pageConfigs[i].HideTitle);
                            if (aggregator == null)
                            {
                                aggregator = content;
                            }
                            else
                            {
                                aggregator.Append(content);
                            }
                        }
                    }
                }

                var ret = new List<PageReference>();
                if (aggregator != null)
                {
                    markdownPage.OverrideMarkdownText(aggregator.ToString(), rootPath, m_reduceTitleLevelBy1, false); // the aggregator was already stripped of xml
                    markdownPage = InstanceToAsset(markdownPage, pageAssetPath);

                    var soPage = new ScriptableObjectPage(markdownPage, TutorialName, markdownPage.HierarchyName, markdownPage.Priority, ShowBanner ? Banner : null);
                    ret.Add(new PageReference()
                    {
                        Page = soPage,
                        Info = soPage
                    });
                }

                // now create pages and references for the children
                for (var i = 0; i < level0Indices.Length; i++)
                {
                    if (m_pageConfigs[i].ShowSectionAsChild)
                    {
                        pageAssetPath = GetChildPageAssetPath(m_pageConfigs[i].SectionTitle);
                        isInstance = CreateOrLoadInstance<MetaHubMarkdownPage>(pageAssetPath, out var childPage, forceCreate);
                        if (isInstance)
                        {
                            childPage.OverrideMarkdownText(m_parsedMarkdown.GetCollapsedSection0(i, m_pageConfigs[i].HideTitle).ToString(), rootPath, m_reduceTitleLevelBy1,false); // xml is already stripped
                            childPage.OverrideContext(this, prefixHierarchyName: $"{markdownPage.HierarchyName}/", displayName: m_pageConfigs[i].SectionTitle);
                            childPage = InstanceToAsset(childPage, pageAssetPath);
                        }

                        var childSoPage = new ScriptableObjectPage(childPage, TutorialName, childPage.HierarchyName, childPage.Priority);
                        ret.Add(new PageReference()
                        {
                            Page = childSoPage,
                            Info = childSoPage
                        });
                    }
                }

                return ret.ToArray();
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(TutorialName))
            {
                // Debug.LogError("TutorialContext is empty");
                m_isValid = false;
            }
            else if (string.IsNullOrEmpty(m_markdownPath))
            {
                // Debug.LogError("Path to root doc is empty");
                m_isValid = false;
            }
            else if (!System.IO.File.Exists(m_markdownPath))
            {
                // Debug.LogError("Path to root doc does not exist");
                m_isValid = false;
            }
            else
            {
                m_isValid = true;
            }

            if (!m_isValid)
            {
                m_parsedMarkdown = null;
                SavePageConfigs();
                m_pageConfigs = null;
                m_lastLoadedPath = null;
                return;
            }

            if (m_parsedMarkdown == null ||
                string.Compare(m_lastLoadedPath, m_markdownPath, StringComparison.OrdinalIgnoreCase) != 0 ||
                m_reduceTitleLevelBy1 != m_lastReduceTitleLevelValue)
            {
                m_lastLoadedPath = m_markdownPath;
                m_lastReduceTitleLevelValue = m_reduceTitleLevelBy1;
                if (m_isValid)
                {
                    GenerateParsedMarkdown();
                }
            }

            if (m_parsedMarkdown != null)
            {
                var level0Indices = m_parsedMarkdown.Level0SectionIndices;
                if (m_pageConfigs == null || m_pageConfigs.Count != level0Indices.Length)
                {
                    SavePageConfigs();
                    m_pageConfigs = new List<PageConfig>();
                    foreach (var t in level0Indices)
                    {
                        var title = m_parsedMarkdown.Sections[t].Title;
                        m_savedPageConfigs.TryGetValue(title, out var restorePageConfig);
                        m_pageConfigs.Add(new PageConfig()
                        {
                            SectionTitle = title,
                            ShowSectionInContext = restorePageConfig != null ? restorePageConfig.ShowSectionInContext: true,
                            ShowSectionAsChild = restorePageConfig != null ? restorePageConfig.ShowSectionAsChild: false,
                            HideTitle = restorePageConfig != null ? restorePageConfig.HideTitle: false,
                        });
                    }
                }
                else
                {
                    for (var i = 0; i < m_pageConfigs.Count; i++)
                    {
                        if (string.Compare(m_pageConfigs[i].SectionTitle, m_parsedMarkdown.Sections[level0Indices[i]].Title, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            var pageConfig = m_pageConfigs[i];
                            pageConfig.SectionTitle = m_parsedMarkdown.Sections[level0Indices[i]].Title;
                            m_pageConfigs[i] = pageConfig;
                        }
                    }
                }
            }
        }

        public void ToggleAllPageConfigsToAppearInContext()
        {
            if (m_pageConfigs == null || m_pageConfigs.Count == 0)
            {
                return;
            }

            var newValue = !m_pageConfigs[0].ShowSectionInContext;
            foreach (var pageConfig in m_pageConfigs)
            {
                pageConfig.ShowSectionInContext = newValue;
            }
        }

        public void ToggleAllPageConfigsToAppearAsChildren()
        {
            if (m_pageConfigs == null || m_pageConfigs.Count == 0)
            {
                return;
            }

            var newValue = !m_pageConfigs[0].ShowSectionAsChild;
            foreach (var pageConfig in m_pageConfigs)
            {
                pageConfig.ShowSectionAsChild = newValue;
            }
        }
        
        private void GenerateParsedMarkdown()
        {
            var markdownPage = CreateInstance<MetaHubMarkdownPage>();
            markdownPage.OverrideMarkdownText(ReadTextFromFile(m_markdownPath), System.IO.Path.GetDirectoryName(m_markdownPath) + '/', m_reduceTitleLevelBy1);
            markdownPage.OverrideContext(this, prefixHierarchyName: TutorialName);

            m_parsedMarkdown = markdownPage.ParsedMarkdown;
            if (Application.isPlaying)
            {
                Destroy(markdownPage);
            }
            else
            {
                DestroyImmediate(markdownPage);
            }
        }

        private void SavePageConfigs()
        {
            m_savedPageConfigs ??= new Dictionary<string, PageConfig>();
            if (m_pageConfigs != null)
            {
                foreach (var config in m_pageConfigs)
                {
                    m_savedPageConfigs[config.SectionTitle] = config;
                }
            }
        }
    }
}