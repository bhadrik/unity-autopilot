using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityAutopilot.Utils;
using UnityEditor;
using UnityEngine;
using Logger = UnityAutopilot.Utils.Logger;

namespace UnityAutopilot.Tools.Command
{
    public class ManageShader : BaseCommand
    {
        // + Parameter class ----------------------------------
        public enum Action
        {
            create,
            read,
            update,
            delete
        }

        public class Args
        {
            [ToolParam("Operation to perform", isRequired: true)]
            [ToolParamEnum(typeof(Action))]
            public Action? action;

            [ToolParam("Shader name (no .shader extension).")]
            public string name;

            [ToolParam("Asset path")]
            public string path;

            [ToolParam("Shader code for 'create'/'update'.")]
            public string contents;
        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } =
            new ToolMetaData(
                name: "manage_shader",
                description: "Manages shader scripts in Unity (create, read, update, delete).",
                paramType: typeof(Args),
                toolType: typeof(ManageShader)
            );
        // ----------------------------------------------------

        public async override UniTask<Response> Execute()
        {
            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"[{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return await HandleCommand(param);
        }

        public async override UniTask<Response> Undo()
        {
            await UniTask.Yield();

            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"Undo [{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return Response.Success("Successfully undo new game object creation");
        }

        public async UniTask<Response> HandleCommand(Args args)
        {
            await UniTask.Yield();

            // Validate required parameters
            if (args.action == null)
            {
                return Response.Error("Action parameter is required.");
            }
            if (string.IsNullOrEmpty(args.name))
            {
                return Response.Error("Name parameter is required.");
            }
            // Basic name validation (alphanumeric, underscores, cannot start with number)
            if (!Regex.IsMatch(args.name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return Response.Error(
                    $"Invalid shader name: '{args.name}'. Use only letters, numbers, underscores, and don't start with a number."
                );
            }

            // Ensure path is relative to Assets/, removing any leading "Assets/"
            // Set default directory to "Shaders" if path is not provided
            string relativeDir = args.path ?? "Shaders"; // Default to "Shaders" if path is null
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }
            // Handle empty string case explicitly after processing
            if (string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = "Shaders"; // Ensure default if path was provided as "" or only "/" or "Assets/"
            }

            // Construct paths
            string shaderFileName = $"{args.name}.shader";
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
            string fullPath = Path.Combine(fullPathDir, shaderFileName);
            string relativePath = Path.Combine("Assets", relativeDir, shaderFileName)
                .Replace('\\', '/'); // Ensure "Assets/" prefix and forward slashes

            // Ensure the target directory exists for create/update
            if (args.action == Action.create || args.action == Action.update)
            {
                try
                {
                    Directory.CreateDirectory(fullPathDir);
                }
                catch (Exception e)
                {
                    return Response.Error(
                        $"Could not create directory '{fullPathDir}': {e.Message}"
                    );
                }
            }

            // Route to specific action handlers
            switch (args.action)
            {
                case Action.create:
                    return await CreateShader(fullPath, relativePath, args.name, args.contents);
                case Action.read:
                    return ReadShader(fullPath, relativePath);
                case Action.update:
                    return UpdateShader(fullPath, relativePath, args.name, args.contents);
                case Action.delete:
                    return DeleteShader(fullPath, relativePath);
                default:
                    return Response.Error(
                        $"Unknown action: '{args.action}'. Valid actions are: create, read, update, delete."
                    );
            }
        }

        private async static UniTask<Response> CreateShader(
            string fullPath,
            string relativePath,
            string name,
            string contents
        )
        {
            await UniTask.Yield();

            // Check if shader already exists
            if (File.Exists(fullPath))
            {
                return Response.Error(
                    $"Shader already exists at '{relativePath}'. Use 'update' action to modify."
                );
            }

            // Generate default content if none provided
            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultShaderContent(name);
            }

            try
            {
                File.WriteAllText(fullPath, contents);

                var res = Response.Success(
                    $"Shader '{name}.shader' created successfully at '{relativePath}'.",
                    new { path = relativePath }
                );

                res.isRecompileRequired = true;

                return res;
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create shader '{relativePath}': {e.Message}");
            }
        }

        private Response ReadShader(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Shader not found at '{relativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(fullPath);

                // Return both normal and encoded contents for larger files
                bool isLarge = contents.Length > 10000; // If content is large, include encoded version
                var responseData = new
                {
                    path = relativePath,
                    contents = contents
                };

                return Response.Success(
                    $"Shader '{Path.GetFileName(relativePath)}' read successfully.",
                    responseData
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to read shader '{relativePath}': {e.Message}");
            }
        }

        private Response UpdateShader(
            string fullPath,
            string relativePath,
            string name,
            string contents
        )
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error(
                    $"Shader not found at '{relativePath}'. Use 'create' action to add a new shader."
                );
            }
            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Content is required for the 'update' action.");
            }

            try
            {
                File.WriteAllText(fullPath, contents);

                var res = Response.Success(
                    $"Shader '{Path.GetFileName(relativePath)}' updated successfully.",
                    new { path = relativePath }
                );

                res.isRecompileRequired = true;

                return res;
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to update shader '{relativePath}': {e.Message}");
            }
        }

        private Response DeleteShader(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Shader not found at '{relativePath}'.");
            }

            try
            {
                // Delete the asset through Unity's AssetDatabase first
                bool success = AssetDatabase.DeleteAsset(relativePath);
                if (!success)
                {
                    return Response.Error($"Failed to delete shader through Unity's AssetDatabase: '{relativePath}'");
                }

                // If the file still exists (rare case), try direct deletion
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                var res = Response.Success($"Shader '{Path.GetFileName(relativePath)}' deleted successfully.");
                res.isRecompileRequired = true;

                return res;
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to delete shader '{relativePath}': {e.Message}");
            }
        }

        private static string GenerateDefaultShaderContent(string name)
        {
            return @"Shader """ + name + @"""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}";
        }
    }
}