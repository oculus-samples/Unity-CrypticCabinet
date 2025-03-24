// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR

using Meta.XR.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Utility Editor class to quickly replace fonts within the game.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class FontReplacerEditor : Editor
    {
        [MenuItem("CrypticCabinet/Utils/Replace Fonts")]
        public static void ReplaceFonts()
        {
            var textElements = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            var textInputs = Resources.FindObjectsOfTypeAll<TMP_InputField>();
            var textOthers = Resources.FindObjectsOfTypeAll<TextMeshPro>();

            var newFont =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CrypticCabinet/UI/Fonts/Montserrat-Regular SDF.asset");

            foreach (var textElement in textElements)
            {
                textElement.font = newFont;
                EditorUtility.SetDirty(textElement);
            }

            foreach (var textElement in textOthers)
            {
                textElement.font = newFont;
                EditorUtility.SetDirty(textElement);
            }

            foreach (var textElement in textInputs)
            {
                textElement.fontAsset = newFont;
                EditorUtility.SetDirty(textElement);
            }
        }
    }
}

#endif