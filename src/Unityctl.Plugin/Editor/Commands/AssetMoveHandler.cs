using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetMoveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetMove;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var source = request.GetParam("source", null);
            if (string.IsNullOrEmpty(source))
                return InvalidParameters("Parameter 'source' is required.");

            var destination = request.GetParam("destination", null);
            if (string.IsNullOrEmpty(destination))
                return InvalidParameters("Parameter 'destination' is required.");

            var sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(source);
            if (string.IsNullOrEmpty(sourceGuid))
                return Fail(StatusCode.NotFound, $"Source asset not found: {source}");

            var result = UnityEditor.AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(result))
                return Fail(StatusCode.UnknownError, $"Move failed: {result}");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination);

            return Ok($"Moved '{source}' to '{destination}'", new JObject
            {
                ["path"] = destination,
                ["guid"] = guid
            });
#else
            return NotInEditor();
#endif
        }
    }
}
