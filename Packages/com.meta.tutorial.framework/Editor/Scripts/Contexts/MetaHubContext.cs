// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Tutorial.Framework.Hub.Utilities;
using Meta.Tutorial.Framework.Hub.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{
    /// <summary>
    /// A documentation context that contains pages.
    /// </summary>
    public abstract class MetaHubContext : ScriptableObject, IComparable<MetaHubContext>
    {
        private const string GLOBAL_SHOW_ON_STARTUP_KEY = "Meta_Hub_Show_On_Startup_";

        /// <summary>
        /// A page that is part of a context.
        /// </summary>
        [Serializable]
        public class ScriptableObjectReflectionPage
        {
            [Tooltip("The type of scriptable object to reflect.")]
            public string ScriptableObjectType;

            [Tooltip("The foldout hierarchy name for this object.")]
            public string HierarchyName;

            [Tooltip("A modifier the priority of all pages of this type.")]
            public int PriorityModifier;
        }


        [Header("Context Configuration")]


        [SerializeField, Tooltip("The title of the context")]
        private string m_title;

        [SerializeField, Tooltip("The priority of this context. Lower number means higher probability that this will be the primary context.")]
        private int m_priority = 1000;

        /// <summary>
        /// Whether to show any context on startup.
        /// </summary>
        public static bool GlobalShowOnStartup
        {
            get => EditorPrefs.GetBool(GLOBAL_SHOW_ON_STARTUP_KEY + Application.productName, true);
            set => EditorPrefs.SetBool(GLOBAL_SHOW_ON_STARTUP_KEY + Application.productName, value);
        }

        /// <summary>
        /// The name of the context.
        /// </summary>
        public virtual string Name => name;

        /// <summary>
        /// The priority of the context. Lower number means higher probability that this will be the primary context.
        /// </summary>
        public virtual int Priority => m_priority;

        /// <summary>
        /// The title of the window if this is the primary context.
        /// </summary>
        public virtual string Title => m_title;

        /// <summary>
        /// The name of the project if this is the primary context.
        /// </summary>
        public virtual string ProjectName => m_title;

        public virtual string TelemetryContext => Telemetry.META_HUB_CONTEXT;

        /// <summary>
        /// Whether the user has chosen to show this context on startup.
        /// </summary>
        public virtual bool ShowOnStartup
        {
            get => GlobalShowOnStartup;// && EditorPrefs.GetBool(StartupKey, true);
            set => EditorPrefs.SetBool(StartupKey, value);
        }

        /// <summary>
        /// The key to use for startup preference.
        /// </summary>
        private string StartupKey => $"{Name}_Startup";

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public virtual int CompareTo(MetaHubContext other)
        {
            return ReferenceEquals(this, other) ? 0 : other is null ? 1 : m_priority.CompareTo(other.m_priority);
        }

        /// <summary>
        /// Show the default window for this context.
        /// </summary>
        public virtual MetaHubBase ShowDefaultWindow()
            => MetaHubBase.ShowWindow<MetaHubBase>(Name);
    }
}