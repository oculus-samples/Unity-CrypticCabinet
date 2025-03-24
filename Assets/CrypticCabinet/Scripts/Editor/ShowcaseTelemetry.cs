// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEditor;

namespace CrypticCabinet.Editor
{
    /// <summary>
    /// This class helps us track the usage of this showcase
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [InitializeOnLoad]
    public static class ShowcaseTelemetry
    {
        // This is the name of this showcase
        private const string PROJECT_NAME = "Unity-CrypticCabinet";
        private const string SESSION_KEY = "OculusTelemetry-module_loaded-" + PROJECT_NAME;

        static ShowcaseTelemetry() => Collect();

        private static void Collect(bool force = false)
        {
            if (SessionState.GetBool(SESSION_KEY, false) == false)
            {
                _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
                _ = OVRPlugin.SendEvent("module_loaded", PROJECT_NAME, "integration");
                SessionState.SetBool(SESSION_KEY, true);
            }
        }
    }
}