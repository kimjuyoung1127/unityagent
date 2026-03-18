using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PrefabEditHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PrefabEdit;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var property = request.GetParam("property", null);
            var value = request.GetParam("value", null);
            var childPath = request.GetParam("childPath", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(property))
                return InvalidParameters("Parameter 'property' is required.");
            if (value == null)
                return InvalidParameters("Parameter 'value' is required.");

            var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return Fail(StatusCode.NotFound, $"Prefab not found at: {path}");

            try
            {
                var target = root;

                // Navigate to child if childPath specified
                if (!string.IsNullOrEmpty(childPath))
                {
                    var childTransform = root.transform.Find(childPath);
                    if (childTransform == null)
                    {
                        return Fail(StatusCode.NotFound,
                            $"Child not found at path '{childPath}' in prefab '{path}'.");
                    }
                    target = childTransform.gameObject;
                }

                // Find the property across all components
                var components = target.GetComponents<UnityEngine.Component>();
                string matchedComponentType = null;
                string readBack = null;

                foreach (var component in components)
                {
                    if (component == null) continue;

                    var so = new UnityEditor.SerializedObject(component);
                    var prop = so.FindProperty(property);

                    if (prop == null) continue;

                    if (!SetPropertyValue(prop, value))
                    {
                        return InvalidParameters(
                            $"Failed to set '{property}' to '{value}'. " +
                            $"Property type: {prop.propertyType}");
                    }

                    so.ApplyModifiedProperties();
                    matchedComponentType = component.GetType().Name;

                    // Read back the value
                    so.Update();
                    prop = so.FindProperty(property);
                    readBack = ReadPropertyValue(prop);
                    break;
                }

                if (matchedComponentType == null)
                {
                    return InvalidParameters(
                        $"Property '{property}' not found on any component of " +
                        $"'{(string.IsNullOrEmpty(childPath) ? "root" : childPath)}'.");
                }

                // Save the modified prefab
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);

                return Ok($"{matchedComponentType}.{property} = {readBack}", new JObject
                {
                    ["path"] = path,
                    ["componentType"] = matchedComponentType,
                    ["property"] = property,
                    ["value"] = readBack,
                    ["childPath"] = childPath
                });
            }
            finally
            {
                UnityEditor.PrefabUtility.UnloadPrefabContents(root);
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool SetPropertyValue(UnityEditor.SerializedProperty prop, string valueStr)
        {
            try
            {
                JToken jsonValue = null;
                try { jsonValue = JToken.Parse(valueStr); }
                catch { /* not JSON, treat as raw string */ }

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
                        if (jsonValue is JValue jv && jv.Type == JTokenType.String)
                            prop.stringValue = jv.Value<string>();
                        else
                            prop.stringValue = valueStr;
                        return true;

                    case UnityEditor.SerializedPropertyType.Enum:
                        if (int.TryParse(valueStr, out var enumIndex))
                            prop.enumValueIndex = enumIndex;
                        else
                        {
                            var names = prop.enumNames;
                            for (int i = 0; i < names.Length; i++)
                            {
                                if (string.Equals(names[i], valueStr,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    prop.enumValueIndex = i;
                                    return true;
                                }
                            }
                            return false;
                        }
                        return true;

                    case UnityEditor.SerializedPropertyType.Color:
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

                    case UnityEditor.SerializedPropertyType.Vector3:
                        if (jsonValue is JObject v3Obj)
                        {
                            prop.vector3Value = new UnityEngine.Vector3(
                                v3Obj.Value<float>("x"),
                                v3Obj.Value<float>("y"),
                                v3Obj.Value<float>("z"));
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Vector2:
                        if (jsonValue is JObject v2Obj)
                        {
                            prop.vector2Value = new UnityEngine.Vector2(
                                v2Obj.Value<float>("x"),
                                v2Obj.Value<float>("y"));
                            return true;
                        }
                        return false;

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
                    return prop.enumNames[prop.enumValueIndex];
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
