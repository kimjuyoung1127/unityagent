using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetCopyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetCopy;

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

            var success = UnityEditor.AssetDatabase.CopyAsset(source, destination);
            if (!success)
                return Fail(StatusCode.UnknownError, $"Failed to copy '{source}' to '{destination}'.");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination);

            return Ok($"Copied '{source}' to '{destination}'", new JObject
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
