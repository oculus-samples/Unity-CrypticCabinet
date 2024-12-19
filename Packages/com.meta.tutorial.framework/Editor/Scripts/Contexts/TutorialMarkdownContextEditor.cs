// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{

    [CustomEditor(typeof(TutorialMarkdownContext))]
    public class TutorialMarkdownContextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var context = target as TutorialMarkdownContext;
#if META_EDIT_TUTORIALS
            base.OnInspectorGUI();

            // layout the buttons horizontally
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle in context"))
            {
                context.ToggleAllPageConfigsToAppearInContext();
            }
            if (GUILayout.Button("Toggle as children"))
            {
                context.ToggleAllPageConfigsToAppearAsChildren();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate"))
            {
                context.CreatePageReferences(true);
            }
            EditorGUILayout.Space();
#endif
            if (GUILayout.Button("Open Tutorial Hub"))
            {
                Telemetry.OnOpenTutorialButton($"CONFIG::{context.Title}");
                context.ShowDefaultWindow();
            }
            EditorGUILayout.Space();
        }
    }
}