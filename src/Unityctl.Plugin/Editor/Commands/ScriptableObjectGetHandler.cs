#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptableObjectGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptableObjectGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            var property = request.GetParam("property", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
            if (asset == null)
                return Fail(StatusCode.NotFound, $"ScriptableObject not found at: {path}");

            var guid = AssetDatabase.AssetPathToGUID(path);
            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["typeName"] = asset.GetType().FullName ?? asset.GetType().Name,
                ["name"] = asset.name
            };

            if (!string.IsNullOrWhiteSpace(property))
            {
                using (var serializedObject = new SerializedObject(asset))
                {
                    var serializedProperty = serializedObject.FindProperty(property);
                    if (serializedProperty == null)
                        return Fail(StatusCode.NotFound, $"Property '{property}' not found on '{path}'.");

                    data["property"] = property;
                    data["propertyType"] = serializedProperty.propertyType.ToString();
                    data["value"] = SerializedPropertyJsonUtility.ToJsonValue(serializedProperty);
                }

                return Ok($"ScriptableObject property '{property}'", data);
            }

            // Inline visible properties for ScriptableObject (utility only accepts Component)
            var properties = new JObject();
            using (var so = new SerializedObject(asset))
            {
                var iterator = so.GetIterator();
                while (iterator.NextVisible(true))
                {
                    var val = SerializedPropertyJsonUtility.ToJsonValue(iterator);
                    if (val != null)
                        properties[iterator.propertyPath] = val;
                }
            }
            data["properties"] = properties;
            return Ok($"ScriptableObject at '{path}'", data);
        }
    }
}
#endif
