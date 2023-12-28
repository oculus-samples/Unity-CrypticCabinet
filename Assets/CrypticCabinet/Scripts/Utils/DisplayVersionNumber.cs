// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Responsible for showing the version number for the app.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DisplayVersionNumber : MonoBehaviour
    {
        private TextMeshProUGUI m_displayText;

        private void Start()
        {
            m_displayText = GetComponent<TextMeshProUGUI>();
            m_displayText.SetText($"Version: {GetVersionName()}\n Version Code: {GetVersionCode()}");
        }

        /// <summary>
        /// https://gist.github.com/kibotu/7f5e705e485e17a72834
        /// </summary>
        /// <returns></returns>
        private static int GetVersionCode()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
        var contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var context = contextCls.GetStatic<AndroidJavaObject>("currentActivity"); 
        var packageMngr = context.Call<AndroidJavaObject>("getPackageManager");
        var packageName = context.Call<string>("getPackageName");
        var packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
        return packageInfo.Get<int>("versionCode");
#else
            return -1;
#endif
        }

        /// <summary>
        /// https://gist.github.com/kibotu/7f5e705e485e17a72834
        /// </summary>
        /// <returns></returns>
        private static string GetVersionName()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
        var contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var context = contextCls.GetStatic<AndroidJavaObject>("currentActivity"); 
        var packageMngr = context.Call<AndroidJavaObject>("getPackageManager");
        var packageName = context.Call<string>("getPackageName");
        var packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
        return packageInfo.Get<string>("versionName");
#else
            return Application.version;
#endif
        }
    }
}