// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace Utilities.Extensions.Editor
{
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Checks if the underlying <see cref="Object"/> reference is null or missing.
        /// </summary>
        /// <param name="property"></param>
        /// <returns>True, if <see cref="Object"/> reference is null or missing.</returns>
        public static bool IsMissingObjectReference(this SerializedProperty property)
        {
            if (property == null) { return true; }
            return property.propertyType switch
            {
                SerializedPropertyType.ObjectReference => ReferenceEquals(property.objectReferenceValue, null),
                SerializedPropertyType.ExposedReference => ReferenceEquals(property.exposedReferenceValue, null),
                _ => false
            };
        }

        public static bool IsDefaultValue(this SerializedProperty property)
        {
            if (property == null) { return false; }
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue == 0,
                SerializedPropertyType.Float => Mathf.Approximately(property.floatValue, 0f),
                SerializedPropertyType.Boolean => property.boolValue == false,
                SerializedPropertyType.String => string.IsNullOrWhiteSpace(property.stringValue),
                SerializedPropertyType.Vector2 => property.vector2Value == Vector2.zero,
                SerializedPropertyType.Vector3 => property.vector3Value == Vector3.zero,
                SerializedPropertyType.Vector4 => property.vector4Value == Vector4.zero,
                SerializedPropertyType.Quaternion => property.quaternionValue == Quaternion.identity,
                SerializedPropertyType.ObjectReference => ReferenceEquals(property.objectReferenceValue, null),
                SerializedPropertyType.ExposedReference => ReferenceEquals(property.exposedReferenceValue, null),
                _ => false
            };
        }

        public static void SetDefaultValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = default;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = default;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = default;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = default;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = default;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = default;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    break;
            }
        }

        public static string GetUniqueIdentifier(this SerializedProperty property)
            => $"{property.serializedObject.targetObject.GetInstanceID()}/{property.propertyPath}";
    }
}
