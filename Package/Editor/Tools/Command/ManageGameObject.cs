using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityAutopilot.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = UnityAutopilot.Utils.Logger;

namespace UnityAutopilot.Tools.Command
{
    public class ManageGameObject : BaseCommand
    {
        // + Parameter class ----------------------------------
        [Serializable]
        public enum Action
        {
            create,
            modify,
            delete,
            find,
            get_components,
            add_component,
            remove_component,
            set_component_property
        }

        [Serializable]
        public enum SearchMethod
        {
            undefined,
            by_name,
            by_id,
            by_path,
            by_tag,
            by_layer,
            by_component
        }

        [Serializable]
        public class Args
        {
            [ToolParam("Operation to be performed on Unity GameObject", isRequired: true)]
            public Action? action;

            [ToolParam("GameObject identifier. Can be integer for instance id of object or string name/path of object. for modify/delete/component actions.")]
            [ToolParamMultiType(typeof(int), typeof(string))]
            // This can be string or int (instance id)
            public object target;

            [ToolParam("GameObject name - used for both 'create' (initial name) and 'modify' (rename). To find object by name use 'target' not 'name'.")]
            public string name;

            [ToolParam("Tag name - used for both 'create' (initial tag) and 'modify' (change tag)")]
            public string tag;

            [ToolParam("Parent GameObject identifier. can be integer if instance id of object or string name/path of parent object that you want refer.")]
            [ToolParamMultiType(typeof(int), typeof(string))]
            // This can be string or int (instance id)
            public object parent;

            [ToolParam("Layer name - used for both 'create' (initial layer) and 'modify' (change layer).")]
            public string layer;

            [ToolParam("bool")]
            public bool? setActive;

            [ToolParam("bool")]
            public bool? saveAsPrefab;

            [ToolParam("Unity PrimitiveType enum")]
            [ToolParamEnum(typeof(PrimitiveType))]
            public PrimitiveType? primitiveType;

            [ToolParam("Vector3 postion")]
            public Vector3? position;

            [ToolParam("Vector3 rotation")]
            public Vector3? rotation;

            [ToolParam("Vector3 local scale")]
            public Vector3? scale;

            [ToolParam("path")]
            public string prefabPath;

            [ToolParam("path")]
            public string prefabFolder;

            [ToolParam(
@"Dict mapping Component names to their properties to set.
Example: {""Rigidbody"": {""CompnentName"":""Rigidbody"", ""mass"": 10.0, ""useGravity"": True}},
To set references:
- Use asset path string for Prefabs/Materials, e.g., {""MeshRenderer"": {""material"": ""Assets/Materials/MyMat.mat""}}
- Use a dict for scene objects/components, e.g.:
{""MyScript"": {""otherObject"": {""find"": ""Player"", ""method"": ""by_name""}}} (assigns GameObject)
{""MyScript"": {""playerHealth"": {""find"": ""Player"", ""component"": ""HealthComponent""}}} (assigns Component)
Example set nested property:
- Access shared material: {""MeshRenderer"": {""sharedMaterial.color"": [1, 0, 0, 1]}}")]
            [JsonProperty()]
            public Dictionary<string, Dictionary<string, object>> componentProperties;

            [ToolParam("array of component names to add, can pass component property in 'componentProperties'")]
            public string[] components_to_add;

            [ToolParam("array of component names to remove, can have componentProperties with it")]
            public string[] componentsToRemove;

            [ToolParam("to find object used with searchMethod")]
            public string searchTerm;

            [ToolParam("How to find object(s) described in searchTerm. used with 'find' or some 'target' lookups.")]
            [ToolParamEnum(typeof(SearchMethod))]
            public SearchMethod? searchMethod;

            [ToolParam("bool")]
            public bool? findAll;

            [ToolParam("bool")]
            public bool? searchInChildren;

            [ToolParam("bool")]
            public bool? searchInactive;

            [ToolParam("component Name")]
            public string componentName;
        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } = 
            new ToolMetaData(
                name: "manage_gameObject",
                description:
@"Manages GameObjects: create, modify, delete, find, and component operations.
Action-specific arguments (e.g., position, rotation, scale for create/modify;
component_name for component actions;
search_term, find_all for 'find').
Returns: Dictionary with operation results ('success', 'message', 'data').",
                paramType: typeof(Args),
                toolType: typeof(ManageGameObject)
            );
        // ----------------------------------------------------

        public override async UniTask<Response> Execute()
        {
            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"[{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return await HandleCommand(param);
        }

        public override async UniTask<Response> Undo()
        {
            await UniTask.Yield();

            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"Undo s[{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return Response.Success("Successfully undo new game object creation");
        }

        public static async UniTask<Response> HandleCommand(Args args)
        {
            if (args.action == null)
            {
                return Response.Error("Error: Action parameter is required.");
            }

            // --- Prefab Redirection Check ---
            string targetPath = args.target as string;
            if (!string.IsNullOrEmpty(targetPath) &&
                targetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                if (args.action == Action.modify || args.action == Action.set_component_property)
                {
                    Debug.Log(
                        $"[ManageGameObject->ManageAsset] Redirecting action '{args.action}' for prefab '{targetPath}' to ManageAsset."
                    );

                    var manageAssetArgs = new ManageAssetCommand.Args()
                    {
                        action = ManageAssetCommand.Action.modify,
                        path = targetPath,
                        properties = null
                    };

                    if (args.action == Action.set_component_property)
                    {
                        if (string.IsNullOrEmpty(args.componentName))
                            return Response.Error("Error: Missing 'componentName' for 'set_component_property' on prefab.");

                        if (args.componentProperties == null ||
                            !args.componentProperties.TryGetValue(args.componentName, out var compPropsDict))
                            return Response.Error($"Error: Missing or invalid 'componentProperties' for component '{args.componentName}'.");

                        manageAssetArgs.properties = new();
                        manageAssetArgs.properties[args.componentName] = args.componentProperties[args.componentName];
                    }
                    else
                    {
                        // Assuming only single component property given in componentProperties for single modify
                        manageAssetArgs.properties = args.componentProperties.FirstOrDefault().Value;

                        if (manageAssetArgs.properties == null)
                            return Response.Error("Missing 'componentProperties' for 'modify' action on prefab.");
                    }

                    // Call ManageAsset
                    ManageAssetCommand manageAsset = new ManageAssetCommand();
                    manageAsset.SetArgumentData(Json.convert.SerializeObject(manageAssetArgs));

                    return await manageAsset.Execute();
                }
                else if (
                    args.action == Action.delete ||
                    args.action == Action.add_component ||
                    args.action == Action.remove_component ||
                    args.action == Action.get_components)
                {
                    return Response.Error($"Error: Action '{args.action}' on a prefab asset ('{targetPath}') should be performed using the 'manage_asset' command.");
                }
            }
            // --- End Prefab Redirection Check ---

            try
            {
                switch (args.action)
                {
                    case Action.create:
                        return await CreateGameObject(args);
                    case Action.modify:
                        return ModifyGameObject(args);
                    case Action.delete:
                        return DeleteGameObject(args);
                    case Action.find:
                        return FindGameObjects(args);
                    case Action.get_components:
                        return GetComponentsFromTarget(args);
                    case Action.add_component:
                        return AddComponentToTarget(args);
                    case Action.remove_component:
                        return RemoveComponentFromTarget(args);
                    case Action.set_component_property:
                        return SetComponentPropertyOnTarget(args);
                    default:
                        return Response.Error($"Unknown action: '{args.action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageGameObject] Action '{args.action}' failed: {e}");
                return Response.Error($"Error: Internal error processing action '{args.action}': {e.Message}");
            }
        }

        private async static UniTask<Response> CreateGameObject(Args args)
        {
            if (string.IsNullOrEmpty(args.name))
            {
                return Response.Error("'name' parameter is required for 'create' action.");
            }

            GameObject newGo = null;

            string originalPrefabPath = args.prefabPath; // Keep original for messages
            string prefabId = null;

            if (!string.IsNullOrEmpty(args.prefabPath))
            {
                if (!args.prefabPath.Contains("/") && !args.prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    string prefabNameOnly = args.prefabPath;

                    Debug.Log(
                        $"[ManageGameObject.Create] Searching for prefab named: '{prefabNameOnly}'"
                    );

                    string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefabNameOnly}");

                    if (guids.Length == 0)
                    {
                        return Response.Error($"Prefab named '{prefabNameOnly}' not found anywhere in the project.");
                    }
                    else if (guids.Length > 1)
                    {
                        string foundPaths = string.Join(", ", guids.Select(g => AssetDatabase.GUIDToAssetPath(g)));

                        return Response.Error($"Multiple prefabs found matching name '{prefabNameOnly}': {foundPaths}. Please provide a more specific path.");
                    }
                    else // Exactly one found
                    {
                        prefabId = AssetDatabase.GUIDToAssetPath(guids[0]); // Update prefabPath with the full path
                        Debug.Log(
                            $"[ManageGameObject.Create] Found unique prefab at path: '{prefabId}'"
                        );
                    }
                }
                else if (!args.prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    // If it looks like a path but doesn't end with .prefab, assume user forgot it and append it.
                    // We could also error here, but appending might be more user-friendly.
                    Debug.LogWarning(
                        $"[ManageGameObject.Create] Provided prefabPath '{args.prefabPath}' does not end with .prefab. Assuming it's missing and appending."
                    );
                    prefabId += ".prefab";
                    // Note: This path might still not exist, AssetDatabase.LoadAssetAtPath will handle that.
                }

                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabId);

                if (prefabAsset != null)
                {
                    try
                    {
                        // Instantiate the prefab, initially place it at the root
                        // Parent will be set later if specified
                        newGo = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;

                        if (newGo == null)
                        {
                            // This might happen if the asset exists but isn't a valid GameObject prefab somehow
                            Debug.LogError(
                                $"[ManageGameObject.Create] Failed to instantiate prefab at '{prefabId}', asset might be corrupted or not a GameObject."
                            );

                            return Response.Error($"Failed to instantiate prefab at '{prefabId}'.");
                        }

                        // Name the instance based on the 'name' parameter, not the prefab's default name
                        if (!string.IsNullOrEmpty(args.name))
                        {
                            newGo.name = args.name;
                        }

                        // Register Undo for prefab instantiation
                        UnityEditor.Undo.RegisterCreatedObjectUndo(
                            newGo,
                            $"Instantiate Prefab '{prefabAsset.name}' as '{newGo.name}'"
                        );
                        Debug.Log(
                            $"[ManageGameObject.Create] Instantiated prefab '{prefabAsset.name}' from path '{prefabId}' as '{newGo.name}'."
                        );
                    }
                    catch (Exception e)
                    {
                        return Response.Error($"Error instantiating prefab '{prefabId}': {e.Message}");
                    }
                }
                else
                {
                    // Only return error if prefabPath was specified but not found.
                    // If prefabPath was empty/null, we proceed to create primitive/empty.
                    Debug.LogWarning(
                        $"[ManageGameObject.Create] Prefab asset not found at path: '{prefabId}'. Will proceed to create new object if specified."
                    );
                    // Do not return error here, allow fallback to primitive/empty creation
                }
            }

            // --- Fallback: Create Primitive or Empty GameObject ---
            bool createdNewObject = false; // Flag to track if we created (not instantiated)
            if (newGo == null) // Only proceed if prefab instantiation didn't happen
            {
                if (args.primitiveType != null)
                {
                    try
                    {
                        newGo = GameObject.CreatePrimitive(args.primitiveType.GetValueOrDefault());

                        // Set name *after* creation for primitives
                        if (args.name != null)
                            newGo.name = args.name;
                        else
                            return Response.Error("'name' parameter is required when creating a primitive.");
                        createdNewObject = true;
                    }
                    catch (ArgumentException)
                    {
                        return Response.Error($"Invalid primitive type: '{args.primitiveType}'. Valid types: {string.Join(", ", Enum.GetNames(typeof(PrimitiveType)))}");
                    }
                    catch (Exception e)
                    {
                        return Response.Error($"Failed to create primitive '{args.primitiveType}': {e.Message}");
                    }
                }
                else // Create empty GameObject
                {
                    if (args.name == null)
                    {
                        return Response.Error("'name' parameter is required for 'create' action when not instantiating a prefab or creating a primitive.");
                    }
                    newGo = new GameObject(args.name);
                    createdNewObject = true;
                }

                // Record creation for Undo *only* if we created a new object
                if (createdNewObject)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(newGo, $"Create GameObject '{newGo.name}'");
                }
            }

