// Copyright (c) Meta Platforms, Inc. and affiliates.

#define SEGMENTS_MD

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Meta.Tutorial.Framework.Hub.Interfaces;
using Meta.Tutorial.Framework.Hub.Pages.Images;
using Meta.Tutorial.Framework.Hub.Parsing;
using Meta.Tutorial.Framework.Hub.UIComponents;
using Meta.Tutorial.Framework.Hub.Utilities;
using Meta.Tutorial.Framework.Hub.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Pages.Markdown
{
    /// <summary>
    /// Custom editor for <see cref="MetaHubMarkdownPage"/>.
    /// </summary>
    [CustomEditor(typeof(MetaHubMarkdownPage))]
    public class MetaHubMarkdownPageEditor : Editor, IOverrideSize, IWindowUpdater
    {
        private GUIStyle m_linkStyle;
        private GUIStyle m_normalTextStyle;
        private GUIStyle m_imageLabelStyle;
        private static Dictionary<string, EmbeddedImage> s_cachedImages = new();

#if META_EDIT_TUTORIALS
        [MenuItem("Meta/Tutorial Hub/Edit Tutorials/Clear Image Cache", priority = 2)]
#endif
        public static void ClearCache()
        {
            s_cachedImages.Clear();
        }

        private Vector2 m_scrollView;

        /// <inheritdoc cref="IOverrideSize.OverrideWidth"/>
        public float OverrideWidth { get; set; } = -1;

        /// <inheritdoc cref="IOverrideSize.OverrideHeight"/>
        public float OverrideHeight { get; set; } = -1;

        // private readonly Regex m_imageRegex = new(@"!\[(.*?)\]\((.*?)\)", RegexOptions.Compiled);
        // private readonly Regex m_splitRegex = new(@"(!\[.*?\]\(.*?\))|(https?://[^\s]+)", RegexOptions.Compiled);
        private readonly Regex m_orderedContent = new(@"^(\s*)(?:(\d+(?:\.\d*)+)|[-•*]) (.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex m_orderedCount = new(@"(\d)+", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex m_blockRegex = new(@"^(\s*)>\s*(.*)", RegexOptions.Compiled | RegexOptions.Multiline);

        private const float PADDING = Styles.Markdown.PADDING;

        private float RenderedWindowWidth => (OverrideWidth > 0 ? OverrideWidth : EditorGUIUtility.currentViewWidth) - PADDING;

        private List<Action> m_drawingCallbacks = new();
        private bool m_repaint;
        private bool m_isFirstText;
        private bool m_listStarted;
        private Texture2D m_emptyTexture;
        private Dictionary<string, EmbeddedImage> m_embeddedImages = new();
        private bool m_registeredToUpdate = false;

        /// <summary>
        /// The Parent window using this page.
        /// </summary>
        protected EditorWindow ParentWindow { get; private set; }

        private Texture2D EmptyTexture
        {
            get
            {
                if (m_emptyTexture == null)
                {
                    m_emptyTexture = new Texture2D(8, 8);
                }
                return m_emptyTexture;
            }
        }

        /// <inheritdoc cref="Editor.OnInspectorGUI()"/>
        public override void OnInspectorGUI()
        {
            if (m_drawingCallbacks.Count == 0)
            {
                Initialize();

                if (m_drawingCallbacks.Count == 0)
                {
                    base.OnInspectorGUI();
                }
            }

            for (var i = 0; i < m_drawingCallbacks.Count; i++)
            {
                m_drawingCallbacks[i].Invoke();
            }

            if (m_repaint)
            {
                Refresh();
            }
        }

        public bool Update()
        {
            var updated = false;
            foreach (var embeddedImage in m_embeddedImages)
            {
                if (embeddedImage.Value.Update())
                {
                    updated = true;
                }
            }

            return updated;
        }

        private void OnEditorUpdate()
        {
            if (Update())
            {
                Repaint();
                if (ParentWindow != null)
                {
                    ParentWindow.Repaint();
                }
            }
        }

        private void OnEnable()
        {
            if (!m_registeredToUpdate)
            {
                EditorApplication.update += OnEditorUpdate;
                m_registeredToUpdate = true;
            }
        }

        private void OnDisable()
        {
            if (m_registeredToUpdate)
            {
                EditorApplication.update -= OnEditorUpdate;
                m_registeredToUpdate = false;
            }
        }

        /// <summary>
        /// Initializes the Editor.
        /// </summary>
        protected void Initialize()
        {
            m_repaint = false;
            m_listStarted = false;
            m_isFirstText = true;
            m_drawingCallbacks.Clear();
            m_embeddedImages.Clear();

            var markdownPage = (MetaHubMarkdownPage)target;
            if (!markdownPage)
            {
                return;
            }

            Telemetry.OnPageLoaded(markdownPage.TelemetryContext, markdownPage);
            var text = markdownPage.MarkdownText;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            m_linkStyle ??= Styles.Markdown.Hyperlink.Style;

            m_normalTextStyle ??= Styles.Markdown.Text.Style;

            var currentEvent = Event.current;

            Draw(() =>
            {
                m_scrollView = GUILayout.BeginScrollView(m_scrollView);
                GUILayout.BeginVertical(GUILayout.Width(RenderedWindowWidth));
            });

            #region Render Markdown as segments
            // now render segments, just as we did for the markdown text parts
            var segments = (target as MetaHubMarkdownPage).TryParsedMarkdown;
            var prevIsImage = false;
            foreach (var segment in segments)
            {
                if (segment is ParsedMD.HyperlinkSegment hyperlink)
                {
                    if (hyperlink.IsImage)
                    {
                        var imagePath = hyperlink.URL;
                        if (!s_cachedImages.ContainsKey(imagePath))
                        {
                            var isLocal = EmbeddedImage.IsUrlPath(imagePath);
                            var updatedPath = !isLocal || imagePath.StartsWith(markdownPage.MarkdownRoot)
                                ? imagePath // it already begins at the root level
                                : Path.Combine(markdownPage.MarkdownRoot, imagePath);

                            var embeddedImage = new EmbeddedImage(updatedPath);
                            s_cachedImages[imagePath] = embeddedImage;
                            embeddedImage.LoadImage();
                            DelayedRefresh();
                        }

                        m_embeddedImages[imagePath] = s_cachedImages[imagePath];

                        if (!prevIsImage)
                        {
                            Draw(() =>
                            {
                                _ = EditorGUILayout.BeginHorizontal();
                            });
                        }

                        Draw(() =>
                        {
                            Texture2D img = null;
                            float width = 100;
                            float height = 100;
                            var showLoading = true;
                            if (s_cachedImages.TryGetValue(imagePath, out var embeddedImage) && embeddedImage != null &&
                                embeddedImage.IsLoaded)
                            {
                                img = embeddedImage.CurrentTexture;
                                if (img == null)
                                {
                                    embeddedImage.LoadImage();
                                }
                                else
                                {
                                    showLoading = false;
                                    width = img.width;
                                    height = img.height;
                                }
                            }
                            if (showLoading || img == null)
                            {
                                img = EmptyTexture;
                                width = 100;
                                height = 100;
                            }

                            var aspectRatio = img.GetAspectRatio();
                            if (hyperlink.Properties != null)
                            {
                                if (hyperlink.Properties.TryGetValue("width", out var widthStr))
                                {
                                    if (widthStr.Contains('%'))
                                    {
                                        var pct = float.Parse(widthStr.Replace("%", ""));
                                        var maxWidth = RenderedWindowWidth * pct / 100;
                                        width = showLoading ? maxWidth : Mathf.Min(width, maxWidth);
                                    }
                                    else
                                    {
                                        width = float.Parse(widthStr);
                                    }

                                    height = width / aspectRatio;
                                }
                            }

                            if (width > RenderedWindowWidth - PADDING)
                            {
                                width = RenderedWindowWidth - PADDING;
                                height = width / aspectRatio;
                            }

                            if (null == m_imageLabelStyle)
                            {
                                m_imageLabelStyle = Styles.Markdown.ImageLabel.Style;
                            }

                            var content = new GUIContent(img);
                            var imageLabelRect = GUILayoutUtility.GetRect(content, m_imageLabelStyle,
                                GUILayout.Height(height), GUILayout.Width(width));

                            if (GUI.Button(imageLabelRect, content, m_imageLabelStyle))
                            {
                                ImageViewer.ShowWindow(embeddedImage, Path.GetFileNameWithoutExtension(imagePath));
                                Telemetry.OnImageClicked(markdownPage.TelemetryContext, markdownPage, img.name);
                            }
                        });
                        prevIsImage = true;
                    }
                    else // blue text hyperlink. TODO: distinguish web links from source code links
                    {
                        if (prevIsImage)
                        {
                            Draw(() =>
                            {
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            });
                            prevIsImage = false;
                        }
                        var url = hyperlink.URL;
                        Draw(() =>
                        {
                            _ = EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUI.indentLevel * Styles.Markdown.INDENT_SPACE);
                            GUILayout.Label($"<color={Styles.DefaultLinkColor}>" + hyperlink.Text + "</color>", m_linkStyle,
                                                               GUILayout.MaxWidth(RenderedWindowWidth));
                            var linkRect = GUILayoutUtility.GetLastRect();
                            if (currentEvent.type == EventType.MouseDown && linkRect.Contains(currentEvent.mousePosition))
                            {
                                if (url.StartsWith('.')) // this is a local file
                                {
                                    // if it's a source code file, open it in the editor
                                    if (url.EndsWith(".cs"))
                                    {
                                        MarkdownUtils.NavigateToSourceFile(url);
                                    }
                                }
                                Application.OpenURL(url);
                            }
                            EditorGUILayout.EndHorizontal();
                        });
                    }
                }
                else
                {
                    if (prevIsImage && !string.IsNullOrEmpty(segment.Text.Trim()))
                    {
                        Draw(() =>
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        });
                        prevIsImage = false;
                    }
                    ProcessSections(segment.Text, m_blockRegex, ProcessBlock);
                }
            }
            #endregion

            Draw(() =>
            {
                GUILayout.Space(Styles.Markdown.PADDING);
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            });
        }

        /// <summary>
        /// Draws the given action.
        /// </summary>
        /// <param name="action">The action to draw.</param>
        private void Draw(Action action)
        {
            m_drawingCallbacks.Add(action);
        }

        /// <summary>
        /// Draws the given text.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        private void DrawText(string text, GUIStyle styleOverride = null)
        {
            var parsedText = MarkdownUtils.ParseMarkdown(text);
            // If we render the beginning of the file we want to remove the new lines before the header
            if (m_isFirstText)
            {
                m_isFirstText = false;
                parsedText = parsedText.TrimStart();
            }
            Draw(() =>
            {
                _ = EditorGUILayout.TextArea(
                    parsedText,
                    styleOverride ?? m_normalTextStyle,
                    GUILayout.MaxWidth(RenderedWindowWidth)
                );
            });
        }

        /// <summary>
        /// Processes a block of text.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <param name="block">The block to process, if any.</param>
        private void ProcessBlock(string text, Match block)
        {
            ProcessSections(text, m_orderedContent, ProcessOrderedList);
            if (null != block)
            {
                Draw(() =>
                {
                    GUILayout.Space(Styles.Markdown.SPACING);
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(RenderedWindowWidth));
                    GUILayout.Space(Styles.Markdown.SPACING);
                    GUILayout.BeginVertical();
                    GUILayout.BeginVertical(Styles.Markdown.Box.Style);
                    GUILayout.Space(Styles.Markdown.BOX_SPACING);
                });

                var markdown = MarkdownUtils.ParseMarkdown(block.Groups[2].Value);
                DrawText(markdown);

                Draw(() =>
                {
                    GUILayout.Space(Styles.Markdown.BOX_SPACING);
                    GUILayout.EndVertical();
                    GUILayout.EndVertical();
                    GUILayout.Space(Styles.Markdown.SPACING);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(Styles.Markdown.SPACING);
                });
            }
        }


        /// <summary>
        /// Processes an ordered list.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <param name="orderedContentMatch">The match for the ordered content, if any.</param>
        private void ProcessOrderedList(string text, Match orderedContentMatch)
        {
            var trimedText = text.Trim();
            if (!string.IsNullOrEmpty(trimedText))
            {
                if (m_listStarted)
                {
                    // Only add spacing if the next is not a header
                    if (!trimedText.StartsWith("#"))
                    {
                        // add extra line spacing at the end of the list
                        Draw(
                            () =>
                            {
                                GUILayout.Space(Styles.Markdown.LIST_SPACING * 4);
                            });
                    }

                    m_listStarted = false;
                }
                DrawText(text);
            }

            if (null != orderedContentMatch)
            {
                if (!m_listStarted)
                {
                    // add extra line spacing at the start of the list
                    Draw(() =>
                    {
                        GUILayout.Space(Styles.Markdown.LIST_SPACING * 4);
                    });

                    m_listStarted = true;
                }
                
                var point = orderedContentMatch.Groups[2].Value;
                if (string.IsNullOrEmpty(point))
                {
                    point = "•";
                }
                var indentation = 0;

                var spaces = orderedContentMatch.Groups[1].Value.TrimStart('\n');
                var matches = m_orderedCount.Matches(point);
                if (matches.Count > 0)
                {
                    indentation = matches.Count - 1;
                }
                else if (!string.IsNullOrEmpty(spaces))
                {
                    indentation = Mathf.CeilToInt(spaces.Length / 2f);
                }
                
                Draw(() =>
                {
                    GUILayout.Space(indentation > 0 ? Styles.Markdown.LIST_SPACING_HALF : Styles.Markdown.LIST_SPACING);
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(RenderedWindowWidth));
                    if (indentation > 0)
                    {
                        GUILayout.Space(Styles.Markdown.INDENT_SPACE * indentation);
                    }

                    Styles.Markdown.Text.DrawHorizontalTextArea(point);
                });

                var style = new GUIStyle(m_normalTextStyle);
                style.padding.left = 0;
                DrawText(orderedContentMatch.Groups[3].Value, style);

                Draw(() =>
                {
                    GUILayout.Space(Styles.Markdown.SPACING);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(indentation > 0 ? Styles.Markdown.LIST_SPACING_HALF : Styles.Markdown.LIST_SPACING);
                });
            }
        }

        /// <summary>
        /// Processes the sections in the input text.
        /// </summary>
        /// <param name="input">The input text to process.</param>
        /// <param name="regex">The regular expression to use to match the sections in the input text.</param>
        /// <param name="onProcessSection">Triggered when a section is processed. The first parameter is the section
        /// before the match, the second parameter is the match itself.</param>
        private void ProcessSections(string input, Regex regex, Action<string, Match> onProcessSection)
        {
            var normalizedInput = regex == m_orderedContent ? regex.Replace(input, match =>
            {
                var space = match.Groups[1].Value; // Captured number or empty for bullets
                var prefix = match.Groups[2].Value; // Captured number or empty for bullets
                var bullet = string.IsNullOrEmpty(prefix) ? "•" : prefix; // Normalize '-' and '*' to '•'
                return $"{space}{bullet} {match.Groups[3].Value}"; // Reconstruct the line
            }) : input;

            var matches = regex.Matches(normalizedInput);

            var start = 0;

            foreach (Match match in matches)
            {
                // Get the section before the match
                var sectionBefore = normalizedInput[start..match.Index];

                // Process the section before the match and the match itself
                onProcessSection(sectionBefore, match.Success ? match : null);

                // Update the start position for the next iteration
                start = match.Index + match.Length;
            }

            // Process the section after the last match
            var sectionAfter = normalizedInput[start..];
            onProcessSection(sectionAfter, null);
        }

        /// <summary>
        /// Refreshes the editor on the next repaint.
        /// </summary>
        private void DelayedRefresh()
        {
            m_repaint = true;
        }

        /// <summary>
        /// Refreshes and repaints the editor.
        /// </summary>
        private void Refresh()
        {
            m_repaint = false;
            Repaint();
        }

        /// <inheritdoc cref="IMetaHubPage.RegisterWindow"/>
        public void RegisterWindow(EditorWindow window)
        {
            ParentWindow = window;
        }

        /// <inheritdoc cref="IMetaHubPage.UnregisterWindow"/>
        public void UnregisterWindow(EditorWindow window)
        {
            if (window == ParentWindow)
            {
                ParentWindow = null;
            }
        }
    }
}