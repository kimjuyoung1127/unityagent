#if UNITY_EDITOR
using System.Collections;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UitkElementResolver.FindUidocumentType() == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var name = request.GetParam("name", null);
            var locator = request.GetParam("locator", null);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(locator))
                return InvalidParameters("Parameter 'name' or 'locator' is required.");

            if (!UitkElementResolver.TryResolveSingle(name, locator, out var resolved, out var candidates, out var ambiguous))
            {
                if (ambiguous)
                {
                    return Fail(StatusCode.InvalidParameters, "Multiple UI Toolkit elements matched the query. Retry with locator.",
                        new JObject { ["candidates"] = candidates });
                }

                return Fail(StatusCode.NotFound, $"UI Toolkit element not found for name='{name}' locator='{locator}'.");
            }

            var element = resolved.Element;
            var elType = element.GetType();
            var data = UitkElementResolver.ToSummary(resolved);
            data["enabledInHierarchy"] = GetBoolProp(element, "enabledInHierarchy");

            var valueProp = elType.GetProperty("value");
            if (valueProp != null)
            {
                var val = valueProp.GetValue(element);
                data["value"] = val?.ToString() ?? "null";
                data["valueType"] = valueProp.PropertyType.Name;
            }

            var textProp = elType.GetProperty("text");
            if (textProp != null && textProp.PropertyType == typeof(string))
                data["text"] = textProp.GetValue(element) as string ?? string.Empty;

            var getClassesMethod = element.GetType().GetMethod("GetClasses");
            if (getClassesMethod != null)
            {
                var classes = getClassesMethod.Invoke(element, null) as IEnumerable;
                var classArr = new JArray();
                if (classes != null)
                {
                    foreach (var cls in classes)
                        classArr.Add(cls?.ToString() ?? string.Empty);
                }

                data["classes"] = classArr;
            }

            var childCountProp = elType.GetProperty("childCount");
            if (childCountProp != null)
                data["childCount"] = (int)childCountProp.GetValue(element);

            return Ok($"UI Toolkit element '{resolved.Name}'", data);
        }

        private static bool GetBoolProp(object obj, string propName)
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return true;
        }
    }
}
#endif