            // --- Common Setup (Parent, Transform, Tag, Components) - Applied AFTER object exists ---
            if (newGo == null)
            {
                // Should theoretically not happen if logic above is correct, but safety check.
                return Response.Error("Failed to create or instantiate the GameObject.");
            }

            // Record potential changes to the existing prefab instance or the new GO
            // Record transform separately in case parent changes affect it
            UnityEditor.Undo.RecordObject(newGo.transform, "Set GameObject Transform");
            UnityEditor.Undo.RecordObject(newGo, "Set GameObject Properties");

            // Set Parent
            if (args.parent != null)
            {
                GameObject parentGo = FindObjectInternal(args.parent, SearchMethod.undefined, args);

                if (parentGo == null)
                {
                    UnityEngine.Object.DestroyImmediate(newGo); // Clean up created object
                    return Response.Error($"Parent specified ('{args.parent}') but not found.");
                }
                newGo.transform.SetParent(parentGo.transform, true); // worldPositionStays = true
            }

            if (args.position.HasValue)
                newGo.transform.localPosition = args.position.Value;
            if (args.rotation.HasValue)
                newGo.transform.localEulerAngles = args.rotation.Value;
            if (args.scale.HasValue)
                newGo.transform.localScale = args.scale.Value;

            // Set Tag (added for create action)
            if (!string.IsNullOrEmpty(args.tag))
            {
                // Similar logic as in ModifyGameObject for setting/creating tags
                string tagToSet = string.IsNullOrEmpty(args.tag) ? "Untagged" : args.tag;
                try
                {
                    newGo.tag = tagToSet;
                }
                catch (UnityException ex)
                {
                    if (ex.Message.Contains("is not defined"))
                    {
                        Debug.LogWarning(
                            $"[ManageGameObject.Create] Tag '{tagToSet}' not found. Attempting to create it."
                        );
                        try
                        {
                            InternalEditorUtility.AddTag(tagToSet);
                            newGo.tag = tagToSet; // Retry
                            Debug.Log(
                                $"[ManageGameObject.Create] Tag '{tagToSet}' created and assigned successfully."
                            );
                        }
                        catch (Exception innerEx)
                        {
                            UnityEngine.Object.DestroyImmediate(newGo); // Clean up
                            return Response.Error($"Failed to create or assign tag '{tagToSet}' during creation: {innerEx.Message}.");
                        }
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(newGo); // Clean up
                        return Response.Error($"Failed to set tag to '{tagToSet}' during creation: {ex.Message}.");
                    }
                }
            }

            // Set Layer (new for create action)
            if (!string.IsNullOrEmpty(args.layer))
            {
                int layerId = LayerMask.NameToLayer(args.layer);
                if (layerId != -1)
                {
                    newGo.layer = layerId;
                }
                else
                {
                    Debug.LogWarning(
                        $"[ManageGameObject.Create] Layer '{args.layer}' not found. Using default layer."
                    );
                }
            }

            // Add Components
            if (args.components_to_add != null)
            {
                foreach (var compName in args.components_to_add)
                {
                    if (!string.IsNullOrEmpty(compName))
                    {
                        var addResult = AddComponentInternal(newGo, compName, args.componentProperties?[compName]);
                        if (addResult != null) // Check if AddComponentInternal returned an error object
                        {
                            UnityEngine.Object.DestroyImmediate(newGo); // Clean up
                            return addResult; // Return the error response
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ManageGameObject] Invalid component format in componentsToAdd: {compName}"
                        );
                    }
                }
            }

