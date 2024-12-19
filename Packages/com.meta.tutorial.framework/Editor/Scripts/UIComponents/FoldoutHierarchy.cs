// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.Tutorial.Framework.Hub.UIComponents
{
    /// <summary>
    /// A foldout hierarchy that can be used to display a tree of items in the editor.
    /// </summary>
    /// <typeparam name="T">The type of item that will be displayed in the hierarchy.</typeparam>
    public class FoldoutHierarchy<T>
    {
        private Dictionary<string, FoldoutGroup<T>> m_groups = new();
        private List<FoldoutGroup<T>> m_orderedGroups = new();

        /// <summary>
        /// Add an item to the hierarchy.
        /// </summary>
        /// <param name="path">The path to the item in the hierarchy. The path should be a forward-slash separated string.</param>
        /// <param name="item">The item to add to the hierarchy.</param>
        public void Add(string path, FoldoutHierarchyItem<T> item)
        {
            var parts = path.Split('/');
            FoldoutGroup<T> currentGroup = null;

            for (var i = 0; i < parts.Length; i++)
            {
                var key = string.Join("/", parts, 0, i + 1);

                if (!m_groups.ContainsKey(key))
                {
                    var newGroup = new FoldoutGroup<T>(parts[i]);
                    m_groups.Add(key, newGroup);
                    m_orderedGroups.Add(newGroup);

                    if (currentGroup != null)
                    {
                        currentGroup.AddChild(newGroup, item, i == parts.Length - 1);
                    }
                }

                currentGroup = m_groups[key];
            }
        }

        /// <summary>
        /// Draws the hierarchy in the editor.
        /// </summary>
        public void Draw()
        {
            foreach (var group in m_orderedGroups)
            {
                if (group.Parent == null)
                {
                    group.Draw();
                }
            }
        }
    }

    /// <summary>
    /// A single item in a foldout hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of item that will be displayed in the hierarchy.</typeparam>
    public class FoldoutHierarchyItem<T>
    {
        public string Path;
        public T Item;
        public Action<T> OnDraw;
    }

    /// <summary>
    /// A group of items in a foldout hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of item that will be displayed in the hierarchy.</typeparam>
    public class FoldoutGroup<T>
    {
        private List<object> m_children = new();
        private List<FoldoutHierarchyItem<T>> m_data = new();
        private int m_indentSpace = Styles.Foldout.INDENT_SPACE;

        private bool m_isFoldedOut = false;

        /// <summary>
        /// The name of the group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The parent group this group belongs to.
        /// </summary>
        public FoldoutGroup<T> Parent { get; private set; }

        /// <summary>
        /// Create a new foldout group.
        /// </summary>
        /// <param name="name">The name of the foldout group.</param>
        public FoldoutGroup(string name) => Name = name;

        /// <summary>
        /// Add a child to the group.
        /// </summary>
        /// <param name="child">The child to add to the group. This can be another <see cref="FoldoutGroup{T}"/> or a
        /// <see cref="FoldoutHierarchyItem{T}"/>.</param>
        /// <param name="data">The data to add to the group. This should be a <see cref="FoldoutHierarchyItem{T}"/>.
        /// </param>
        /// <param name="isLeaf">Whether the child is a leaf node. If true, the child will be added to the group as a
        /// leaf.</param>
        public void AddChild(FoldoutGroup<T> child, FoldoutHierarchyItem<T> data, bool isLeaf)
        {
            child.Parent = this;
            m_data.Add(data);

            if (isLeaf)
            {
                m_children.Add(data);
            }
            else
            {
                m_children.Add(child);
            }
        }

        /// <summary>
        /// Draws the group in the editor.
        /// </summary>
        /// <param name="indentLevel">The level of indentation to apply to the group.</param>
        public void Draw(int indentLevel = 0)
        {
            if (string.IsNullOrEmpty(Name))
            {
                DrawExpanded(indentLevel);
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    if (indentLevel >= 0)
                    {
                        GUILayout.Space(m_indentSpace);
                    }

                    GUILayout.BeginVertical();
                    {
                        m_isFoldedOut = EditorGUILayout.Foldout(m_isFoldedOut, Name, toggleOnLabelClick: true,
                            Styles.Foldout.Style);

                        if (m_isFoldedOut)
                        {
                            DrawExpanded(indentLevel);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }

            /*
            if (!string.IsNullOrEmpty(_name))
            {
                _isFoldedOut = EditorGUILayout.Foldout(_isFoldedOut, _name, true);
                if (!_isFoldedOut)
                {
                    return;
                }
            }
            else
            {
                _isFoldedOut = true;
            }

            EditorGUI.indentLevel++;
            foreach (var child in _children)
            {
                if (child is FoldoutGroup<T> foldoutGroup)
                {
                    foldoutGroup.Draw(indentLevel);
                }
                else if (child is FoldoutHierarchyItem<T> leaf)
                {
                    // EditorGUI.indentLevel++;
                    leaf.onDraw(leaf.item);
                    // EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
            */
        }

        /// <summary>
        /// Draws the group when it is expanded.
        /// </summary>
        /// <param name="indentLevel">The level of indentation to apply to the group.</param>
        private void DrawExpanded(int indentLevel)
        {
            foreach (var child in m_children)
            {
                if (child is FoldoutGroup<T> foldoutGroup)
                {
                    foldoutGroup.Draw(indentLevel);
                }
                else if (child is FoldoutHierarchyItem<T> leaf)
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (indentLevel >= 0)
                        {
                            GUILayout.Space(m_indentSpace);
                        }

                        GUILayout.BeginVertical();
                        {
                            leaf.OnDraw(leaf.Item);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}