// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Parsing
{
    /// <summary>
    /// This class ingests a README.md file, extracts each section, as well as associated images, and stores them in structured data.
    /// </summary>
    public class ParsedMD : IEnumerable<ParsedMD.Segment>, IEnumerable
    {
        public struct Hyperlink
        {
            public readonly string Text;
            public readonly string Url;
            public readonly string AltText;

            public Hyperlink(string text, string url, string altText)
            {
                Text = text;
                Url = url;
                AltText = altText;
            }

            public static Hyperlink ImageXML(string xml) // <img src="path" alt="text">
            {
                var src = xml.Split('"')[1];
                var alt = xml.Split('"')[3];
                return new Hyperlink(src, src, alt);
            }

            public override string ToString() => $"[{Text}]({Url} \"{AltText}\")";
        }

        public class Section
        {
            public readonly int Level; // how many #s
            public readonly string Title;

            public string RawContent { get; private set; }
            public string[] ImagePaths { get; private set; }
            public Hyperlink[] Hyperlinks { get; private set; }

            public Section(Section other)
            {
                Level = other.Level;
                Title = other.Title;
                RawContent = other.RawContent;
                ImagePaths = other.ImagePaths;
                Hyperlinks = other.Hyperlinks;
            }

            public Section(int level, string title, string rawContent, string pathRoot, string[] imagePaths, IEnumerable<Hyperlink> hyperlinks)
                : this(level, title, rawContent, imagePaths.Select(p => Path.Combine(pathRoot, p)).ToArray(), hyperlinks.ToArray()) // convert relative paths to absolute paths
            { }

            private Section(int level, string title, string rawContent, string[] imagePaths, IEnumerable<Hyperlink> hyperlinks)
            {
                Level = level;
                Title = title;

                RawContent = rawContent;
                ImagePaths = imagePaths;
                Hyperlinks = hyperlinks.ToArray();
            }

            public void Append(Section other)
            {
                RawContent += "\n" + other.RawContent;
                ImagePaths = ImagePaths.Concat(other.ImagePaths).ToArray();
                Hyperlinks = Hyperlinks.Concat(other.Hyperlinks).ToArray();
            }
            
            public void RemoveTitle()
            {
                RawContent = Regex.Replace(RawContent, @$"#+\s{Title}", "");
            }

            public override string ToString() => RawContent;
        }

        private static Texture2D LoadImage(string path)
        {
            var tex = new Texture2D(2, 2); // dummy size
            if (tex.LoadImage(File.ReadAllBytes(path)))
            {
                return tex;
            }
            else
            {
                Object.Destroy(tex);
                return null;
            }
        }

        private string m_pathRoot;
        private List<Section> m_sections;
        public IReadOnlyList<Section> Sections => m_sections;

        /// <summary>
        /// Returns the indices of the sections that are level 0, since the non-level-zero sections
        /// that immediately follow can then be considered children of these sections.
        /// </summary>
        public int[] Level0SectionIndices => m_sections.Select((s, i) => (s, i)).Where(t => t.s.Level == 0).Select(t => t.i).ToArray();

        /// <summary>
        /// Collapses all sections that immediately follow a level 0 section into that section.
        /// </summary>
        /// <returns>An array of sections that are all level 0.</returns>
        public Section[] CollapseSectionsToLevel0()
        {
            var indices = Level0SectionIndices;
            var ret = new Section[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                var section = m_sections[indices[i]];
                for (var j = indices[i] + 1; j < m_sections.Count; j++)
                {
                    if (m_sections[j].Level <= section.Level)
                    {
                        break;
                    }
                    section.Append(m_sections[j]);
                }
                ret[i] = section;
            }

            return ret;
        }

        /// <summary>
        /// Returns the section at the given index, and collapses all sections that immediately follow it into that section.
        /// </summary>
        /// <param name="l0Index">The non-absolute level-0 index of the section to collapse.</param>
        /// <returns></returns>
        public Section GetCollapsedSection0(int l0Index, bool hideTitle = false)
        {
            var index = Level0SectionIndices[l0Index]; // absolute index
            var collapsedSection = new Section(m_sections[index]);
            for (var j = index + 1; j < m_sections.Count; j++)
            {
                if (m_sections[j].Level <= collapsedSection.Level)
                {
                    break;
                }
                collapsedSection.Append(m_sections[j]);
            }

            if (hideTitle)
            {
                collapsedSection.RemoveTitle();
            }
            return collapsedSection;
        }

        /// <summary>
        /// This class is used to order the segments as they will be rendered by MarkdownPageEditor 
        /// </summary>
        public class Segment
        {
            public readonly string Text;

            public Segment(string text)
                => Text = text;
        }

        public class HyperlinkSegment : Segment
        {
            public readonly string URL;
            public readonly string AltText;
            public readonly bool IsImage;
            public IReadOnlyDictionary<string, string> Properties => m_properties;

            private Dictionary<string, string> m_properties;

            public HyperlinkSegment(string text, string url, string altText = null, Dictionary<string, string> properties = null) : base(text)
            {
                URL = url;
                AltText = altText ?? text;
                IsImage = URL.EndsWith(".png") || URL.EndsWith(".jpg") || URL.EndsWith(".jpeg") || URL.EndsWith(".gif");
                m_properties = properties;
            }
        }

        private List<Segment> m_segments;
        public Segment this[int idx] => m_segments[idx];
        public int Count => m_segments.Count;
        public IEnumerator<Segment> GetEnumerator() => m_segments.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private ParsedMD() { }
        public ParsedMD(string filePath)
         => LoadMD(filePath);

        public static ParsedMD LoadFromString(string markdown, string pathRoot, bool reduceTitleLevelBy1 = false, bool stripXML = true)
        {
            var strippedMarkdown = markdown;
            if (stripXML)
            {
                strippedMarkdown = StripXMLFromMarkdown(strippedMarkdown);
                strippedMarkdown = ConvertMDLinksToHtml(strippedMarkdown);
            }

            var parsed = new ParsedMD { m_pathRoot = pathRoot };
            parsed.ParseMDSegments(strippedMarkdown);
            parsed.ParseMDSections(strippedMarkdown, reduceTitleLevelBy1);
            return parsed;
        }

        /// <summary>
        /// "Sanitizes" a string by removing all XML tags from it, except for `<img>` tags, which are converted to Markdown format.
        /// We only respect [label](url) linking and handle `<img>` as `![alt text](image_url)`.
        /// This method knows to ignore XML tags that are written in a code block sample.
        /// We still want to preserve <b></b>, <i></i>, and <color></color> tags.
        /// </summary>
        /// <param name="markdown">Markdown document</param>
        /// <returns>Sanitized string with converted `<img>` tags and other XML tags removed</returns>
        private static string StripXMLFromMarkdown(string markdown)
        {
            // Define patterns
            const string IMG_PATTERN = @"<img\s+([^>]+)>"; // Match <img> tags and capture all attributes
            const string CODE_BLOCK_PATTERN = @"```[\s\S]*?```"; // Match code blocks
            const string XML_PATTERN = @"<(?!(\/?(b|i|color|img)\b))[^>]+>"; // Match all XML tags except <b>, <i>, <color>, <img>

            // Step 1: Preserve code blocks
            var codeBlocks = new List<string>();
            markdown = Regex.Replace(markdown, CODE_BLOCK_PATTERN, match =>
            {
                codeBlocks.Add(match.Value); // Save code block content
                return $"__CODE_BLOCK_{codeBlocks.Count - 1}__"; // Replace code block with a placeholder
            });

            // Step 2: Convert <img> to Markdown with all attributes
            markdown = Regex.Replace(markdown, IMG_PATTERN, match =>
            {
                var attributes = match.Groups[1].Value; // Capture all attributes in the <img> tag
                var srcMatch = Regex.Match(attributes, @"src=[""']([^""']+)[""']"); // Extract src
                var src = srcMatch.Success ? srcMatch.Groups[1].Value : string.Empty;

                // Build the properties string from all attributes
                var properties = Regex.Matches(attributes, @"(\w+)=[""']([^""']+)[""']")
                    .Cast<Match>()
                    .Where(m => !string.Equals(m.Groups[1].Value, "src", System.StringComparison.OrdinalIgnoreCase)) // Exclude src
                    .Select(m => $"{m.Groups[1].Value}:{m.Groups[2].Value};")
                    .Aggregate("", (current, next) => current + next);

                properties = !string.IsNullOrEmpty(properties) ? $"{{style=\"{properties.TrimEnd(';')}\"}}" : string.Empty;
                return $"![alt text]({src}){properties}";
            });

            // Step 3: Remove unwanted XML tags, except <b>, <i>, <color>
            markdown = Regex.Replace(markdown, XML_PATTERN, string.Empty);

            // Step 4: Restore code blocks
            markdown = Regex.Replace(markdown, @"__CODE_BLOCK_(\d+)__", match =>
            {
                var index = int.Parse(match.Groups[1].Value);
                return codeBlocks[index];
            });

            return markdown;
        }

        /// <summary>
        /// Takes in a markdown string and converts all links `[label](url "alt")` that aren't images `![alt](imageurl)` to HTML links
        /// </summary>
        /// <param name="markdown">Input markdown to process</param>
        private static string ConvertMDLinksToHtml(string markdown)
        {
            var split = markdown.Split("](");
            if (split.Length == 1)
            {
                return markdown;
            }

            var result = new StringBuilder();
            var needAppendLast = false;
            for (var i = 1; i < split.Length; i++)
            {
                var prev = split[i - 1];
                var next = split[i];
                var labelStart = prev.LastIndexOf('[');
                if (prev[Mathf.Max(0, labelStart - 1)] == '!') // if this is an image, skip it
                {
                    _ = result.Append(prev + "](");
                    needAppendLast = true;
                    continue;
                }

                needAppendLast = false;
                var label = prev[(labelStart + 1)..];
                var urlAndAltEnd = next.IndexOf(')');
                var urlAndAlt = next[..urlAndAltEnd].Split(' ');
                var url = urlAndAlt[0];
                var alt = urlAndAlt.Length > 1 ? urlAndAlt[1] : "";
                if (!url.StartsWith("http"))
                {
                    url = url.Replace("/./", "/"); // remove repeating './././' from the URL, leaving one
                    var indexOfAssets = url.IndexOf("Assets/");
                    if (indexOfAssets > -1)
                    {
                        url = "./" + url[indexOfAssets..];
                    }
                }

                // append link as html hyperlink
                _ = result
                    .Append(prev[..labelStart])
                    .Append($"<a href=\"{url}\"")
                    .Append(string.IsNullOrEmpty(alt) ? ">" : $" alt=\"{alt}\">")
                    .Append($"{label}</a>");

                split[i] = next[(urlAndAltEnd + 1)..]; // remove the hyperlink from the next segment
                if (i == split.Length - 1) // if this is the last split, add the remaining text as a segment
                {
                    _ = result.Append(split[i]);
                }
            }

            if (needAppendLast)
            {
                _ = result.Append(split[^1]);
            }
            return result.ToString();
        }

        private void LoadMD(string filePath)
        {
            using var reader = new StreamReader(filePath);
            m_pathRoot = Path.GetDirectoryName(filePath);
        }

        private void ParseMDSegments(string markdown)
        {
            m_segments = new List<Segment>();
            var split = markdown.Split("]("); // split on the middle of a hyperlink, ex.: [text](url "alt text") or [text](url)
            if (split.Length == 1) // no hyperlinks in the markdown, just one segment
            {
                m_segments.Add(new Segment(markdown));
                return;
            }

            // each split infers a hyperlink, so we need to recombine the split parts,
            // and remove the markup from the display text of both preceding and proceeding segments.
            // the final result should be a list of alternating HyperlinkSegments and Segments
            for (var i = 1; i < split.Length; i++)
            {
                var prev = split[i - 1];
                var next = split[i];

                var hyperlinkStart = prev.LastIndexOf('[');
                var label = prev[(hyperlinkStart + 1)..];
                if ((hyperlinkStart > 0) && (prev[hyperlinkStart - 1] == '!')) // if the label is preceded by a '!', it's an image
                {
                    hyperlinkStart--;
                }

                var hyperlinkEnd = next.IndexOf(')');
                var urlAndAlt = next[..hyperlinkEnd].Split(' ');

                Dictionary<string, string> properties = null;

                // check for css style properties
                if (hyperlinkEnd + 1 < next.Length && next[hyperlinkEnd + 1] == '{')
                {
                    var styleEnd = next.IndexOf('}', hyperlinkEnd + 1);
                    if (styleEnd > -1)
                    {
                        // extract the style from: {style="key1:value1;key2:value2"}
                        var style = next[(hyperlinkEnd + "{style=\"".Length + 1)..(styleEnd - 1)];
                        properties = style
                            .Split(';')
                            .Select(s => s.Split(':'))
                            .ToDictionary(s => s[0], s => s[1]);

                        hyperlinkEnd = styleEnd;
                    }
                }

                var url = urlAndAlt[0];
                var alt = urlAndAlt.Length > 1 ? urlAndAlt[1] : "";

                if (!url.StartsWith("http"))
                {
                    url = Path.Combine(m_pathRoot, url).Replace("\\", "/");
                    url = url.Replace("/./", "/"); // remove repeating './././' from the URL, leaving one
                    url = Regex.Replace(url, @"/[^\.]+/\.\./", "/");
                    var indexOfAssets = url.IndexOf("Assets/");
                    if (indexOfAssets > -1)
                    {
                        url = "./" + url[indexOfAssets..];
                    }
                }

                m_segments.Add(new Segment(prev[..hyperlinkStart]));
                m_segments.Add(new HyperlinkSegment(label, url, alt, properties));
                split[i] = next[(hyperlinkEnd + 1)..]; // remove the hyperlink from the next segment

                if (i == split.Length - 1) // if this is the last split, add the remaining text as a segment
                {
                    m_segments.Add(new Segment(split[i]));
                }
            }
        }

        private void ParseMDSections(string markdown, bool reduceTitleLevelBy1)
        {
            using var reader = new StringReader(markdown);
            LoadMDSections(reader, m_pathRoot, reduceTitleLevelBy1);
        }

        private void LoadMDSections(StringReader reader, string pathRoot, bool reduceTitleLevelBy1)
        {
            m_sections = new List<Section>();
            var imagePaths = new List<string>();
            var level = 0;
            var title = "";
            var rawContent = new StringBuilder();
            var hyperlinkList = new List<Hyperlink>();
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                // Read the text line by line
                // For each line, check if it's a section header (starts with #)
                // If it is, add the current section to the list and start a new one
                if (line.StartsWith('#'))
                {
                    // if we're on the first line, don't add a section yet
                    // but add any content before the first header
                    if (!string.IsNullOrEmpty(title) || rawContent.Length > 0)
                    {
                        // if we haven't reached a title yet
                        var sectionTitle = string.IsNullOrEmpty(title) ? "Header" : title;
                        var finalLevel = reduceTitleLevelBy1 ? Mathf.Max(0, level - 1) : level;
                        m_sections.Add(new Section(finalLevel, sectionTitle, rawContent.ToString(), Path.GetDirectoryName(m_pathRoot), imagePaths.ToArray(), hyperlinkList));
                        rawContent = new StringBuilder(); // clear the content
                        imagePaths.Clear();
                        hyperlinkList.Clear();
                    }

                    for (level = 0; level < line.Length; level++)
                    {
                        if (line[level] != '#')
                        {
                            break;
                        }
                    }
                    --level; // zero based level
                    title = line[(level + 1)..].Trim();
                }
                else
                {
                    // Check for hyperlinks and images, scanning the line for [text](url "alt text") and <img src="path" alt="text">
                    // while taking into account the possibility of multiple hyperlinks/images in a single line
                    for (var i = 0; i < line.Length; i++)
                    {
                        if (line[i] == '[')
                        {
                            var end = line.IndexOf(']', i);
                            var urlStart = line.IndexOf('(', end);
                            var urlEnd = line.IndexOf(')', urlStart);

                            var urlWithAlt = line[(urlStart + 1)..urlEnd].Trim().Split('"');
                            var url = urlWithAlt[0].Trim();
                            var alt = urlWithAlt.Length > 1 ? urlWithAlt[1].Trim() : "";

                            if (!url.StartsWith("http"))
                            {
                                url = Path.Combine(pathRoot, url);
                                var indexOfAssets = url.IndexOf("Assets/");
                                if (indexOfAssets > -1)
                                {
                                    url = "./" + url[indexOfAssets..];
                                }
                            }

                            hyperlinkList.Add(new Hyperlink(line[(i + 1)..end], url, alt));
                            i = urlEnd;

                            if (url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg"))
                            {
                                imagePaths.Add(url);
                            }
                        }
                    }
                }

                // always add the line to the content
                _ = rawContent.Append(line);
                _ = rawContent.Append("\n");
            }
            m_sections.Add(new Section(level, title, rawContent.ToString(), Path.GetDirectoryName(m_pathRoot), imagePaths.ToArray(), hyperlinkList));
        }
    }
}
