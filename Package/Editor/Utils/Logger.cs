using UnityEngine;

namespace UnityAutopilot.Utils
{
    public static class Logger
    {
        public const string defaultColor = "#b8b8b8";
        public const string erroColor = "#ff7b61";
        public const string warning = "#f7ff66";
        public const string silentWarning = "#9c9657";
        public const string success = "#79e359";
        public const string faild = "#e05b41";

        public const string msgColor = "#42bbeb";
        public const string msgL1Color = "#79e359";
        public const string msgL2Color = "#FF71CD";
        public const string msgL3Color = "#F7EEDD";

        public const string rpcSend = "#456ced";
        public const string rpcReceived = "#eb3f98";

        public static bool isLoggerOn = true;


        [HideInCallstack]
        public static void Log(string msg, string color = defaultColor, Object obj = null) => BaseLog(msg, color, obj);

        [HideInCallstack]
        public static void LogSimple(string msg, Object obj = null)
        {
            if (isLoggerOn)
            {
                Debug.Log(msg, obj);
            }
        }

        [HideInCallstack]
        public static void LogMsg(string msg, Object obj = null) => BaseLog(msg, msgColor, obj);

        [HideInCallstack]
        public static void LogMsgL1(string msg, Object obj = null) => BaseLog(msg, msgL1Color, obj);

        [HideInCallstack]
        public static void LogMsgL2(string msg, Object obj = null) => BaseLog(msg, msgL2Color, obj);

        [HideInCallstack]
        public static void LogMsgL3(string msg, Object obj = null) => BaseLog(msg, msgL3Color, obj);

        [HideInCallstack]
        public static void LogError(string msg, Object obj = null) => BaseLog(msg, erroColor, obj);

        [HideInCallstack]
        public static void LogWarning(string msg, Object obj = null) => BaseLog(msg, warning, obj);

        [HideInCallstack]
        public static void LogSilentWarning(string msg, Object obj = null) => BaseLog(msg, silentWarning, obj);

        [HideInCallstack]
        public static void LogSuccess(string msg, Object obj = null) => BaseLog(msg, success, obj);

        [HideInCallstack]
        public static void LogFaild(string msg, Object obj = null) => BaseLog(msg, faild, obj);

        [HideInCallstack]
        public static void LogRPCSend(string msg, Object obj = null) => BaseLog(msg, rpcSend, obj);

        [HideInCallstack]
        public static void LogRPCReceived(string msg, Object obj = null) => BaseLog(msg, rpcReceived, obj);

        [HideInCallstack]
        private static void BaseLog(string msg, string color, Object obj = null)
        {
            if (isLoggerOn)
            {
                Debug.Log($"<color={color}>{msg}</color>", obj);
            }
        }
    }
}
