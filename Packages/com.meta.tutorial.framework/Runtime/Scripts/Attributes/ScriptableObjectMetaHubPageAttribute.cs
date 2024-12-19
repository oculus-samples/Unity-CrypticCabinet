// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Meta.Tutorial.Framework.Hub.Attributes
{
    /// <summary>
    /// Attribute to define a Meta Hub page for a <see cref="UnityEngine.ScriptableObject"/>.
    /// </summary>
    public class ScriptableObjectMetaHubPageAttribute : MetaHubPageAttribute
    {
        /// <summary>
        /// Constructs a new Meta Hub page attribute for a <see cref="UnityEngine.ScriptableObject"/>.
        /// </summary>
        /// <param name="context">The context for which the page is relevant.</param>
        public ScriptableObjectMetaHubPageAttribute(string context = "") : base(context: context)
        {
        }
    }
}