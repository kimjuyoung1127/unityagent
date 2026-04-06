#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ExecInvokeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ExecInvoke;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var typeName = request.GetParam("type", null);
            if (string.IsNullOrWhiteSpace(typeName))
                return InvalidParameters("Parameter 'type' is required.");

            var methodName = request.GetParam("method", null);
            if (string.IsNullOrWhiteSpace(methodName))
                return InvalidParameters("Parameter 'method' is required.");

            var argsJson = request.GetParam("args", null);

            try
            {
                var result = ExecReflectionUtility.InvokeStructured(typeName.Trim(), methodName.Trim(), argsJson);
                return Ok($"Invoked {typeName}.{methodName}", new JObject
                {
                    ["type"] = typeName,
                    ["method"] = methodName,
                    ["args"] = string.IsNullOrWhiteSpace(argsJson) ? new JArray() : JToken.Parse(argsJson),
                    ["result"] = result != null ? JToken.FromObject(result) : JValue.CreateNull()
                });
            }
            catch (ExecSecurityException ex)
            {
                return Fail(StatusCode.InvalidParameters, $"Security violation: {ex.Message}");
            }
            catch (ExecParseException ex)
            {
                return Fail(StatusCode.InvalidParameters, ex.Message);
            }
            catch (ExecInvocationException ex)
            {
                return Fail(
                    StatusCode.UnknownError,
                    $"Execution error: {ex.InnerTypeName}: {ex.Message}",
                    new JObject
                    {
                        ["type"] = ex.TypeName,
                        ["method"] = ex.MemberName,
                        ["innerExceptionType"] = ex.InnerTypeName
                    },
                    string.IsNullOrWhiteSpace(ex.InnerStackTrace)
                        ? null
                        : new System.Collections.Generic.List<string> { ex.InnerStackTrace });
            }
            catch (System.Exception ex)
            {
                return Fail(StatusCode.UnknownError, $"Execution error: {ex.Message}");
            }
        }
    }
}
#endif
