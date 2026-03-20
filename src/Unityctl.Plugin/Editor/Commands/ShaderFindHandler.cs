#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ShaderFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ShaderFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var filter = request.GetParam("filter", null);
            var includeBuiltin = request.GetParam<bool>("includeBuiltin");
            var limit = request.GetParam<int>("limit");

            var results = new JArray();

            // Project shaders via AssetDatabase
            var guids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in guids)
            {
                if (limit > 0 && results.Count >= limit)
                    break;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(assetPath);
                if (shader == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(filter)
                    && shader.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                results.Add(new JObject
                {
                    ["name"] = shader.name,
                    ["path"] = assetPath,
                    ["guid"] = guid,
                    ["isBuiltin"] = false,
                    ["propertyCount"] = shader.GetPropertyCount()
                });
            }

            // Built-in shaders via ShaderUtil
            if (includeBuiltin)
            {
                var shaderCount = ShaderUtil.GetAllShaderInfo().Length;
                foreach (var info in ShaderUtil.GetAllShaderInfo())
                {
                    if (limit > 0 && results.Count >= limit)
                        break;

                    // Skip project shaders already added
                    var shader = UnityEngine.Shader.Find(info.name);
                    if (shader == null)
                        continue;

                    var shaderPath = AssetDatabase.GetAssetPath(shader);
                    if (!string.IsNullOrEmpty(shaderPath) && shaderPath.StartsWith("Assets"))
                        continue;

                    if (!string.IsNullOrWhiteSpace(filter)
                        && info.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    results.Add(new JObject
                    {
                        ["name"] = info.name,
                        ["path"] = shaderPath ?? "",
                        ["guid"] = "",
                        ["isBuiltin"] = true,
                        ["propertyCount"] = shader.GetPropertyCount()
                    });
                }
            }

            return Ok($"Found {results.Count} shader(s)", new JObject
            {
                ["shaders"] = results
            });
        }
    }
}
#endif
