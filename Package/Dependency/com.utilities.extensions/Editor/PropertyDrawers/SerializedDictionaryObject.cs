// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utilities.Extensions.Editor
{
    public class SerializedDictionaryObject
    {
        public SerializedDictionaryObject(SerializedProperty property)
        {
            this.property = property;
            guid = property.GetUniqueIdentifier();
            keyData = property.FindPropertyRelative(nameof(keyData));
            valueData = property.FindPropertyRelative(nameof(valueData));
            Debug.Assert(keyData.isArray && valueData.isArray && keyData.arraySize == valueData.arraySize);
        }

        public readonly string guid;

        private readonly SerializedProperty property;

        public readonly SerializedProperty keyData;

        public readonly SerializedProperty valueData;

        internal int? selectedElement { get; set; }

        public int size => IsNull() ? 0 : keyData.arraySize;

        public bool IsNull() => property.serializedObject.IsNull();

        public KeyValuePair<SerializedProperty, SerializedProperty> GetArrayElementAtIndex(int index)
        {
            UpdateSerializedObject();
            return new KeyValuePair<SerializedProperty, SerializedProperty>(
                keyData.GetArrayElementAtIndex(index),
                valueData.GetArrayElementAtIndex(index));
        }

        public List<KeyValuePair<SerializedProperty, SerializedProperty>> ToList()
        {
            UpdateSerializedObject();
            var list = new List<KeyValuePair<SerializedProperty, SerializedProperty>>(size);

            for (var i = 0; i < size; i++)
            {
                var keyProperty = keyData.GetArrayElementAtIndex(i);
                var valueProperty = valueData.GetArrayElementAtIndex(i);
                list.Add(new KeyValuePair<SerializedProperty, SerializedProperty>(keyProperty, valueProperty));
            }

            return list;
        }

        internal bool CanAddNewItem()
        {
            if (IsNull()) { return false; }

            var items = ToList();

            foreach (var (key, _) in items)
            {
                if (key.IsDefaultValue())
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryAddNewEmptyItem(out KeyValuePair<SerializedProperty, SerializedProperty> item)
        {
            if (!CanAddNewItem())
            {
                item = default;
                return false;
            }

            var index = keyData.arraySize;
            keyData.InsertArrayElementAtIndex(index);
            valueData.InsertArrayElementAtIndex(index);
            var keyProperty = keyData.GetArrayElementAtIndex(index);
            keyProperty.SetDefaultValue();
            var valueProperty = valueData.GetArrayElementAtIndex(index);
            valueProperty.SetDefaultValue();
            ApplyModifiedProperties();
            item = new KeyValuePair<SerializedProperty, SerializedProperty>(keyProperty, valueProperty);
            return true;
        }

        public void RemoveItemAt(int index)
        {
            UpdateSerializedObject();
            keyData.DeleteArrayElementAtIndex(index);
            valueData.DeleteArrayElementAtIndex(index);
            ApplyModifiedProperties();
        }

        public void RemoveLastItem() => RemoveItemAt(size - 1);

        public void UpdateSerializedObject()
        {
            property.serializedObject.Update();
            keyData.serializedObject.Update();
            valueData.serializedObject.Update();
        }

        public void ApplyModifiedProperties()
        {
            property.serializedObject.ApplyModifiedProperties();
            keyData.serializedObject.ApplyModifiedProperties();
            valueData.serializedObject.ApplyModifiedProperties();
        }

        public void ApplyModifiedPropertiesWithoutUndo()
        {
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            keyData.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            valueData.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
