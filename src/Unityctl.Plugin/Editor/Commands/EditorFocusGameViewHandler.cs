#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class EditorFocusGameViewHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.EditorFocusGameView;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            const string menuPath = "Window/General/Game";
            var success = UnityEditor.EditorApplication.ExecuteMenuItem(menuPath);
            if (!success)
            {
                return Fail(StatusCode.UnknownError, "Failed to focus Game View via menu item", new JObject
                {
                    ["menuPath"] = menuPath,
                    ["isCompiling"] = UnityEditor.EditorApplication.isCompiling,
                    ["isUpdating"] = UnityEditor.EditorApplication.isUpdating,
                    ["recommendedAction"] = "Wait for compilation/domain reload to finish, then retry editor focus-gameview."
                });
            }

            return Ok("Game View focused", new JObject
            {
                ["focused"] = true,
                ["menuPath"] = menuPath
            });
        }
    }
}
#endif
