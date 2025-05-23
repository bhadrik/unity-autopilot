// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utilities.Encoding.Wav.Editor
{
    internal static class WavEditorUtilities
    {
        [MenuItem("CONTEXT/AudioClip/Convert to wav...", true)]
        public static bool ConvertToWavValidate(MenuCommand menuCommand)
        {
            if (menuCommand.context is AudioClip audioClip)
            {
                var oldClipPath = AssetDatabase.GetAssetPath(audioClip);
                return !oldClipPath.Contains(".wav");
            }

            return false;
        }

        [MenuItem("CONTEXT/AudioClip/Convert to wav...", false)]
        public static void ConvertToWav(MenuCommand menuCommand)
        {
            var audioClip = menuCommand.context as AudioClip;
            var oldClipPath = AssetDatabase.GetAssetPath(audioClip);
            var newClipPath = oldClipPath.Replace(Path.GetExtension(oldClipPath), ".wav");

            if (File.Exists(newClipPath) &&
                !EditorUtility.DisplayDialog("Attention!", "Do you want to overwrite the exiting file?", "Yes", "No"))
            {
                return;
            }

            EditorUtility.DisplayProgressBar("Converting clip", $"{oldClipPath} -> {newClipPath}", -1);

            try
            {
                File.WriteAllBytes(newClipPath, audioClip.EncodeToWav());
                AssetDatabase.ImportAsset(newClipPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
