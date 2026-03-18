using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class RedoHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Redo;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(
                    StatusCode.InvalidParameters,
                    "redo is IPC-only. Batch mode does not preserve editor undo history across invocations.");
            }

            var groupName = UnityEditor.Undo.GetCurrentGroupName();
            UnityEditor.Undo.PerformRedo();

            return Ok("Performed redo", new JObject
            {
                ["action"] = "redo",
                ["groupName"] = string.IsNullOrEmpty(groupName) ? "last undone operation" : groupName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
