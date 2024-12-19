// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Pages;
using Meta.Tutorial.Framework.Hub.UIComponents;
using Meta.Tutorial.Framework.Hub.Utilities;
using Meta.Tutorial.Framework.Hub.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Windows
{
    public class TutorialFrameworkHub : MetaHubBase
    {
        public const string CONTEXT = "TutorialFrameworkHub";

        public static readonly List<string> Contexts = new()
        {
            CONTEXT
        };

        protected override string TelemetryContext => Telemetry.TUTORIAL_HUB_CONTEXT;

        private List<string> m_tutorialFrameworkContexts;
        private Dictionary<string, PageReference> m_pages;
        private static TutorialFrameworkHub s_instance;

        public override List<string> ContextFilter
        {
            get
            {
                if (null == m_tutorialFrameworkContexts || m_tutorialFrameworkContexts.Count == 0)
                {
                    m_tutorialFrameworkContexts = Contexts.ToList();
                    AddChildContexts(m_tutorialFrameworkContexts);
                }

                return m_tutorialFrameworkContexts;
            }
        }

        public static string GetPageId(string pageName)
        {
            return CONTEXT + "::" + pageName;
        }

        [MenuItem("Meta/Tutorial Hub/&Show Hub")]
        public static MetaHubBase ShowWindow()
            => s_instance = ShowWindow<TutorialFrameworkHub>(Contexts.ToArray());

        protected override void OnEnable()
        {
            m_tutorialFrameworkContexts = null;
            maxSize = new Vector2(2000, 10000);
            base.OnEnable();
        }

        // We don't need this to be easily accessible. It's only meant to be used for tutorial development purposes.
        // Devs who want to author new tutorials should manually add "META_EDIT_TUTORIALS" to the project's Scripting Define Symbols.
        /*
        [MenuItem("Meta/Tutorial Hub/Toggle Edit Tutorials/Enable", priority = 1)]
        private static void EnableEditTutorials()
        {
            ToggleEditTutorials(true);
        }
        */

#if META_EDIT_TUTORIALS
        [MenuItem("Meta/Tutorial Hub/Edit Tutorials/Disable", priority = 2)]
#endif
        private static void DisableEditTutorials()
        {
            ToggleEditTutorials(false);
        }

        private static void ToggleEditTutorials(bool bEnabled)
        {
            // add EDIT_TUTORIALS to the list of defines
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!defines.Contains("META_EDIT_TUTORIALS") && bEnabled)
            {
                defines += ";META_EDIT_TUTORIALS";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
            else if (defines.Contains("META_EDIT_TUTORIALS") && !bEnabled)
            {
                defines = defines.Replace(";META_EDIT_TUTORIALS", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
        }

        /// <summary>
        /// The title content for the window.
        /// </summary>
        public override GUIContent TitleContent => new("Meta Tutorials Hub", m_icon);

        /// <summary>
        /// Updates the context filter.
        /// </summary>
        public override void UpdateContextFilter()
        {
            if (null == m_rootPageGroup)
            {
                m_rootPageGroup = new PageGroup(null, DrawPageEntry);
            }

            m_contexts = ContextFinder.FindAllContextAssets<MetaHubContext>().FindAll(context => context is BaseTutorialHubContext).ToList();

            var activePageSet = false;
            m_pageGroups.Clear();
            foreach (BaseTutorialHubContext context in m_contexts)
            {
                if (context.TutorialName == null)
                {
                    continue;
                }

                m_contextMap[context.TutorialName] = context; // Add to map for quick lookup

                var pageGroup = m_pageGroups.Find(pg
                    => pg is TutorialPageGroup tpg
                    && string.Compare(tpg.TutorialContext, context.TutorialName, StringComparison.OrdinalIgnoreCase) == 0
                    );
                if (pageGroup == null)
                {
                    pageGroup = new TutorialPageGroup(context, DrawPageEntry)
                    {
                        Logo = context.TutorialConfig.NavHeaderImage
                    };
                    m_pageGroups.Add(pageGroup);
                }

                var pages = context.CreatePageReferences();
                if (pages == null || pages.Length == 0)
                {
#if META_EDIT_TUTORIALS
                    Debug.LogWarning(context.name + " has no pages", this);
#endif
                    continue;
                }

                pageGroup.AddPagesWithParent(pages);
                m_pageGroupMap[context] = pageGroup;

                if (!activePageSet)
                {
                    ActivePage = pages[0].Page;
                    SelectedPage = pages[0].PageId;
                    activePageSet = true;
                }

                foreach (var page in pages)
                {
                    page.Page.RegisterWindow(this);
                }
            }

            // Sort the pages by priority then alpha
            foreach (var group in m_pageGroupMap.Values)
            {
                group.Sort();
            }
        }

        protected override bool IsGroupVisible(PageGroup group) => true; // for now, always show all groups

        protected override void DrawLeftPanel()
        {
            _ = EditorGUILayout.BeginVertical(GUILayout.Width(m_leftPanelWidth));
            {
                m_leftScroll = GUILayout.BeginScrollView(m_leftScroll);
                {
                    foreach (var context in m_pageGroups)
                    {
                        DrawPageGroup(context);
                        GUILayout.Space(20.0f); // Add some space between groups
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.FlexibleSpace();

                DrawShowOnStartupToggle();
            }
            EditorGUILayout.EndVertical();
        }

        protected override void DrawPageGroup(PageGroup group)
        {
            if (!IsGroupVisible(group))
            {
                return;
            }

            // Draw logo image
            var logo = group.Logo;
            if (logo)
            {
                var paddedWidth = m_leftPanelWidth - 10.0f;
                GUILayout.Box(
                    logo,
                    GUILayout.Width(paddedWidth),
                    GUILayout.Height(paddedWidth / logo.GetAspectRatio())
                );
            }


            var searchMatchedGroupContext = ContextFilter.Count != 1 && group.IsMatch(m_searchString);

            var pages = new List<PageReference>();

            if (!IsSearchEmpty && !searchMatchedGroupContext)
            {
                foreach (var page in group.Pages)
                {
                    if (page.IsMatch(m_searchString))
                    {
                        pages.Add(page);
                    }
                }
            }

            if (!IsSearchEmpty)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    DrawPageEntry(pages[i]);
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(3.0f);

                var tutorialConfig = (group.Context as BaseTutorialHubContext).TutorialConfig;
                Styles.GroupTitle.Draw(tutorialConfig.Name);

                GUILayout.EndHorizontal();

                group.Hierarchy.Draw();

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        static TutorialFrameworkHub()
            => EditorGUI.hyperLinkClicked += EditorGUI_hyperLinkClicked;

        private static void EditorGUI_hyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if (window == s_instance)
            {
                var hyperLinkData = args.hyperLinkData;
                if (hyperLinkData != null && hyperLinkData.ContainsKey("href"))
                {
                    var href = hyperLinkData["href"];
                    var split = href.Split('#');
                    href = split[0];
                    var anchor = split.Length > 1 ? split[1] : null;
                    if (href.ToLower().EndsWith(".md"))
                    {
                        // find the context that uses this markdown in the current instance's context
                        var context = s_instance.m_contexts.Find(context =>
                        {
                            return context is TutorialMarkdownContext tutorialContext
                                   && tutorialContext.MarkdownPath == href;
                        });

                        if (context && context is TutorialMarkdownContext tutorialContext)
                        {
                            if (anchor != null)
                            {
                                try
                                {
                                    var sectionTitle = tutorialContext.PageConfigs.First(p => string.Compare(anchor, p.SectionTitleAsAnchorId) == 0).SectionTitle;
                                    foreach (var pageGroup in s_instance.m_pageGroups)
                                    {
                                        foreach (var page in pageGroup.Pages)
                                        {
                                            if (string.Compare(sectionTitle, page.Info.Name) == 0)
                                            {
                                                s_instance.ActivePage = page.Page;
                                                break;
                                            }
                                        }
                                    }

                                    // var page = instance._rootPageGroup.Pages.First(p => string.Compare(sectionTitle, p.info.Name) == 0).page;
                                    // instance.ActivePage = page;
                                }
                                catch (Exception)
                                {
#if META_EDIT_TUTORIALS
                                    Debug.LogWarning($"Couldn't find anchor '{anchor}' in {href}");
#endif
                                    anchor = null;
                                }
                            }
                            else // if (string.IsNullOrEmpty(anchor))
                            {
                                // Find the first page in the instance that has the same title as the context: context.Title
                                foreach (var group in s_instance.m_pageGroups)
                                {
                                    foreach (var page in group.Pages)
                                    {
                                        var sepIdx = page.Info.HierarchyName.IndexOf('/'); // since we have no anchor, pick the first page that matches the parent name
                                        var parentNameIfChild = sepIdx > -1 ? page.Info.HierarchyName[..sepIdx] : page.Info.HierarchyName;
                                        if (string.Compare(context.Title, parentNameIfChild) == 0 || string.Compare(context.Title, page.Info.Name) == 0)
                                        {
                                            s_instance.ActivePage = page.Page;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var openingPath = Path.Combine(Directory.GetCurrentDirectory(), href);
                            Debug.Log("Opening: " + openingPath);
                            System.Diagnostics.Process.Start(openingPath);
                        }
                    }
                    else if (string.Compare(href, "TUT_REF") == 0)
                    { // this is a link generated by a DynamicReference, which we can recreate from additional parameters in the <a> tag
                        DynamicReference.Invoke(hyperLinkData);
                    }
                    else if (href.StartsWith("./Assets") || href.StartsWith("/Assets") || href.StartsWith("./Packages") || href.StartsWith("/Packages"))
                    {
                        var assetPath = href[(href.IndexOf('/') + 1)..]; // remove the leading './' or '/'
                        // this is a link to a file in the project, we just need to find it and ping it
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (asset)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                        else
                        {
                            Debug.LogWarning($"Couldn't find asset at path: {assetPath}");
                        }
                    }
                    else if (href.EndsWith("LICENSE"))
                    {
                        // this is a link to a license file, we can just open it
                        var licensePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), href));
                        System.Diagnostics.Process.Start(licensePath);
                    }
                    else
                    {
#if META_EDIT_TUTORIALS
                        Debug.LogWarning($"Unhandled link: {href}");
#endif
                    }
                }
            }
        }
    }
}