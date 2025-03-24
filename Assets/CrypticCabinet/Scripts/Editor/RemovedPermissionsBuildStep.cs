// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using System.Linq;
using Meta.XR.Samples;
using UnityEditor.Android;
using UnityEngine;

namespace CrypticCabinet.Editor
{
    /// <summary>
    /// Callback between the Unity export and the Gradle build to removed unnecessary permissions in the AndroidManifest.xml
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class RemovedPermissionsBuildStep : IPostGenerateGradleAndroidProject
    {
        /// <summary>
        /// List of permissions to be removed.
        /// </summary>
        private readonly string[] m_permissionsToRemove =
        {
            "android.permission.MODIFY_AUDIO_SETTINGS",
            "android.permission.RECORD_AUDIO",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.READ_MEDIA_AUDIO",
            "android.permission.READ_MEDIA_VIDEO",
            "android.permission.READ_MEDIA_IMAGES",
            "android.permission.ACCESS_MEDIA_LOCATION",
            "android.permission.READ_MEDIA_IMAGE",
        };

        /// <summary>
        /// Required execution order getter.
        /// </summary>
        public int callbackOrder { get; }

        /// <summary>
        /// The callback that receives the path to the gradle project.
        /// </summary>
        /// <param name="path"></param>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Log so we remember to take this out later.
            Debug.Log($"Removing un-used permission added by Unity. Removing: {string.Join(",", m_permissionsToRemove)}");

            // Get the AndroidManifest.xml files and remove the necessary permissions.
            var manifestFiles = Directory.GetFiles(Path.Combine(path, ".."), "AndroidManifest.xml", SearchOption.AllDirectories);
            foreach (var manifestFile in manifestFiles)
            {
                Debug.Log($"Removing permissions from: {manifestFile}");
                RemovePermissionsLine(manifestFile);
            }
        }

        /// <summary>
        /// Reads all the lines in the provided files and removed the lines containing the strings in
        /// <see cref="m_permissionsToRemove"/> array,
        /// </summary>
        /// <param name="path">Path to the AndroidManifest.xml file.</param>
        private void RemovePermissionsLine(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var allLines = File.ReadAllLines(path).ToList();

            foreach (var permission in m_permissionsToRemove)
            {
                _ = allLines.RemoveAll(s => s.Contains(permission));
            }

            File.WriteAllLines(path, allLines);
        }
    }
}