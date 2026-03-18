using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetDeleteHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetDelete;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.NotFound, $"Asset not found: {path}");

            var success = UnityEditor.AssetDatabase.DeleteAsset(path);
            if (!success)
                return Fail(StatusCode.UnknownError, $"Failed to delete asset: {path}");

            return Ok($"Deleted asset at '{path}'", new JObject
            {
                ["path"] = path,
                ["deleted"] = true
            });
#else
            return NotInEditor();
#endif
        }
    }
}
