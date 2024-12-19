// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Tutorial.Framework.Hub.Contexts;
using Meta.Tutorial.Framework.Hub.Interfaces;
using Meta.Tutorial.Framework.Hub.UIComponents;
using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages
{
    [CustomEditor(typeof(MetaHubWalkthroughPage))]
    public class MetaHubWalkthroughPageEditor : Editor, IOverrideSize
    {
        /// <inheritdoc cref="IOverrideSize.OverrideWidth"/>
        public float OverrideWidth { get; set; } = -1;

        /// <inheritdoc cref="IOverrideSize.OverrideHeight"/>
        public float OverrideHeight { get; set; } = -1;

        private Vector2 m_scrollPos = Vector2.zero;

        private GUIStyle m_textStyle;

        private bool m_isInitialized = false;
        private GUIStyle TextStyle
            => m_textStyle ??= new GUIStyle(Styles.DefaultTextStyle)
            {
                padding = new RectOffset(20, 20, 0, 0),
            };

        private GUIStyle HeaderStyle
        {
            get
            {
                var style = new GUIStyle(TextStyle)
                {
                    fontSize = MarkdownUtils.HeaderSize(0),
                };
                return style;
            }
        }

        private GUIStyle m_buttonStyle;
        private GUIStyle ButtonStyle
                => m_buttonStyle ??= new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    padding = new RectOffset(10, 10, 5, 5),
                };

        private MetaHubWalkthroughPage m_metaHubWalkthroughPage;
        private MetaHubWalkthroughPage Target
            => m_metaHubWalkthroughPage ??= target as MetaHubWalkthroughPage;

        private void DrawGroup(TutorialReferencesContext.DynamicReferenceEntry entry)
        {
            var header = string.IsNullOrEmpty(entry.Header) ? "" : $"<size=26>{entry.Header}</size>";
            EditorGUILayout.LabelField($"{header}", TextStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            var obj = entry.Reference.GetObject(true);
            var buttonText = "Select " + (obj ? obj.GetType().Name : "GameObject"); // if we don't have an object, then it must be a GameObject from a scene
            var tooltip = entry.Reference.RefType == DynamicReference.ReferenceType.SCENE_OBJECT ?
                "This will open the scene if it's not already loaded" :
                null;
            var guiContent = new GUIContent(buttonText, tooltip);
            var size = ButtonStyle.CalcSize(new GUIContent(buttonText));
            if (GUILayout.Button(guiContent, ButtonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y)))
            {
                var reference = entry.Reference;
                HighlightUtils.HighlightDynamicReference(reference);
                var page = (MetaHubWalkthroughPage)target;
                Telemetry.OnHighlightDynamicReference(page.TelemetryContext, reference.GetReferenceId(), page);
            }

            if (obj is SceneAsset)
            {
                GUILayout.Space(10);
                buttonText = "Open Scene";
                size = ButtonStyle.CalcSize(new GUIContent(buttonText));
                var enabled = GUI.enabled;
                GUI.enabled = !Application.isPlaying;
                if (GUILayout.Button(buttonText, ButtonStyle, GUILayout.Width(size.x), GUILayout.Height(size.y)))
                {
                    _ = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(obj));
                    var page = (MetaHubWalkthroughPage)target;
                    Telemetry.OnSceneOpen(page.TelemetryContext, obj.name, page);
                }
                GUI.enabled = enabled;
            }
            GUILayout.EndHorizontal();
            var description = string.IsNullOrEmpty(entry.Description) ? "" : $"{entry.Description}";
            EditorGUILayout.LabelField($"{description}", TextStyle);
        }

        public override void OnInspectorGUI()
        {
            Initialize();
            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

            foreach (var reference in Target.References)
            {
                DrawGroup(reference);
                GUILayout.Space(20);
            }

            GUILayout.EndScrollView();
        }

        private void Initialize()
        {
            if (m_isInitialized)
            {
                return;
            }

            var page = (MetaHubWalkthroughPage)target;
            Telemetry.OnPageLoaded(page.TelemetryContext, page);
            m_isInitialized = true;
        }
    }
}