#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class EditorFocusSceneViewHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.EditorFocusSceneView;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            const string menuPath = "Window/General/Scene";
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return Fail(StatusCode.NotFound, "No active SceneView found", new JObject
                {
                    ["menuPath"] = menuPath,
                    ["isCompiling"] = UnityEditor.EditorApplication.isCompiling,
                    ["isUpdating"] = UnityEditor.EditorApplication.isUpdating,
                    ["recommendedAction"] = "Open a Scene View tab or wait for domain reload to finish before retrying."
                });
            }

            sceneView.Focus();

            return Ok("Scene View focused", new JObject
            {
                ["focused"] = true,
                ["menuPath"] = menuPath
            });
        }
    }
}
#endif
