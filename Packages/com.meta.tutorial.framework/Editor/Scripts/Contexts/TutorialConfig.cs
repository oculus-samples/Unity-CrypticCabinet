// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Meta.Tutorial.Framework.Hub.UIComponents;
using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.Tutorial.Framework.Hub.Contexts
{
#if META_EDIT_TUTORIALS
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Meta Tutorial Hub/Tutorial Config", order = 1)]
#endif
    public class TutorialConfig : ScriptableObject
    {
        [SerializeField] private Texture2D m_navHeaderImage; // for now, this feature is disabled
        [field: SerializeField] public string Name { get; set; }
        [SerializeField] private Banner m_bannerConfig;

        public Texture2D NavHeaderImage => m_navHeaderImage;
        public Banner BannerConfig => m_bannerConfig;

        [Serializable]
        public class Banner
        {
            public string ImagePathOrURL;
            public string Title;
            [TextArea(1, 2)] public string Description;
            public float Height = 120.0f;
            public Color TextColor = Color.white;

            public string TextColorHex => Styles.ColorToHex(TextColor);

            private Texture2D m_loadedBannerImage; // cached image
            private string m_loadedBannerImageURL; // cached image URL
            public Texture2D Image
            {
                get
                {
                    if (!m_loadedBannerImage || string.CompareOrdinal(m_loadedBannerImageURL, ImagePathOrURL) != 0)
                    {
                        void LoadImageFromUrl(string url)
                        {
                            var request = UnityWebRequestTexture.GetTexture(url);
                            request.SendWebRequest().completed += operation =>
                            {
                                if (request.responseCode == ResponseCodes.OK)
                                {
                                    m_loadedBannerImage = DownloadHandlerTexture.GetContent(request);
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load image from URL [Error {request.responseCode}]: {url}");
                                }
                            };
                        }

                        var urlRegex = new Regex(@"(https?://[^\s]+)", RegexOptions.Compiled);
                        if (urlRegex.IsMatch(ImagePathOrURL))
                        {
                            LoadImageFromUrl(ImagePathOrURL);
                        }
                        else
                        {
                            m_loadedBannerImage = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePathOrURL); // try to load from assets first
                            if (!m_loadedBannerImage)
                            {
                                m_loadedBannerImage = new Texture2D(2, 2);
                                if (!m_loadedBannerImage.LoadImage(File.ReadAllBytes(ImagePathOrURL))) // load from disk
                                {
                                    Debug.LogError($"Failed to load image at path: {ImagePathOrURL}");
                                }
                                else
                                {
                                    m_loadedBannerImage.wrapMode = TextureWrapMode.Clamp;
                                    m_loadedBannerImageURL = ImagePathOrURL;
                                }
                            }
                            else
                            {
                                m_loadedBannerImageURL = ImagePathOrURL;
                            }
                        }
                    }
                    return m_loadedBannerImage;
                }
            }

        }
    }
}
