// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Meta.Tutorial.Framework.Hub.Interfaces
{
    /// <summary>
    /// Adheres to providing its own width and height.
    /// </summary>
    public interface IOverrideSize
    {
        /// <summary>
        /// The calculated width.
        /// </summary>
        float OverrideWidth { get; set; }

        /// <summary>
        /// The calculated height.
        /// </summary>
        float OverrideHeight { get; set; }
    }
}