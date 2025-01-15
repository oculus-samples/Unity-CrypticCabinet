// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.UIComponents
{
    /// <summary>
    /// A class that contains styles for the Meta Hub and its components.
    /// </summary>
    public static partial class Styles
    {
        public const string DEFAULT_HUB_TITLE = "Meta Hub";

        public const float MIN_WINDOW_WIDTH = 400;
        public const float MIN_WINDOW_HEIGHT = 400;
        public static readonly Vector2 MinWindowSize = new(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);

        public const int SIDE_PANEL_WIDTH = 200;

        public const int NAV_BAR_FONT_SIZE = 12;
        public const int DEFAULT_FONT_SIZE = 14;
        public const int DEFAULT_PADDING = 8;
        public const int DEFAULT_INDENT_SPACE = 10;
        public const int SMALL_PADDING = 3;

        public const float MIN_ZOOM = 0.1f;
        public const float MAX_ZOOM = 10f;
        public const float ZOOM_SPEED = 0.01f;

        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            var r = (byte)Convert.ToInt32(hex[..2], 16);
            var g = (byte)Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = (byte)Convert.ToInt32(hex.Substring(4, 2), 16);
            byte a = 255;

            if (hex.Length == 8)
            {
                a = (byte)Convert.ToInt32(hex.Substring(6, 2), 16);
            }

            return new Color32(r, g, b, a);
        }

        public static string ColorToHex(Color color)
        {
            return $"#{Mathf.RoundToInt(color.r * 255):X2}{Mathf.RoundToInt(color.g * 255):X2}{Mathf.RoundToInt(color.b * 255):X2}";
        }

        public static readonly GUIStyle DefaultTextStyle = new(GUI.skin.label)
        {
            fontSize = DEFAULT_FONT_SIZE,
            //alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(20, 0, 0, 0)
        };

        public static readonly GUIStyle NavBarStyle = new(DefaultTextStyle)
        {
            fontSize = NAV_BAR_FONT_SIZE
        };

        public static readonly GUIStyle DefaultBodyStyle = new(GUI.skin.label)
        {
            fontSize = DEFAULT_FONT_SIZE,
            //alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(20, 0, 0, 0)
        };

        public static readonly GUIStyle DefaultBoldStyle = new(EditorStyles.boldLabel)
        {
            fontSize = DEFAULT_FONT_SIZE,
            //alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIStyle NavBarBoldStyle = new(DefaultBoldStyle)
        {
            fontSize = NAV_BAR_FONT_SIZE
        };

        public static readonly GUIStyle DefaultToggleStyle = new(GUI.skin.toggle)
        {
            fontSize = DEFAULT_FONT_SIZE,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(18, 0, 0, 0)
        };

        public static readonly GUIStyle DefaultFoldoutStyle = new(EditorStyles.foldout)
        {
            fontSize = NAV_BAR_FONT_SIZE,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(18, 0, 0, 0)
        };

        public static readonly Color DefaultTextColor = HexToColor("#dddddd");//DefaultTextStyle.normal.textColor;
        public static readonly Color DefaultBackgroundColor = Color.clear;
        public static Color DefaultRecessedBackgroundColor => new(0.0f, 0.0f, 0.0f, 0.10f);
        public static Color DarkBackgroundColor => new(0.0f, 0.0f, 0.0f, 0.25f);

        public static Color RecessedBackgroundColor
            => EditorGUIUtility.isProSkin
                ? DarkBackgroundColor
                : DefaultRecessedBackgroundColor;

        public static readonly string DefaultCodeBlockTextColorHex = "#105025";
        public static readonly Color DefaultCodeBlockTextColor = HexToColor(DefaultCodeBlockTextColorHex);

        public static readonly string DarkCodeBlockTextColorHex = "#a1b56c";
        public static readonly Color DarkCodeBlockTextColor = HexToColor(DarkCodeBlockTextColorHex);

        public static string CodeBlockTextColorHex
            => EditorGUIUtility.isProSkin
                ? DarkCodeBlockTextColorHex
                : DefaultCodeBlockTextColorHex;

        public static readonly Color SelectedTextColor = HexToColor("#dddddd");//Color.white;
        public static readonly Color DefaultSelectionColor = HexToColor("#1977f3");//new Color(0.24f, 0.48f, 0.90f);
        public static readonly Color ProSelectionColor = HexToColor("#1977f3");//new Color(0.22f, 0.44f, 0.88f);

        public static readonly string DefaultLinkColor = "#7e92c2";




        /// <summary>
        /// A class that contains a color that changes based on the editor's skin.
        /// </summary>
        public class SkinBasedColor
        {
            private readonly Color m_defaultColor;

            private readonly Color m_proColor;

            /// <summary>
            /// The color to use based on the editor's skin.
            /// </summary>
            public Color Color => EditorGUIUtility.isProSkin ? m_proColor : m_defaultColor;

            public SkinBasedColor(Color defaultColor, Color? proColor = null)
            {
                m_defaultColor = defaultColor;
                m_proColor = proColor ?? defaultColor;
            }
        }

        /// <summary>
        /// A class that contains a text style for the Meta Hub.
        /// </summary>
        public class TextStyle
        {
            /// <summary>
            /// The padding around the text.
            /// </summary>
            public RectOffset Padding { get; set; } = new RectOffset(0, 0, 0, 0);

            /// <summary>
            /// The color of the text.
            /// </summary>
            public SkinBasedColor Text { get; set; }

            /// <summary>
            /// The color of the background.
            /// </summary>
            public SkinBasedColor Background { get; set; }

            /// <summary>
            /// The layout options for the text.
            /// </summary>
            public GUILayoutOption[] LayoutOptions { get; set; }

            /// <summary>
            /// The style of the text.
            /// </summary>
            public GUIStyle Style { get; set; } = new(NavBarStyle);

            public TextStyle(SkinBasedColor text = null, SkinBasedColor background = null, RectOffset padding = null,
                GUIStyle style = null, params GUILayoutOption[] layoutOptions)
            {
                if (style != null)
                {
                    Style = style;

                    text ??= new SkinBasedColor(style.normal.textColor);
                }

                text ??= new SkinBasedColor(DefaultTextColor);

                background ??= new SkinBasedColor(DefaultBackgroundColor);

                Text = text;
                Background = background;

                Style.normal.background = null;
                Style.normal.textColor = Text.Color;

                if (padding != null)
                {
                    Padding = padding;
                }

                if (layoutOptions.Length > 0)
                {
                    LayoutOptions = layoutOptions;
                }
            }

            /// <summary>
            /// Draws the text with the given label.
            /// </summary>
            /// <param name="label">The label to draw.</param>
            public void Draw(string label)
            {
                DrawVertical(label);
            }

            /// <summary>
            /// Draws the text with the given label vertically.
            /// </summary>
            /// <param name="label">The label to draw.</param>
            public void DrawVertical(string label)
            {
                GUILayout.Space(Padding.top);
                GUILayout.Label(label, Style, LayoutOptions);
                GUILayout.Space(Padding.bottom);
            }

            /// <summary>
            /// Draws the text with the given label horizontally.
            /// </summary>
            /// <param name="label">The label to draw.</param>
            public void DrawHorizontal(string label)
            {
                GUILayout.Space(Padding.left);
                GUILayout.Label(label, Style, LayoutOptions);
                GUILayout.Space(Padding.right);
            }

            public void DrawHorizontalTextArea(string label)
            {
                GUILayout.Space(Padding.left);
                _ = EditorGUILayout.TextArea(label, Style, LayoutOptions);
                GUILayout.Space(Padding.right);
            }
        }

        /// <summary>
        /// A class that contains a style for a rectangle.
        /// </summary>
        public class RectStyle
        {
            /// <summary>
            /// The padding around the rectangle.
            /// </summary>
            public RectOffset Padding { get; set; }

            /// <summary>
            /// The style of the rectangle.
            /// </summary>
            public GUIStyle Style { get; set; }

            public RectStyle(RectOffset padding = null, GUIStyle style = null)
            {
                Padding = padding ?? new RectOffset(0, 0, 0, 0);
                Style = style ?? new GUIStyle(GUI.skin.box);
            }
        }

        /// <summary>
        /// A class that contains a style for a background.
        /// </summary>
        public class BackgroundStyle
        {
            /// <summary>
            /// The style of the background.
            /// </summary>
            public GUIStyle Style { get; set; } = new GUIStyle();

            /// <summary>
            /// The texture of the background.
            /// </summary>
            public Texture2D Texture { get; set; }

            /// <summary>
            /// The layout options for the background.
            /// </summary>
            public GUILayoutOption[] LayoutOptions { get; set; }
                = { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };

            public BackgroundStyle(Color color = default, Texture2D texture = null,
                GUIStyle style = null, params GUILayoutOption[] layoutOptions)
            {
                if (layoutOptions.Length > 0)
                {
                    LayoutOptions = layoutOptions;
                }

                if (style != null)
                {
                    Style = style;
                }

                if (texture == null)
                {
                    texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, color);
                    texture.Apply();
                }

                Texture = texture;
                Style.normal.background = Texture;
            }

            /// <summary>
            /// Begins a vertical group with the given layout options.
            /// </summary>
            /// <param name="layoutOptions"></param>
            /// <returns>The rectangle of the vertical group.</returns>
            public Rect BeginVertical(params GUILayoutOption[] layoutOptions)
            {
                if (layoutOptions.Length == 0)
                {
                    layoutOptions = LayoutOptions;
                }

                return EditorGUILayout.BeginVertical(Style, layoutOptions);
            }
        }

        /// <summary>
        /// A class that contains a style for a selectable item.
        /// </summary>
        public class SelectableStyle
        {
            /// <summary>
            /// The style of the selected item.
            /// </summary>
            public TextStyle Selected { get; set; }

            /// <summary>
            /// The style of the unselected item.
            /// </summary>
            public TextStyle Unselected { get; set; }

            /// <summary>
            /// The layout options for the selectable item.
            /// </summary>
            public GUILayoutOption[] LayoutOptions { get; set; }
                = { GUILayout.ExpandWidth(true), GUILayout.Height(20) };

            /// <summary>
            /// The text color of the selectable item.
            /// </summary>
            /// <param name="isSelected">Whether the item is selected.</param>
            /// <returns>The text color of the selectable item.</returns>
            public Color TextColor(bool isSelected)
            {
                return isSelected ? Selected.Text.Color : Unselected.Text.Color;
            }

            /// <summary>
            /// The background color of the selectable item.
            /// </summary>
            /// <param name="isSelected">Whether the item is selected.</param>
            /// <returns>The background color of the selectable item.</returns>
            public Color BackgroundColor(bool isSelected)
            {
                return isSelected ? Selected.Background.Color : Unselected.Background.Color;
            }

            public SelectableStyle(TextStyle selected = null, TextStyle unselected = null,
                params GUILayoutOption[] layoutOptions)
            {
                if (layoutOptions.Length > 0)
                {
                    LayoutOptions = layoutOptions;
                }

                selected ??= new TextStyle(
                        text: new SkinBasedColor(SelectedTextColor),
                        background: new SkinBasedColor(DefaultSelectionColor, ProSelectionColor),
                        style: new GUIStyle(NavBarStyle)
                        {
                            wordWrap = false
                        },
                        layoutOptions: LayoutOptions);

                unselected ??= new TextStyle(
                        style: new GUIStyle(NavBarStyle)
                        {
                            wordWrap = false
                        },
                        layoutOptions: LayoutOptions
                    );

                Selected = selected;
                Unselected = unselected;
            }
        }

        /// <summary>
        /// A class that contains a style for a text field.
        /// </summary>
        public class TextFieldStyle
        {
            /// <summary>
            /// The style of the text field.
            /// </summary>
            public GUIStyle Style { get; set; } = EditorStyles.toolbarSearchField;

            public TextFieldStyle(GUIStyle style = null)
            {
                if (style != null)
                {
                    Style = style;
                }
            }

            /// <summary>
            /// Draws the text field with the given text.
            /// </summary>
            /// <param name="text">The text to draw.</param>
            /// <returns>The text field's text.</returns>
            public string Draw(string text)
            {
                GUILayout.FlexibleSpace();
                return EditorGUILayout.TextField(text, Style);
            }
        }

        /// <summary>
        /// A class that contains a style for a toggle.
        /// </summary>
        public class ToggleStyle
        {
            /// <summary>
            /// The padding around the toggle.
            /// </summary>
            public RectOffset Padding { get; set; }

            /// <summary>
            /// The style of the toggle.
            /// </summary>
            public GUIStyle Style { get; set; } = new GUIStyle(EditorStyles.toggle);

            public ToggleStyle(RectOffset padding = null, GUIStyle style = null)
            {
                Padding = padding ?? new RectOffset(0, 0, 0, 0);

                if (style != null)
                {
                    Style = style;
                }
            }

            /// <summary>
            /// Draws the toggle with the given value and label.
            /// </summary>
            /// <param name="value">The value of the toggle.</param>
            /// <param name="label">The label of the toggle.</param>
            /// <returns>The value of the toggle after it has been drawn.</returns>
            public bool Draw(bool value, string label)
            {
                bool result;

                EditorGUILayout.Space(Padding.top);

                _ = EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space(Padding.left);

                    result = GUILayout.Toggle(value, label, Style);

                    EditorGUILayout.Space(Padding.right);

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(Padding.bottom);

                return result;
            }
        }

        /// <summary>
        /// The style of context menus in the Meta Hub.
        /// </summary>
        public static class ContextMenu
        {
            public static readonly SelectableStyle Entry = new();
        }

        /// <summary>
        /// The style of the groups title.
        /// </summary>
        public static readonly TextStyle GroupTitle = new(
            padding: new RectOffset(0, 0, DEFAULT_PADDING, 0),
            style: NavBarBoldStyle
        );

        /// <summary>
        /// The style of the panel background.
        /// </summary>
        public static BackgroundStyle PanelBackground => new(
            color: RecessedBackgroundColor
        );

        /// <summary>
        /// The style of the search field.
        /// </summary>
        public static readonly TextFieldStyle SearchField = new();

        /// <summary>
        /// The style of the show on startup toggle.
        /// </summary>
        public static readonly ToggleStyle ShowOnStartupToggle = new(
            padding: new RectOffset(SMALL_PADDING, 0, 0, DEFAULT_PADDING),
            style: DefaultToggleStyle
        );

        /// <summary>
        /// The style of foldouts in the Meta Hub.
        /// </summary>
        public static class Foldout
        {
            public const int INDENT_SPACE = DEFAULT_INDENT_SPACE;

            public static readonly GUIStyle Style = DefaultFoldoutStyle;
        }

        /// <summary>
        /// The style of Markdown content in the Meta Hub.
        /// </summary>
        public static class Markdown
        {
            public const float PADDING = 55;
            public const float INDENT_SPACE = 15;
            public const int SPACING = 16;
            public const int LIST_SPACING = 2;
            public const int BOX_SPACING = 8;
            public const int LIST_SPACING_HALF = 1;

            public static readonly TextStyle Text = new(
                padding: new RectOffset(SPACING, 0, 0, 0),
                style: DefaultBodyStyle
            );

            public static readonly TextStyle Hyperlink = new(
                style: new(EditorStyles.linkLabel)
                {
                    margin = new RectOffset(3, 3, 2, 2),
                    fontSize = DEFAULT_FONT_SIZE,
                    richText = true,
                    wordWrap = true,
                    fontStyle = FontStyle.Bold,
                    //alignment = TextAnchor.UpperLeft
                }
            );

            public static readonly TextStyle ImageLabel = new(
                style: new GUIStyle(DefaultTextStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageAbove
                }
            );

            public static readonly RectStyle Box = new(
                style: new GUIStyle(EditorStyles.helpBox)
            );
        }

        /// <summary>
        /// Clamps the zoom level to the minimum and maximum zoom levels.
        /// </summary>
        /// <param name="zoom">The zoom level to clamp.</param>
        /// <returns>The clamped zoom level.</returns>
        public static float ClampZoom(float zoom)
        {
            return Mathf.Clamp(zoom, MIN_ZOOM, MAX_ZOOM);
        }
    }
}