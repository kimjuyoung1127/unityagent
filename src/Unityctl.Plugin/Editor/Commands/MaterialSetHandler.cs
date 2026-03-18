using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class MaterialSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.MaterialSet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var property = request.GetParam("property", null);
            var propertyType = request.GetParam("propertyType", null);
            var value = request.GetParam("value", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");
            if (string.IsNullOrEmpty(propertyType))
                return InvalidParameters("Parameter 'propertyType' is required.");
            if (value == null)
                return InvalidParameters("Parameter 'value' is required.");

            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (mat == null)
                return Fail(StatusCode.NotFound, $"Material not found at: {path}");

            if (!mat.HasProperty(property))
                return Fail(StatusCode.NotFound, $"Property '{property}' not found on material.");

            var undoName = $"unityctl: material-set: {property}";
            UnityEditor.Undo.RecordObject(mat, undoName);

            try
            {
                switch (propertyType.ToLowerInvariant())
                {
                    case "color":
                        var colorArr = JArray.Parse(value);
                        var color = new UnityEngine.Color(
                            colorArr[0].Value<float>(),
                            colorArr[1].Value<float>(),
                            colorArr[2].Value<float>(),
                            colorArr.Count > 3 ? colorArr[3].Value<float>() : 1f);
                        mat.SetColor(property, color);
                        break;

                    case "float":
                        mat.SetFloat(property, float.Parse(value));
                        break;

                    case "int":
                        mat.SetInt(property, int.Parse(value));
                        break;

                    case "texture":
                        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture>(value);
                        if (tex == null && !string.IsNullOrEmpty(value))
                            return Fail(StatusCode.NotFound, $"Texture not found at: {value}");
                        mat.SetTexture(property, tex);
                        break;

                    case "vector":
                        var vecArr = JArray.Parse(value);
                        var vec = new UnityEngine.Vector4(
                            vecArr[0].Value<float>(),
                            vecArr[1].Value<float>(),
                            vecArr.Count > 2 ? vecArr[2].Value<float>() : 0f,
                            vecArr.Count > 3 ? vecArr[3].Value<float>() : 0f);
                        mat.SetVector(property, vec);
                        break;

                    default:
                        return InvalidParameters(
                            $"Unknown propertyType: '{propertyType}'. " +
                            "Valid types: color, float, int, texture, vector");
                }
            }
            catch (Exception e)
            {
                return Fail(StatusCode.InvalidParameters,
                    $"Failed to set '{property}': {e.Message}");
            }

            UnityEditor.EditorUtility.SetDirty(mat);
            UnityEditor.AssetDatabase.SaveAssets();

            return Ok($"Set material property '{property}'", new JObject
            {
                ["path"] = path,
                ["property"] = property,
                ["propertyType"] = propertyType,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
