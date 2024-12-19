// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Meta.Tutorial.Framework.Hub.Interfaces
{
    /// <summary>
    /// Adheres to the Meta Hub documentation page information.
    /// </summary>
    public interface IPageInfo
    {
        /// <summary>
        /// The name of the page.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The context for which the page is relevant.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// The priority of the page relative to other pages.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// The prefix to distinguish the page from others with the same name.
        /// </summary>
        string HierarchyName { get; }

        /// <summary>
        /// The project for which the page is relevant.
        /// </summary>
        string ProjectName { get; }
    }
}