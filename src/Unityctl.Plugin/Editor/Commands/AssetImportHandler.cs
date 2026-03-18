using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetImportHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetImport;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var optionsStr = request.GetParam("options", null);
            var importOptions = UnityEditor.ImportAssetOptions.Default;

            if (!string.IsNullOrEmpty(optionsStr))
            {
                if (!System.Enum.TryParse<UnityEditor.ImportAssetOptions>(optionsStr, true, out var parsed))
                    return InvalidParameters(
                        $"Invalid import options: '{optionsStr}'. " +
                        "Valid values: Default, ForceUpdate, ForceSynchronousImport, ImportRecursive, " +
                        "DontDownloadFromCacheServer, ForceUncompressedImport.");

                importOptions = parsed;
            }

            UnityEditor.AssetDatabase.ImportAsset(path, importOptions);

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            return Ok($"Imported asset at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid ?? "",
                ["options"] = importOptions.ToString()
            });
#else
            return NotInEditor();
#endif
        }
    }
}
