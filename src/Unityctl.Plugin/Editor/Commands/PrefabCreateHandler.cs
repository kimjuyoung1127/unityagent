using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PrefabCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PrefabCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var targetId = request.GetParam("target", null);
            var path = request.GetParam("path", null);

            if (string.IsNullOrEmpty(targetId))
                return InvalidParameters("Parameter 'target' is required.");
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(targetId);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {targetId}");

            bool success;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path, out success);

            if (!success)
                return Fail(StatusCode.UnknownError, $"Failed to save prefab at: {path}");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            return Ok($"Prefab created at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["sourceGameObject"] = go.name
            });
#else
            return NotInEditor();
#endif
        }
    }
}
