// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Interfaces;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    /// <summary>
    /// A reference to a page in the Meta Hub window.
    /// </summary>
    public struct PageReference : IComparable<PageReference>
    {
        /// <summary>
        /// The page to reference.
        /// </summary>
        public IMetaHubPage Page;

        /// <summary>
        /// The information about the page.
        /// </summary>
        public IPageInfo Info;

        /// <summary>
        /// The ID of the page.
        /// </summary>
        public string PageId => Info.Context + "::" + Info.HierarchyName;

        /// <summary>
        /// Whether the page has a context.
        /// </summary>
        public bool HasContext => !string.IsNullOrEmpty(Info.Context);

        /// <summary>
        /// Checks if the page is in the search.
        /// </summary>
        /// <param name="search">The search to check.</param>
        /// <returns>Whether the page is in the search.</returns>
        public bool IsMatch(string search)
        {
#if UNITY_2021_1_OR_NEWER
            return Info.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
#else
            return info.Name.ToLower().Contains(search.ToLower());
#endif
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(PageReference other)
        {
            var compare = Info.Priority.CompareTo(other.Info.Priority);
            if (compare == 0) compare = string.Compare(other.Info.Name, other.Info.Name);
            return compare;
        }
    }
}