#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiClickHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiClick;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var id = request.GetParam("id", null);
            if (string.IsNullOrWhiteSpace(id))
                return InvalidParameters("Parameter 'id' is required.");

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
                return modeFailure;

            if (!string.Equals(effectiveMode, "play", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(
                    StatusCode.InvalidParameters,
                    "`ui click` currently requires Play Mode. Start Play Mode first or use `--mode play` once the Editor is already playing.");
            }

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.Button>(id, "Button", out var button, out var failure))
                return failure;

            if (!TryMatchScene(button.gameObject, request.GetParam("scene", null), out var sceneFailure))
                return sceneFailure;

            if (!button.gameObject.activeInHierarchy)
                return Fail(StatusCode.InvalidParameters, $"Button '{button.gameObject.name}' is inactive in hierarchy.");

            if (!button.interactable)
                return Fail(StatusCode.InvalidParameters, $"Button '{button.gameObject.name}' is not interactable.");

            button.onClick.Invoke();

            return Ok($"Clicked Button '{button.gameObject.name}'", new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(button),
                ["gameObjectName"] = button.gameObject.name,
                ["uiType"] = "Button",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["scenePath"] = button.gameObject.scene.path,
                ["sceneName"] = button.gameObject.scene.name,
                ["interactable"] = button.interactable,
                ["activeInHierarchy"] = button.gameObject.activeInHierarchy,
                ["eventsTriggered"] = true
            });
        }

        private static bool TryMatchScene(UnityEngine.GameObject gameObject, string sceneFilter, out CommandResponse failure)
        {
            failure = null;
            if (string.IsNullOrWhiteSpace(sceneFilter) || string.Equals(sceneFilter, "active", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(sceneFilter, "active", StringComparison.OrdinalIgnoreCase))
                    return true;

                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (activeScene == gameObject.scene)
                    return true;

                failure = Fail(
                    StatusCode.InvalidParameters,
                    $"Button '{gameObject.name}' belongs to scene '{gameObject.scene.path}', not the active scene '{activeScene.path}'.");
                return false;
            }

            if (string.Equals(gameObject.scene.path, sceneFilter, StringComparison.OrdinalIgnoreCase)
                || string.Equals(gameObject.scene.name, sceneFilter, StringComparison.OrdinalIgnoreCase)
                || string.Equals(System.IO.Path.GetFileNameWithoutExtension(gameObject.scene.path), sceneFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            failure = Fail(
                StatusCode.InvalidParameters,
                $"Button '{gameObject.name}' belongs to scene '{gameObject.scene.path}', which does not match scene filter '{sceneFilter}'.");
            return false;
        }
    }
}
#endif
