using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityAutopilot.Agent;
using UnityAutopilot.Tools.Command;
using UnityEngine;

namespace UnityAutopilot.Tools
{
    public static class ToolRegistry
    {
        private readonly static Dictionary<string, Type> registry = new Dictionary<string, Type>();

        public static Dictionary<string, Type> Registry
        {
            get => registry;
        }

        public static void RegisterTool(string id, Type t)
        {
            if (!typeof(ICommand).IsAssignableFrom(t))
            {
                Debug.Log($"[Tool Registry]: Type {t.Name} does not implement ICommand interface. {id}");

                return;
            }

            registry[id] = t;

            //Debug.Log($"[Tool Registry]: Tool registered: {id}    ==> {t.Name}");
        }

        public static ICommand GetCommand(string toolType, string data)
        {
            ICommand cmd = null;

            if (registry.TryGetValue(toolType, out var type))
            {
                cmd = (ICommand)Activator.CreateInstance(type);
                cmd.SetArgumentData(data);
            }

            return cmd;
        }

        public static List<ToolCall> GetRegisteredTools()
        {
            List<ToolCall> tools = new();

            //Debug.Log($"<color=white>{ToolRegistry.Registry.Count}</color>");

            int index = 0;

            foreach (var tool in ToolRegistry.Registry)
            {
                var prop = tool.Value.GetProperty("ToolMetaData", BindingFlags.Static | BindingFlags.Public);

                if (prop == null)
                {
                    Debug.LogError($"Can not find \"ToolMetaData\" in {tool.Value}");
                    continue;
                }

                ToolMetaData toolAttribute = prop.GetValue(null) as ToolMetaData;

                var functionName = toolAttribute.Name;
                var functionDescription = toolAttribute.Description;
                var functionParameters = JToken.Parse(toolAttribute.ParametersSchema);

                //Debug.Log($"{functionParameters}");

                var chatTool = new ToolCall()
                {
                    index = index++,
                    type = "function",
                    function = new Function()
                    {
                        name = functionName,
                        description = functionDescription,
                        parameters = functionParameters,
                        strict = false
                    }
                };

                tools.Add(chatTool);

                //Debug.Log(functionName);
                //Debug.Log(functionDescription);
                //Debug.Log(functionParameters);
            }

            return tools;
        }
    }
}