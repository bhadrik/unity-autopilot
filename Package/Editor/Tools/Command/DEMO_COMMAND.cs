/*using Cysharp.Threading.Tasks;
using UnityAutoPilot.Utils;
using Logger = UnityAutoPilot.Utils.Logger;

namespace UnityAutoPilot.Tools.Command
{
    public class NewCommand : BaseCommand
    {
        // + Parameter class ----------------------------------
        public enum Action
        {

        }
        public class Args
        {
            [ToolParam("Description", isRequired: true)]
            [ToolParamEnum(typeof(Action))]
            public Action? action;
        }
        // ----------------------------------------------------


        // + Meta data creation -------------------------------
        public static ToolMetaData ToolMetaData { get; set; } =
            new ToolMetaData(
                name: "new_command",
                description: "Full command description for LLM to understand",
                paramType: typeof(Args),
                toolType: typeof(NewCommand)
            );
        // ----------------------------------------------------

        public async override UniTask<Response> Execute()
        {
            await UniTask.Yield();

            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"[{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return Response.Success("Successfully created new game object");
        }

        public async override UniTask<Response> Undo()
        {
            await UniTask.Yield();

            var param = Json.convert.DeserializeObject<Args>(ArgumentData);

            Logger.LogMsg($"Undo [{ToolMetaData.Name}]: {Json.convert.SerializeObject(param)}");

            return Response.Success("Successfully undo new game object creation");
        }
    }
}
*/