using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProjectSettingsSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProjectSettingsSet;

        private static readonly Dictionary<string, string> ScopeToPath =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            var value = request.GetParam("value", null);

            if (string.IsNullOrEmpty(scope))
                return InvalidParameters("Parameter 'scope' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");
            if (value == null)
                return InvalidParameters("Parameter 'value' is required.");

            if (!ScopeToPath.TryGetValue(scope, out var assetPath))
            {
                return InvalidParameters(
                    $"Unknown scope: '{scope}'. Valid scopes: {string.Join(", ", ScopeToPath.Keys)}");
            }

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return Fail(StatusCode.NotFound, $"Settings asset not found: {assetPath}");

            var targetObject = assets[0];
            var undoName = $"unityctl: project-settings-set: {scope}.{property}";
            UnityEditor.Undo.RecordObject(targetObject, undoName);

            var so = new UnityEditor.SerializedObject(targetObject);
            var prop = so.FindProperty(property);
            if (prop == null)
                return Fail(StatusCode.NotFound,
                    $"Property '{property}' not found in scope '{scope}'.");

            if (!SetPropertyValue(prop, value))
                return InvalidParameters(
                    $"Failed to set '{property}' to '{value}'. Property type: {prop.propertyType}");

            so.ApplyModifiedProperties();

            // Read back value
            so.Update();
            prop = so.FindProperty(property);
            var readBack = ReadPropertyValue(prop);

            return Ok($"{scope}.{property} set to {readBack}", new JObject
            {
                ["scope"] = scope,
                ["property"] = property,
                ["value"] = readBack,
                ["propertyType"] = prop.propertyType.ToString(),
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool SetPropertyValue(UnityEditor.SerializedProperty prop, string valueStr)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case UnityEditor.SerializedPropertyType.Integer:
                        prop.intValue = int.Parse(valueStr);
                        return true;
                    case UnityEditor.SerializedPropertyType.Boolean:
                        prop.boolValue = bool.Parse(valueStr);
                        return true;
                    case UnityEditor.SerializedPropertyType.Float:
                        prop.floatValue = float.Parse(valueStr);
                        return true;
                    case UnityEditor.SerializedPropertyType.String:
                        prop.stringValue = valueStr;
                        return true;
                    case UnityEditor.SerializedPropertyType.Enum:
                        if (int.TryParse(valueStr, out var enumIndex))
                        {
                            prop.enumValueIndex = enumIndex;
                            return true;
                        }
                        var names = prop.enumNames;
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.Equals(names[i], valueStr, StringComparison.OrdinalIgnoreCase))
                            {
                                prop.enumValueIndex = i;
                                return true;
                            }
                        }
                        return false;
                    case UnityEditor.SerializedPropertyType.Color:
                        var cArr = JArray.Parse(valueStr);
                        prop.colorValue = new UnityEngine.Color(
                            cArr[0].Value<float>(), cArr[1].Value<float>(),
                            cArr[2].Value<float>(), cArr.Count > 3 ? cArr[3].Value<float>() : 1f);
                        return true;
                    case UnityEditor.SerializedPropertyType.Vector2:
                        var v2Arr = JArray.Parse(valueStr);
                        prop.vector2Value = new UnityEngine.Vector2(
                            v2Arr[0].Value<float>(), v2Arr[1].Value<float>());
                        return true;
                    case UnityEditor.SerializedPropertyType.Vector3:
                        var v3Arr = JArray.Parse(valueStr);
                        prop.vector3Value = new UnityEngine.Vector3(
                            v3Arr[0].Value<float>(), v3Arr[1].Value<float>(), v3Arr[2].Value<float>());
                        return true;
                    case UnityEditor.SerializedPropertyType.Vector4:
                        var v4Arr = JArray.Parse(valueStr);
                        prop.vector4Value = new UnityEngine.Vector4(
                            v4Arr[0].Value<float>(), v4Arr[1].Value<float>(),
                            v4Arr[2].Value<float>(), v4Arr[3].Value<float>());
                        return true;
                    case UnityEditor.SerializedPropertyType.LayerMask:
                        prop.intValue = int.Parse(valueStr);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ReadPropertyValue(UnityEditor.SerializedProperty prop)
        {
            if (prop == null) return "null";

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
                default:
                    return prop.type;
            }
        }
#endif
    }
}
