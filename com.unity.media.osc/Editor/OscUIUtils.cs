using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Media.Osc.Editor
{
    static class OscUIUtils
    {
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void OnValueChanged<TElement>(this TElement element, SerializedObject serializedObject, Action<bool> onChange)
            where TElement : BindableElement, INotifyValueChanged<bool>
        {
            OnValueChanged(element, serializedObject, prop => prop.boolValue, onChange);
        }

        public static void OnValueChanged<TElement>(this TElement element, SerializedObject serializedObject, Action<int> onChange)
            where TElement : BindableElement, INotifyValueChanged<int>
        {
            OnValueChanged(element, serializedObject, prop => prop.intValue, onChange);
        }

        public static void OnValueChanged<TElement>(this TElement element, SerializedObject serializedObject, Action<float> onChange)
            where TElement : BindableElement, INotifyValueChanged<float>
        {
            OnValueChanged(element, serializedObject, prop => prop.floatValue, onChange);
        }

        public static void OnValueChanged<TElement>(this TElement element, SerializedObject serializedObject, Action<string> onChange)
            where TElement : BindableElement, INotifyValueChanged<string>
        {
            OnValueChanged(element, serializedObject, prop => prop.stringValue, onChange);
        }

        public static void OnValueChanged<TValue, TElement>(this TElement element, SerializedObject serializedObject, Func<SerializedProperty, TValue> getValue, Action<TValue> onChange)
            where TElement : BindableElement, INotifyValueChanged<TValue>
        {
            var initialValue = getValue(serializedObject.FindProperty(element.bindingPath));

            onChange.Invoke(initialValue);

            element.RegisterValueChangedCallback(evt =>
            {
                onChange(evt.newValue);
            });
        }

        public static void OnValueChanged<TValue>(this PropertyField element, SerializedObject serializedObject, Func<SerializedProperty, TValue> getValue, Action<TValue> onChange)
        {
            var initialValue = getValue(serializedObject.FindProperty(element.bindingPath));

            onChange.Invoke(initialValue);

            element.RegisterValueChangeCallback(evt =>
            {
                onChange(getValue(evt.changedProperty));
            });
        }
    }
}
