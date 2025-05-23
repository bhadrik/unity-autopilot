// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_ADDRESSABLES && UTILITIES_ASYNC

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Utilities.Extensions
{
    public static class SceneInstanceExtensions
    {
        public static async Task UnloadAsync(this SceneInstance instance, UnloadSceneOptions options = UnloadSceneOptions.None)
        {
            if (instance.IsValid())
            {
                if (instance.Scene.isLoaded)
                {
                    var unloadOp = Addressables.UnloadSceneAsync(instance, options, false);
                    await unloadOp.Task;
                    unloadOp.Release();
                }
                else
                {
                    Debug.LogWarning("Scene was not unloaded");
                }
            }
            else
            {
                throw new Exception("Invalid Scene Instance!");
            }
        }

        public static bool IsValid(this SceneInstance sceneInstance)
            => !string.IsNullOrWhiteSpace(sceneInstance.Scene.path) && sceneInstance.Scene.IsValid();
    }
}
#endif // UNITY_ADDRESSABLES && UTILITIES_ASYNC
