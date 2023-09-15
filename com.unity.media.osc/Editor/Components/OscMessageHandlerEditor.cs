using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    [CustomEditor(typeof(OscMessageHandler))]
    sealed class OscMessageHandlerEditor : OscMessageBaseEditor
    {
        static readonly (Type type, ArgumentHandlerAttribute attr)[] s_ArgumentHandlerTypes = TypeCache.GetTypesWithAttribute<ArgumentHandlerAttribute>()
            .Where(t => typeof(ArgumentHandler).IsAssignableFrom(t))
            .Select(type => (type, type.GetCustomAttribute(typeof(ArgumentHandlerAttribute)) as ArgumentHandlerAttribute))
            .Where(tuple => tuple.Item2 != null)
            .ToArray();

        static readonly Dictionary<Type, int> s_ArgumentHandlerTypeToIndex = s_ArgumentHandlerTypes
            .Select((t, i) => (t.type, i))
            .ToDictionary(t => t.type, t => t.i);

        static readonly GUIContent[] s_ArgumentHandlerOptions = s_ArgumentHandlerTypes
            .Select(t => new GUIContent(t.attr.DisplayName))
            .ToArray();

        static readonly string[] s_ExcludeEventProperties =
        {
            "m_MethodName",
        };

        [SerializeField]
        UnityEvent m_TempEvent;

        /// <inheritdoc />
        public override VisualElement CreateInspectorGUI()
        {
            var root = base.CreateInspectorGUI();

            var receiverRequiredMessage = root.Q<HelpBox>("receiver-required-message");

            root.Q<PropertyField>("receiver").OnValueChanged(serializedObject, prop => prop.objectReferenceValue, receiver =>
            {
                receiverRequiredMessage.SetDisplay(receiver == null);
            });

            return root;
        }

        protected override void DoArgumentTitleGUI(Rect rect, SerializedProperty argumentProp)
        {
            var typeName = argumentProp.managedReferenceFullTypename;
            var argumentType = Type.GetType(ManagedReferenceTypeToAssemblyQualified(typeName));

            if (argumentType == null || !s_ArgumentHandlerTypeToIndex.ContainsKey(argumentType))
            {
                argumentProp.managedReferenceValue = new ArgumentHandlerVoid();
                argumentType = typeof(ArgumentHandlerVoid);
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var index = s_ArgumentHandlerTypeToIndex[argumentType];
                var newIndex = EditorGUI.Popup(rect, index, s_ArgumentHandlerOptions);

                if (change.changed)
                {
                    // We should copy any many compatible properties as possible when we change
                    // the type of the serialized event. We need to copy the event properties
                    // to a temporary serialized event so they can persist though the assignment
                    // of the new argument instance, then copy the properties back to the new event.
                    using var tempSerialized = new SerializedObject(this);
                    var tempEventProp = tempSerialized.FindProperty(nameof(m_TempEvent));
                    var argumentEventProp = argumentProp.FindPropertyRelative("m_Event");

                    DeepCopy(argumentEventProp, tempEventProp, s_ExcludeEventProperties);

                    var newType = s_ArgumentHandlerTypes[newIndex].type;
                    var newArgument = Activator.CreateInstance(newType);
                    argumentProp.managedReferenceValue = newArgument;
                    argumentEventProp = argumentProp.FindPropertyRelative("m_Event");

                    DeepCopy(tempEventProp, argumentEventProp, s_ExcludeEventProperties);
                }
            }
        }

        protected override void DoArgumentGUI(SerializedProperty argumentProp)
        {
            var eventProp = argumentProp.FindPropertyRelative("m_Event");
            EditorGUILayout.PropertyField(eventProp, GUIContent.none);
        }

        protected override void OnAddArgument()
        {
            var handler = target as OscMessageHandler;
            handler.AddArgument(new ArgumentHandlerVoid());

            // Set the default event handler to reference the GameObject the component
            // is on, which is the typical case.
            serializedObject.Update();

            var argumentsProp = serializedObject.FindProperty("m_Arguments");
            var argumentProp = argumentsProp.GetArrayElementAtIndex(argumentsProp.arraySize - 1);
            var callsProp = argumentProp.FindPropertyRelative("m_Event.m_PersistentCalls.m_Calls");

            callsProp.InsertArrayElementAtIndex(callsProp.arraySize);
            var callProp = callsProp.GetArrayElementAtIndex(callsProp.arraySize - 1);

            var eventTargetProp = callProp.FindPropertyRelative("m_Target");
            var eventTargetTypeProp = callProp.FindPropertyRelative("m_TargetAssemblyTypeName");
            var eventCallStateProp = callProp.FindPropertyRelative("m_CallState");

            eventTargetProp.objectReferenceValue = handler.gameObject;
            eventTargetTypeProp.stringValue = typeof(GameObject).AssemblyQualifiedName;
            eventCallStateProp.intValue = (int)UnityEventCallState.RuntimeOnly;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        protected override void OnDuplicateArgument(SerializedProperty argumentProp, SerializedProperty newArgumentProp)
        {
            // we must do a deep copy of the argument, by default the duplicated argument is a shallow copy.
            // To do this we can create a new argument instance and copy over the serialized properties.
            var argument = newArgumentProp.managedReferenceValue;

            if (argument == null)
            {
                return;
            }

            var type = argument.GetType();
            var newArgument = Activator.CreateInstance(type);
            newArgumentProp.managedReferenceValue = newArgument;

            DeepCopy(argumentProp, newArgumentProp);
        }

        static string ManagedReferenceTypeToAssemblyQualified(string type)
        {
            try
            {
                var split = type.Split(' ');
                return $"{split[1]}, {split[0]}";
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        static void DeepCopy(SerializedProperty srcProp, SerializedProperty dstProp, params string[] exclude)
        {
            var srcItr = srcProp.Copy();
            var dstItr = dstProp.Copy();
            var endProp = srcProp.GetEndProperty();

            while (true)
            {
                var included = exclude.All(n => srcItr.name != n);

                if (included)
                {
                    switch (srcItr.propertyType)
                    {
                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ManagedReference:
                            break;
                        default:
                            dstItr.boxedValue = srcItr.boxedValue;
                            break;
                    }
                }

                if (!srcItr.Next(included) || !dstItr.Next(included) || SerializedProperty.EqualContents(srcItr, endProp))
                {
                    return;
                }
            }
        }
    }
}
