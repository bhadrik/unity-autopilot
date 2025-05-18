using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityAutopilot.Tools.Command;
using UnityEngine;

namespace UnityAutopilot.Tools
{
    public static class ToolManager
    {
        private readonly static Stack<ICommand> undoStack = new();
        private readonly static Stack<ICommand> redoStack = new();

        public static async UniTask<Response> ExecuteCommand(ICommand command)
        {
            var result = await command.Execute();
            undoStack.Push(command);
            redoStack.Clear();

            return result;
        }

        public static async UniTask<Response> Undo()
        {
            if (undoStack.Count == 0) return Response.Error("Undo stack is empty, nothing to undo.");

            var command = undoStack.Pop();
            var result = await command.Undo();
            redoStack.Push(command);

            return result;
        }

        public static async UniTask<Response> Redo()
        {
            if (redoStack.Count == 0) return Response.Error("Redo stack is empty, nothing to redo");
            var command = redoStack.Pop();
            var result = await command.Execute();
            undoStack.Push(command);

            return result;
        }

        public static void RefreshTools()
        {
            // Find all types inheriting from BaseCommand in loaded assemblies
            var baseType = typeof(BaseCommand);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                    continue;

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || !baseType.IsAssignableFrom(type))
                        continue;

                    // Look for a static ToolMetaData field/property
                    var metaProp = type.GetProperty("ToolMetaData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    object metaValue = null;
                    if (metaProp != null)
                        metaValue = metaProp.GetValue(null);
                }
            }
        }
    }

    public class ToolMetaData
    {
        /// <summary>
        /// Name of the tool. This is the name you will use to call the tool in your AI agent.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the tool with full parameter usage description.
        /// Ex. Creates a new GameObject with following parameters:
        ///     [Name]: the name of the GameObject to be created
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// JSON schema of the parameters for this tool.
        /// Ex. {
        ///       "type": "object",
        ///       "properties": {
        ///           "Name": {
        ///               "type": "string",
        ///               "description": "Name of the game object to be created"
        ///            }
        ///        },
        ///        "required": [ "Name" ]
        ///     }
        /// </summary>
        public string ParametersSchema { get; }

        public ToolMetaData(string name, string description, Type paramType, Type toolType)
        {
            Name = name;
            Description = description;

            var gen = new JSchemaGenerator()
            {
                DefaultRequired = Newtonsoft.Json.Required.AllowNull,
                SchemaLocationHandling = SchemaLocationHandling.Inline,
                GenerationProviders =
                {
                    new StringEnumGenerationProvider(),
                    new ToolParamSchemaProvider(),
                    new DictionaryAsFlatObjectSchemaProvider(),
                    new Vector3SchemaProvider()
                },
            };

            var schema = gen.Generate(paramType);

            schema.AllowAdditionalProperties = false;

            ParametersSchema = schema.ToString();

            //StringBuilder sb = new StringBuilder();

            //sb.AppendLine($"<color={Logger.msgL1Color}>Tool: {Name}</color>");
            //sb.AppendLine($"Description: {Description}");
            //sb.AppendLine($"ParametersSchema: {ParametersSchema}");

            //Logger.LogSimple(sb.ToString());

            ToolRegistry.RegisterTool(name, toolType);
        }
    }

    public class ToolParamSchemaProvider : StringEnumGenerationProvider
    {
        List<Type> excludeType = new()
        {
            typeof(Vector3),
            typeof(IDictionary<,>)
        };


        public override bool CanGenerateSchema(JSchemaTypeGenerationContext context)
        {
            return !excludeType.Contains(context.ObjectType) && !excludeType.Contains(context.ObjectType.UnderlyingSystemType);
        }

        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            var schema = context.Generator.Generate(context.ObjectType);

            // Dont want any other logic to put reuired fields
            schema.Required.Clear();

            // Handle both properties and fields
            foreach (var member in context.ObjectType.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (member.MemberType != MemberTypes.Property && member.MemberType != MemberTypes.Field)
                    continue;

                string name = member.Name;
                if (!schema.Properties.TryGetValue(name, out JSchema memberSchema))
                    continue;

                // Description + Required
                var descAttr = member.GetCustomAttribute<ToolParamAttribute>();
                if (descAttr != null)
                {
                    memberSchema.Description = descAttr.Description;
                    if (descAttr.IsRequired)
                        schema.Required.Add(name);
                }

                // Multi-type logic
                var multiTypeAttr = member.GetCustomAttribute<ToolParamMultiTypeAttribute>();
                if (multiTypeAttr != null && multiTypeAttr.Types.Length > 0)
                {
                    memberSchema.Type = 0; // Reset type
                    foreach (var type in multiTypeAttr.Types)
                    {
                        if (TryMapClrTypeToJSchemaType(type, out JSchemaType jsonType))
                        {
                            memberSchema.Type |= jsonType;
                        }
                    }
                }
            }

            schema.AllowAdditionalProperties = false;

            return schema;
        }

        private bool TryMapClrTypeToJSchemaType(Type clrType, out JSchemaType jsonType)
        {
            if (clrType == typeof(string)) jsonType = JSchemaType.String;
            else if (clrType == typeof(int) || clrType == typeof(long)) jsonType = JSchemaType.Integer;
            else if (clrType == typeof(float) || clrType == typeof(double) || clrType == typeof(decimal)) jsonType = JSchemaType.Number;
            else if (clrType == typeof(bool)) jsonType = JSchemaType.Boolean;
            else if (clrType == typeof(object)) jsonType = JSchemaType.Object;
            else if (clrType.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(clrType)) jsonType = JSchemaType.Array;
            else jsonType = default;

            return jsonType != default;
        }
    }

    public class DictionaryAsFlatObjectSchemaProvider : JSchemaGenerationProvider
    {
        public override bool CanGenerateSchema(JSchemaTypeGenerationContext context)
        {
            return context.ObjectType.IsAssignableFrom(typeof(IDictionary<,>));
        }

        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            return new JSchema
            {
                Type = JSchemaType.Object,
                AllowAdditionalProperties = false
            };
        }
    }

    public class Vector3SchemaProvider : JSchemaGenerationProvider
    {
        public override bool CanGenerateSchema(JSchemaTypeGenerationContext context)
        {
            return context.ObjectType == typeof(Vector3) || context.ObjectType.IsAssignableFrom(typeof(Vector3));
        }

        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            return new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "x", new JSchema { Type = JSchemaType.Number } },
                    { "y", new JSchema { Type = JSchemaType.Number } },
                    { "z", new JSchema { Type = JSchemaType.Number } }
                },
                Required = { "x", "y", "z" },
                AllowAdditionalProperties = false
            };
        }
    }
}