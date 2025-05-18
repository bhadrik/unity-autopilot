using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityAutopilot.Utils;
using UnityEditor;
using UnityEngine;
using Logger = UnityAutopilot.Utils.Logger;

namespace UnityAutopilot.Tools.Command
{
    public class ReadConsole : BaseCommand
    {
        public enum Action
        {
            get,
            clear
        }

        // + Parameter class ----------------------------------
        [Serializable]
        public class Args
        {
            [ToolParam("action to perform on the console")]
            [ToolParamEnum(typeof(Action))]
            public Action action = Action.get;

            [ToolParam("Filter log by the type you want, must have isAllType=false to affect type")]
            [ToolParamEnum(typeof(LogType))]
            public LogType types = LogType.Error;

            [ToolParam("Set true if require all log without any type filter")]
            public bool isAllType = false;

            [ToolParam("Max number of messages to return.")]
            public int count = 5;

            [ToolParam("Log filter by string")]
            public string filterText = string.Empty;

            [ToolParam("Include stack traces in output.")]
            public bool includeStacktrace = false;
        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } = 
            new ToolMetaData(
                name: "read_console",
                description: "this function help reading console log, error and warnings for debugging.",
                paramType: typeof(Args),
                toolType: typeof(ReadConsole)
            );

        static ReadConsole()
        {
            // For time-stemp and detaild log we need to recoard each log
            // using following line of code.
            //Application.logMessageReceived += (condation, stackTrace, type)

            Application.logMessageReceived += OnLogReceived;
        }

        // ----------------------------------------------------


        // Static list to cache logs
        private static readonly List<LogEntry> CachedLogs = new();



        public async override UniTask<Response> Execute()
        {
            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"[{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return await HandleCommand(param);
        }

        public async override UniTask<Response> Undo()
        {
            await UniTask.Yield();

            return Response.Success("Nothing to undo in console commands");
        }

        public static async UniTask<Response> HandleCommand(Args args)
        {
            await UniTask.Yield();

            switch (args.action)
            {
                case Action.get:
                    return GetLogs(args);
                case Action.clear:
                    return ClearEditorLog();
                default:
                    return Response.Error("Invalid action specified");
            }
        }

        private static Response GetLogs(Args args)
        {
            try
            {
                // Filter logs based on the provided arguments  
                var filteredLogs = CachedLogs
                    .Where(log => args.isAllType || log.Type == args.types) // Filter by type unless isAllType is true  
                    .Where(log => string.IsNullOrEmpty(args.filterText) || log.Condition.Contains(args.filterText)) // Filter by text if provided  
                    .OrderByDescending(log => log.Timestamp) // Order by timestamp descending  
                    .Take(args.count) // Limit the number of logs to the specified count  
                    .ToList();

                var logs = filteredLogs.Select(x => x.GetLog(args.includeStacktrace));

                return Response.Success("Logs retrieved successfully", string.Join('\n', logs));
            }
            catch (Exception e)
            {
                return Response.Error($"Error while retrieving logs => {e.Message}");
            }
        }

        private static Response ClearEditorLog()
        {
            try
            {
                var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                var type = assembly.GetType("UnityEditor.LogEntries");
                var method = type.GetMethod("Clear");
                method.Invoke(null, null);

                // Optional: Display a confirmation message in the editor
                EditorUtility.DisplayDialog(
                    "Editor Log",
                    "Editor Log cleared successfully!",
                    "OK"
                );

                return Response.Success("Editor log cleared successfully");
            }
            catch (Exception e)
            {
                return Response.Error($"Error while clearing logs => {e.Message}");
            }
        }


        private static void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            // Cache the log details
            CachedLogs.Add(new LogEntry
            {
                Condition = condition,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.Now
            });

            // Optionally, log to the console for debugging
            //Debug.Log($"[{type}] {condition} \n {stackTrace}");
        }

        // Class to store log details
        private class LogEntry
        {
            public string Condition { get; set; }
            public string StackTrace { get; set; }
            public LogType Type { get; set; }
            public DateTime Timestamp { get; set; }

            public string GetLog(bool includeStackTrace = false)
            {
                if (includeStackTrace)
                {
                    return $"[{Timestamp}] [{Type}]: {Condition} \n {StackTrace}";
                }
                else
                {
                    return $"[{Timestamp}] [{Type}]: {Condition}";
                }
            }

            public override string ToString()
            {
                return GetLog();
            }
        }
    }
}