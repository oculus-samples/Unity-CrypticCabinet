// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using Meta.Tutorial.Framework.Hub.Pages;
using Meta.Tutorial.Framework.Hub.UIComponents;
using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Windows
{
    /// <summary>
    /// The MetaHubBase class is the main window for the Meta Tutorial Framework. It is responsible for displaying the Meta
    /// Hub UI and managing the pages that are displayed.
    /// </summary>
    public class MetaHubBase : EditorWindow
    {
        /// <summary>
        /// The default context for the Meta Hub window.
        /// </summary>
        public const string DEFAULT_CONTEXT = "";

        private const string SHOW_ON_STARTUP_KEY = "Meta_Hub_Show_On_Startup";

        [SerializeField, Tooltip("The icon to display in the window title bar.")]
        protected Texture2D m_icon;

        protected int m_leftPanelWidth = Styles.SIDE_PANEL_WIDTH;

        protected List<MetaHubContext> m_contexts = new();
        protected Dictionary<string, MetaHubContext> m_contextMap = new();

        protected List<PageGroup> m_pageGroups = new();
        protected Dictionary<MetaHubContext, PageGroup> m_pageGroupMap = new();

        private string m_selectedPageId;
        protected PageGroup m_rootPageGroup;

        protected string m_searchString = "";
        private IMetaHubPage m_selectedPage;
        protected Vector2 m_scroll;
        protected Vector2 m_leftScroll;

        protected virtual string TelemetryContext => Telemetry.META_HUB_CONTEXT;

        private static bool s_initialized;
        /// <summary>
        /// The primary context for the Meta Hub window.
        /// </summary>
        public MetaHubContext PrimaryContext
        {
            get
            {
                if (ContextFilter.Count > 0)
                {
                    var filter = ContextFilter.First();
                    if (m_contextMap.TryGetValue(filter, out var context))
                    {
                        return context;
                    }
                }

                return m_contexts[0];
            }
        }

        /// <summary>
        /// The title content for the window.
        /// </summary>
        public virtual GUIContent TitleContent => new(PrimaryContext.Title, m_icon);

        /// <summary>
        /// The filtered context for the Meta Hub window.
        /// </summary>
        public virtual List<string> ContextFilter => new();

        /// <summary>
        /// The selected page for the Meta Hub window.
        /// </summary>
        public string SelectedPage
        {
            get
            {
                if (null == m_selectedPageId)
                {
                    m_selectedPageId = SessionState.GetString(SessionKeySelPage, null);
                }

                return m_selectedPageId;
            }
            set
            {
                if (m_selectedPageId != value)
                {
                    m_selectedPageId = value;
                    Telemetry.OnNavigation(TelemetryContext, m_selectedPageId);

                    if (!string.IsNullOrEmpty(value))
                    {
                        SessionState.SetString(SessionKeySelPage, value);
                    }
                }
            }
        }

        private string SelectedProjectName => SelectedPage[..SelectedPage.IndexOf("::")];

        /// <summary>
        /// The key for the selected page in the session state.
        /// </summary>
        private string SessionKeySelPage => GetType().Namespace + "." + GetType().Name + "::SelectedPage";

        /// <summary>
        /// The active page for the Meta Hub window.
        /// </summary>
        protected IMetaHubPage ActivePage
        {
            get => m_selectedPage;
            set
            {
                if (m_selectedPage != value)
                {
                    if (null != m_selectedPage)
                    {
                        DisableSelectedPage(m_selectedPage);
                    }
                    m_selectedPage = value;
                    EnableSelectedPage(m_selectedPage);
                }
            }
        }

        /// <summary>
        /// Whether the search is empty.
        /// </summary>
        protected bool IsSearchEmpty => string.IsNullOrEmpty(m_searchString);

        /// <summary>
        /// Whether a page is selected.
        /// </summary>
        private bool HasSelectedPage => !string.IsNullOrEmpty(SelectedPage);

        /// <summary>
        /// Whether to show the Meta Hub window on startup.
        /// </summary>
        private static bool ShouldShowOnStartup
        {
            get => SessionState.GetBool(SHOW_ON_STARTUP_KEY, true);
            set => SessionState.SetBool(SHOW_ON_STARTUP_KEY, value);
        }

        /// <summary>
        /// Initializes the Meta Hub.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;

            if (ShouldShowOnStartup)
            {
                ShouldShowOnStartup = false;
                EditorApplication.delayCall += OpenStartupContext;
                Debug.Log("Meta Hub: Showing startup context on startup");
            }
        }

        /// <summary>
        /// Opens the startup context.
        /// </summary>
        private static void OpenStartupContext()
        {
            EditorApplication.delayCall -= OpenStartupContext;
            var startupContexts = ContextFinder.FindStartupContexts();

            foreach (var startupContext in startupContexts)
            {
                if (startupContext.ShowDefaultWindow())
                {
                    Debug.Log("Meta Hub: Showing startup context: " + startupContext.Name);
                    break;
                }
            }
        }

        /// <summary>
        /// Shows the Meta Hub window.
        /// </summary>
        /// <param name="contexts">The contexts to show in the window.</param>
        /// <typeparam name="T">The type of the Meta Hub window to show.</typeparam>
        /// <returns>The Meta Hub window that was shown.</returns>
        public static T ShowWindow<T>(params string[] contexts) where T : MetaHubBase
        {
            var window = GetWindow<T>();
            window.ActivePage = null;
            window.titleContent = new GUIContent(Styles.DEFAULT_HUB_TITLE);
            window.UpdateContextFilter();
            window.Show();
            return window;
        }

        /// <summary>
        /// Updates the context filter.
        /// </summary>
        public virtual void UpdateContextFilter()
        {
            if (null == m_rootPageGroup)
            {
                m_rootPageGroup = new PageGroup(null, DrawPageEntry);
            }

            m_contexts = ContextFinder.FindAllContextAssets<MetaHubContext>();

            foreach (var context in m_contexts)
            {
                m_contextMap[context.Name] = context; // Add to map for quick lookup
                var pageGroup = new PageGroup(context, DrawPageEntry);

                if (!m_pageGroupMap.ContainsKey(context))
                {
                    m_pageGroups.Add(pageGroup);
                    m_pageGroupMap[context] = pageGroup;
                }
            }

            foreach (var page in ContextFinder.FindAllContextAssets<MetaHubPage>())
            {
                AddPage(new PageReference
                {
                    Page = page,
                    Info = page
                });
            }

            foreach (var pageType in PageFinder.FindPages())
            {
                var pageInfo = PageFinder.GetPageInfo(pageType);

                if (pageInfo is ScriptableObjectMetaHubPageAttribute)
                {
                    var pages = PageFinder.FindPages(pageType);
                    foreach (var page in pages)
                    {
                        var soPage = new ScriptableObjectPage(page, pageInfo);
                        AddPage(new PageReference
                        {
                            Page = soPage,
                            Info = soPage
                        });
                    }
                }
                else
                {
                    var page = pageType.IsSubclassOf(typeof(ScriptableObject))
                        ? (IMetaHubPage)CreateInstance(pageType)
                        : Activator.CreateInstance(pageType) as IMetaHubPage;
                    if (page is IPageInfo info)
                    {
                        AddPage(new PageReference { Page = page, Info = info });
                    }
                    else
                    {
                        AddPage(new PageReference { Page = page, Info = pageInfo });
                    }
                }
            }

            // Sort the pages by priority then alpha
            foreach (var group in m_pageGroupMap.Values)
            {
                group.Sort();
            }
        }

        protected virtual void OnDestroy()
        {
            Telemetry.OnWindowClosed(TelemetryContext, m_selectedPageId);
        }

        /// <summary>
        /// Draws the Meta Hub window.
        /// </summary>
        protected virtual void OnGUI()
        {
            minSize = Styles.MinWindowSize;
            titleContent = TitleContent;

            GUILayout.BeginHorizontal();
            {
                m_searchString = Styles.SearchField.Draw(m_searchString);
            }
            GUILayout.EndHorizontal();

            _ = EditorGUILayout.BeginHorizontal();
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adds the parent contexts to the filter.
        /// </summary>
        /// <param name="filter">The filter to add the parent contexts to.</param>
        protected virtual void AddChildContexts(List<string> filter)
        {
            var parents = new HashSet<string>();
            foreach (var parent in filter)
            {
                _ = parents.Add(parent);
            }

            var contexts = ContextFinder.FindAllContextAssets<MetaHubContext>();
            foreach (var context in contexts)
            {
                if (!context)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Called when the window is enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            wantsMouseMove = true; // we want to track mouse movement for navigation hover
            UpdateContextFilter();
            Telemetry.OnWindowOpen(TelemetryContext, SelectedPage);
        }

        /// <summary>
        /// Draws the right panel of the Meta Hub window.
        /// </summary>
        protected virtual void DrawRightPanel()
        {
            // Apply the dark background style to the right panel
            var rect = Styles.PanelBackground.BeginVertical();
            {
                if (m_selectedPage is ScriptableObjectPage soPage)
                {
                    if (soPage.CustomEditor is IOverrideSize size && Event.current.type == EventType.Layout)
                    {
                        size.OverrideWidth = EditorGUIUtility.currentViewWidth - m_leftPanelWidth;
                    }

                    if (soPage.Banner != null)
                    {
                        DrawBannerGroup(rect, soPage.Banner.Image, soPage.Banner.Title, soPage.Banner.Description, soPage.Banner.Height, soPage.Banner.TextColorHex);
                    }
                    else
                    {
                        GUILayout.Space(Styles.Markdown.SPACING);
                    }
                }


                m_selectedPage?.OnGUI();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBannerGroup(Rect container, Texture2D banner, string header, string description, float bannerHeight = 120.0f, string textColorHex = "#FFFFFF")
        {
            GUILayout.BeginVertical(GUILayout.Height(bannerHeight));
            {
                // Draw the banner image
                var bannerRect = new Rect(container.x, container.y, container.width, bannerHeight);

                if (banner != null)
                {
                    GUI.DrawTexture(bannerRect, banner, ScaleMode.ScaleAndCrop);
                }
                else
                {
                    EditorGUI.DrawRect(bannerRect, new Color(0.1f, 0.1f, 0.1f)); // Fallback gray background
                }

                // Positioning text inside the banner
                var headerRect = new Rect(container.x + 5, bannerHeight * 2 / 3, container.width, 30.0f); // Padding inside the banner
                var descriptionRect = new Rect(container.x + 5, bannerHeight * 2 / 3 + 30.0f, container.width, 40.0f);

                var ul = new GUIStyle(Styles.Markdown.Text.Style)
                {
                    alignment = TextAnchor.UpperLeft
                };
                GUI.Label(headerRect, $"<b><size=24><color={textColorHex}>{header}</color></size></b>", ul);
                GUI.Label(descriptionRect, $"<size=14><color={textColorHex}>{description}</color></size>", ul);
            }
            GUILayout.EndVertical();

            GUILayout.Space(bannerHeight + Styles.Markdown.SPACING);
        }

        /// <summary>
        /// Checks if the group is visible.
        /// </summary>
        /// <param name="group">The group to check.</param>
        /// <returns>Whether the group is visible.</returns>
        protected virtual bool IsGroupVisible(PageGroup group)
        {
            return (!group.IsEmpty &&
                   ContextFilter.Count == 0) ||
                   ContextFilter.Contains(group.Context ? group.Context.Name : "");
        }

        /// <summary>
        /// Called when the window is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (null != m_selectedPage)
            {
                ActivePage = null;
            }
        }

        /// <summary>
        /// Invokes a lifecycle method on a page.
        /// </summary>
        /// <param name="page"> The page on which to invoke the lifecycle method.</param>
        /// <param name="lifecycleMethod">The name of the lifecycle method to invoke.</param>
        private void InvokeLifecyle(IMetaHubPage page, string lifecycleMethod)
        {
            if (null == page)
            {
                return;
            }

            var method = page.GetType().GetMethod(lifecycleMethod, BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance);
            _ = (method?.Invoke(page, new object[0]));
        }

        /// <summary>
        /// Disables the selected page.
        /// </summary>
        /// <param name="page">The page to disable.</param>
        private void DisableSelectedPage(IMetaHubPage page)
        {
            InvokeLifecyle(page, "OnDisable");
        }

        /// <summary>
        /// Enables the selected page.
        /// </summary>
        /// <param name="page">The page to enable.</param>
        private void EnableSelectedPage(IMetaHubPage page)
        {
            if (null == page)
            {
                return;
            }

            var method = page.GetType().GetMethod("OnWindow", BindingFlags.NonPublic
                                                              | BindingFlags.Public | BindingFlags.Instance);
            _ = (method?.Invoke(page, new object[] { this }));
            InvokeLifecyle(page, "OnEnable");
        }

        /// <summary>
        /// Handles the selection change event.
        /// </summary>
        private void OnSelectionChange()
        {
            InvokeLifecyle(ActivePage, "OnSelectionChange");
        }

        /// <summary>
        /// Adds a page to the Meta Hub window.
        /// </summary>
        /// <param name="page">The page to add.</param>
        protected virtual void AddPage(PageReference page)
        {
            if (!page.HasContext)
            {
                m_rootPageGroup.AddPage(page);
            }
            else if (m_contextMap.TryGetValue(page.Info.Context, out var groupKey)
                     && m_pageGroupMap.TryGetValue(groupKey, out var group))
            {
                group.AddPage(page);
            }
        }

        /// <summary>
        /// Draws the left panel of the Meta Hub window.
        /// </summary>
        protected virtual void DrawLeftPanel()
        {
            _ = EditorGUILayout.BeginVertical(GUILayout.Width(m_leftPanelWidth));
            {
                m_leftScroll = GUILayout.BeginScrollView(m_leftScroll);
                {
                    DrawPageGroup(m_rootPageGroup);
                    foreach (var context in m_pageGroups)
                    {
                        DrawPageGroup(context);
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.FlexibleSpace();

                DrawShowOnStartupToggle();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a page group in the Meta Hub window.
        /// </summary>
        /// <param name="group">The page group to draw.</param>
        protected virtual void DrawPageGroup(PageGroup group)
        {
            if (!IsGroupVisible(group))
            {
                return;
            }

            // Draw logo image
            var logo = group.Logo;
            if (logo)
            {
                GUILayout.Box(
                    logo,
                    GUILayout.Width(m_leftPanelWidth),
                    GUILayout.Height(m_leftPanelWidth / logo.GetAspectRatio())
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

            if (ContextFilter.Count != 1 && (IsSearchEmpty || pages.Count > 0))
            {
                if (group.Context != PrimaryContext && null != group.Context &&
                    ((IsSearchEmpty && !group.IsEmpty) || pages.Count > 0))
                {
                    Styles.GroupTitle.Draw(group.Context.Name);
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
                group.Hierarchy.Draw();
            }
        }

        /// <summary>
        /// Draws a page entry in the Meta Hub window.
        /// </summary>
        /// <param name="page">The page to draw.</param>
        protected virtual void DrawPageEntry(PageReference page)
        {
            GUIStyle optionStyle = new(Styles.ContextMenu.Entry.Unselected.Style)
            {
                fixedWidth = m_leftPanelWidth - 28.0f
            };

            if (!HasSelectedPage)
            {
                ActivePage = page.Page;
                SelectedPage = page.PageId;
            }
            else if (null == ActivePage)
            {
                if (page.PageId == SelectedPage)
                {
                    ActivePage = page.Page;
                }
            }

            var container = EditorGUILayout.BeginHorizontal();
            {
                var content = new GUIContent(page.Info.Name);
                // Show tooltip only on label longer than the width of the container
                var contentSize = Styles.ContextMenu.Entry.Unselected.Style.CalcSize(content);
                if (container.width < contentSize.x)
                {
                    content.tooltip = page.Info.Name;
                }

                var optionRect = GUILayoutUtility.GetRect(content, optionStyle,
                    Styles.ContextMenu.Entry.Unselected.LayoutOptions);

                var isSelected = page.Page == m_selectedPage;
                var backgroundColor = Styles.ContextMenu.Entry.BackgroundColor(isSelected);

                var isHover = optionRect.Contains(Event.current.mousePosition);
                // only set hover color when not selected
                if (!isSelected && isHover)
                {
                    backgroundColor.a += 0.1f;
                    if (Event.current.type == EventType.MouseMove)
                    {
                        Repaint();
                    }
                }

                EditorGUI.DrawRect(optionRect, backgroundColor);
                GUI.Label(optionRect, content, optionStyle);

                if (Event.current.type == EventType.MouseDown && isHover)
                {
                    ActivePage = page.Page;
                    SelectedPage = page.PageId;
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the show on startup toggle.
        /// </summary>
        protected virtual void DrawShowOnStartupToggle()
        {
            var prev = MetaHubContext.GlobalShowOnStartup;
            var newVal = Styles.ShowOnStartupToggle.Draw(prev,
                "Show on Startup");
            if (prev != newVal)
            {
                MetaHubContext.GlobalShowOnStartup = newVal;
                Telemetry.OnShowOnStartupToggled(TelemetryContext, newVal, SelectedProjectName);
            }
        }
    }
}