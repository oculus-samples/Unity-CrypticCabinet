// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    [Serializable]
    public class DynamicReference
    {
        public const int PROPERTY_COUNT = 8;

        public enum ReferenceType
        {
            SERIALIZED_OBJECT,
            CLASS_TYPE,
            ASSET_PATH,
            GUID,
            INSTANCE_ID,
            SCENE_OBJECT,
        }

        [SerializeField, Tooltip("The type of reference.")]
        private ReferenceType m_referenceType = ReferenceType.SERIALIZED_OBJECT;

        [SerializeField, Tooltip("The object reference.")]
        private Object m_obj;

        [SerializeField, Tooltip("The class type.")]
        private string m_classType;

        [SerializeField, Tooltip("The scene reference.")]
        private SceneAsset m_scene;

        [SerializeField, Tooltip("The name of the reference.")]
        private string m_name;

        [SerializeField, Tooltip("The path of the reference.")]
        private string m_path;

        [SerializeField, Tooltip("The GUID of the reference.")]
        private string m_guid;

        [SerializeField, Tooltip("The instance ID of the reference.")]
        private int m_instanceID;

        public ReferenceType RefType => m_referenceType;

        public SceneAsset SceneAsset => m_scene;

        public Object GetObject(bool forceRefresh = false)
        {
            if (m_obj != null && !forceRefresh)
            {
                return m_obj;
            }

            switch (m_referenceType)
            {
                case ReferenceType.SERIALIZED_OBJECT:
                    SetWithObject(m_obj);
                    break;
                case ReferenceType.CLASS_TYPE:
                    SetWithClassType(m_classType, m_name);
                    break;
                case ReferenceType.ASSET_PATH:
                    SetWithPath(m_path);
                    break;
                case ReferenceType.GUID:
                    SetWithGuid(m_guid);
                    break;
                case ReferenceType.INSTANCE_ID:
                    SetWithInstanceID(m_instanceID);
                    break;
                case ReferenceType.SCENE_OBJECT:
                    SetWithName(m_name);
                    break;
                default:
                    break;
            }

            return m_obj;
        }

        public void PingObject(bool andSelect = true)
        {
            if (!m_obj)
            {
                GetObject();
            }

            if (m_obj)
            {
                if (andSelect)
                {
                    Selection.activeObject = m_obj;
                }
                EditorGUIUtility.PingObject(m_obj);
            }
#if META_EDIT_TUTORIALS
            else
            {
                Debug.LogError("Cannot find object to ping.");
            }
#endif
        }

        public string GetReferenceId()
        {
            switch (m_referenceType)
            {
                case ReferenceType.SERIALIZED_OBJECT:
                    return $"ref:{m_obj.name}";
                case ReferenceType.CLASS_TYPE:
                    return $"class:{m_classType}|{m_name}";
                case ReferenceType.ASSET_PATH:
                    return $"path:{m_path}";
                case ReferenceType.GUID:
                    return $"guid:{m_guid}";
                case ReferenceType.INSTANCE_ID:
                    return $"instance:{m_instanceID}";
                case ReferenceType.SCENE_OBJECT:
                    return $"{m_scene.name}::{m_name}";
            }

            return null;
        }

        public void SetWithObject(Object obj)
        {
            m_referenceType = ReferenceType.SERIALIZED_OBJECT;
            m_obj = obj;

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)))
            {
                // if the object is not an asset, then it must be a scene object
                if (m_obj is GameObject gameObject)
                {
                    SetWithGameObject(gameObject);
                }

                if (m_obj is Component component)
                {
                    SetWithGameObject(component.gameObject);
                }
            }
        }

        public void SetWithClassType(string classType, string name = "")
        {
            var type = Type.GetType(classType);
            SetWithClassType(type, name);
        }

        public void SetWithClassType(Type classType, string name = "")
        {
            m_referenceType = ReferenceType.CLASS_TYPE;
            m_classType = classType.FullName;
            m_name = name;

            var objs = Resources.FindObjectsOfTypeAll(classType);
            m_obj = string.IsNullOrEmpty(name)
                ? objs.FirstOrDefault()
                : objs.FirstOrDefault(o => o.name == name);
        }

        public void SetWithName(string name)
        {
            m_referenceType = ReferenceType.SCENE_OBJECT;
            m_name = name;
            m_obj = GameObject.Find(name);
        }

        public void SetWithSceneObject(SceneAsset scene, string scenePath)
        {
            m_referenceType = ReferenceType.SCENE_OBJECT;
            m_scene = scene;
            m_name = scenePath;

            var splitIdx = scenePath.IndexOf('/');
            if (splitIdx > -1)
            {
                var root = scenePath[..splitIdx];
                var sub = scenePath[(splitIdx + 1)..];
                var rootObj = GameObject.Find(root);
                if ((rootObj != null) && !rootObj.transform.parent) // only search for root objects
                {
                    m_obj = rootObj.transform.Find(sub);
                }
            }
        }

        public void SetWithGameObject(GameObject gameObject)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(gameObject.scene.path);
            var name = GetGameObjectPath(gameObject);
            SetWithSceneObject(scene, name);
        }

        public void SetWithPath(string path)
        {
            m_referenceType = ReferenceType.ASSET_PATH;
            m_path = path;
            m_obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        public void SetWithGuid(string guid)
        {
            m_referenceType = ReferenceType.GUID;
            m_guid = guid;
            m_path = AssetDatabase.GUIDToAssetPath(guid);
            m_obj = AssetDatabase.LoadAssetAtPath<Object>(m_path);
        }

        public void SetWithInstanceID(int instanceID)
        {
            m_referenceType = ReferenceType.INSTANCE_ID;
            m_instanceID = instanceID;
            m_obj = EditorUtility.InstanceIDToObject(instanceID);
        }

        public Dictionary<string, string> ToLinkData()
        {
            // create a dictionary that contains the only the information relevant to the current reference type
            var ret = new Dictionary<string, string>
            {
                { "referenceType", m_referenceType.ToString() }
            };
            switch (m_referenceType)
            {
                case ReferenceType.SERIALIZED_OBJECT:
                    ret.Add("instanceID", m_obj.GetInstanceID().ToString());
                    break;

                case ReferenceType.CLASS_TYPE:
                    ret.Add("classType", m_classType);
                    if (!string.IsNullOrEmpty(m_name))
                    {
                        ret.Add("name", m_name);
                    }
                    break;

                case ReferenceType.ASSET_PATH:
                    ret.Add("path", m_path);
                    break;

                case ReferenceType.GUID:
                    ret.Add("guid", m_guid);
                    break;

                case ReferenceType.INSTANCE_ID:
                    ret.Add("instanceID", m_instanceID.ToString());
                    break;

                case ReferenceType.SCENE_OBJECT:
                    ret.Add("name", m_name);
                    ret.Add("scene", AssetDatabase.GetAssetPath(m_scene));
                    break;

                default:
                    break;
            }

            return ret;
        }

        /// <summary>
        /// Invokes the reference that was rendered as a hyperlink and then clicked.
        /// </summary>
        /// <param name="hyperLinkData">All parameters associated with the click</param>
        public static void Invoke(Dictionary<string, string> hyperLinkData)
        {
            var sb = new StringBuilder("Invoking: ");
            foreach (var kvp in hyperLinkData)
            {
                _ = sb.AppendLine(kvp.Key + ": " + kvp.Value);
            }

            var reference = new DynamicReference();

            var referenceType = Enum.Parse<ReferenceType>(hyperLinkData["referenceType"]);
            switch (referenceType)
            {
                case ReferenceType.SERIALIZED_OBJECT:
                    reference.SetWithInstanceID(int.Parse(hyperLinkData["instanceID"]));
                    break;

                case ReferenceType.CLASS_TYPE:
                    reference.SetWithClassType(hyperLinkData["classType"]);
                    break;

                case ReferenceType.ASSET_PATH:
                    reference.SetWithPath(hyperLinkData["path"]);
                    break;

                case ReferenceType.GUID:
                    reference.SetWithGuid(hyperLinkData["guid"]);
                    break;

                case ReferenceType.INSTANCE_ID:
                    reference.SetWithInstanceID(int.Parse(hyperLinkData["instanceID"]));
                    break;

                case ReferenceType.SCENE_OBJECT:
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(hyperLinkData["scene"]);
                    var cleanScenePath = AssetDatabase.GetAssetPath(sceneAsset);

                    // check if the scene is currently open
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == cleanScenePath)
                    {
                        reference.SetWithSceneObject(sceneAsset, hyperLinkData["name"]);
                    }
                    else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        _ = EditorSceneManager.OpenScene(cleanScenePath);
                        reference.SetWithSceneObject(sceneAsset, hyperLinkData["name"]);
                    }
                    break;

                default:
                    break;
            }

            reference.PingObject();
            // Debug.Log(sb.ToString());
        }

#if META_EDIT_TUTORIALS
        [MenuItem("GameObject/Copy Hierarchy Path", false, -100)]
        private static void CopyHierarchyPath()
        {
            // Get the selected GameObject
            var selectedObject = Selection.activeGameObject;

            if (selectedObject != null)
            {
                // Build the full hierarchy path
                var path = GetGameObjectPath(selectedObject);

                // Copy to clipboard
                GUIUtility.systemCopyBuffer = path;
                Debug.Log($"Copied GameObject path: {path}");
            }
            else
            {
                Debug.LogWarning("No GameObject selected.");
            }
        }

        [MenuItem("GameObject/Copy Hierarchy Path", true)]
        private static bool CopyHierarchyPathValidation()
            => Selection.activeGameObject != null; // Enable the menu item only if a GameObject is selected
#endif

        public static string GetGameObjectPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var current = gameObject.transform;

            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }
    }
}