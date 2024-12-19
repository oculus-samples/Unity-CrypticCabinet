// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Text.RegularExpressions;
using Meta.Tutorial.Framework.Hub.UIComponents;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    /// <summary>
    /// A utility class for parsing Markdown text.
    /// </summary>
    public static class MarkdownUtils
    {
        /// <summary>
        /// Parses Markdown text to Unity rich text.
        /// </summary>
        /// <param name="markdown">The Markdown text to parse and convert.</param>
        /// <returns>The Unity rich text equivalent of the Markdown text.</returns>
        public static string ParseMarkdown(string markdown)
        {
            markdown = markdown.Trim();

            // Headers
            markdown = Regex.Replace(markdown, @"^######\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(5)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^#####\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(4)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^####\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(3)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^###\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(2)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^##\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(1)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^#\s(.*?)$", $"__PRE_HEADER__<size={HeaderSize(0)}><b>$1</b></size>__NEW_PARA__", RegexOptions.Multiline);

            // Bold
            markdown = Regex.Replace(markdown, @"\*\*(.*?)\*\*", "<b>$1</b>", RegexOptions.Multiline);

            // Italic
            markdown = Regex.Replace(markdown, @"\*(.*?)\*", "<i>$1</i>", RegexOptions.Multiline);

            // Code blocks
            markdown = Regex.Replace(markdown, @"(?s)```(.*?)```", m =>
            {
                var codeLines = m.Groups[1].Value.Trim().Split('\n');
                var result = string.Empty;
                if (codeLines.Length == 1)
                {
                    return $"<color={Styles.CodeBlockTextColorHex}>{codeLines[0]}</color>";
                }

                for (var i = 1; i < codeLines.Length; i++)
                {
                    result += $"    <color={Styles.CodeBlockTextColorHex}>{codeLines[i]}</color>\n";
                }

                return result;
            }, RegexOptions.Multiline);

            // Blockquotes
            markdown = Regex.Replace(markdown, @"^>\s?(.*?)$", "<color=#a1b56c>$1</color>", RegexOptions.Multiline);

            // Raw Urls, while ignoring urls that are already part of a hyperlink
            markdown = Regex.Replace(markdown,
                @"(?<!<a href=""|</a>)\b(https?:\/\/[^\s""'<>]+)\b",
                "<a href=\"$1\">$1</a>", RegexOptions.Multiline);

            // Handle new paragraph logic of markdown (\n\n or 2 spaces or more and \n is new paragraph),
            // 1 \n is a space
            markdown = Regex.Replace(markdown, @"\n\n+", "__NEW_PARA__");
            markdown = Regex.Replace(markdown, @"  +\n", "__NEW_PARA__");
            markdown = Regex.Replace(markdown, @"__NEW_PARA__\n+", "__NEW_PARA__");
            markdown = Regex.Replace(markdown, @"(__NEW_PARA__)+", "__NEW_PARA__");
            markdown = Regex.Replace(markdown, @"\n*__PRE_HEADER__\n*", "__PRE_HEADER__");
            markdown = Regex.Replace(markdown, @"(__NEW_PARA__)*__PRE_HEADER__", "__PRE_HEADER__");
            markdown = Regex.Replace(markdown, @"\n", " ");
            markdown = Regex.Replace(markdown, @"__NEW_PARA__", "\n\n");
            markdown = Regex.Replace(markdown, @"__PRE_HEADER__", "\n\n\n");

            // Unordered lists
            // markdown = Regex.Replace(markdown, @"^\s*\*\s(.*?)$", "• $1", RegexOptions.Multiline);

            // Unordered lists
            // markdown = Regex.Replace(markdown, @"^\s*\-\s(.*?)$", "• $1", RegexOptions.Multiline);

            // Ordered lists
            // markdown = Regex.Replace(markdown, @"^(\d+)\.\s(.*?)$", "$1. $2", RegexOptions.Multiline);

            return markdown.TrimEnd();
        }

        /// <summary>
        /// Returns the size of a header based on the level.
        /// </summary>
        /// <param name="level">The level of the header.</param>
        /// <returns>0 @ 26, 1 @ 24, 2 @ 22, 3 @ 20, 4 @ 18, 5 @ 16</returns>
        public static int HeaderSize(int level)
        {
            const int DEFAULT_TEXT_SIZE = Styles.DEFAULT_FONT_SIZE;
            const int LEVEL_INCREMENT = 2;

            return DEFAULT_TEXT_SIZE + LEVEL_INCREMENT * (5 - level); // levelInverse
        }

        /// <summary>
        /// Opens the source IDE to the specified file and line number.
        /// </summary>
        /// <param name="formatedPath">ex.: "/Assets/CrypticCabinet/Scripts/Passthrough/PassthroughChanger.cs:13"</param>
        public static void NavigateToSourceFile(string formatedPath)
        {
            var parts = formatedPath.Split(':');
            var filePath = parts[0];
            var lineNumber = parts.Length > 1 ? int.Parse(parts[1]) : -1;

            if (System.IO.File.Exists(filePath))
            {
                var normalizedPath = filePath.Replace("\\", "/").Replace("./", "");

                // Determine if the path is in "Assets/" or "Packages/"
                if (normalizedPath.StartsWith("Assets/") || normalizedPath.StartsWith("Packages/"))
                {
                    // Try to load the asset as a MonoScript
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(normalizedPath);
                    if (!script || !AssetDatabase.OpenAsset(script, lineNumber > 0 ? lineNumber : 1))
                    {
                        Debug.LogError($"Failed to load script: {normalizedPath}");
                    }
                    else
                    {
                        if (lineNumber > 0)
                        {
                            Debug.Log($"Opened script: {normalizedPath} at line {lineNumber}");
                        }
                        else
                        {
                            Debug.Log($"Opened script: {normalizedPath}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Unsupported path: {filePath}. Ensure it starts with 'Assets/' or 'Packages/'.");
                }
            }
            else
            {
                Debug.LogError($"File not found: {filePath}");
            }
        }

    }
}