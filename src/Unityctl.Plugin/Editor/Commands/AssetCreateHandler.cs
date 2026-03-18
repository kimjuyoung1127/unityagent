using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var type = request.GetParam("type", null);
            if (string.IsNullOrEmpty(type))
                return InvalidParameters("Parameter 'type' is required.");

            var assetType = System.Type.GetType(type)
                ?? System.Type.GetType($"UnityEngine.{type}, UnityEngine")
                ?? System.Type.GetType($"UnityEditor.{type}, UnityEditor");

            if (assetType == null)
                return Fail(StatusCode.InvalidParameters, $"Unknown type: {type}");

            if (!typeof(UnityEngine.ScriptableObject).IsAssignableFrom(assetType))
                return Fail(StatusCode.InvalidParameters,
                    $"Type '{type}' is not a ScriptableObject. Only ScriptableObject types can be created as assets.");

            var instance = UnityEngine.ScriptableObject.CreateInstance(assetType);
            if (instance == null)
                return Fail(StatusCode.UnknownError, $"Failed to create instance of type '{type}'.");

            UnityEditor.AssetDatabase.CreateAsset(instance, path);
            UnityEditor.AssetDatabase.SaveAssets();

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                return Fail(StatusCode.UnknownError, $"Asset created but GUID lookup failed for: {path}");
            }

            return Ok($"Created asset at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["type"] = assetType.FullName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
