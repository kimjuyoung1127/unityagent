using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var name = request.GetParam("name", null);
            var text = request.GetParam("text", null);
            var type = request.GetParam("type", null);
            var parent = request.GetParam("parent", null);
            var canvas = request.GetParam("canvas", null);
            var scene = request.GetParam("scene", null);
            var includeInactive = request.GetParam<bool>("includeInactive");
            var limit = request.GetParam<int>("limit");
            var interactable = TryGetOptionalBool(request, "interactable");
            var active = TryGetOptionalBool(request, "active");
            var effectiveIncludeInactive = includeInactive || active == false;

            var results = new JArray();
            var loadedScenes = GetTargetScenes(scene);
            if (loadedScenes.Count == 0)
            {
                return Fail(StatusCode.NotFound, $"No loaded scene matched '{scene}'");
            }

            foreach (var loadedScene in loadedScenes)
            {
                foreach (var root in loadedScene.GetRootGameObjects())
                {
                    if (Traverse(root, string.Empty, results, name, text, type, parent, canvas, interactable, active, effectiveIncludeInactive, limit))
                    {
                        return Ok($"Found {results.Count} UI element(s)", new JObject
                        {
                            ["results"] = results
                        });
                    }
                }
            }

            return Ok($"Found {results.Count} UI element(s)", new JObject
            {
                ["results"] = results
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static System.Collections.Generic.List<UnityEngine.SceneManagement.Scene> GetTargetScenes(string sceneFilter)
        {
            var scenes = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
            if (string.Equals(sceneFilter, "active", StringComparison.OrdinalIgnoreCase))
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    scenes.Add(activeScene);
                }

                return scenes;
            }

            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!loadedScene.isLoaded)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sceneFilter)
                    || string.Equals(loadedScene.path, sceneFilter, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(loadedScene.name, sceneFilter, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(System.IO.Path.GetFileNameWithoutExtension(loadedScene.path), sceneFilter, StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(loadedScene);
                }
            }

            return scenes;
        }

        private static bool Traverse(
            UnityEngine.GameObject gameObject,
            string parentPath,
            JArray results,
            string name,
            string text,
            string type,
            string parent,
            string canvas,
            bool? interactable,
            bool? active,
            bool includeInactive,
            int limit)
        {
            if (!includeInactive && !gameObject.activeSelf)
            {
                return false;
            }

            var hierarchyPath = SceneExplorationUtility.GetHierarchyPath(gameObject, parentPath);
            if (Matches(gameObject, hierarchyPath, name, text, type, parent, canvas, interactable, active))
            {
                results.Add(UiReadUtility.CreateUiFindSummary(gameObject, hierarchyPath));
                if (limit > 0 && results.Count >= limit)
                {
                    return true;
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                if (Traverse(
                    gameObject.transform.GetChild(i).gameObject,
                    hierarchyPath,
                    results,
                    name,
                    text,
                    type,
                    parent,
                    canvas,
                    interactable,
                    active,
                    includeInactive,
                    limit))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Matches(
            UnityEngine.GameObject gameObject,
            string hierarchyPath,
            string name,
            string text,
            string type,
            string parent,
            string canvas,
            bool? interactable,
            bool? active)
        {
            if (!UiReadUtility.IsUiGameObject(gameObject))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(name)
                && gameObject.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                var primaryText = UiReadUtility.GetPrimaryText(gameObject);
                if (string.IsNullOrEmpty(primaryText)
                    || primaryText.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(type)
                && !UiReadUtility.MatchesUiType(gameObject, type))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(parent))
            {
                var parentTransform = gameObject.transform.parent;
                var parentId = parentTransform != null
                    ? GlobalObjectIdResolver.GetId(parentTransform.gameObject)
                    : null;
                if (!string.Equals(parentId, parent, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(canvas))
            {
                if (!UiReadUtility.TryGetRootCanvas(gameObject, out var rootCanvas))
                {
                    return false;
                }

                var canvasId = GlobalObjectIdResolver.GetId(rootCanvas.gameObject);
                if (!string.Equals(canvasId, canvas, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (interactable.HasValue)
            {
                var actualInteractable = UiReadUtility.GetInteractable(gameObject);
                if (!actualInteractable.HasValue || actualInteractable.Value != interactable.Value)
                {
                    return false;
                }
            }

            if (active.HasValue && gameObject.activeSelf != active.Value)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(hierarchyPath);
        }

        private static bool? TryGetOptionalBool(CommandRequest request, string key)
        {
            if (request.parameters == null || request.parameters[key] == null || request.parameters[key].Type == JTokenType.Null)
            {
                return null;
            }

            if (request.parameters[key].Type == JTokenType.Boolean)
            {
                return request.parameters[key].Value<bool>();
            }

            if (request.parameters[key].Type == JTokenType.String
                && bool.TryParse(request.parameters[key].Value<string>(), out var parsed))
            {
                return parsed;
            }

            return null;
        }
#endif
    }
}
