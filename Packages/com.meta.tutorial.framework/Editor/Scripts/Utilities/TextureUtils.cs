// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    /// <summary>
    /// A utility class for texture operations.
    /// </summary>
    public static partial class TextureUtils
    {
        /// <summary>
        /// Get the aspect ratio of a <see cref="Texture"/>.
        /// </summary>
        /// <param name="texture">The texture to get the aspect ratio of.</param>
        /// <returns>The aspect ratio of the texture.</returns>
        public static float GetAspectRatio(this Texture texture)
        {
            return texture.width / (float)texture.height;
        }
    }
}