using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PrefabApplyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PrefabApply;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);

            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(go))
                return InvalidParameters($"'{go.name}' is not a prefab instance.");

            var prefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (string.IsNullOrEmpty(prefabPath))
                return Fail(StatusCode.UnknownError,
                    $"Could not determine prefab asset path for '{go.name}'.");

            UnityEditor.PrefabUtility.ApplyPrefabInstance(
                go,
                UnityEditor.InteractionMode.UserAction);

            return Ok($"Applied overrides from '{go.name}' to '{prefabPath}'", new JObject
            {
                ["name"] = go.name,
                ["prefabPath"] = prefabPath,
                ["globalObjectId"] = id
            });
#else
            return NotInEditor();
#endif
        }
    }
}
