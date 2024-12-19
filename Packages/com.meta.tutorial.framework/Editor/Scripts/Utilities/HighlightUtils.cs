// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    public static class HighlightUtils
    {
        public static void HighlightObject(Object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Cannot highlight null object.");
                return;
            }

            Selection.activeObject = obj;
        }

        public static void HighlightObject<T>(string name = "") where T : Object
        {
            var objs = Resources.FindObjectsOfTypeAll<T>();
            var obj = string.IsNullOrEmpty(name)
                ? objs.FirstOrDefault()
                : objs.FirstOrDefault(o => o.name == name);
            HighlightObject(obj);
        }

        public static void HighlightObject(Type type, string name = "")
        {
            var objs = Resources.FindObjectsOfTypeAll(type);
            var obj = string.IsNullOrEmpty(name)
                ? objs.FirstOrDefault()
                : objs.FirstOrDefault(o => o.name == name);
            HighlightObject(obj);
        }

        public static void HighlightByPath(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            HighlightObject(obj);
        }

        public static void HighlightByGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            HighlightByPath(path);
        }

        public static void HighlightByInstanceID(int instanceID)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            HighlightObject(obj);
        }

        public static void HighlightSceneObject(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Cannot highlight null object.");
                return;
            }

            Selection.activeGameObject = obj;
        }

        public static void HighlightSceneObjectByName(string name)
        {
            var obj = GameObject.Find(name);
            HighlightSceneObject(obj);
        }

        public static void HighlightSceneObjectByTag(string tag)
        {
            var obj = GameObject.FindWithTag(tag);
            HighlightSceneObject(obj);
        }

        public static void HighlightSceneObjectByInstanceID(int instanceID)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            HighlightSceneObject(obj);
        }

        public static void HighlightComponent<T>(string name = "") where T : Component
        {
            var objs = Object.FindObjectsOfType<T>();
            var obj = string.IsNullOrEmpty(name)
                ? objs.FirstOrDefault()
                : objs.FirstOrDefault(o => o.name == name);
            HighlightObject(obj);
        }

        public static void HighlightComponent(Type type, string name = "")
        {
            var objs = Object.FindObjectsOfType(type);
            var obj = string.IsNullOrEmpty(name)
                ? objs.FirstOrDefault()
                : objs.FirstOrDefault(o => o.name == name);
            HighlightObject(obj);
        }

        private static bool IsSceneOpen(string scenePath)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.Compare(scene.path, scenePath) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void HighlightDynamicReference(DynamicReference reference)
        {
            if (reference == null)
            {
                Debug.LogWarning("Cannot highlight null object.");
                return;
            }
            if (reference.RefType == DynamicReference.ReferenceType.SCENE_OBJECT)
            {
                var sceneAsset = reference.SceneAsset;
                if (sceneAsset)
                {
                    var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    if (IsSceneOpen(scenePath))
                    {
                        reference.PingObject();
                    }
                    else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    { // load the scene, and ping the object
                        _ = EditorSceneManager.OpenScene(scenePath);
                        reference.PingObject();
                    }
                }
                // else { } // do nothing
            }
            else
            {
                reference.PingObject();
            }
        }
    }
}