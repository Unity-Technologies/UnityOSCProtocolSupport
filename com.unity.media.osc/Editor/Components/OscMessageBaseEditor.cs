using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    abstract class OscMessageBaseEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIContent SettingsIcon = EditorGUIUtility.IconContent("Settings");
            public static readonly GUIStyle SettingsIconStyle = new GUIStyle
            {
                padding = new RectOffset(0, 0, 1, 1),
            };

            public static readonly GUIContent RemoveArgument = new GUIContent("Remove Argument");
            public static readonly GUIContent DuplicateArgument = new GUIContent("Duplicate Argument");
            public static readonly GUIContent MoveArgumentUp = new GUIContent("Move Up");
            public static readonly GUIContent MoveArgumentDown = new GUIContent("Move Down");
            public static readonly string UndoAddArgument = "Add Argument";
        }

        [SerializeField]
        VisualTreeAsset m_InspectorUxml;

        SerializedProperty m_Arguments;

        protected virtual void OnEnable()
        {
            m_Arguments = serializedObject.FindProperty("m_Arguments");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            m_InspectorUxml.CloneTree(root);

            var argumentsList = root.Q<IMGUIContainer>("arguments");
            argumentsList.onGUIHandler += OnArgumentsGUI;

            var addButton = root.Q<Button>("add-argument");
            addButton.RegisterCallback<PointerUpEvent>(_ =>
            {
                Undo.RecordObject(target, Contents.UndoAddArgument);
                OnAddArgument();
            });

            root.Bind(serializedObject);
            return root;
        }

        void OnArgumentsGUI()
        {
            serializedObject.Update();

            var hierarchyMode = EditorGUIUtility.hierarchyMode;

            for (var i = 0; i < m_Arguments.arraySize; i++)
            {
                var argumentProp = m_Arguments.GetArrayElementAtIndex(i);

                EditorGUIUtility.hierarchyMode = true;

                var rect = EditorGUILayout.GetControlRect();
                var foldoutRect = new Rect(rect)
                {
                    width = EditorGUIUtility.labelWidth,
                };
                var buttonRect = new Rect(rect)
                {
                    xMin = rect.xMax - 16f,
                };
                var titleRect = new Rect(rect)
                {
                    xMin = foldoutRect.xMax,
                    xMax = buttonRect.xMin,
                };

                var isExpanded = argumentProp.isExpanded;

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUIUtility.hierarchyMode = false;
                    var newIsExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, $"Argument {i}", true);
                    EditorGUIUtility.hierarchyMode = true;

                    if (change.changed)
                    {
                        argumentProp.isExpanded = newIsExpanded;
                    }
                }

                DoArgumentTitleGUI(titleRect, argumentProp);

                if (GUI.Button(buttonRect, Contents.SettingsIcon, Contents.SettingsIconStyle))
                {
                    var menu = new GenericMenu();
                    var argIndex = i;

                    menu.AddItem(Contents.RemoveArgument, false, () =>
                    {
                        m_Arguments.DeleteArrayElementAtIndex(argIndex);
                        serializedObject.ApplyModifiedProperties();
                    });
                    menu.AddItem(Contents.DuplicateArgument, false, () =>
                    {
                        m_Arguments.InsertArrayElementAtIndex(argIndex);
                        OnDuplicateArgument(
                            m_Arguments.GetArrayElementAtIndex(argIndex),
                            m_Arguments.GetArrayElementAtIndex(argIndex + 1)
                        );
                        serializedObject.ApplyModifiedProperties();
                    });

                    if (i > 0)
                    {
                        menu.AddItem(Contents.MoveArgumentUp, false, () =>
                        {
                            m_Arguments.MoveArrayElement(argIndex, argIndex - 1);
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(Contents.MoveArgumentUp);
                    }

                    if (i < m_Arguments.arraySize - 1)
                    {
                        menu.AddItem(Contents.MoveArgumentDown, false, () =>
                        {
                            m_Arguments.MoveArrayElement(argIndex, argIndex + 1);
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(Contents.MoveArgumentDown);
                    }

                    menu.ShowAsContext();
                }

                if (isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        DoArgumentGUI(argumentProp);
                    }
                }
            }

            EditorGUIUtility.hierarchyMode = hierarchyMode;

            serializedObject.ApplyModifiedProperties();
        }

        protected abstract void DoArgumentTitleGUI(Rect rect, SerializedProperty argumentProp);
        protected abstract void DoArgumentGUI(SerializedProperty argumentProp);
        protected abstract void OnAddArgument();
        protected virtual void OnDuplicateArgument(SerializedProperty argumentProp, SerializedProperty newArgumentProp) { }
    }
}
