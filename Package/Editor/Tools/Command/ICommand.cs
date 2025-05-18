using Cysharp.Threading.Tasks;

namespace UnityAutopilot.Tools.Command
{
    public interface ICommand
    {
        UniTask<Response> Execute();
        UniTask<Response> Undo();

        void SetArgumentData(string argumentData);
    }

    public abstract class BaseCommand : ICommand
    {
        protected string ArgumentData { get; set; }

        public abstract UniTask<Response> Execute();

        public void SetArgumentData(string argumentData)
        {
            ArgumentData = argumentData;
        }

        public abstract UniTask<Response> Undo();
    }
}