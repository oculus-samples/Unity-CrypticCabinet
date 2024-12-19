// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Contexts
{

    [CustomEditor(typeof(TutorialReferencesContext))]
    public class TutorialReferencesContextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var context = target as TutorialReferencesContext;
#if META_EDIT_TUTORIALS
            base.OnInspectorGUI();
            if (GUILayout.Button("Generate"))
            {
                context.CreatePageReferences(true);
            }
            EditorGUILayout.Space();
#endif
            if (GUILayout.Button("Open Tutorial Hub"))
            {
                Telemetry.OnOpenTutorialButton(context.Title);
                context.ShowDefaultWindow();
            }
            EditorGUILayout.Space();
        }
    }
}