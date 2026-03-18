using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UndoHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Undo;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(
                    StatusCode.InvalidParameters,
                    "undo is IPC-only. Batch mode does not preserve editor undo history across invocations.");
            }

            var groupName = UnityEditor.Undo.GetCurrentGroupName();
            UnityEditor.Undo.PerformUndo();

            return Ok("Performed undo", new JObject
            {
                ["action"] = "undo",
                ["groupName"] = string.IsNullOrEmpty(groupName) ? "last operation" : groupName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
