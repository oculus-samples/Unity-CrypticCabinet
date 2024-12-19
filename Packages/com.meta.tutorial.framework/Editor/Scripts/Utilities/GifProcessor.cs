// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    public static class GifProcessor
    {
        /// <summary>
        /// Extract all frames from gif.
        /// </summary>
        /// <param name="path">.gif file path.</param>
        /// <returns>Array of Texture2D from all frames</returns>
        public static Texture2D[] ExtractAllFramesFromGif(string path)
        {
            var gifImg = Image.FromFile(path, true);
            var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
            var frameCount = gifImg.GetFrameCount(dimension);
            var frames = new Texture2D[frameCount];
            for (var i = 0; i < frameCount; ++i)
            {
                _ = gifImg.SelectActiveFrame(dimension, i);

                var texture = ConvertImageToTexture2D(gifImg as Bitmap);
                texture.name = Path.GetFileName(path);
                frames[i] = texture;
            }

            return frames;
        }

        /// <summary>
        /// Extract an image from a gif. Default frame selected is the middle one.
        /// You can specify a function to select a different frame.
        /// </summary>
        /// <param name="path">.gif file path.</param>
        /// <param name="frameSelector">Function that returns the frame to extract</param>
        /// <returns>Created Texture2D from the selected frame</returns>
        public static Texture2D ExtractImageFromGif(string path, Func<int, int> frameSelector = null)
        {
            var gifImg = Image.FromFile(path, true);
            var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
            var frameCount = gifImg.GetFrameCount(dimension);

            // call the frameSelector to get the desired frame, default to use the middle frame
            var selectedFrame = frameSelector?.Invoke(frameCount) ?? SelectMiddleFrame(frameCount);
            selectedFrame = Math.Clamp(selectedFrame, 0, frameCount);
            // select the desired frame
            _ = gifImg.SelectActiveFrame(dimension, selectedFrame);

            var texture = ConvertImageToTexture2D(gifImg as Bitmap);
            texture.name = Path.GetFileName(path);

            return texture;
        }

        public class ImageHandler : IDisposable
        {
            public string Path { get; }
            public bool IsDoneLoading { get; private set; }
            public bool FirstFrameLoaded { get; private set; }

            public bool FramesCompleted { get; private set; }

            private List<Color32[]> m_frameColors;
            private Texture2D[] m_frames;
            private Texture2D m_firstFrame;
            private int m_width;
            private int m_height;
            private Func<int, int> m_frameSelector;

            private int m_frameProcessIndex = 0;

            public ImageHandler(string path, Func<int, int> frameSelector)
            {
                Path = path;
                m_frameSelector = frameSelector;
            }

            public void Process()
            {
                var gifImg = Image.FromFile(Path, true);
                var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
                var frameCount = gifImg.GetFrameCount(dimension);

                // call the frameSelector to get the desired frame, default to use the middle frame
                var selectedFrame = m_frameSelector?.Invoke(frameCount) ?? SelectMiddleFrame(frameCount);
                selectedFrame = Math.Clamp(selectedFrame, 0, frameCount);
                // select the desired frame
                _ = gifImg.SelectActiveFrame(dimension, selectedFrame);
                m_width = gifImg.Width;
                m_height = gifImg.Height;

                m_frameColors = new List<Color32[]>(1) { ExtractColors(gifImg as Bitmap) };
                gifImg.Dispose();
                IsDoneLoading = true;
                FirstFrameLoaded = true;
            }

            public void ProcessAllFrames()
            {
                var gifImg = Image.FromFile(Path, true);
                var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
                var frameCount = gifImg.GetFrameCount(dimension);
                m_width = gifImg.Width;
                m_height = gifImg.Height;

                m_frameColors = new List<Color32[]>(frameCount);
                for (var i = 0; i < frameCount; ++i)
                {
                    _ = gifImg.SelectActiveFrame(dimension, i);

                    m_frameColors.Add(ExtractColors(gifImg as Bitmap));
                    FirstFrameLoaded = true;
                }

                gifImg.Dispose();
                IsDoneLoading = true;
            }

            public bool CreateTextureFrames(float maxTime)
            {
                if (!IsDoneLoading)
                {
                    return false;
                }

                if (FramesCompleted)
                {
                    return true;
                }

                m_frames ??= new Texture2D[m_frameColors.Count];

                var startTime = EditorApplication.timeSinceStartup;
                if (m_frameProcessIndex == 0 && m_firstFrame != null)
                {
                    m_frames[0] = m_firstFrame;
                    m_frameProcessIndex++;
                }
                for (; m_frameProcessIndex < m_frameColors.Count; ++m_frameProcessIndex)
                {
                    var texture = new Texture2D(m_width, m_height, TextureFormat.ARGB32, false);
                    texture.SetPixels32(m_frameColors[m_frameProcessIndex]);
                    texture.Apply(false);
                    m_frames[m_frameProcessIndex] = texture;
                    if (maxTime >= 0 && EditorApplication.timeSinceStartup - startTime >= maxTime)
                    {
                        break;
                    }
                }

                FramesCompleted = m_frameProcessIndex >= m_frameColors.Count;

                return FramesCompleted;
            }


            public Texture2D[] GetFrames()
            {
                return !IsDoneLoading ? null : !FramesCompleted ? null : m_frames;
            }

            public Texture2D GetFirstFrame()
            {
                if (!FirstFrameLoaded)
                {
                    return null;
                }

                if (m_firstFrame != null)
                {
                    return m_firstFrame;
                }

                m_firstFrame = new Texture2D(m_width, m_height, TextureFormat.ARGB32, false);
                m_firstFrame.SetPixels32(m_frameColors[0]);
                m_firstFrame.Apply(false);

                return m_firstFrame;
            }

            public void Dispose()
            {
                m_frames = null;
                m_frameColors.Clear();
            }
        }

        public static ImageHandler ExtractImageFromGifAsync(string path,
            Func<int, int> frameSelector = null)
        {
            var imageHandler = new ImageHandler(path, frameSelector);
            var thread = new Thread(
                () =>
                {
                    imageHandler.Process();
                });
            thread.Start();

            return imageHandler;
        }

        public static ImageHandler ExtractAllFramesFromGifAsync(string path,
            Func<int, int> frameSelector = null)
        {
            var imageHandler = new ImageHandler(path, frameSelector);
            var thread = new Thread(
                () =>
                {
                    imageHandler.ProcessAllFrames();
                });
            thread.Start();

            return imageHandler;
        }

        /// <summary>
        /// Select the middle frame of framecount
        /// </summary>
        /// <param name="framecount">Number of frames</param>
        /// <returns>Framecount / 2</returns>
        public static int SelectMiddleFrame(int framecount)
        {
            return framecount / 2;
        }

        /// <summary>
        /// Select a frame based on a given percentage
        /// </summary>
        /// <param name="framecount">Number of frame</param>
        /// <param name="percentage">Percentage between 0-100</param>
        /// <returns>Selected frame = framecount * percentage</returns>
        public static int SelectFrameByPercentage(int framecount, int percentage)
        {
            var pct01 = Math.Clamp(percentage / 100f, 0, 1);
            return Mathf.RoundToInt(framecount * pct01);
        }

        private static Color32[] ExtractColors(Bitmap image)
        {
            if (image == null)
            {
                return null;
            }

            // Extract the colors into an array
            var colors = new Color32[image.Width * image.Height];
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var color = image.GetPixel(x, y);
                    colors[(image.Height - y - 1) * image.Width + x] = new Color32(color.R, color.G, color.B, color.A);
                }
            }

            return colors;
        }

        /// <summary>
        /// Convert the image into a new Texture 2D by reading the pixels and assigning them
        /// to the Texture2D.
        /// </summary>
        /// <param name="image">The input image to convert</param>
        /// <returns>The generated Texture2D</returns>
        private static Texture2D ConvertImageToTexture2D(Bitmap image)
        {
            if (image == null)
            {
                return null;
            }

            var colors = ExtractColors(image);
            if (colors == null)
            {
                return null;
            }

            var texture = new Texture2D(image.Width, image.Height, TextureFormat.ARGB32, false);
            texture.SetPixels32(colors);
            texture.Apply(false);
            return texture;
        }
    }
}