// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    /// <summary>
    /// A utility class for reflection operations.
    /// </summary>
    internal static class ReflectionUtils
    {
        private const string NAMESPACE_PREFIX = "Meta";

        /// <summary>
        /// Gets all types in the project that satisfy the given condition.
        /// </summary>
        /// <param name="isValid">The condition that a type must satisfy to be included in the result.</param>
        /// <returns>A list of all types in the project that satisfy the given condition.</returns>
        private static List<Type> GetTypes(Func<Type, bool> isValid)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return new Type[] { };
                    }
                })
                .Where(IsValidNamespace)
                .Where(isValid)
                .ToList();
        }

        /// <summary>
        /// Gets all types in the project that have the given attribute.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <returns>A list of all types in the project that have the given attribute.</returns>
        internal static List<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            var attributeType = typeof(T);
            return GetTypes(type => type.GetCustomAttributes(attributeType, false).Length > 0);
        }

        /// <summary>
        /// Checks if the type is in the Meta namespace.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Whether the type is in the Meta namespace.</returns>
        private static bool IsValidNamespace(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith(NAMESPACE_PREFIX);
    }
}