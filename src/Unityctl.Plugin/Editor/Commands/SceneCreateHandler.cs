using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var template = request.GetParam("template", "default");
            var mode = request.GetParam("mode", "single");
            var force = request.GetParam<bool>("force");
            var saveCurrentModified = request.GetParam<bool>("saveCurrentModified");

            if (string.IsNullOrWhiteSpace(path))
                return InvalidParameters("Parameter 'path' is required.");

            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Scene path must end with '.unity'.");

            var directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory) || !UnityEditor.AssetDatabase.IsValidFolder(directory))
                return InvalidParameters($"Scene directory does not exist: {directory}");

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) != null)
                return InvalidParameters($"Scene already exists: {path}");

            if (!TryParseSceneSetup(template, out var setup))
                return InvalidParameters($"Invalid template '{template}'. Must be 'default' or 'empty'.");

            if (!TryParseNewSceneMode(mode, out var newSceneMode))
                return InvalidParameters($"Invalid mode '{mode}'. Must be 'single' or 'additive'.");

            if (newSceneMode == NewSceneMode.Single)
            {
                var dirtyScenes = GetDirtyLoadedScenePaths();
                if (dirtyScenes.Count > 0)
                {
                    if (saveCurrentModified)
                    {
                        if (!EditorSceneManager.SaveOpenScenes())
                            return Fail(StatusCode.UnknownError, "Failed to save dirty scenes before creating a new scene.");
                    }
                    else if (!force)
                    {
                        return Fail(
                            StatusCode.InvalidParameters,
                            "Dirty loaded scenes exist. Pass force=true to discard or saveCurrentModified=true to save first.",
                            new JObject
                            {
                                ["dirtyScenes"] = JArray.FromObject(dirtyScenes)
                            });
                    }
                }
            }

            var scene = EditorSceneManager.NewScene(setup, newSceneMode);
            if (!scene.IsValid() || !scene.isLoaded)
                return Fail(StatusCode.UnknownError, "Failed to create a new scene.");

            if (!EditorSceneManager.SaveScene(scene, path))
                return Fail(StatusCode.UnknownError, $"Failed to save new scene: {path}");

            return Ok($"Created scene '{path}'", new JObject
            {
                ["scenePath"] = path,
                ["sceneName"] = scene.name,
                ["template"] = template,
                ["mode"] = mode,
                ["isLoaded"] = scene.isLoaded,
                ["isActive"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene.path
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool TryParseSceneSetup(string? template, out NewSceneSetup setup)
        {
            if (string.Equals(template, "empty", StringComparison.OrdinalIgnoreCase))
            {
                setup = NewSceneSetup.EmptyScene;
                return true;
            }

            if (string.IsNullOrWhiteSpace(template) || string.Equals(template, "default", StringComparison.OrdinalIgnoreCase))
            {
                setup = NewSceneSetup.DefaultGameObjects;
                return true;
            }

            setup = NewSceneSetup.DefaultGameObjects;
            return false;
        }

        private static bool TryParseNewSceneMode(string? mode, out NewSceneMode newSceneMode)
        {
            if (string.Equals(mode, "additive", StringComparison.OrdinalIgnoreCase))
            {
                newSceneMode = NewSceneMode.Additive;
                return true;
            }

            if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, "single", StringComparison.OrdinalIgnoreCase))
            {
                newSceneMode = NewSceneMode.Single;
                return true;
            }

            newSceneMode = NewSceneMode.Single;
            return false;
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
