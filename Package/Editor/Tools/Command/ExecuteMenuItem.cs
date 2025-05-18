using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityAutopilot.Utils;
using UnityEditor;
using UnityEngine;
using Logger = UnityAutopilot.Utils.Logger;

namespace UnityAutopilot.Tools.Command
{
    public class ExecuteMenuItem : BaseCommand
    {
        // + Parameter class ----------------------------------
        public enum Action
        {
            execute,
            get_all
        }
        public class Args
        {
            [ToolParam("The operation to perform (default: 'execute').", isRequired: true)]
            [ToolParamEnum(typeof(Action))]
            public Action action = Action.execute;

            [ToolParam("The full path of the menu item to execute.")]
            public string menuPath;

            [ToolParam("Optional parameters for the menu item (rarely used).")]
            public Dictionary<string, object> parameters;
        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } = 
            new ToolMetaData(
                name: "execute_menu_item",
                description: "Executes a Unity Editor menu item via its path (e.g., \"File/Save Project\"). Returns: A dictionary indicating success or failure, with optional message/error.",
                paramType: typeof(Args),
                toolType: typeof(ExecuteMenuItem)
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
        /// Main handler for executing menu items or getting available ones.
        /// </summary>
        public static async UniTask<Response> HandleCommand(Args args)
        {
            try
            {
                switch (args.action)
                {
                    case Action.execute:
                        return await ExecuteItem(args);
                    case Action.get_all:
                        // Getting a comprehensive list of *all* menu items dynamically is very difficult
                        // and often requires complex reflection or maintaining a manual list.
                        // Returning a placeholder/acknowledgement for now.
                        Debug.LogWarning(
                            "[ExecuteMenuItem] 'get_available_menus' action is not fully implemented. Dynamically listing all menu items is complex."
                        );
                        // Returning an empty list as per the refactor plan's requirements.
                        return Response.Success(
                            "'get_available_menus' action is not fully implemented. Returning empty list.",
                            new List<string>()
                        );
                    // TODO: Consider implementing a basic list of common/known menu items or exploring reflection techniques if this feature becomes critical.
                    default:
                        return Response.Error(
                            $"Unknown action: '{args.action}'. Valid actions are 'execute', 'get_available_menus'."
                        );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExecuteMenuItem] Action '{args.action}' failed: {e}");
                return Response.Error($"Internal error processing action '{args.action}': {e.Message}");
            }
        }

        // Basic blacklist to prevent accidental execution of potentially disruptive menu items.
        // This can be expanded based on needs.
        private static readonly HashSet<string> _menuPathBlacklist = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "File/Quit",
            // Add other potentially dangerous items like "Edit/Preferences...", "File/Build Settings..." if needed
        };

        /// <summary>
        /// Executes a specific menu item.
        /// </summary>
        private async static UniTask<Response> ExecuteItem(Args args)
        {
            await UniTask.Yield();

            if (string.IsNullOrWhiteSpace(args.menuPath))
            {
                return Response.Error("Required parameter 'menu_path' is missing or empty.");
            }

            // Validate against blacklist
            if (_menuPathBlacklist.Contains(args.menuPath))
            {
                return Response.Error(
                    $"Execution of menu item '{args.menuPath}' is blocked for safety reasons."
                );
            }

            // TODO: Implement alias lookup here if needed (Map alias to actual menuPath).
            // if (!string.IsNullOrEmpty(alias)) { menuPath = LookupAlias(alias); if(menuPath == null) return Response.Error(...); }

            // TODO: Handle parameters ('parameters' object) if a viable method is found.
            // This is complex as EditorApplication.ExecuteMenuItem doesn't take arguments directly.
            // It might require finding the underlying EditorWindow or command if parameters are needed.

            try
            {
                await UniTask.WaitForSeconds(0.1f);

                bool executed = EditorApplication.ExecuteMenuItem(args.menuPath);
                // Log potential failure inside the delayed call.
                if (!executed)
                {
                    return Response.Error($"Menu item not found on path: '{args.menuPath}'");
                }

                // Report attempt immediately, as execution is delayed.
                return Response.Success($"Execution successfully started: '{args.menuPath}'.");
            }
            catch (Exception e)
            {
                return Response.Error(
                    $"Error setting up execution for menu item '{args.menuPath}': {e.Message}"
                );
            }
        }

        // TODO: Add helper for alias lookup if implementing aliases.
        // private static string LookupAlias(string alias) { ... return actualMenuPath or null ... }
    }
}