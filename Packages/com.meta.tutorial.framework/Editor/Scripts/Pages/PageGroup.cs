// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.UIComponents;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    /// <summary>
    /// A group of pages.
    /// </summary>
    public class PageGroup
    {
        private List<PageReference> m_pages = new();
        private HashSet<string> m_addedPages = new();
        private readonly Action<PageReference> m_onDrawPage;

        /// <summary>
        /// The context for the page group.
        /// </summary>
        public MetaHubContext Context { get; }

        /// <summary>
        /// The pages in the group.
        /// </summary>
        public IEnumerable<PageReference> Pages => m_pages;

        /// <summary>
        /// The number of pages in the group.
        /// </summary>
        public int PageCount => m_pages.Count;

        /// <summary>
        /// Whether the group is empty.
        /// </summary>
        public bool IsEmpty => m_pages.Count == 0;

        /// <summary>
        /// The foldout hierarchy of the pages in the group.
        /// </summary>
        public FoldoutHierarchy<PageReference> Hierarchy { get; private set; } = new FoldoutHierarchy<PageReference>();

        public Texture2D Logo { get; set; }

        /// <summary>
        /// Constructs a new page group.
        /// </summary>
        /// <param name="context">The context for the page group.</param>
        /// <param name="onDrawPage">Triggered when a page is drawn. The page reference is passed as a parameter.</param>
        public PageGroup(MetaHubContext context, Action<PageReference> onDrawPage)
        {
            Context = context;
            m_onDrawPage = onDrawPage;
        }

        /// <summary>
        /// Checks if the group is in the search.
        /// </summary>
        /// <param name="search">The search string.</param>
        /// <returns>Whether the group is in the search.</returns>
        public bool IsMatch(string search)
        {
#if UNITY_2021_1_OR_NEWER
            return Context && Context.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
#else
            return Context && Context.Name.ToLower().Contains(search.ToLower());
#endif
        }

        private string m_lastAddedPath; // hacky, but works for now
        /// <summary>
        /// Adds a page to the group.
        /// </summary>
        /// <param name="page">The page to add to the group.</param>
        public void AddPage(PageReference page)
        {
            var pageId = page.PageId;

            if (!m_addedPages.Contains(pageId))
            {
                _ = m_addedPages.Add(pageId);
                m_pages.Add(page);
                var hierarchyName = page.Info.HierarchyName?.Trim(new char[] { '/' }) ?? "";
                // if (hierarchyName.Length > 0)
                // {
                //     hierarchyName += "/";
                // }

                var path = "/" + hierarchyName; // + page.info.Name;
                // var path = prefix + page.info.Name;
                m_lastAddedPath = path;
                Hierarchy.Add(
                    path,
                    new FoldoutHierarchyItem<PageReference>
                    {
                        Path = path,
                        Item = page,
                        OnDraw = m_onDrawPage
                    }
                );
            }
        }

        /// <summary>
        /// Does the same as AddPage for the first page, but adds the rest of the pages as children of the first page.
        /// </summary>
        /// <param name="pages">The pages to add</param>
        public void AddPagesWithParent(PageReference[] pages)
        {
            if (pages.Length == 0)
            {
                return;
            }
            var parent = pages[0];
            AddPage(parent);

            for (var i = 1; i < pages.Length; i++)
            {
                var page = pages[i];
                var pageId = page.PageId;

                if (!m_addedPages.Contains(pageId))
                {
                    _ = m_addedPages.Add(pageId);
                    m_pages.Add(page);

                    // var path = $"{lastAddedPath}_B/{page.info.Name}";
                    var path = $"{m_lastAddedPath}/{page.Info.HierarchyName}";
                    Hierarchy.Add(
                        path,
                        new FoldoutHierarchyItem<PageReference>
                        {
                            Path = path,
                            Item = page,
                            OnDraw = m_onDrawPage
                        }
                    );
                }
            }
        }

        /// <summary>
        /// Sorts the pages in the group.
        /// </summary>
        public void Sort()
        {
            Sort(m_pages);
        }

        /// <summary>
        /// Sorts the pages.
        /// </summary>
        /// <param name="pages">The pages to sort.</param>
        public void Sort(List<PageReference> pages)
        {
            pages.Sort();

            Hierarchy = new FoldoutHierarchy<PageReference>();

            foreach (var page in m_pages)
            {
                var path = "/" + page.Info.HierarchyName;
                // if (!path.EndsWith("/"))
                // {
                //     path += "/";
                // }
                // path += page.info.Name;

                Hierarchy.Add(
                    path, new FoldoutHierarchyItem<PageReference> { Path = path, Item = page, OnDraw = m_onDrawPage });
            }
        }
    }
}