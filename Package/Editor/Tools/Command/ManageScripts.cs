using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityAutopilot.Utils;
using UnityEditor;
using UnityEngine;
using Logger = UnityAutopilot.Utils.Logger;

namespace UnityAutopilot.Tools.Command
{
    public class ManageSripts : BaseCommand
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
            [ToolParam("Operation ('create', 'read', 'update', 'delete')", isRequired: true)]
            [ToolParamEnum(typeof(Action))]
            public Action? action;

            [ToolParam("Script name (no .cs extension). If path not give name will bed used as search tearm.", isRequired: true)]
            public string name;

            [ToolParam("Asset path")]
            public string path;

            [ToolParam("C# code for 'create'/'update'.")]
            public string contents;

            [ToolParam("Type hint (e.g., 'MonoBehaviour').")]
            public string scriptType;

            [ToolParam("Script namespace")]
            public string scriptNamespace;

        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } = 
            new ToolMetaData(
                name: "manage_scripts",
                description: "Manages C# scripts in Unity (create, read, update, delete). Make reference variables public for easier access in the Unity Editor.",
                paramType: typeof(Args),
                toolType: typeof(ManageSripts)
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

        /// <summary>
        /// Main handler for script management actions.
        /// </summary>
        public async static UniTask<Response> HandleCommand(Args args)
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
                    $"Invalid script name: '{args.name}'. Use only letters, numbers, underscores, and don't start with a number."
                );
            }

            // Ensure path is relative to Assets/, removing any leading "Assets/"
            // Set default directory to "Scripts" if path is not provided
            string relativeDir = args.path ?? "Scripts"; // Default to "Scripts" if path is null

            // Check if relativeDir contains a file name (e.g., scriptname.cs) and remove it
            if (relativeDir.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                relativeDir = Path.GetDirectoryName(relativeDir)?.Replace('\\', '/') ?? "Scripts";
            }

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
                relativeDir = "Scripts"; // Ensure default if path was provided as "" or only "/" or "Assets/"
            }

            // Construct paths
            string scriptFileName = $"{args.name}.cs";
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir); // Application.dataPath ends in "Assets"
            string fullPath = Path.Combine(fullPathDir, scriptFileName);
            string relativePath = Path.Combine("Assets", relativeDir, scriptFileName)
                .Replace('\\', '/'); // Ensure "Assets/" prefix and forward slashes

            // Ensure the target directory exists for create/update
            if (!File.Exists(fullPathDir) && (args.action == Action.create || args.action == Action.update))
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
                    return CreateScript(
                        fullPath,
                        relativePath,
                        args.name,
                        args.contents,
                        args.scriptType,
                        args.scriptNamespace
                    );
                case Action.read:
                    return ReadScript(fullPath, relativePath);
                case Action.update:
                    return UpdateScript(fullPath, relativePath, args.name, args.contents);
                case Action.delete:
                    return DeleteScript(fullPath, relativePath);
                default:
                    return Response.Error(
                        $"Unknown action: '{args.action}'. Valid actions are: create, read, update, delete."
                    );
            }
        }

        private static Response CreateScript(
            string fullPath,
            string relativePath,
            string name,
            string contents,
            string scriptType,
            string namespaceName
        )
        {
            // Check if script already exists
            if (File.Exists(fullPath))
            {
                return Response.Error(
                    $"Script already exists at '{relativePath}'. Use 'update' action to modify."
                );
            }

            // Generate default content if none provided
            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultScriptContent(name, scriptType, namespaceName);
            }

            // Validate syntax (basic check)
            if (!ValidateScriptSyntax(contents))
            {
                // Optionally return a specific error or warning about syntax
                // return Response.Error("Provided script content has potential syntax errors.");
                Debug.LogWarning($"Potential syntax error in script being created: {name}");
            }

            try
            {
                File.WriteAllText(fullPath, contents);

                var res = Response.Success(
                    $"Script '{name}.cs' created successfully at '{relativePath}'.",
                    new { path = relativePath }
                );

                res.isRecompileRequired = true;

                return res;
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create script '{relativePath}': {e.Message}");
            }
        }

        private static Response ReadScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(fullPath);

                var responseData = new
                {
                    path = relativePath,
                    contents = contents,
                };

                return Response.Success(
                    $"Script '{Path.GetFileName(relativePath)}' read successfully.",
                    responseData
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to read script '{relativePath}': {e.Message}");
            }
        }

        private  static Response UpdateScript(
            string fullPath,
            string relativePath,
            string name,
            string contents
        )
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error(
                    $"Script not found at '{relativePath}'. Use 'create' action to add a new script."
                );
            }
            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Content is required for the 'update' action.");
            }

            // Validate syntax (basic check)
            if (!ValidateScriptSyntax(contents))
            {
                Debug.LogWarning($"Potential syntax error in script being updated: {name}");
                // Consider if this should be a hard error or just a warning
            }

            try
            {
                File.WriteAllText(fullPath, contents);

                var res = Response.Success(
                    $"Script '{name}.cs' updated successfully at '{relativePath}'.",
                    new { path = relativePath }
                );

                res.isRecompileRequired = true;

                return res;
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to update script '{relativePath}': {e.Message}");
            }
        }

        private static Response DeleteScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'. Cannot delete.");
            }

            try
            {
                // Use AssetDatabase.MoveAssetToTrash for safer deletion (allows undo)
                bool deleted = AssetDatabase.MoveAssetToTrash(relativePath);
                if (deleted)
                {
                    var res = Response.Success(
                        $"Script '{Path.GetFileName(relativePath)}' moved to trash successfully."
                    );

                    res.isRecompileRequired = true;

                    return res;
                }
                else
                {
                    // Fallback or error if MoveAssetToTrash fails
                    return Response.Error(
                        $"Failed to move script '{relativePath}' to trash. It might be locked or in use."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting script '{relativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Generates basic C# script content based on name and type.
        /// </summary>
        private static string GenerateDefaultScriptContent(
            string name,
            string scriptType,
            string namespaceName
        )
        {
            string usingStatements = "using UnityEngine;\nusing System.Collections;\n";
            string classDeclaration;
            string body =
                "\n    // Use this for initialization\n    void Start() {\n\n    }\n\n    // Update is called once per frame\n    void Update() {\n\n    }\n";

            string baseClass = "";
            if (!string.IsNullOrEmpty(scriptType))
            {
                if (scriptType.Equals("MonoBehaviour", StringComparison.OrdinalIgnoreCase))
                    baseClass = " : MonoBehaviour";
                else if (scriptType.Equals("ScriptableObject", StringComparison.OrdinalIgnoreCase))
                {
                    baseClass = " : ScriptableObject";
                    body = ""; // ScriptableObjects don't usually need Start/Update
                }
                else if (
                    scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase)
                    || scriptType.Equals("EditorWindow", StringComparison.OrdinalIgnoreCase)
                )
                {
                    usingStatements += "using UnityEditor;\n";
                    if (scriptType.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                        baseClass = " : Editor";
                    else
                        baseClass = " : EditorWindow";
                    body = ""; // Editor scripts have different structures
                }
                // Add more types as needed
            }

            classDeclaration = $"public class {name}{baseClass}";

            string fullContent = $"{usingStatements}\n";
            bool useNamespace = !string.IsNullOrEmpty(namespaceName);

            if (useNamespace)
            {
                fullContent += $"namespace {namespaceName}\n{{\n";
                // Indent class and body if using namespace
                classDeclaration = "    " + classDeclaration;
                body = string.Join("\n", body.Split('\n').Select(line => "    " + line));
            }

            fullContent += $"{classDeclaration}\n{{\n{body}\n}}";

            if (useNamespace)
            {
                fullContent += "\n}"; // Close namespace
            }

            return fullContent.Trim() + "\n"; // Ensure a trailing newline
        }

        /// <summary>
        /// Performs a very basic syntax validation (checks for balanced braces).
        /// TODO: Implement more robust syntax checking if possible.
        /// </summary>
        private static bool ValidateScriptSyntax(string contents)
        {
            if (string.IsNullOrEmpty(contents))
                return true; // Empty is technically valid?

            int braceBalance = 0;
            foreach (char c in contents)
            {
                if (c == '{')
                    braceBalance++;
                else if (c == '}')
                    braceBalance--;
            }

            return braceBalance == 0;
            // This is extremely basic. A real C# parser/compiler check would be ideal
            // but is complex to implement directly here.
        }
    }
}