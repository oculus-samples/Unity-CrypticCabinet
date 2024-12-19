// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Pages;
using Meta.Tutorial.Framework.Hub.Utilities;
using Meta.Tutorial.Framework.Hub.Windows;
using Meta.Tutorial.Framework.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{
    [MetaHubContext(TutorialFrameworkHub.CONTEXT)]
    public abstract class BaseTutorialHubContext : MetaHubContext
    {
        [SerializeField] private TutorialConfig m_tutorialConfig; // The name of the tutorial this context falls under
        [SerializeField] private bool m_showBanner;

        private bool m_isInstance;

        public TutorialConfig TutorialConfig => m_tutorialConfig;
        public string TutorialName => m_tutorialConfig?.Name;

        public override string ProjectName => TutorialName;

        public override string TelemetryContext => Telemetry.TUTORIAL_HUB_CONTEXT;

        protected TutorialConfig.Banner Banner => m_tutorialConfig.BannerConfig;

        protected bool ShowBanner => m_showBanner;

        protected string GetCurrentDirectory()
            => Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

        protected string GetRootPageAssetPath()
            => Path.Combine(GetCurrentDirectory(), "pages", $"{Title}.asset");

        protected string GetChildPageAssetPath(string childName)
            => Path.Combine(GetCurrentDirectory(), "pages", $"{Title}_{childName}.asset");

        protected void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
        }

        /// <inheritdoc cref="MetaHubContext.ShowDefaultWindow"/>
        public override MetaHubBase ShowDefaultWindow() => TutorialFrameworkHub.ShowWindow();

        /// <summary>
        /// Create or load an instance of a ScriptableObject. When in edit mode, it will create a new instance
        /// </summary>
        /// <param name="assetPath">The path to the asset. When in edit mode, it will create the asset at this path.</param>
        /// <param name="result">Loaded or instantiated object.</param>
        /// <param name="forceCreate">Pass true to force the creation of an instance.</param>
        /// <returns>True if the instance was created, false if it was loaded.
        /// </returns>
        protected bool CreateOrLoadInstance<T>(string assetPath, out T result, bool forceCreate = false) where T : ScriptableObject
        {
#if META_EDIT_TUTORIALS
            if (!forceCreate && File.Exists(assetPath))
            {
                result = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                m_isInstance = false;
                return m_isInstance;
            }
            else
            {
                result = CreateInstance<T>();
                m_isInstance = true;
            }
            return m_isInstance;
#else
            // when not in edit mode, we shouldn't create assets, just load them.
            result = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            m_isInstance = false;
            return m_isInstance;
#endif
        }

        protected T InstanceToAsset<T>(T instance, string assetPath) where T : ScriptableObject
        {
#if META_EDIT_TUTORIALS
            if (m_isInstance)
            {
                EditorUtility.SetDirty(instance);
                EnsureDirectoryExists(assetPath);
                AssetDatabase.CreateAsset(instance, assetPath);
                m_isInstance = false;
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
#endif
            // this already is an asset when not in edit mode
            return instance;
        }

        /// <summary>
        /// Create the page references for the tutorial hub
        /// </summary>
        /// <param name="forceCreate">Force the regeneration of the pages.</param>
        /// <returns>An array with the created page references. When more than one page is returned,
        /// the first page should be considered as the parent, although its foldout path will not reflect that.</returns>
        public abstract PageReference[] CreatePageReferences(bool forceCreate = false);

        public override int CompareTo(MetaHubContext other)
        {
            if (other is BaseTutorialHubContext)
            {
                return base.CompareTo(other);
            }

            return 1; // TutorialHubContexts are always greater than other contexts
        }
    }
}