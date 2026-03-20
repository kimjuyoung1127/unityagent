#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptableObjectSetPropertyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptableObjectSetProperty;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            var property = request.GetParam("property", null);
            var value = request.GetParam("value", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");
            if (value == null)
                return InvalidParameters("Parameter 'value' is required.");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
            if (asset == null)
                return Fail(StatusCode.NotFound, $"ScriptableObject not found at: {path}");

            UnityEngine.Undo.RecordObject(asset, $"Set {property} on {asset.name}");

            using (var serializedObject = new SerializedObject(asset))
            {
                var serializedProperty = serializedObject.FindProperty(property);
                if (serializedProperty == null)
                    return Fail(StatusCode.NotFound, $"Property '{property}' not found on '{path}'.");

                if (!SetPropertyValue(serializedProperty, value))
                    return InvalidParameters(
                        $"Failed to set '{property}' to '{value}'. Property type: {serializedProperty.propertyType}");

                serializedObject.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            return Ok($"Set '{property}' on '{path}'", new JObject
            {
                ["path"] = path,
                ["property"] = property,
                ["value"] = value
            });
        }

        private static bool SetPropertyValue(SerializedProperty prop, string valueStr)
        {
            try
            {
                JToken jsonValue = null;
                try { jsonValue = JToken.Parse(valueStr); }
                catch { /* not JSON, treat as raw string */ }

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = int.Parse(valueStr);
                        return true;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = bool.Parse(valueStr);
                        return true;
                    case SerializedPropertyType.Float:
                        prop.floatValue = float.Parse(valueStr);
                        return true;
                    case SerializedPropertyType.String:
                        if (jsonValue is JValue jv && jv.Type == JTokenType.String)
                            prop.stringValue = jv.Value<string>();
                        else
                            prop.stringValue = valueStr;
                        return true;
                    case SerializedPropertyType.Enum:
                        if (int.TryParse(valueStr, out var enumIndex))
                            prop.enumValueIndex = enumIndex;
                        else
                        {
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
                        }
                        return true;
                    case SerializedPropertyType.Color:
                        if (jsonValue is JObject colorObj)
                        {
                            prop.colorValue = new UnityEngine.Color(
                                colorObj.Value<float>("r"),
                                colorObj.Value<float>("g"),
                                colorObj.Value<float>("b"),
                                colorObj.Value<float>("a"));
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector2:
                        if (jsonValue is JObject v2Obj)
                        {
                            prop.vector2Value = new UnityEngine.Vector2(
                                v2Obj.Value<float>("x"),
                                v2Obj.Value<float>("y"));
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector3:
                        if (jsonValue is JObject v3Obj)
                        {
                            prop.vector3Value = new UnityEngine.Vector3(
                                v3Obj.Value<float>("x"),
                                v3Obj.Value<float>("y"),
                                v3Obj.Value<float>("z"));
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector4:
                        if (jsonValue is JObject v4Obj)
                        {
                            prop.vector4Value = new UnityEngine.Vector4(
                                v4Obj.Value<float>("x"),
                                v4Obj.Value<float>("y"),
                                v4Obj.Value<float>("z"),
                                v4Obj.Value<float>("w"));
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.LayerMask:
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
    }
}
#endif
