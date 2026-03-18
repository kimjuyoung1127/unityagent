using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PrefabUnpackHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PrefabUnpack;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            var mode = request.GetParam("mode", "outermost");

            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(go))
                return InvalidParameters($"'{go.name}' is not a prefab instance.");

            UnityEditor.PrefabUnpackMode unpackMode;
            if (string.Equals(mode, "completely", StringComparison.OrdinalIgnoreCase))
                unpackMode = UnityEditor.PrefabUnpackMode.Completely;
            else if (string.Equals(mode, "outermost", StringComparison.OrdinalIgnoreCase))
                unpackMode = UnityEditor.PrefabUnpackMode.OutermostRoot;
            else
                return InvalidParameters(
                    $"Invalid mode '{mode}'. Must be 'outermost' or 'completely'.");

            var undoName = $"unityctl: prefab-unpack: {go.name} ({mode})";
            using (new UndoScope(undoName))
            {
                UnityEditor.PrefabUtility.UnpackPrefabInstance(
                    go,
                    unpackMode,
                    UnityEditor.InteractionMode.UserAction);

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

                return Ok($"Unpacked '{go.name}' ({mode})", new JObject
                {
                    ["name"] = go.name,
                    ["mode"] = mode,
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
