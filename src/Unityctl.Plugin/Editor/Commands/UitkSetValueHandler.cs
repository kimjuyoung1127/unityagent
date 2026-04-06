#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkSetValueHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkSetValue;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UitkElementResolver.FindUidocumentType() == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var name = request.GetParam("name", null);
            var locator = request.GetParam("locator", null);
            if (string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrWhiteSpace(locator))
                    return InvalidParameters("Parameter 'name' or 'locator' is required.");
            }

            var valueStr = request.GetParam("value", null);
            if (valueStr == null)
                return InvalidParameters("Parameter 'value' is required.");

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
            var valueProp = elType.GetProperty("value");
            if (valueProp == null || !valueProp.CanWrite)
            {
                return Fail(StatusCode.InvalidParameters,
                    $"Element '{resolved.Name}' ({elType.Name}) does not have a writable value property.");
            }

            object parsedValue;
            try
            {
                parsedValue = ConvertValue(valueStr, valueProp.PropertyType);
            }
            catch (Exception ex)
            {
                return InvalidParameters($"Cannot convert '{valueStr}' to {valueProp.PropertyType.Name}: {ex.Message}");
            }

            var previousValue = valueProp.GetValue(element)?.ToString() ?? "null";
            var setWithoutNotify = elType.GetMethod("SetValueWithoutNotify");
            if (setWithoutNotify != null)
            {
                try
                {
                    setWithoutNotify.Invoke(element, new[] { parsedValue });
                }
                catch
                {
                    valueProp.SetValue(element, parsedValue);
                }
            }
            else
            {
                valueProp.SetValue(element, parsedValue);
            }

            return Ok($"Set '{resolved.Name}' value to '{valueStr}'", new JObject
            {
                ["name"] = resolved.Name,
                ["type"] = elType.Name,
                ["documentName"] = resolved.DocumentName,
                ["elementPath"] = resolved.ElementPath,
                ["locator"] = resolved.Locator,
                ["previousValue"] = previousValue,
                ["currentValue"] = valueProp.GetValue(element)?.ToString() ?? "null"
            });
        }

        private static object ConvertValue(string str, Type targetType)
        {
            if (targetType == typeof(string)) return str;
            if (targetType == typeof(float)) return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return int.Parse(str);
            if (targetType == typeof(bool)) return bool.Parse(str);
            if (targetType == typeof(double)) return double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType.IsEnum) return Enum.Parse(targetType, str, true);
            return Convert.ChangeType(str, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }

    }
}
#endif
