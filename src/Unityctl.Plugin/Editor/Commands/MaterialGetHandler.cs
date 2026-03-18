using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class MaterialGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.MaterialGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (mat == null)
                return Fail(StatusCode.NotFound, $"Material not found at: {path}");

            var property = request.GetParam("property", null);

            if (!string.IsNullOrEmpty(property))
            {
                return GetSingleProperty(mat, path, property);
            }

            return GetMaterialOverview(mat, path);
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private CommandResponse GetSingleProperty(UnityEngine.Material mat, string path, string property)
        {
            if (!mat.HasProperty(property))
                return Fail(StatusCode.NotFound, $"Property '{property}' not found on material.");

            var data = new JObject
            {
                ["path"] = path,
                ["shader"] = mat.shader != null ? mat.shader.name : "null",
                ["property"] = property
            };

            var shader = mat.shader;
            int propIndex = -1;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyName(i) == property)
                {
                    propIndex = i;
                    break;
                }
            }

            if (propIndex >= 0)
            {
                var propType = shader.GetPropertyType(propIndex);
                data["propertyType"] = propType.ToString();

                switch (propType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var c = mat.GetColor(property);
                        data["value"] = new JObject
                        {
                            ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a
                        };
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        data["value"] = mat.GetFloat(property);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        var tex = mat.GetTexture(property);
                        data["value"] = tex != null
                            ? UnityEditor.AssetDatabase.GetAssetPath(tex)
                            : null;
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        var v = mat.GetVector(property);
                        data["value"] = new JObject
                        {
                            ["x"] = v.x, ["y"] = v.y, ["z"] = v.z, ["w"] = v.w
                        };
                        break;
                    default:
                        data["value"] = "unsupported";
                        break;
                }
            }

            return Ok($"Material property '{property}'", data);
        }

        private CommandResponse GetMaterialOverview(UnityEngine.Material mat, string path)
        {
            var shader = mat.shader;
            var properties = new JArray();

            if (shader != null)
            {
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var propName = shader.GetPropertyName(i);
                    var propType = shader.GetPropertyType(i);

                    var propInfo = new JObject
                    {
                        ["name"] = propName,
                        ["type"] = propType.ToString(),
                        ["description"] = shader.GetPropertyDescription(i)
                    };

                    properties.Add(propInfo);
                }
            }

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            return Ok($"Material at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["shader"] = shader != null ? shader.name : "null",
                ["renderQueue"] = mat.renderQueue,
                ["passCount"] = mat.passCount,
                ["properties"] = properties
            });
        }
#endif
    }
}
