// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrypticCabinet.Editor
{
    /// <summary>
    /// Small class for build the application.
    /// Currently depends on the correct scenes being in the build settings.
    /// </summary>
    public static class AppBuilder
    {
        /// <summary>
        /// In editor build option for a release build, using the default debug signing. 
        /// </summary>
        [MenuItem("CrypticCabinet/Build/Release Build")]
        public static void ReleaseBuild()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("Incorrect build platform, switch to android to build for Quest.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = "Builds/ReleaseBuild.apk",
                options = BuildOptions.None,
                target = BuildTarget.Android,
            };

            Build(options);
        }

        /// <summary>
        /// Editor debug build that allows for script debugging.
        /// </summary>
        [MenuItem("CrypticCabinet/Build/Debug Build")]
        public static void DebugBuild()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("Incorrect build platform, switch to android to build for Quest.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = "Builds/DebugBuild.apk",
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
                target = BuildTarget.Android,
            };

            Build(options);
        }

        /// <summary>
        /// Parses custom command line variables to set keystore and version number information.
        /// -keystorename Path to the keystore file
        /// -keystorepass Password for the keystore
        /// -keyaliasname Select keystore alias name
        /// -keyaliaspass Alias password
        /// -bundleversioncode The Android version code
        /// -bundlenumber App version number.
        /// </summary>
        public static void BuildWithCommandLineArgs()
        {
            var commandLineArgs = System.Environment.GetCommandLineArgs();

            for (var i = 0; i < commandLineArgs.Length; ++i)
            {
                var command = commandLineArgs[i].ToLower();

                if (command.Equals("-keystorename") && i + 1 < commandLineArgs.Length)
                {
                    PlayerSettings.Android.keystoreName = commandLineArgs[i + 1];
                    Debug.Log($"Keystore Name set: {PlayerSettings.Android.keystoreName}");
                }
                else if (command.Equals("-keystorepass") && i + 1 < commandLineArgs.Length)
                {
                    PlayerSettings.Android.keystorePass = commandLineArgs[i + 1];
                    Debug.Log("Keystore Password set: -redacted-");
                }
                else if (command.Equals("-keyaliasname") && i + 1 < commandLineArgs.Length)
                {
                    PlayerSettings.Android.keyaliasName = commandLineArgs[i + 1];
                    Debug.Log($"Keystore Alias set: {PlayerSettings.Android.keyaliasName}");
                }
                else if (command.Equals("-keyaliaspass") && i + 1 < commandLineArgs.Length)
                {
                    PlayerSettings.Android.keyaliasPass = commandLineArgs[i + 1];
                    Debug.Log("Keystore Alias Password set: -redacted-");
                }
                else if (command.Equals("-bundleversioncode") && i + 1 < commandLineArgs.Length)
                {
                    if (int.TryParse(commandLineArgs[i + 1], out var versionCode))
                    {
                        PlayerSettings.Android.bundleVersionCode = versionCode;
                        Debug.Log($"Android Bundle Version Code set: {PlayerSettings.Android.bundleVersionCode}");
                    }
                }
                else if (command.Equals("-bundlenumber") && i + 1 < commandLineArgs.Length)
                {
                    PlayerSettings.bundleVersion = commandLineArgs[i + 1];
                    Debug.Log($"Version Number set: {PlayerSettings.bundleVersion}");
                }
            }

            ReleaseBuild();
        }

        /// <summary>
        /// Builds a release build using the provided keystore credentials. 
        /// </summary>
        /// <param name="outputPath">Output build file, this should include the .apk file extension.</param>
        /// <param name="keystorePath">Path to the  keystore.</param>
        /// <param name="keystorePass">Keystore password.</param>
        /// <param name="keyaliasName">Alias in keystore to be used.</param>
        /// <param name="keyaliasPass">Alias password.</param>
        public static void PipelineBuildWithKeystore(string outputPath, string keystorePath, string keystorePass,
            string keyaliasName, string keyaliasPass)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyaliasName;
            PlayerSettings.Android.keyaliasPass = keyaliasPass;

            PipelineBuild(outputPath);
        }

        /// <summary>
        /// Builds the application using the signing that is set up in the project into the target folder.
        /// </summary>
        /// <param name="outputPath">Output build file, this should include the .apk file extension.</param>
        public static void PipelineBuild(string outputPath)
        {
            var fileExtension = Path.GetExtension(outputPath);
            if (!fileExtension.ToLower().EndsWith("apk"))
            {
                Debug.LogError("Output file path must include file name with .apk extension.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("Failed to switch to android build platform.");
                    return;
                }
            }

            var options = new BuildPlayerOptions
            {
                locationPathName = outputPath,
                options = BuildOptions.None,
                target = BuildTarget.Android,
            };

            Build(options);
        }

        /// <summary>
        /// Builds the application given the supplied options.
        /// </summary>
        /// <param name="options"></param>
        private static void Build(BuildPlayerOptions options)
        {
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build successful - Build written to {options.locationPathName}");
            }
            else if (report.summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed.");
            }
        }
    }
}