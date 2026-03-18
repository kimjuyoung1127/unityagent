using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProjectSettingsGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProjectSettingsGet;

        private static readonly Dictionary<string, string> ScopeToPath =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "editor", "ProjectSettings/EditorSettings.asset" },
                { "graphics", "ProjectSettings/GraphicsSettings.asset" },
                { "quality", "ProjectSettings/QualitySettings.asset" },
                { "physics", "ProjectSettings/DynamicsManager.asset" },
                { "time", "ProjectSettings/TimeManager.asset" },
                { "audio", "ProjectSettings/AudioManager.asset" }
            };

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scope = request.GetParam("scope", null);
            var property = request.GetParam("property", null);

            if (string.IsNullOrEmpty(scope))
                return InvalidParameters("Parameter 'scope' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");

            if (!ScopeToPath.TryGetValue(scope, out var assetPath))
            {
                return InvalidParameters(
                    $"Unknown scope: '{scope}'. Valid scopes: {string.Join(", ", ScopeToPath.Keys)}");
            }

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return Fail(StatusCode.NotFound, $"Settings asset not found: {assetPath}");

            var so = new UnityEditor.SerializedObject(assets[0]);
            var prop = so.FindProperty(property);
            if (prop == null)
                return Fail(StatusCode.NotFound,
                    $"Property '{property}' not found in scope '{scope}'.");

            var value = ReadPropertyValue(prop);
            return Ok($"{scope}.{property} = {value}", new JObject
            {
                ["scope"] = scope,
                ["property"] = property,
                ["value"] = value,
                ["propertyType"] = prop.propertyType.ToString()
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static string ReadPropertyValue(UnityEditor.SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case UnityEditor.SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case UnityEditor.SerializedPropertyType.Float:
                    return prop.floatValue.ToString();
                case UnityEditor.SerializedPropertyType.String:
                    return prop.stringValue;
                case UnityEditor.SerializedPropertyType.Enum:
                    return prop.enumValueIndex < prop.enumNames.Length
                        ? prop.enumNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case UnityEditor.SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return $"{{\"r\":{c.r},\"g\":{c.g},\"b\":{c.b},\"a\":{c.a}}}";
                case UnityEditor.SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return $"{{\"x\":{v2.x},\"y\":{v2.y}}}";
                case UnityEditor.SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return $"{{\"x\":{v3.x},\"y\":{v3.y},\"z\":{v3.z}}}";
                case UnityEditor.SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return $"{{\"x\":{v4.x},\"y\":{v4.y},\"z\":{v4.z},\"w\":{v4.w}}}";
                case UnityEditor.SerializedPropertyType.LayerMask:
                    return prop.intValue.ToString();
                default:
                    return prop.type;
            }
        }
#endif
    }
}