            // Save as Prefab ONLY if we * created * a new object AND saveAsPrefab is true
            GameObject finalInstance = newGo; // Use this for selection and return data
            if (createdNewObject && args.saveAsPrefab.GetValueOrDefault())
            {
                string finalPrefabPath = args.prefabPath; // Use a separate variable for saving path
                // This check should now happen *before* attempting to save
                if (string.IsNullOrEmpty(finalPrefabPath))
                {
                    // Clean up the created object before returning error
                    UnityEngine.Object.DestroyImmediate(newGo);
                    return Response.Error("'prefabPath' is required when 'saveAsPrefab' is true and creating a new object.");
                }
                // Ensure the *saving* path ends with .prefab
                if (!finalPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log(
                        $"[ManageGameObject.Create] Appending .prefab extension to save path: '{finalPrefabPath}' -> '{finalPrefabPath}.prefab'"
                    );
                    finalPrefabPath += ".prefab";
                }

                // Removed the error check here as we now ensure the extension exists
                // if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                // {
                //     UnityEngine.Object.DestroyImmediate(newGo);
                //     return Response.Error($"'prefabPath' must end with '.prefab'. Provided: '{prefabPath}'");
                // }

                try
                {
                    // Ensure directory exists using the final saving path
                    string directoryPath = System.IO.Path.GetDirectoryName(finalPrefabPath);
                    if (
                        !string.IsNullOrEmpty(directoryPath)
                        && !System.IO.Directory.Exists(directoryPath)
                    )
                    {
                        System.IO.Directory.CreateDirectory(directoryPath);
                        AssetDatabase.Refresh(); // Refresh asset database to recognize the new folder

                        await UniTask.WaitWhile(() =>
                        {
                            return EditorApplication.isUpdating;
                        });

                        Debug.Log(
                            $"[ManageGameObject.Create] Created directory for prefab: {directoryPath}"
                        );
                    }

                    // Use SaveAsPrefabAssetAndConnect with the final saving path
                    finalInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
                        newGo,
                        finalPrefabPath,
                        InteractionMode.UserAction
                    );

                    if (finalInstance == null)
                    {
                        // Destroy the original if saving failed somehow (shouldn't usually happen if path is valid)
                        UnityEngine.Object.DestroyImmediate(newGo);
                        return Response.Error($"Failed to save GameObject '{args.name}' as prefab at '{finalPrefabPath}'. Check path and permissions.");
                    }
                    Debug.Log(
                        $"[ManageGameObject.Create] GameObject '{args.name}' saved as prefab to '{finalPrefabPath}' and instance connected."
                    );
                    // Mark the new prefab asset as dirty? Not usually necessary, SaveAsPrefabAsset handles it.
                    // EditorUtility.SetDirty(finalInstance); // Instance is handled by SaveAsPrefabAssetAndConnect
                }
                catch (Exception e)
                {
                    // Clean up the instance if prefab saving fails
                    UnityEngine.Object.DestroyImmediate(newGo); // Destroy the original attempt
                    return Response.Error($"Error saving prefab '{finalPrefabPath}': {e.Message}");
                }
            }

            // Select the instance in the scene (either prefab instance or newly created/saved one)
            Selection.activeGameObject = finalInstance;

            // Determine appropriate success message using the potentially updated or original path
            string messagePrefabPath =
                finalInstance == null
                    ? originalPrefabPath
                    : AssetDatabase.GetAssetPath(
                        PrefabUtility.GetCorrespondingObjectFromSource(finalInstance)
                            ?? (UnityEngine.Object)finalInstance
                    );
            string successMessage;
            if (!createdNewObject && !string.IsNullOrEmpty(messagePrefabPath)) // Instantiated existing prefab
            {
                successMessage =
                    $"Prefab '{messagePrefabPath}' instantiated successfully as '{finalInstance.name}'.";
            }
            else if (createdNewObject && args.saveAsPrefab.GetValueOrDefault() && !string.IsNullOrEmpty(messagePrefabPath)) // Created new and saved as prefab
            {
                successMessage =
                    $"GameObject '{finalInstance.name}' created and saved as prefab to '{messagePrefabPath}'.";
            }
            else // Created new primitive or empty GO, didn't save as prefab
            {
                successMessage =
                    $"GameObject '{finalInstance.name}' created successfully in scene.";
            }

