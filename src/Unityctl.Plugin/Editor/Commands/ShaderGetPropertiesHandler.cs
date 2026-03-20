#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ShaderGetPropertiesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ShaderGetProperties;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            var shader = UnityEngine.Shader.Find(name);
            if (shader == null)
                return Fail(StatusCode.NotFound, $"Shader not found: {name}");

            var properties = new JArray();
            var propCount = shader.GetPropertyCount();
            for (int i = 0; i < propCount; i++)
            {
                var propName = shader.GetPropertyName(i);
                var propType = shader.GetPropertyType(i);
                var propDesc = shader.GetPropertyDescription(i);

                var propInfo = new JObject
                {
                    ["name"] = propName,
                    ["type"] = propType.ToString(),
                    ["description"] = propDesc
                };

                // Add range info for Range properties
                if (propType == UnityEngine.Rendering.ShaderPropertyType.Range)
                {
                    var rangeLimits = shader.GetPropertyRangeLimits(i);
                    propInfo["rangeMin"] = rangeLimits.x;
                    propInfo["rangeMax"] = rangeLimits.y;
                }

                // Add default value
                var defaultValue = shader.GetPropertyDefaultFloatValue(i);
                if (propType == UnityEngine.Rendering.ShaderPropertyType.Float
                    || propType == UnityEngine.Rendering.ShaderPropertyType.Range)
                {
                    propInfo["defaultValue"] = defaultValue;
                }
                else if (propType == UnityEngine.Rendering.ShaderPropertyType.Color
                    || propType == UnityEngine.Rendering.ShaderPropertyType.Vector)
                {
                    var vec = shader.GetPropertyDefaultVectorValue(i);
                    propInfo["defaultValue"] = new JObject
                    {
                        ["x"] = vec.x, ["y"] = vec.y, ["z"] = vec.z, ["w"] = vec.w
                    };
                }

                properties.Add(propInfo);
            }

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(shader);
            return Ok($"Shader '{name}' has {propCount} properties", new JObject
            {
                ["name"] = name,
                ["path"] = assetPath ?? "",
                ["propertyCount"] = propCount,
                ["properties"] = properties
            });
        }
    }
}
#endif
