using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneOpenHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneOpen;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var mode = request.GetParam("mode", "single");
            var dirtyPolicy = ResolveDirtyPolicy(request);
            var force = request.GetParam<bool>("force");
            var saveCurrentModified = request.GetParam<bool>("saveCurrentModified");

            if (string.IsNullOrWhiteSpace(path))
                return InvalidParameters("Parameter 'path' is required.");

            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Scene path must end with '.unity'.");

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) == null)
                return Fail(StatusCode.NotFound, $"Scene asset not found: {path}");

            if (!TryParseOpenMode(mode, out var openMode))
                return InvalidParameters($"Invalid mode '{mode}'. Must be 'single' or 'additive'.");

            if (openMode == OpenSceneMode.Single)
            {
                var dirtyScenes = GetDirtyLoadedScenePaths();
                if (dirtyScenes.Count > 0)
                {
                    if (dirtyPolicy == "save" || saveCurrentModified)
                    {
                        if (!EditorSceneManager.SaveOpenScenes())
                            return Fail(StatusCode.UnknownError, "Failed to save dirty scenes before opening a new scene.");
                    }
                    else if (dirtyPolicy == "discard" || force)
                    {
                        // Explicit discard; EditorSceneManager.OpenScene(single) will replace the setup.
                    }
                    else
                    {
                        return Fail(
                            StatusCode.InvalidParameters,
                            "Dirty loaded scenes exist. Retry with dirtyPolicy=save or dirtyPolicy=discard.",
                            new JObject
                            {
                                ["dirtyScenes"] = JArray.FromObject(dirtyScenes),
                                ["dirtyPolicy"] = dirtyPolicy,
                                ["retryExamples"] = new JArray
                                {
                                    "scene open --dirty-policy save",
                                    "scene open --dirty-policy discard"
                                }
                            });
                    }
                }
            }

            var scene = EditorSceneManager.OpenScene(path, openMode);
            if (!scene.IsValid() || !scene.isLoaded)
                return Fail(StatusCode.UnknownError, $"Failed to open scene: {path}");

            return Ok($"Opened '{scene.path}'", new JObject
            {
                ["scenePath"] = scene.path,
                ["sceneName"] = scene.name,
                ["mode"] = mode,
                ["dirtyPolicy"] = dirtyPolicy,
                ["isLoaded"] = scene.isLoaded,
                ["isActive"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene.path
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool TryParseOpenMode(string? mode, out OpenSceneMode openMode)
        {
            if (string.Equals(mode, "additive", StringComparison.OrdinalIgnoreCase))
            {
                openMode = OpenSceneMode.Additive;
                return true;
            }

            if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, "single", StringComparison.OrdinalIgnoreCase))
            {
                openMode = OpenSceneMode.Single;
                return true;
            }

            openMode = OpenSceneMode.Single;
            return false;
        }

        private static string ResolveDirtyPolicy(CommandRequest request)
        {
            var dirtyPolicy = request.GetParam("dirtyPolicy", null);
            if (!string.IsNullOrWhiteSpace(dirtyPolicy))
                return dirtyPolicy.Trim().ToLowerInvariant();
            if (request.GetParam<bool>("saveCurrentModified"))
                return "save";
            if (request.GetParam<bool>("force"))
                return "discard";
            return "fail";
        }

        private static List<string> GetDirtyLoadedScenePaths()
        {
            var dirtyScenes = new List<string>();
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                    dirtyScenes.Add(scene.path);
            }

            return dirtyScenes;
        }
#endif
    }
}
