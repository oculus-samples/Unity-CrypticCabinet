// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using System.Text.RegularExpressions;
using Meta.Tutorial.Framework.Hub.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.Tutorial.Framework.Hub.Pages.Images
{
    public class EmbeddedImage
    {
        private const float GIF_FRAME_SEC = 0.033f; // 30fps for now we use a fix time for gif animations
        private const int MAX_RETRIES = 3;
        private static readonly Regex s_urlRegex = new(@"(https?://[^\s]+)", RegexOptions.Compiled);

        public static bool IsUrlPath(string imagePath)
        {
            return s_urlRegex.IsMatch(imagePath);
        }

        public string ImagePath { get; }
        public bool IsLocal { get; }

        public bool IsLoaded
        {
            get => m_isLoaded;
            private set
            {
                m_isLoaded = value;
                if (m_isLoaded)
                {
                    m_isLoading = false;
                }
            }
        }
        public Texture2D CurrentTexture { get; private set; }

        public int Width => IsLoaded ? CurrentTexture.width : m_loadingWidth;
        public int Height => IsLoaded ? CurrentTexture.width : m_loadingHeight;

        private int m_loadingWidth;
        private int m_loadingHeight;

        private int m_retries = 0;
        private bool m_isLoaded = false;
        private bool m_isLoading = false;

        private Texture2D[] m_gifFrames = null;
        private int m_currentFrame = 0;
        private bool m_isGif;
        private double m_lastFrameTime;
        private GifProcessor.ImageHandler m_gifLoadingImageHandler;

        public EmbeddedImage(string imagePath, int loadingWidth = 100, int loadingHeight = 100)
        {
            ImagePath = imagePath;
            IsLocal = !IsUrlPath(imagePath);
            m_loadingWidth = loadingWidth;
            m_loadingHeight = loadingHeight;
        }

        public void LoadImage()
        {
            if (m_isLoading)
            {
                return;
            }

            IsLoaded = false;
            m_isLoading = true;
            if (IsLocal)
            {
                LoadLocalTexture();
            }
            else
            {
                LoadFromUrl();
            }
        }

        public bool Update()
        {
            if (m_isLoading && IsLocal)
            {
                if (m_gifLoadingImageHandler != null)
                {
                    if (m_gifLoadingImageHandler.IsDoneLoading)
                    {
                        if (m_gifLoadingImageHandler.CreateTextureFrames(0.01f))
                        {
                            m_gifFrames = m_gifLoadingImageHandler.GetFrames();
                            m_currentFrame = 0;
                            CurrentTexture = m_gifFrames[0];
                            m_lastFrameTime = EditorApplication.timeSinceStartup;
                            m_gifLoadingImageHandler.Dispose();
                            m_gifLoadingImageHandler = null;
                            IsLoaded = true;
                            return true;
                        }
                    }

                    if (!IsLoaded && m_gifLoadingImageHandler.FirstFrameLoaded)
                    {
                        CurrentTexture = m_gifLoadingImageHandler.GetFirstFrame();
                        IsLoaded = true;
                        m_isLoading = true; // we are still loading
                        return true;
                    }
                }

                return false;
            }

            if (IsLoaded)
            {
                if (m_isGif && m_gifFrames.Length > 1)
                {
                    var span = EditorApplication.timeSinceStartup - m_lastFrameTime;

                    if (span < GIF_FRAME_SEC)
                    {
                        return false;
                    }

                    m_lastFrameTime = EditorApplication.timeSinceStartup;
                    m_currentFrame = (m_currentFrame + 1) % m_gifFrames.Length;
                    CurrentTexture = m_gifFrames[m_currentFrame];
                    return true;
                }
            }

            return false;
        }

        public bool DidFrameChange(ref int lastFrame)
        {
            var changed = lastFrame != m_currentFrame;
            lastFrame = m_currentFrame;
            return changed;
        }

        private void LoadLocalTexture()
        {
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath); // try to load from assets first
            if (!image)
            {
                image = new Texture2D(2, 2);

                if (m_retries < MAX_RETRIES)
                {
                    if (ImagePath.EndsWith(".gif"))
                    {
                        m_gifLoadingImageHandler = GifProcessor.ExtractAllFramesFromGifAsync(ImagePath);
                        m_isGif = true;
                    }
                    else if (image.LoadImage(File.ReadAllBytes(ImagePath))) // load from disk
                    {
                        image.name = Path.GetFileName(ImagePath);
                        CurrentTexture = image;
                        m_retries = 0;
                        IsLoaded = true;
                    }
                    else
                    {
#if META_EDIT_TUTORIALS
                        Debug.LogError($"Failed to load image at path: {ImagePath}");
#endif
                    }
                }
                else
                {
                    // we've tried to load this image 3 times and failed, so we'll just ignore future load requests
                    CurrentTexture = new Texture2D(2, 2);
                    IsLoaded = true;
                }
            }
        }

        private void LoadFromUrl()
        {
            var request = UnityWebRequestTexture.GetTexture(ImagePath);
            request.SendWebRequest().completed += operation =>
            {
                if (request.responseCode == ResponseCodes.OK)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    CurrentTexture = texture;
                    IsLoaded = true;
                }
                else
                {
                    Debug.LogError($"Failed to load image from URL [Error {request.responseCode}]: {ImagePath}");
                }
            };
        }
    }
}