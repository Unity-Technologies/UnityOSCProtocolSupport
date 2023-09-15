using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    [CustomEditor(typeof(OscMessageOutput))]
    sealed class OscMessageOutputEditor : OscMessageBaseEditor
    {
        static class Contents
        {
            public static readonly GUIContent ComponentField = new GUIContent("Component", "The component to get the argument data from.");
            public static readonly GUIContent PropertyField = new GUIContent("Property", "The field, property, or method to get the argument data from.");
        }

        static Dictionary<Type, Type> s_TypeToArgumentOutputType = TypeCache.GetTypesWithAttribute<ArgumentOutputAttribute>()
            .Where(t => typeof(IArgumentOutput).IsAssignableFrom(t))
            .Select(type => (type, type.GetCustomAttribute(typeof(ArgumentOutputAttribute)) as ArgumentOutputAttribute))
            .Where(tuple => tuple.Item2 != null)
            .GroupBy(tuple => tuple.Item2.Type)
            .ToDictionary(t => t.Key, t => t.OrderByDescending(tuple => tuple.Item2.Priority).First().type);

        /// <inheritdoc />
        public override VisualElement CreateInspectorGUI()
        {
            var root = base.CreateInspectorGUI();

            var senderRequiredMessage = root.Q<HelpBox>("sender-required-message");

            root.Q<PropertyField>("sender").OnValueChanged(serializedObject, prop => prop.objectReferenceValue, sender =>
            {
                senderRequiredMessage.SetDisplay(sender == null);
            });

            return root;
        }

        protected override void DoArgumentTitleGUI(Rect rect, SerializedProperty argumentProp)
        {
            var memberValueTypeProp = argumentProp.FindPropertyRelative("m_MemberValueType");

            if (!string.IsNullOrEmpty(memberValueTypeProp.stringValue))
            {
                EditorGUI.LabelField(rect, memberValueTypeProp.stringValue);
            }
        }

        protected override void DoArgumentGUI(SerializedProperty argumentProp)
        {
            var objectProp = argumentProp.FindPropertyRelative("m_Object");
            var sourceProp = argumentProp.FindPropertyRelative("m_Component");
            var memberTypeProp = argumentProp.FindPropertyRelative("m_MemberType");
            var memberValueTypeProp = argumentProp.FindPropertyRelative("m_MemberValueType");
            var memberNameProp = argumentProp.FindPropertyRelative("m_MemberName");
            var outputProp = argumentProp.FindPropertyRelative("m_Output");

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(objectProp);

                if (change.changed)
                {
                    sourceProp.objectReferenceValue = null;
                    memberTypeProp.intValue = (int)PropertyOutput.MemberType.Invalid;
                    memberValueTypeProp.stringValue = null;
                    memberNameProp.stringValue = null;
                    outputProp.managedReferenceValue = null;
                }
            }

            using (new EditorGUI.DisabledScope(objectProp.objectReferenceValue == null))
            {
                {
                    var source = sourceProp.objectReferenceValue;
                    var rect = EditorGUILayout.GetControlRect();

                    rect = EditorGUI.PrefixLabel(rect, Contents.ComponentField);

                    if (GUI.Button(rect, source != null ? source.GetType().Name : "None", EditorStyles.popup))
                    {
                        var gameObject = objectProp.objectReferenceValue as GameObject;
                        var components = gameObject.GetComponents<Component>();

                        var menu = new GenericMenu();

                        foreach (var component in components)
                        {
                            menu.AddItem(new GUIContent(component.GetType().Name), component == source, () =>
                            {
                                sourceProp.objectReferenceValue = component;
                                memberTypeProp.intValue = (int)PropertyOutput.MemberType.Invalid;
                                memberValueTypeProp.stringValue = null;
                                memberNameProp.stringValue = null;
                                outputProp.managedReferenceValue = null;

                                serializedObject.ApplyModifiedProperties();
                            });
                        }

                        menu.DropDown(rect);
                    }
                }

                using (new EditorGUI.DisabledScope(sourceProp.objectReferenceValue == null))
                {
                    var memberName = memberNameProp.stringValue;
                    var rect = EditorGUILayout.GetControlRect();

                    rect = EditorGUI.PrefixLabel(rect, Contents.PropertyField);

                    if (GUI.Button(rect, !string.IsNullOrEmpty(memberName) ? memberName : "None", EditorStyles.popup))
                    {
                        var source = sourceProp.objectReferenceValue;
                        var members = GetMembers(source.GetType());

                        var menu = new GenericMenu();

                        foreach (var member in members)
                        {
                            var name = $"{member.valueType.Name} {member.name}";

                            if (member.type == PropertyOutput.MemberType.Method)
                            {
                                name += " ()";
                            }

                            menu.AddItem(new GUIContent(name), member.name == memberName, () =>
                            {
                                var outputType = s_TypeToArgumentOutputType[member.valueType];
                                var output = Activator.CreateInstance(outputType) as IArgumentOutput;

                                memberTypeProp.intValue = (int)member.type;
                                memberValueTypeProp.stringValue = member.valueType.Name;
                                memberNameProp.stringValue = member.name;
                                outputProp.managedReferenceValue = output;

                                serializedObject.ApplyModifiedProperties();
                            });
                        }

                        menu.DropDown(rect);
                    }

                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        if (sourceProp.objectReferenceValue != null && !string.IsNullOrEmpty(outputProp.managedReferenceFullTypename))
                        {
                            var prop = outputProp.Copy();
                            var endProp = outputProp.GetEndProperty();

                            while (prop.Next(true) && !SerializedProperty.EqualContents(prop, endProp))
                            {
                                EditorGUILayout.PropertyField(prop);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnAddArgument()
        {
            var output = target as OscMessageOutput;
            output.AddArgumentOutput();
        }

        static (PropertyOutput.MemberType type, string name, Type valueType)[] GetMembers(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => s_TypeToArgumentOutputType.ContainsKey(m.FieldType))
                .Select(m => (PropertyOutput.MemberType.Field, m.Name, m.FieldType))
                .OrderBy(m => m.Name)
                .ToArray();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => s_TypeToArgumentOutputType.ContainsKey(m.PropertyType) && m.GetGetMethod() != null)
                .Select(m => (PropertyOutput.MemberType.Property, m.Name, m.PropertyType))
                .OrderBy(m => m.Name)
                .ToArray();

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => !m.IsSpecialName && m.GetParameters().Length == 0 && s_TypeToArgumentOutputType.ContainsKey(m.ReturnType))
                .Select(m => (PropertyOutput.MemberType.Method, m.Name, m.ReturnType))
                .OrderBy(m => m.Name)
                .ToArray();

            return fields.Concat(properties).Concat(methods)
                .ToArray();
        }
    }
}
