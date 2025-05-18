using Cysharp.Threading.Tasks;
using System.Text;
using UnityAutopilot.Tools;
using UnityEditor;
using UnityEngine;


namespace UnityAutopilot.Test
{
    public class ToolsTester : MonoBehaviour
    {
        [SerializeField] public string toolName;
        [TextArea(3, 6)]
        [SerializeField] public string parameters;
    }

    [CustomEditor(typeof(ToolsTester))]
    public class ToolTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var sb = new StringBuilder();

            foreach (var item in ToolRegistry.Registry)
            {
                sb.AppendLine(item.Key);
            }
            EditorGUILayout.TextArea(sb.ToString());


            base.OnInspectorGUI();

            var instance = (ToolsTester)target;

            if (GUILayout.Button("Call Tool"))
            {
                var cmd = ToolRegistry.GetCommand(instance.toolName, instance.parameters);

                UniTask.Void(async () =>
                {
                    var rslt = await ToolManager.ExecuteCommand(cmd);

                    Debug.Log($"{rslt}");
                });
            }
        }
    }
}