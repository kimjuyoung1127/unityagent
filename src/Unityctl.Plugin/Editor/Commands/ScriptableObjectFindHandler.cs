#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptableObjectFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptableObjectFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var type = request.GetParam("type", null);
            var folder = request.GetParam("folder", null);
            var limit = request.GetParam<int>("limit");

            var filter = string.IsNullOrWhiteSpace(type)
                ? "t:ScriptableObject"
                : $"t:{type}";

            string[] searchFolders = string.IsNullOrWhiteSpace(folder)
                ? null
                : new[] { folder };

            var guids = searchFolders != null
                ? AssetDatabase.FindAssets(filter, searchFolders)
                : AssetDatabase.FindAssets(filter);

            var results = new JArray();
            foreach (var guid in guids)
            {
                if (limit > 0 && results.Count >= limit)
                    break;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(assetPath);
                if (asset == null)
                    continue;

                results.Add(new JObject
                {
                    ["path"] = assetPath,
                    ["guid"] = guid,
                    ["typeName"] = asset.GetType().FullName ?? asset.GetType().Name,
                    ["name"] = asset.name
                });
            }

            return Ok($"Found {results.Count} ScriptableObject(s)", new JObject
            {
                ["results"] = results
            });
        }
    }
}
#endif
