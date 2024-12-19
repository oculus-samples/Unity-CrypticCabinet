// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Interfaces;

namespace Meta.Tutorial.Framework.Hub.Attributes
{
    /// <summary>
    /// Attribute to define a Meta Hub page.
    /// </summary>
    public class MetaHubPageAttribute : Attribute, IPageInfo, IComparable<IPageInfo>
    {
        /// <inheritdoc cref="IPageInfo.Name"/>
        public string Name { get; private set; }

        /// <inheritdoc cref="IPageInfo.Context"/>
        public string Context { get; private set; }

        /// <inheritdoc cref="IPageInfo.Priority"/>
        public int Priority { get; private set; }

        /// <inheritdoc cref="IPageInfo.HierarchyName"/>
        public string HierarchyName { get; private set; }

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName => Context;

        /// <summary>
        /// Constructs a new Meta Hub page attribute.
        /// </summary>
        /// <param name="name">The name of the page.</param>
        /// <param name="context">The context for which the page is relevant.</param>
        /// <param name="prefix">The prefix to distinguish the page from others with the same name.</param>
        /// <param name="priority">The priority of the page relative to other pages.</param>
        public MetaHubPageAttribute(string name = null, string context = "", string prefix = "", int priority = 0)
        {
            Name = name;
            Context = context;
            Priority = priority;
            HierarchyName = prefix;
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }
    }
}