// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Interfaces;

namespace Meta.Tutorial.Framework.Hub.Attributes
{
    /// <summary>
    /// Adheres to the Meta Hub documentation context information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MetaHubContextAttribute : Attribute, IPageInfo, IComparable<IPageInfo>
    {
        /// <summary>
        /// The name of the context.
        /// </summary>
        public string Name => Context;

        /// <summary>
        /// The context for which it is relevant.
        /// </summary>
        public string Context { get; private set; }

        /// <summary>
        /// The priority of the context relative to other contexts.
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// The prefix to distinguish the context from others with the same name.
        /// </summary>
        public string HierarchyName { get; private set; }

        /// <inheritdoc cref="IPageInfo.ProjectName"/>
        public string ProjectName => Context;

        /// <summary>
        /// The asset path to the logo of the context.
        /// </summary>
        public string LogoPath { get; private set; }

        /// <summary>
        /// Constructs a new Meta Hub context attribute.
        /// </summary>
        /// <param name="context">The context for which it is relevant.</param>
        /// <param name="priority">The priority of the context relative to other contexts.</param>
        /// <param name="prefix">The prefix to distinguish the context from others with the same name.</param>
        /// <param name="pathToLogo">The asset path to the logo of the context.</param>
        public MetaHubContextAttribute(string context, int priority = 1000, string prefix = "", string pathToLogo = "")
        {
            Context = context;
            Priority = priority;
            HierarchyName = prefix;
            LogoPath = pathToLogo;
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public int CompareTo(IPageInfo other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : Priority.CompareTo(other.Priority);
        }
    }
}