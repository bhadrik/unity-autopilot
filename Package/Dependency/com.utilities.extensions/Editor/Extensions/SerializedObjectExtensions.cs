// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;

namespace Utilities.Extensions.Editor
{
    public static class SerializedObjectExtensions
    {
        public static bool IsNull(this SerializedObject @object)
        {
            try
            {
                return @object == null || @object.targetObject == null;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsNotNull(this SerializedObject @object)
            => !IsNull(@object);
    }
}
