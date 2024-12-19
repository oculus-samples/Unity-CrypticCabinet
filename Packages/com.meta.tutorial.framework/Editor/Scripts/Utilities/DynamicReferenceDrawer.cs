// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.Utilities
{
    [CustomPropertyDrawer(typeof(DynamicReference), false)]
    public class DynamicReferenceDrawer : PropertyDrawer
    {
        private const int LINE_HEIGHT = 16;
        private const int SPACING = 4;

        private static Dictionary<long, int> s_lineCounts = new();

        private static long GetPropertyHash(SerializedProperty property)
        {
            long hash = 17;
            hash = hash * 31 + property.serializedObject.targetObject.GetHashCode();
            hash = hash * 31 + property.propertyPath.GetHashCode();
            return hash;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hash = GetPropertyHash(property);
            var y = position.y;
            var propertyRect = new Rect(position.x, y, position.width, LINE_HEIGHT);

            if (!EditorGUI.PropertyField(propertyRect, property, label, includeChildren: false))
            {
                s_lineCounts[hash] = 1;
                _ = property.serializedObject.ApplyModifiedProperties();
                return;
            }

            var actualHeight = LINE_HEIGHT + SPACING;

            y += actualHeight;
            EditorGUI.indentLevel++;

            var referenceType = property.FindPropertyRelative("m_referenceType");
            var obj = property.FindPropertyRelative("m_obj");
            var classType = property.FindPropertyRelative("m_classType");
            var scene = property.FindPropertyRelative("m_scene");
            var name = property.FindPropertyRelative("m_name");
            var path = property.FindPropertyRelative("m_path");
            var guid = property.FindPropertyRelative("m_guid");
            var instanceID = property.FindPropertyRelative("m_instanceID");

            var referenceTypeRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
            _ = EditorGUI.PropertyField(referenceTypeRect, referenceType);
            y += actualHeight;

            var lineCount = 2;
            switch ((DynamicReference.ReferenceType)referenceType.enumValueIndex)
            {
                case DynamicReference.ReferenceType.SERIALIZED_OBJECT:
                    {
                        var objRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                        // Create an object field
                        var newObject = EditorGUI.ObjectField(objRect, "Object",
                            obj.objectReferenceValue, typeof(Object), true // Allow scene objects
                        );

                        // Update the property value
                        if (newObject != obj.objectReferenceValue)
                        {
                            if (newObject is GameObject go)
                            {
                                referenceType.enumValueIndex = (int)DynamicReference.ReferenceType.SCENE_OBJECT; // Set the reference type to SceneObject
                                scene.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path);
                                name.stringValue = DynamicReference.GetGameObjectPath(go);
                            }
                            else
                            {
                                obj.objectReferenceValue = newObject;
                            }
                        }

                        lineCount = 3;
                        break;
                    }
                case DynamicReference.ReferenceType.CLASS_TYPE:
                    var classTypeRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(classTypeRect, classType);
                    y += actualHeight;
                    var classNameRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(classNameRect, name);
                    lineCount = 4;
                    break;
                case DynamicReference.ReferenceType.ASSET_PATH:
                    var pathRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(pathRect, path);
                    lineCount = 3;
                    break;
                case DynamicReference.ReferenceType.GUID:
                    var guidRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(guidRect, guid);
                    lineCount = 3;
                    break;
                case DynamicReference.ReferenceType.INSTANCE_ID:
                    var instanceIDRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(instanceIDRect, instanceID);
                    lineCount = 3;
                    break;
                case DynamicReference.ReferenceType.SCENE_OBJECT:
                    var sceneRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(sceneRect, scene);
                    y += actualHeight;
                    var nameRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
                    _ = EditorGUI.PropertyField(nameRect, name);
                    lineCount = 4;
                    break;
            }

            EditorGUI.indentLevel--;
            s_lineCounts[hash] = lineCount;

            _ = property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hash = GetPropertyHash(property);
            var lineCount = s_lineCounts.ContainsKey(hash) ? s_lineCounts[hash] : 1;
            _ = EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
            var maxLines = DynamicReference.PROPERTY_COUNT;
            var actualLineHeight = EditorGUIUtility.singleLineHeight + SPACING;

            if (lineCount == 1)
            {
                return actualLineHeight - SPACING;
            }

            var diff = (maxLines - lineCount) * actualLineHeight;
            return maxLines * actualLineHeight - diff;
        }
    }
}