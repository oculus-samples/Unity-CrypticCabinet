// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Attributes;
using Meta.Tutorial.Framework.Hub.Pages;
using Meta.Tutorial.Framework.Hub.Utilities;
using Meta.Tutorial.Framework.Windows;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{
    [MetaHubContext(TutorialFrameworkHub.CONTEXT)]
#if META_EDIT_TUTORIALS
    [CreateAssetMenu(fileName = "TutorialReferences", menuName = "Meta Tutorial Hub/Tutorial References Context", order = 3)]
#endif
    public class TutorialReferencesContext : BaseTutorialHubContext
    {
        [System.Serializable]
        public struct DynamicReferenceEntry
        {
            public string Header;
            [TextArea(3, 10)] public string Description;
            public DynamicReference Reference;
        }

        [SerializeField] private DynamicReferenceEntry[] m_references;

        public override PageReference[] CreatePageReferences(bool forceCreate = false)
        {
            var pageAssetPath = GetRootPageAssetPath();
            _ = CreateOrLoadInstance<MetaHubWalkthroughPage>(pageAssetPath, out var walkthroughPage, forceCreate);
            walkthroughPage.Name = Title;
            walkthroughPage.References = new DynamicReferenceEntry[m_references.Length];
            for (var i = 0; i < m_references.Length; i++) // Copy references
            {
                walkthroughPage.References[i] = m_references[i];
            }

            walkthroughPage.OverrideContext(this);
            walkthroughPage = InstanceToAsset(walkthroughPage, pageAssetPath);

            var soPage = new ScriptableObjectPage(walkthroughPage, TutorialName, Title, Priority, ShowBanner ? Banner : null);

            return new[]
            {
                new PageReference()
                {
                    Page = soPage,
                    Info = soPage
                }
            };
        }
    }
}