using System;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiCanvasCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiCanvasCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var name = request.GetParam("name", "Canvas");
            var renderModeStr = request.GetParam("renderMode", null);

            var renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            if (!string.IsNullOrEmpty(renderModeStr))
            {
                if (!Enum.TryParse(renderModeStr, true, out renderMode))
                    return InvalidParameters(
                        $"Unknown renderMode: '{renderModeStr}'. " +
                        "Valid values: ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace");
            }

            var undoName = $"unityctl: ui-canvas-create: {name}";
            using (new UndoScope(undoName))
            {
                var go = new UnityEngine.GameObject(name);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, undoName);

                var canvas = go.AddComponent<UnityEngine.Canvas>();
                canvas.renderMode = renderMode;

                go.AddComponent<UnityEngine.UI.CanvasScaler>();
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Ensure EventSystem exists
                var eventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (eventSystem == null)
                {
                    var esGo = new UnityEngine.GameObject("EventSystem");
                    UnityEditor.Undo.RegisterCreatedObjectUndo(esGo, undoName);
                    esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                EditorSceneManager.MarkSceneDirty(go.scene);

                var globalId = GlobalObjectIdResolver.GetId(go);
                return Ok($"Created Canvas '{name}'", new JObject
                {
                    ["globalObjectId"] = globalId,
                    ["name"] = go.name,
                    ["renderMode"] = renderMode.ToString(),
                    ["scenePath"] = go.scene.path,
                    ["sceneDirty"] = true,
                    ["undoGroupName"] = undoName
                });
            }
#else
            return NotInEditor();
#endif
        }
    }
}