            // Return data for the instance in the scene
            return Response.Success(successMessage, GetGameObjectData(finalInstance));
        }

        private static Response ModifyGameObject(Args args)
        {
            GameObject targetGo = FindObjectInternal(args.target, args.searchMethod, args);
            if (targetGo == null)
            {
                Debug.Log($"<color=white>Object not found</color>");
                return Response.Error($"Target GameObject ('{args.target}') not found using method '{args.searchMethod.GetValueOrDefault()}'.");
            }

            // Record state for Undo *before* modifications
            UnityEditor.Undo.RecordObject(targetGo.transform, "Modify GameObject Transform");
            UnityEditor.Undo.RecordObject(targetGo, "Modify GameObject Properties");

            bool modified = false;

            // Rename (using consolidated 'name' parameter)
            if (!string.IsNullOrEmpty(args.name) && targetGo.name != args.name)
            {
                targetGo.name = args.name;
                modified = true;
            }

            // Change Parent (using consolidated 'parent' parameter)
            if (args.parent != null)
            {
                GameObject newParentGo = FindObjectInternal(args.parent, SearchMethod.undefined, args);
                bool isParentInt = int.TryParse(args.parent as string, out _);
                if (newParentGo == null && args.parent != null && !isParentInt && !string.IsNullOrEmpty(args.parent as string))
                {
                    // This if condition can be buggy please test it properly 
                    Debug.Log($"Can be bug here: New parent ('{args.parent}') not found.");
                    return Response.Error($"New parent ('{args.parent}') not found.");
                }
                // Check for hierarchy loops
                if (newParentGo != null && newParentGo.transform.IsChildOf(targetGo.transform))
                {
                    return Response.Error($"Cannot parent '{targetGo.name}' to '{newParentGo.name}', as it would create a hierarchy loop.");
                }
                if (targetGo.transform.parent != (newParentGo?.transform))
                {
                    targetGo.transform.SetParent(newParentGo?.transform, true); // worldPositionStays = true
                    modified = true;
                }
            }

            // Set Active State
            if (args.setActive.HasValue && targetGo.activeSelf != args.setActive.Value)
            {
                targetGo.SetActive(args.setActive.Value);
                modified = true;
            }

            // Only attempt to change tag if a non-null tag is provided and it's different from the current one.
            // Allow setting an empty string to remove the tag (Unity uses "Untagged").
            if (args.tag != null && targetGo.tag != args.tag)
            {
                // Ensure the tag is not empty, if empty, it means "Untagged" implicitly
                string tagToSet = string.IsNullOrEmpty(args.tag) ? "Untagged" : args.tag;

                try
                {
                    // First attempt to set the tag
                    targetGo.tag = tagToSet;
                    modified = true;
                }
                catch (UnityException ex)
                {
                    // Check if the error is specifically because the tag doesn't exist
                    if (ex.Message.Contains("is not defined"))
                    {
                        Debug.LogWarning(
                            $"[ManageGameObject] Tag '{tagToSet}' not found. Attempting to create it."
                        );
                        try
                        {
                            // Attempt to create the tag using internal utility
                            InternalEditorUtility.AddTag(tagToSet);
                            // Wait a frame maybe? Not strictly necessary but sometimes helps editor updates.
                            // yield return null; // Cannot yield here, editor script limitation

                            // Retry setting the tag immediately after creation
                            targetGo.tag = tagToSet;
                            modified = true; // Mark as modified on successful retry
                            Debug.Log(
                                $"[ManageGameObject] Tag '{tagToSet}' created and assigned successfully."
                            );
                        }
                        catch (Exception innerEx)
                        {
                            // Handle failure during tag creation or the second assignment attempt
                            Debug.LogError(
                                $"[ManageGameObject] Failed to create or assign tag '{tagToSet}' after attempting creation: {innerEx.Message}"
                            );
                            return Response.Error($"Failed to create or assign tag '{tagToSet}': {innerEx.Message}. Check Tag Manager and permissions.");
                        }
                    }
                    else
                    {
                        // If the exception was for a different reason, return the original error
                        return Response.Error($"Failed to set tag to '{tagToSet}': {ex.Message}.");
                    }
                }
            }

            // Change Layer (using consolidated 'layer' parameter)
            if (!string.IsNullOrEmpty(args.layer))
            {
                int layerId = LayerMask.NameToLayer(args.layer);
                if (layerId == -1 && args.layer != "Default")
                {
                    return Response.Error($"Invalid layer specified: '{args.layer}'. Use a valid layer name.");
                }
                if (layerId != -1 && targetGo.layer != layerId)
                {
                    targetGo.layer = layerId;
                    modified = true;
                }
            }

            if (args.position.HasValue && targetGo.transform.localPosition != args.position.Value)
            {
                targetGo.transform.localPosition = args.position.Value;
                modified = true;
            }
            if (args.rotation.HasValue && targetGo.transform.localEulerAngles != args.rotation.Value)
            {
                targetGo.transform.localEulerAngles = args.rotation.Value;
                modified = true;
            }
            if (args.scale.HasValue && targetGo.transform.localScale != args.scale.Value)
            {
                targetGo.transform.localScale = args.scale.Value;
                modified = true;
            }

            // --- Component Modifications ---
            // Note: These might need more specific Undo recording per component

            // Remove Components
            if (args.componentsToRemove != null)
            {
                foreach (var compToken in args.componentsToRemove)
                {
                    string typeName = compToken.ToString();
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        modified = true;
                        return RemoveComponentInternal(targetGo, typeName);
                    }
                }
            }

            // Add Components (similar to create)
            if (args.components_to_add != null)
            {
                foreach (var compName in args.components_to_add)
                {
                    if (!string.IsNullOrEmpty(compName))
                    {
                        var addResult = AddComponentInternal(targetGo, compName, args.componentProperties[compName]);
                        if (addResult != null) // Check if AddComponentInternal returned an error object
                        {
                            UnityEngine.Object.DestroyImmediate(targetGo); // Clean up
                            return addResult; // Return the error response
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ManageGameObject] Invalid component format in componentsToAdd: {compName}"
                        );
                    }
                }
            }

            // Set Component Properties
            if (args.componentProperties != null)
            {
                foreach (var prop in args.componentProperties)
                {
                    if (args.componentProperties != null)
                    {
                        var setResult = SetComponentPropertiesInternal(
                            targetGo,
                            prop.Key,
                            prop.Value
                        );
                        if (setResult != null)
                            return setResult;
                        modified = true;
                    }
                }
            }

            if (!modified)
            {
                return Response.Error($"No modifications applied to GameObject '{targetGo.name}'.", GetGameObjectData(targetGo));
            }

            EditorUtility.SetDirty(targetGo); // Mark scene as dirty

            return Response.Success($"GameObject '{targetGo.name}' modified successfully.", GetGameObjectData(targetGo));
        }

        private static Response DeleteGameObject(Args args)
        {
            // Find potentially multiple objects if name/tag search is used without find_all=false implicitly
            // find_all=true for delete safety
            List<GameObject> targets = FindObjectsInternal(args.target, args.searchMethod, true, args);

            if (targets.Count == 0)
            {
                return Response.Error($"Target GameObject(s) ('{args.target}') not found using method '{args.searchMethod}'.");
            }

            List<object> deletedObjects = new List<object>();
            foreach (var targetGo in targets)
            {
                if (targetGo != null)
                {
                    string goName = targetGo.name;
                    int goId = targetGo.GetInstanceID();
                    // Use Undo.DestroyObjectImmediate for undo support
                    UnityEditor.Undo.DestroyObjectImmediate(targetGo);
                    deletedObjects.Add(new { name = goName, instanceID = goId });
                }
            }

            if (deletedObjects.Count > 0)
            {
                string message =
                    targets.Count == 1
                        ? $"GameObject '{deletedObjects[0].GetType().GetProperty("name").GetValue(deletedObjects[0])}' deleted successfully."
                        : $"{deletedObjects.Count} GameObjects deleted successfully.";
                return Response.Success(message, deletedObjects);
            }
            else
            {
                // Should not happen if targets.Count > 0 initially, but defensive check
                return Response.Error("Failed to delete target GameObject(s).");
            }
        }

        private static Response FindGameObjects(Args args)
        {
            bool findAll = args.findAll.GetValueOrDefault();

            List<GameObject> foundObjects = FindObjectsInternal(
                args.target,
                args.searchMethod,
                findAll,
                args
            );

            if (foundObjects.Count == 0)
            {
                return Response.Error("No matching GameObjects found.");
            }

            var results = foundObjects.Select(go => GetGameObjectData(go)).ToList();

            return Response.Success($"Found {results.Count} GameObject(s)." + " Data:" + results);
        }

        private static Response GetComponentsFromTarget(Args args)
        {
            if (args.target == null || string.IsNullOrEmpty(args.target as string))
            {
                return Response.Error("Target game object not provided.");
            }

            GameObject targetGo = FindObjectInternal(args.target, args.searchMethod, args);
            if (targetGo == null)
            {
                return Response.Error($"Target GameObject ('{args.target}') not found using method '{args.searchMethod}'.");
            }

            try
            {
                Component[] components = targetGo.GetComponents<Component>();
                var componentData = components.Select(c => GetComponentData(c)).ToList();

                return Response.Success($"Retrieved {componentData.Count} components from '{targetGo.name}'.", componentData);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting components from '{targetGo.name}': {e.Message}");
            }
        }

        private static Response AddComponentToTarget(Args args)
        {
            GameObject targetGo = FindObjectInternal(args.target, args.searchMethod, args);

            if (targetGo == null)
            {
                return Response.Error($"Target GameObject ('{args.target}') not found using method '{args.searchMethod}'.");
            }

            string typeName = null;
            Dictionary<string, object> properties = null;

            // Allow adding component specified directly or via componentsToAdd array (take first)
            if (args.componentName != null)
            {
                typeName = args.componentName;
                properties = args.componentProperties?[typeName]; // Check if props are nested under name
            }
            else if (args.components_to_add != null && args.components_to_add.Length > 0)
            {
                typeName = args.components_to_add.FirstOrDefault();
                Debug.Log($"Tpe: {typeName}");
                Debug.Log($"Tpe: {args.componentProperties.Count}");
                Debug.Log($"Tpe: {Json.convert.SerializeObject(args.componentProperties)}");
                properties = args.componentProperties?[typeName.ToLower()];
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return Response.Error(
                    "Component type name ('componentName' or first element in 'componentsToAdd') is required."
                );
            }

            var addResult = AddComponentInternal(targetGo, typeName, properties);
            if (addResult != null)
                return addResult; // Return error

            EditorUtility.SetDirty(targetGo);

            return Response.Success(
                $"Component '{typeName}' added to '{targetGo.name}'.",
                GetGameObjectData(targetGo)
            ); // Return updated GO data
        }

        private static Response RemoveComponentFromTarget(Args args)
        {
            GameObject targetGo = FindObjectInternal(args.target, args.searchMethod, args);

            if (targetGo == null)
            {
                return Response.Error(
                    $"Target GameObject ('{args.target}') not found using method '{args.searchMethod}'."
                );
            }

            string typeName = null;
            // Allow removing component specified directly or via componentsToRemove array (take first)
            if (args.componentName != null)
            {
                typeName = args.componentName;
            }
            else if (args.componentsToRemove != null && args.componentsToRemove.Length > 0)
            {
                typeName = args.componentsToRemove.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return Response.Error(
                    "Component type name ('componentName' or first element in 'componentsToRemove') is required."
                );
            }

            var removeResult = RemoveComponentInternal(targetGo, typeName);
            if (removeResult != null)
                return removeResult; // Return error

            EditorUtility.SetDirty(targetGo);
            return Response.Success(
                $"Component '{typeName}' removed from '{targetGo.name}'.",
                GetGameObjectData(targetGo)
            );
        }

        private static Response SetComponentPropertyOnTarget(Args args)
        {
            GameObject targetGo = FindObjectInternal(args.target, args.searchMethod, args);

            if (targetGo == null)
            {
                return Response.Error(
                    $"Target GameObject ('{args.target}') not found using method '{args.searchMethod}'."
                );
            }

            string compName = args.componentName;
            Dictionary<string, object> propertiesToSet = null;

            if (!string.IsNullOrEmpty(compName))
            {
                propertiesToSet = args.componentProperties[compName];
            }
            else
            {
                return Response.Error("'componentName' parameter is required.");
            }

            if (propertiesToSet == null)
            {
                return Response.Error(
                    "'componentProperties' dictionary for the specified component is required and cannot be empty."
                );
            }

            var setResult = SetComponentPropertiesInternal(targetGo, compName, propertiesToSet);
            if (setResult != null)
                return setResult; // Return error

            EditorUtility.SetDirty(targetGo);

            return Response.Success(
                $"Properties set for component '{compName}' on '{targetGo.name}'.",
                GetGameObjectData(targetGo)
            );
        }



        /// <summary>
        /// Finds a single GameObject based on token (ID, name, path) and search method.
        /// </summary>
        private static GameObject FindObjectInternal(
            object target,
            SearchMethod? searchMethod,
            Args args
        )
        {
            // If find_all is not explicitly false, we still want only one for most single-target operations.
            bool findAll = args.findAll ?? false;

            // If a specific target ID is given, always find just that one.
            bool isTargetInt = int.TryParse(target as string, out _);

            if (isTargetInt && (searchMethod == null || searchMethod == SearchMethod.by_id || searchMethod == SearchMethod.undefined))
            {
                findAll = false;
            }
            List<GameObject> results = FindObjectsInternal(target, searchMethod, findAll, args);
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// Core logic for finding GameObjects based on various criteria.
        /// </summary>
        private static List<GameObject> FindObjectsInternal(
            object target,
            SearchMethod? searchMethod,
            bool findAll,
            Args args
        )
        {
            List<GameObject> results = new List<GameObject>();

            if (target == null)
            {
                target = args.name;
            }

            if (string.IsNullOrEmpty(args.searchTerm))
            {
                args.searchTerm = target as string;
            }

            // Default search method if not specified
            if (searchMethod == null)
            {
                if (int.TryParse(target as string, out _))
                    searchMethod = SearchMethod.by_id;
                else if (!string.IsNullOrEmpty(args.searchTerm) && args.searchTerm.Contains('/'))
                    searchMethod = SearchMethod.by_path;
                else
                    searchMethod = SearchMethod.by_name; // Default fallback
            }

            GameObject rootSearchObject = null;
            // If searching in children, find the initial target first
            if (args.searchInChildren.GetValueOrDefault() && target != null)
            {
                rootSearchObject = FindObjectInternal(target, SearchMethod.undefined, args); // Find the root for child search
                if (rootSearchObject == null)
                {
                    Debug.LogWarning(
                        $"[ManageGameObject.Find] Root object '{target}' for child search not found."
                    );
                    return results; // Return empty if root not found
                }
            }

            switch (searchMethod)
            {
                case SearchMethod.by_id:
                    if (int.TryParse(args.searchTerm, out int instanceId))
                    {
                        // EditorUtility.InstanceIDToObject is slow, iterate manually if possible
                        // GameObject obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                        var allObjects = GetAllSceneObjects(args.searchInactive.GetValueOrDefault()); // More efficient
                        GameObject obj = allObjects.FirstOrDefault(go =>
                            go.GetInstanceID() == instanceId
                        );
                        if (obj != null)
                            results.Add(obj);
                    }
                    break;
                case SearchMethod.by_name:
                    IEnumerable<GameObject> searchPoolName = null;
                    if (rootSearchObject != null)
                    {
                        searchPoolName = rootSearchObject.GetComponentsInChildren<Transform>(args.searchInactive.GetValueOrDefault()).Select(t => t.gameObject);
                    }
                    else
                    {
                        searchPoolName = GetAllSceneObjects(args.searchInactive.GetValueOrDefault());
                    }
                    results.AddRange(searchPoolName.Where(go => go.name == args.searchTerm));
                    break;
                case SearchMethod.by_path:
                    // Path is relative to scene root or rootSearchObject
                    Transform foundTransform = rootSearchObject
                        ? rootSearchObject.transform.Find(args.searchTerm)
                        : GameObject.Find(args.searchTerm)?.transform;
                    if (foundTransform != null)
                        results.Add(foundTransform.gameObject);
                    break;
                case SearchMethod.by_tag:
                    var searchPoolTag = rootSearchObject
                        ? rootSearchObject
                            .GetComponentsInChildren<Transform>(args.searchInactive.GetValueOrDefault())
                            .Select(t => t.gameObject)
                        : GetAllSceneObjects(args.searchInactive.GetValueOrDefault());
                    results.AddRange(searchPoolTag.Where(go => go.CompareTag(args.searchTerm)));
                    break;
                case SearchMethod.by_layer:
                    var searchPoolLayer = rootSearchObject
                        ? rootSearchObject
                            .GetComponentsInChildren<Transform>(args.searchInactive.GetValueOrDefault())
                            .Select(t => t.gameObject)
                        : GetAllSceneObjects(args.searchInactive.GetValueOrDefault());
                    if (int.TryParse(args.searchTerm, out int layerIndex))
                    {
                        results.AddRange(searchPoolLayer.Where(go => go.layer == layerIndex));
                    }
                    else
                    {
                        int namedLayer = LayerMask.NameToLayer(args.searchTerm);
                        if (namedLayer != -1)
                            results.AddRange(searchPoolLayer.Where(go => go.layer == namedLayer));
                    }
                    break;
                case SearchMethod.by_component:
                    Type componentType = FindType(args.searchTerm);
                    if (componentType != null)
                    {
                        // Determine FindObjectsInactive based on the searchInactive flag
                        FindObjectsInactive findInactive = args.searchInactive.GetValueOrDefault()
                            ? FindObjectsInactive.Include
                            : FindObjectsInactive.Exclude;
                        // Replace FindObjectsOfType with FindObjectsByType, specifying the sorting mode and inactive state
                        var searchPoolComp = rootSearchObject
                            ? rootSearchObject
                                .GetComponentsInChildren(componentType, args.searchInactive.GetValueOrDefault())
                                .Select(c => (c as Component).gameObject)
                            : UnityEngine
                                .Object.FindObjectsByType(
                                    componentType,
                                    findInactive,
                                    FindObjectsSortMode.None
                                )
                                .Select(c => (c as Component).gameObject);
                        results.AddRange(searchPoolComp.Where(go => go != null)); // Ensure GO is valid
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ManageGameObject.Find] Component type not found: {args.searchTerm}"
                        );
                    }
                    break;
                case SearchMethod.undefined: // Helper method used internally
                    if (int.TryParse(args.searchTerm, out int id))
                    {
                        var allObjectsId = GetAllSceneObjects(true); // Search inactive for internal lookup
                        GameObject objById = allObjectsId.FirstOrDefault(go =>
                            go.GetInstanceID() == id
                        );
                        if (objById != null)
                        {
                            results.Add(objById);
                            break;
                        }
                    }
                    GameObject objByPath = GameObject.Find(args.searchTerm);
                    if (objByPath != null)
                    {
                        results.Add(objByPath);
                        break;
                    }

                    var allObjectsName = GetAllSceneObjects(true);
                    results.AddRange(allObjectsName.Where(go => go.name == args.searchTerm));
                    break;
                default:
                    Debug.LogWarning(
                        $"[ManageGameObject.Find] Unknown search method: {searchMethod}"
                    );
                    break;
            }

            // If only one result is needed, return just the first one found.
            if (!findAll && results.Count > 1)
            {
                return new List<GameObject> { results[0] };
            }

            return results.Distinct().ToList(); // Ensure uniqueness
        }

        private static IEnumerable<GameObject> GetAllSceneObjects(bool includeInactive)
        {
            // SceneManager.GetActiveScene().GetRootGameObjects() is faster than FindObjectsOfType<GameObject>()
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var allObjects = new List<GameObject>();
            foreach (var root in rootObjects)
            {
                allObjects.AddRange(
                    root.GetComponentsInChildren<Transform>(includeInactive)
                        .Select(t => t.gameObject)
                );
            }
            return allObjects;
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Handle common Unity namespaces implicitly
            var type =
                Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule")
                ?? Type.GetType($"UnityEngine.{typeName}, UnityEngine.PhysicsModule")
                ?? // Example physics
                Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI")
                ?? // Example UI
                Type.GetType($"UnityEditor.{typeName}, UnityEditor.CoreModule")
                ?? Type.GetType(typeName); // Try direct name (if fully qualified or in mscorlib)

            if (type != null)
                return type;

            // If not found, search all loaded assemblies (slower)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
                // Also check with namespaces if simple name given
                type = assembly.GetType("UnityEngine." + typeName);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEditor." + typeName);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEngine.UI." + typeName);
                if (type != null)
                    return type;
            }

            return null; // Not found
        }




        /// <summary>
        /// Adds a component by type name and optionally sets properties.
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        private static Response AddComponentInternal(
            GameObject targetGo,
            string typeName,
            Dictionary<string, object> properties
        )
        {
            Type componentType = FindType(typeName);
            if (componentType == null)
            {
                return Response.Error($"Component type '{typeName}' not found or is not a valid Component.");
            }
            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                return Response.Error($"Type '{typeName}' is not a Component.");
            }

            // Prevent adding Transform again
            if (componentType == typeof(Transform))
            {
                return Response.Error("Cannot add another Transform component.");
            }

            // Check for 2D/3D physics component conflicts
            bool isAdding2DPhysics =
                typeof(Rigidbody2D).IsAssignableFrom(componentType)
                || typeof(Collider2D).IsAssignableFrom(componentType);
            bool isAdding3DPhysics =
                typeof(Rigidbody).IsAssignableFrom(componentType)
                || typeof(Collider).IsAssignableFrom(componentType);

            if (isAdding2DPhysics)
            {
                // Check if the GameObject already has any 3D Rigidbody or Collider
                if (
                    targetGo.GetComponent<Rigidbody>() != null
                    || targetGo.GetComponent<Collider>() != null
                )
                {
                    return Response.Error($"Cannot add 2D physics component '{typeName}' because the GameObject '{targetGo.name}' already has a 3D Rigidbody or Collider.");
                }
            }
            else if (isAdding3DPhysics)
            {
                // Check if the GameObject already has any 2D Rigidbody or Collider
                if (
                    targetGo.GetComponent<Rigidbody2D>() != null
                    || targetGo.GetComponent<Collider2D>() != null
                )
                {
                    return Response.Error($"Cannot add 3D physics component '{typeName}' because the GameObject '{targetGo.name}' already has a 2D Rigidbody or Collider.");
                }
            }

            // Check if component already exists (optional, depending on desired behavior)
            // if (targetGo.GetComponent(componentType) != null) {
            //     return Response.Error($"Component '{typeName}' already exists on '{targetGo.name}'.");
            // }

            try
            {
                // Use Undo.AddComponent for undo support
                Component newComponent = UnityEditor.Undo.AddComponent(targetGo, componentType);
                if (newComponent == null)
                {
                    return Response.Error($"Failed to add component '{typeName}' to '{targetGo.name}'. It might be disallowed (e.g., adding script twice).");
                }

                // Set default values for specific component types
                if (newComponent is Light light)
                {
                    // Default newly added lights to directional
                    light.type = LightType.Directional;
                }

                // Set properties if provided
                if (properties != null)
                {
                    var setResult = SetComponentPropertiesInternal(targetGo, typeName, properties, newComponent); // Pass the new component instance
                    if (setResult != null)
                    {
                        // If setting properties failed, maybe remove the added component?
                        UnityEditor.Undo.DestroyObjectImmediate(newComponent);
                        return setResult; // Return the error from setting properties
                    }
                }

                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error($"Error adding component '{typeName}' to '{targetGo.name}': {e.Message}");
            }
        }

        /// <summary>
        /// Sets properties on a component.
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        private static Response SetComponentPropertiesInternal(
            GameObject targetGo,
            string compName,
            Dictionary<string, object> propertiesToSet,
            Component targetComponentInstance = null
        )
        {
            Debug.Log($"Target: {targetGo.name}");

            Component targetComponent = targetComponentInstance ?? targetGo.GetComponent(compName);

            if (targetComponent == null)
            {
                return Response.Error($"Component '{compName}' not found on '{targetGo.name}' to set properties.");
            }

            UnityEditor.Undo.RecordObject(targetComponent, "Set Component Properties");

            foreach (var prop in propertiesToSet)
            {
                try
                {
                    JToken tkn = JToken.FromObject(prop.Value);

                    if (!SetProperty(targetComponent, prop.Key, tkn))
                    {
                        // Log warning if property could not be set
                        Debug.LogWarning(
                            $"[ManageGameObject] Could not set property '{prop.Key}' on component '{compName}' ('{targetComponent.GetType().Name}'). Property might not exist, be read-only, or type mismatch."
                        );
                        // Optionally return an error here instead of just logging
                        // return Response.Error($"Could not set property '{propName}' on component '{compName}'.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[ManageGameObject] Error setting property '{prop.Key}' on '{compName}': {e.Message}"
                    );
                    // Optionally return an error here
                    // return Response.Error($"Error setting property '{propName}' on '{compName}': {e.Message}");
                }
            }
            EditorUtility.SetDirty(targetComponent);

            return null; // Success (or partial success if warnings were logged)
        }


        /// <summary>
        /// Helper to set a property or field via reflection, handling basic types.
        /// </summary>
        private static bool SetProperty(object target, string memberName, JToken value)
        {
            Type type = target.GetType();
            BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            try
            {
                // Handle special case for materials with dot notation (material.property)
                // Examples: material.color, sharedMaterial.color, materials[0].color
                if (memberName.Contains('.') || memberName.Contains('['))
                {
                    return SetNestedProperty(target, memberName, value);
                }

                PropertyInfo propInfo = type.GetProperty(memberName, flags);
                if (propInfo != null && propInfo.CanWrite)
                {
                    object convertedValue = ConvertJTokenToType(value, propInfo.PropertyType);
                    if (convertedValue != null)
                    {
                        propInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                else
                {
                    FieldInfo fieldInfo = type.GetField(memberName, flags);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ConvertJTokenToType(value, fieldInfo.FieldType);
                        if (convertedValue != null)
                        {
                            fieldInfo.SetValue(target, convertedValue);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SetProperty] Failed to set '{memberName}' on {type.Name}: {ex.Message}"
                );
            }
            return false;
        }

        /// <summary>
        /// Sets a nested property using dot notation (e.g., "material.color") or array access (e.g., "materials[0]")
        /// </summary>
        private static bool SetNestedProperty(object target, string path, JToken value)
        {
            try
            {
                // Split the path into parts (handling both dot notation and array indexing)
                string[] pathParts = SplitPropertyPath(path);
                if (pathParts.Length == 0)
                    return false;

                object currentObject = target;
                Type currentType = currentObject.GetType();
                BindingFlags flags =
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

                // Traverse the path until we reach the final property
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    string part = pathParts[i];
                    bool isArray = false;
                    int arrayIndex = -1;

                    // Check if this part contains array indexing
                    if (part.Contains("["))
                    {
                        int startBracket = part.IndexOf('[');
                        int endBracket = part.IndexOf(']');
                        if (startBracket > 0 && endBracket > startBracket)
                        {
                            string indexStr = part.Substring(
                                startBracket + 1,
                                endBracket - startBracket - 1
                            );
                            if (int.TryParse(indexStr, out arrayIndex))
                            {
                                isArray = true;
                                part = part.Substring(0, startBracket);
                            }
                        }
                    }

                    // Get the property/field
                    PropertyInfo propInfo = currentType.GetProperty(part, flags);
                    FieldInfo fieldInfo = null;
                    if (propInfo == null)
                    {
                        fieldInfo = currentType.GetField(part, flags);
                        if (fieldInfo == null)
                        {
                            Debug.LogWarning(
                                $"[SetNestedProperty] Could not find property or field '{part}' on type '{currentType.Name}'"
                            );
                            return false;
                        }
                    }

                    // Get the value
                    currentObject =
                        propInfo != null
                            ? propInfo.GetValue(currentObject)
                            : fieldInfo.GetValue(currentObject);

                    // If the current property is null, we need to stop
                    if (currentObject == null)
                    {
                        Debug.LogWarning(
                            $"[SetNestedProperty] Property '{part}' is null, cannot access nested properties."
                        );
                        return false;
                    }

                    // If this is an array/list access, get the element at the index
                    if (isArray)
                    {
                        if (currentObject is Material[])
                        {
                            var materials = currentObject as Material[];
                            if (arrayIndex < 0 || arrayIndex >= materials.Length)
                            {
                                Debug.LogWarning(
                                    $"[SetNestedProperty] Material index {arrayIndex} out of range (0-{materials.Length - 1})"
                                );
                                return false;
                            }
                            currentObject = materials[arrayIndex];
                        }
                        else if (currentObject is System.Collections.IList)
                        {
                            var list = currentObject as System.Collections.IList;
                            if (arrayIndex < 0 || arrayIndex >= list.Count)
                            {
                                Debug.LogWarning(
                                    $"[SetNestedProperty] Index {arrayIndex} out of range (0-{list.Count - 1})"
                                );
                                return false;
                            }
                            currentObject = list[arrayIndex];
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[SetNestedProperty] Property '{part}' is not an array or list, cannot access by index."
                            );
                            return false;
                        }
                    }

                    // Update type for next iteration
                    currentType = currentObject.GetType();
                }

                // Set the final property
                string finalPart = pathParts[pathParts.Length - 1];

                // Special handling for Material properties (shader properties)
                if (currentObject is Material material && finalPart.StartsWith("_"))
                {
                    // Handle various material property types
                    if (value is JArray jArray)
                    {
                        if (jArray.Count == 4) // Color with alpha
                        {
                            Color color = new Color(
                                jArray[0].ToObject<float>(),
                                jArray[1].ToObject<float>(),
                                jArray[2].ToObject<float>(),
                                jArray[3].ToObject<float>()
                            );
                            material.SetColor(finalPart, color);
                            return true;
                        }
                        else if (jArray.Count == 3) // Color without alpha
                        {
                            Color color = new Color(
                                jArray[0].ToObject<float>(),
                                jArray[1].ToObject<float>(),
                                jArray[2].ToObject<float>(),
                                1.0f
                            );
                            material.SetColor(finalPart, color);
                            return true;
                        }
                        else if (jArray.Count == 2) // Vector2
                        {
                            Vector2 vec = new Vector2(
                                jArray[0].ToObject<float>(),
                                jArray[1].ToObject<float>()
                            );
                            material.SetVector(finalPart, vec);
                            return true;
                        }
                        else if (jArray.Count == 4) // Vector4
                        {
                            Vector4 vec = new Vector4(
                                jArray[0].ToObject<float>(),
                                jArray[1].ToObject<float>(),
                                jArray[2].ToObject<float>(),
                                jArray[3].ToObject<float>()
                            );
                            material.SetVector(finalPart, vec);
                            return true;
                        }
                    }
                    else if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
                    {
                        material.SetFloat(finalPart, value.ToObject<float>());
                        return true;
                    }
                    else if (value.Type == JTokenType.Boolean)
                    {
                        material.SetFloat(finalPart, value.ToObject<bool>() ? 1f : 0f);
                        return true;
                    }
                    else if (value.Type == JTokenType.String)
                    {
                        // Might be a texture path
                        string texturePath = value.ToString();
                        if (
                            texturePath.EndsWith(".png")
                            || texturePath.EndsWith(".jpg")
                            || texturePath.EndsWith(".tga")
                        )
                        {
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                texturePath
                            );
                            if (texture != null)
                            {
                                material.SetTexture(finalPart, texture);
                                return true;
                            }
                        }
                        else
                        {
                            // Materials don't have SetString, use SetTextureOffset as workaround or skip
                            // material.SetString(finalPart, texturePath);
                            Debug.LogWarning(
                                $"[SetNestedProperty] String values not directly supported for material property {finalPart}"
                            );
                            return false;
                        }
                    }

                    Debug.LogWarning(
                        $"[SetNestedProperty] Unsupported material property value type: {value.Type} for {finalPart}"
                    );
                    return false;
                }

                // For standard properties (not shader specific)
                PropertyInfo finalPropInfo = currentType.GetProperty(finalPart, flags);
                if (finalPropInfo != null && finalPropInfo.CanWrite)
                {
                    object convertedValue = ConvertJTokenToType(value, finalPropInfo.PropertyType);
                    if (convertedValue != null)
                    {
                        finalPropInfo.SetValue(currentObject, convertedValue);
                        return true;
                    }
                }
                else
                {
                    FieldInfo finalFieldInfo = currentType.GetField(finalPart, flags);
                    if (finalFieldInfo != null)
                    {
                        object convertedValue = ConvertJTokenToType(
                            value,
                            finalFieldInfo.FieldType
                        );
                        if (convertedValue != null)
                        {
                            finalFieldInfo.SetValue(currentObject, convertedValue);
                            return true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[SetNestedProperty] Could not find final property or field '{finalPart}' on type '{currentType.Name}'"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SetNestedProperty] Error setting nested property '{path}': {ex.Message}"
                );
            }

            return false;
        }


        /// <summary>
        /// Simple JToken to Type conversion for common Unity types.
        /// </summary>
        private static object ConvertJTokenToType(JToken token, Type targetType)
        {
            try
            {
                // Unwrap nested material properties if we're assigning to a Material
                if (typeof(Material).IsAssignableFrom(targetType) && token is JObject materialProps)
                {
                    // Handle case where we're passing shader properties directly in a nested object
                    string materialPath = token["path"]?.ToString();
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        // Load the material by path
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material != null)
                        {
                            // If there are additional properties, set them
                            foreach (var prop in materialProps.Properties())
                            {
                                if (prop.Name != "path")
                                {
                                    SetProperty(material, prop.Name, prop.Value);
                                }
                            }
                            return material;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Could not load material at path: '{materialPath}'"
                            );
                            return null;
                        }
                    }

                    // If no path is specified, could be a dynamic material or instance set by reference
                    return null;
                }

                // Basic types first
                if (targetType == typeof(string))
                    return token.ToObject<string>();
                if (targetType == typeof(int))
                    return token.ToObject<int>();
                if (targetType == typeof(float))
                    return token.ToObject<float>();
                if (targetType == typeof(bool))
                    return token.ToObject<bool>();

                // Vector/Quaternion/Color types
                if (targetType == typeof(Vector2) && token is JArray arrV2 && arrV2.Count == 2)
                    return new Vector2(arrV2[0].ToObject<float>(), arrV2[1].ToObject<float>());
                if (targetType == typeof(Vector3) && token is JArray arrV3 && arrV3.Count == 3)
                    return new Vector3(
                        arrV3[0].ToObject<float>(),
                        arrV3[1].ToObject<float>(),
                        arrV3[2].ToObject<float>()
                    );
                if (targetType == typeof(Vector4) && token is JArray arrV4 && arrV4.Count == 4)
                    return new Vector4(
                        arrV4[0].ToObject<float>(),
                        arrV4[1].ToObject<float>(),
                        arrV4[2].ToObject<float>(),
                        arrV4[3].ToObject<float>()
                    );
                if (targetType == typeof(Quaternion) && token is JArray arrQ && arrQ.Count == 4)
                    return new Quaternion(
                        arrQ[0].ToObject<float>(),
                        arrQ[1].ToObject<float>(),
                        arrQ[2].ToObject<float>(),
                        arrQ[3].ToObject<float>()
                    );
                if (targetType == typeof(Color) && token is JArray arrC && arrC.Count >= 3) // Allow RGB or RGBA
                    return new Color(
                        arrC[0].ToObject<float>(),
                        arrC[1].ToObject<float>(),
                        arrC[2].ToObject<float>(),
                        arrC.Count > 3 ? arrC[3].ToObject<float>() : 1.0f
                    );

                // Enum types
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, token.ToString(), true); // Case-insensitive enum parsing

                // Handle assigning Unity Objects (Assets, Scene Objects, Components)
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    // CASE 1: Reference is a JSON Object specifying a scene object/component find criteria
                    if (token is JObject refObject)
                    {
                        JToken findToken = refObject["find"];
                        SearchMethod findMethod =
                            refObject["method"]?.ToObject<SearchMethod>() ?? SearchMethod.undefined; // Default search
                        string componentTypeName = refObject["component"]?.ToString();

                        if (findToken == null)
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Reference object missing 'find' property: {token}"
                            );
                            return null;
                        }

                        // Find the target GameObject
                        // Pass 'searchInactive: true' for internal lookups to be more robust
                        var args = new Args()
                        {
                            searchInactive = true
                        };
                        GameObject foundGo = FindObjectInternal(findToken, findMethod, args);

                        if (foundGo == null)
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Could not find GameObject specified by reference object: {token}"
                            );
                            return null;
                        }

                        // If a component type is specified, try to get it
                        if (!string.IsNullOrEmpty(componentTypeName))
                        {
                            Type compType = FindType(componentTypeName);
                            if (compType == null)
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Could not find component type '{componentTypeName}' specified in reference object: {token}"
                                );
                                return null;
                            }

                            // Ensure the targetType is assignable from the found component type
                            if (!targetType.IsAssignableFrom(compType))
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found component '{componentTypeName}' but it is not assignable to the target property type '{targetType.Name}'. Reference: {token}"
                                );
                                return null;
                            }

                            Component foundComp = foundGo.GetComponent(compType);
                            if (foundComp == null)
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found GameObject '{foundGo.name}' but could not find component '{componentTypeName}' on it. Reference: {token}"
                                );
                                return null;
                            }
                            return foundComp; // Return the found component
                        }
                        else
                        {
                            // Otherwise, return the GameObject itself, ensuring it's assignable
                            if (!targetType.IsAssignableFrom(typeof(GameObject)))
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found GameObject '{foundGo.name}' but it is not assignable to the target property type '{targetType.Name}' (component name was not specified). Reference: {token}"
                                );
                                return null;
                            }
                            return foundGo; // Return the found GameObject
                        }
                    }
                    // CASE 2: Reference is a string, assume it's an asset path
                    else if (token.Type == JTokenType.String)
                    {
                        string assetPath = token.ToString();
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Attempt to load the asset from the provided path using the target type
                            UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath(
                                assetPath,
                                targetType
                            );
                            if (loadedAsset != null)
                            {
                                return loadedAsset; // Return the loaded asset if successful
                            }
                            else
                            {
                                // Log a warning if the asset could not be found at the path
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from path: '{assetPath}'. Make sure the path is correct and the asset exists."
                                );
                                return null;
                            }
                        }
                        else
                        {
                            // Handle cases where an empty string might be intended to clear the reference
                            return null; // Assign null if the path is empty
                        }
                    }
                    // CASE 3: Reference is null or empty JToken, assign null
                    else if (
                        token.Type == JTokenType.Null
                        || string.IsNullOrEmpty(token.ToString())
                    )
                    {
                        return null;
                    }
                    // CASE 4: Invalid format for Unity Object reference
                    else
                    {
                        Debug.LogWarning(
                            $"[ConvertJTokenToType] Expected a string asset path or a reference object to assign Unity Object of type '{targetType.Name}', but received token type '{token.Type}'. Value: {token}"
                        );
                        return null;
                    }
                }

                // Fallback: Try direct conversion (might work for other simple value types)
                // Be cautious here, this might throw errors for complex types not handled above
                try
                {
                    return token.ToObject(targetType);
                }
                catch (Exception directConversionEx)
                {
                    Debug.LogWarning(
                        $"[ConvertJTokenToType] Direct conversion failed for JToken '{token}' to type '{targetType.Name}': {directConversionEx.Message}. Specific handling might be needed."
                    );
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[ConvertJTokenToType] Could not convert JToken '{token}' to type '{targetType.Name}': {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Split a property path into parts, handling both dot notation and array indexers
        /// </summary>
        private static string[] SplitPropertyPath(string path)
        {
            // Handle complex paths with both dots and array indexers
            List<string> parts = new List<string>();
            int startIndex = 0;
            bool inBrackets = false;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (c == '[')
                {
                    inBrackets = true;
                }
                else if (c == ']')
                {
                    inBrackets = false;
                }
                else if (c == '.' && !inBrackets)
                {
                    // Found a dot separator outside of brackets
                    parts.Add(path.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }

            // Add the final part
            if (startIndex < path.Length)
            {
                parts.Add(path.Substring(startIndex));
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Creates a serializable representation of a GameObject.
        /// </summary>
        private static object GetGameObjectData(GameObject go)
        {
            if (go == null)
                return null;
            return new
            {
                name = go.name,
                instanceID = go.GetInstanceID(),
                tag = go.tag,
                layer = go.layer,
                activeSelf = go.activeSelf,
                activeInHierarchy = go.activeInHierarchy,
                isStatic = go.isStatic,
                scenePath = go.scene.path, // Identify which scene it belongs to
                transform = new // Serialize transform components carefully to avoid JSON issues
                {
                    // Serialize Vector3 components individually to prevent self-referencing loops.
                    // The default serializer can struggle with properties like Vector3.normalized.
                    position = go.transform.position,
                    localPosition = go.transform.localPosition,
                    rotation = go.transform.eulerAngles,
                    localRotation = go.transform.localEulerAngles,
                    scale = go.transform.localScale,
                    //forward = go.transform.transform,
                    //up = go.transform.up,
                    //right = go.transform.right
                },
                parentInstanceID = go.transform.parent?.gameObject.GetInstanceID() ?? 0, // 0 if no parent
                // Optionally include components, but can be large
                // components = go.GetComponents<Component>().Select(c => GetComponentData(c)).ToList()
                // Or just component names:
                componentNames = go.GetComponents<Component>()
                    .Select(c => c.GetType().FullName)
                    .ToList(),
            };
        }





        /// <summary>
        /// Removes a component by type name.
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        private static Response RemoveComponentInternal(GameObject targetGo, string typeName)
        {
            Type componentType = FindType(typeName);
            if (componentType == null)
            {
                return Response.Error($"Component type '{typeName}' not found for removal.");
            }

            // Prevent removing essential components
            if (componentType == typeof(Transform))
            {
                return Response.Error("Cannot remove the Transform component.");
            }

            Component componentToRemove = targetGo.GetComponent(componentType);
            if (componentToRemove == null)
            {
                return Response.Error($"Component '{typeName}' not found on '{targetGo.name}' to remove.");
            }

            try
            {
                // Use Undo.DestroyObjectImmediate for undo support
                UnityEditor.Undo.DestroyObjectImmediate(componentToRemove);
                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error($"Error removing component '{typeName}' from '{targetGo.name}': {e.Message}");
            }
        }


        /// <summary>
        /// Creates a serializable representation of a Component.
        /// TODO: Add property serialization.
        /// </summary>
        private static object GetComponentData(Component c, bool isFullDetail = false)
        {
            if (c == null)
                return "Empty component.";

            if (isFullDetail)
            {
                return c;
            }
            else
            {
                var data = new Dictionary<string, object>
                {
                    { "typeName", c.GetType().FullName },
                    { "instanceID", c.GetInstanceID() },
                };

                return data;
            }
        }
    }
}