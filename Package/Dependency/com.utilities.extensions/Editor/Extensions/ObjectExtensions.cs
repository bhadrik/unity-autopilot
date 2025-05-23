// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace Utilities.Extensions.Editor
{
    /// <summary>
    /// Extensions for <see cref="Object"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Get <see cref="Object"/>.<see cref="AssetDatabase"/> guid.
        /// </summary>
        /// <param name="object">asset.</param>
        /// <returns>guid.</returns>
        public static string GetGuid(this Object @object)
            => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(@object), AssetPathToGUIDOptions.OnlyExistingAssets);
    }
}
